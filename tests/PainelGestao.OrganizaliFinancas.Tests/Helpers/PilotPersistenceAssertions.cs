using Dapper;
using FluentAssertions;
using Foundation.Domain.Interfaces.Data;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class PilotPersistenceAssertions
    {
        public static async Task VerifyAsync(ServiceProvider provider)
        {
            var command = new ProvisionHouseholdCommand(Guid.NewGuid(), "Casa teste", "Titular",
                "owner@example.test", "Synthetic-pass-123!", "test-operator");
            async Task<PilotRecord> Provision(ProvisionHouseholdCommand input)
            {
                await using var scope = provider.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(input);
            }
            var concurrent = await Task.WhenAll(Provision(command), Provision(command));
            concurrent[0].HouseholdCode.Should().Be(concurrent[1].HouseholdCode);
            concurrent[0].Status.Should().Be(SubscriptionStatus.Pending);
            var repeat = await Provision(command with { Password = "Other-password-456!" });
            repeat.UserCode.Should().Be(concurrent[0].UserCode);
            var conflict = () => Provision(command with { HouseholdName = "Outra" });
            await conflict.Should().ThrowAsync<ConflictException>();

            var other = await Provision(command with { Key = Guid.NewGuid(), Email = "other@example.test" });
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ChangeSubscriptionStatusCommand(repeat.HouseholdCode, SubscriptionStatus.Active, "Piloto aprovado", "test-operator"));
            await mediator.Send(new ChangeSubscriptionStatusCommand(repeat.HouseholdCode, SubscriptionStatus.Suspended, "Teste de suspensão", "test-operator"));
            await mediator.Send(new ChangeSubscriptionStatusCommand(repeat.HouseholdCode, SubscriptionStatus.Suspended, "Repetição", "test-operator"));
            using var connection = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>().CreateConnection();
            var storedHash = await connection.QuerySingleAsync<string>("SELECT PasswordHash FROM User WHERE Code=@Code", new { Code = repeat.UserCode });
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Verify(storedHash, command.Password).Should().BeTrue();
            var count = await connection.ExecuteScalarAsync<int>("""
                SELECT COUNT(*) FROM SubscriptionEvent e JOIN Subscription s ON s.Id=e.IdSubscription AND s.IdTenant=e.IdTenant
                JOIN Tenant t ON t.Id=s.IdTenant WHERE t.Code=@Code
                """, new { Code = repeat.HouseholdCode });
            count.Should().Be(3);
            var invalidOwner = () => connection.ExecuteAsync("""
                UPDATE Subscription s JOIN Tenant t ON t.Id=s.IdTenant
                SET s.IdOwnerUser=(SELECT Id FROM User WHERE Code=@OtherUser)
                WHERE t.Code=@Code
                """, new { OtherUser = other.UserCode, Code = repeat.HouseholdCode });
            var error = await invalidOwner.Should().ThrowAsync<MySqlException>();
            error.Which.Number.Should().Be(1452, "the owner must belong to the same household");
        }
    }
}
