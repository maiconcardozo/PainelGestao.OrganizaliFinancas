using Foundation.Dapper;
using Foundation.Domain.Interfaces.Data;
using Foundation.Domain.Interfaces.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Data.MariaDb;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database.Contexts;
using PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Tenants;
using PainelGestao.OrganizaliFinancas.Infrastructure.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Shared.Settings;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(OrganizaliFinancasSettings.SectionName);
            services.AddOptions<OrganizaliFinancasSettings>().Bind(section)
                .Validate(settings => !string.IsNullOrWhiteSpace(settings.ConnectionString), "Configure a conexão do Organizali.")
                .ValidateOnStart();
            var connectionString = section[nameof(OrganizaliFinancasSettings.ConnectionString)]
                ?? throw new InvalidOperationException("Configure a conexão do Organizali.");

            services.AddDbContext<OrganizaliFinancasDbContext>(options => options.UseMySQL(connectionString));
            services.AddDapperServices();
            services.AddScoped<ISqlConnectionFactory, MySqlConnectionFactory>();
            services.AddScoped<OrganizaliFinancasUnitOfWork>();
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrganizaliFinancasUnitOfWork>());
            services.AddScoped<IDapperUnitOfWork>(provider => provider.GetRequiredService<OrganizaliFinancasUnitOfWork>());
            services.AddScoped<Func<IDapperUnitOfWork, ITenantRepository>>(_ => unitOfWork => new TenantRepository(unitOfWork));
            services.AddScoped<SchemaMigrator>();
            services.AddScoped<Func<IDapperUnitOfWork, IHouseholdRepository>>(_ => work => new Repositories.Households.HouseholdRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, IMemberRepository>>(_ => work => new Repositories.Members.MemberRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, Domain.Interfaces.Repositories.IAuthenticationRepository>>(
                _ => work => new Repositories.Authentication.AuthenticationRepository(work));
            services.Configure<Shared.Settings.JwtSettings>(configuration.GetSection(Shared.Settings.JwtSettings.SectionName));
            services.AddScoped<Domain.Interfaces.Security.ITokenService, Security.JwtTokenService>();
            services.AddSingleton<Domain.Interfaces.Security.IRecoveryDelivery, Security.UnavailableRecoveryDelivery>();
            services.AddScoped<Func<IDapperUnitOfWork, Domain.Interfaces.Repositories.IPilotRepository>>(
                _ => work => new Repositories.Pilot.PilotRepository(work));
            services.AddScoped<Domain.Interfaces.Security.IPasswordHasher, Security.PasswordHasherService>();
            services.AddScoped<PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services.IDatabaseReadiness,
                PainelGestao.OrganizaliFinancas.Infrastructure.Services.DatabaseReadiness>();
            return services;
        }
    }
}
