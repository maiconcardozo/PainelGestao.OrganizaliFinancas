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
            return services;
        }
    }
}
