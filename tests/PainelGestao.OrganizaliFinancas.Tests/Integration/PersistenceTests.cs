using FluentAssertions;
using Dapper;
using Foundation.Domain.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Infrastructure.Configuration;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database;
using PainelGestao.OrganizaliFinancas.Tests.Helpers;
using Testcontainers.MariaDb;

namespace PainelGestao.OrganizaliFinancas.Tests.Integration
{
    public sealed class PersistenceTests
    {
        [DatabaseFact]
        public async Task MigrationsAndBufferedWrites_PreserveIdentityIsolationAndRollback()
        {
            await using var database = new MariaDbBuilder("mariadb:11.8")
                .WithDatabase("organizali_tests")
                .WithUsername("root")
                .WithPassword(Guid.NewGuid().ToString("N"))
                .Build();
            await database.StartAsync();

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OrganizaliFinancas:ConnectionString"] = database.GetConnectionString(),
                    ["Jwt:Key"] = "Synthetic-integration-signing-key-at-least-32-chars"
                }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            PainelGestao.OrganizaliFinancas.Application.Configuration.ServiceCollectionExtensions.AddApplication(services);
            services.AddInfrastructure(configuration);
            await using var provider = services.BuildServiceProvider();

            await ApplyMigrationsConcurrently(provider);
            await using (var scope = provider.CreateAsyncScope())
            {
                var readiness = scope.ServiceProvider.GetRequiredService<
                    PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services.IDatabaseReadiness>();
                (await readiness.IsReadyAsync(CancellationToken.None)).Should().BeTrue();
            }
            await VerifyCommitAndTenantLookup(provider);
            await VerifyRollback(provider);
            await VerifyGeneratedIdInSingleCommit(provider);
            await VerifyConcurrentDuplicateCode(provider);
            await PilotPersistenceAssertions.VerifyAsync(provider);
            await AuthenticationAssertions.VerifyAsync(provider, database.GetConnectionString());
            await MemberAssertions.VerifyAsync(provider, database.GetConnectionString());
            await HouseholdAssertions.VerifyAsync(provider, database.GetConnectionString());
            await OperationsAssertions.VerifyAsync(provider, database.GetConnectionString());
        }

        private static async Task ApplyMigrationsConcurrently(ServiceProvider provider)
        {
            int remaining;
            await using (var scope = provider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<
                    PainelGestao.OrganizaliFinancas.Infrastructure.Database.Contexts.OrganizaliFinancasDbContext>();
                var migrations = context.Database.GetMigrations().ToArray();
                var script = context.GetService<IMigrator>().GenerateScript(Migration.InitialDatabase, migrations[0]);
                using var connection = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>().CreateConnection();
                await connection.ExecuteAsync(script);
                remaining = migrations.Length - 1;
            }
            async Task<int> Apply()
            {
                await using var scope = provider.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<SchemaMigrator>().ApplyAsync();
            }

            var applied = await Task.WhenAll(Apply(), Apply());
            applied.Sum().Should().Be(remaining, "only one runner applies migrations after the existing schema");
            (await Apply()).Should().Be(0, "restarting must not reapply the schema");
        }

        private static async Task VerifyCommitAndTenantLookup(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var first = Tenant.Create("Casa A");
            var second = Tenant.Create("Casa B");
            work.TenantRepository.Add(first);
            work.TenantRepository.Add(second);
            work.PendingCount.Should().Be(2);
            (await work.TenantRepository.GetByCodeAsync(first.Code)).Should().BeNull();

            (await work.CommitAsync()).Should().Be(2);
            var saved = await work.TenantRepository.GetByCodeAsync(first.Code);
            saved.Should().NotBeNull();
            saved!.Id.Should().BeGreaterThan(0);
            saved.Code.Should().Be(first.Code);
            (await work.TenantRepository.FindAsync(saved.Id))!.Name.Should().Be("Casa A");
            first.Id.Should().Be(0, "Foundation does not hydrate generated identifiers into buffered entities");

            work.TenantRepository.Remove(saved, "test");
            await work.CommitAsync();
            (await work.TenantRepository.FindAsync(saved.Id)).Should().BeNull();
            (await work.TenantRepository.GetByCodeAsync(second.Code))!.Name.Should().Be("Casa B");
        }

        private static async Task VerifyRollback(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tenant = Tenant.Create("Não persistir");
            work.TenantRepository.Add(tenant);
            work.TenantRepository.Add(tenant);
            var commit = () => work.CommitAsync();
            var failure = await commit.Should().ThrowAsync<MySql.Data.MySqlClient.MySqlException>();
            failure.Which.Number.Should().Be(1062);
            work.PendingCount.Should().Be(0);
            work.HasOpenTransaction.Should().BeFalse();
            (await work.TenantRepository.GetByCodeAsync(tenant.Code)).Should().BeNull();

            work.TenantRepository.Add(Tenant.Create("Descartar buffer"));
            await work.RollbackAsync();
            work.PendingCount.Should().Be(0);
        }

        private static async Task VerifyGeneratedIdInSingleCommit(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var tenant = Tenant.Create("Antes");
            work.TenantRepository.Add(tenant);
            // Technical probe: a dependent statement resolves the generated Id by public Code
            // within the same transaction. No intermediate commit and no fake financial model.
            work.AddPendingWrite("""
                UPDATE Tenant
                SET Name = @Name
                WHERE Id = (SELECT Id FROM (SELECT Id FROM Tenant WHERE Code = @Code) AS inserted)
                """, new { tenant.Code, Name = "Depois" });
            await work.CommitAsync();
            (await work.TenantRepository.GetByCodeAsync(tenant.Code))!.Name.Should().Be("Depois");
        }

        private static async Task VerifyConcurrentDuplicateCode(ServiceProvider provider)
        {
            var tenant = Tenant.Create("Concorrência");
            async Task<bool> Insert()
            {
                await using var scope = provider.CreateAsyncScope();
                var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                work.TenantRepository.Add(tenant);
                try { await work.CommitAsync(); return true; }
                catch (MySql.Data.MySqlClient.MySqlException exception) when (exception.Number == 1062)
                { return false; }
            }

            var results = await Task.WhenAll(Insert(), Insert());
            results.Count(success => success).Should().Be(1);
        }
    }
}
