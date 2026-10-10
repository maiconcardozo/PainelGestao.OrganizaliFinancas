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
                .Validate(settings => settings.ForecastHorizonMonths is >= 1 and <= 36, "O horizonte de previsões deve ficar entre 1 e 36 meses.")
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
            services.AddScoped<Func<IDapperUnitOfWork, IExpenseTypeRepository>>(_ => work => new Repositories.ExpenseTypes.ExpenseTypeRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, IAccountRepository>>(_ => work => new Repositories.Accounts.AccountRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, IIncomeRepository>>(_ => work => new Repositories.Income.IncomeRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, ICardRepository>>(_ => work => new Repositories.Cards.CardRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, IExpenseRepository>>(_ => work => new Repositories.Expenses.ExpenseRepository(work));
            services.AddScoped<Func<IDapperUnitOfWork, IIdempotencyRepository>>(_ => work => new Repositories.Accounts.IdempotencyRepository(work));
            services.AddSingleton(provider => provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrganizaliFinancasSettings>>().Value);
            services.AddScoped<Func<IDapperUnitOfWork, ICatalogRepository>>(_ => work => new Repositories.Catalogs.CatalogRepository(work));
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
