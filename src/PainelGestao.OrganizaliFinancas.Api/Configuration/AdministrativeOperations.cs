using System.Text.Json;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Infrastructure.Database;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    /// <summary>Local operator commands, never mapped as public HTTP endpoints.</summary>
    public static class AdministrativeOperations
    {
        public static async Task<bool> RunAdministrativeOperationAsync(this WebApplication app)
        {
            var operation = app.Configuration["Operation"];
            if (string.IsNullOrEmpty(operation)) return false;
            await using var scope = app.Services.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var config = app.Configuration.GetSection("Pilot");
            switch (operation)
            {
                case "Migrate":
                    var count = await scope.ServiceProvider.GetRequiredService<SchemaMigrator>().ApplyAsync();
                    Console.WriteLine($"Migrations aplicadas: {count}");
                    break;
                case "ProvisionHousehold":
                    var result = await mediator.Send(new ProvisionHouseholdCommand(
                        Guid.Parse(config["Key"] ?? ""), config["HouseholdName"] ?? "", config["OwnerName"] ?? "",
                        config["Email"] ?? "", config["Password"] ?? "", config["Actor"] ?? ""));
                    Console.WriteLine(JsonSerializer.Serialize(new { result.HouseholdCode, result.UserCode, result.SubscriptionCode, result.Status }));
                    break;
                case "InitializeAccess":
                    await scope.ServiceProvider.GetRequiredService<Domain.Interfaces.IUnitOfWork>().PilotRepository.InitializeAccessAsync(
                        Guid.Parse(config["HouseholdCode"] ?? ""), config["Actor"] ?? "", CancellationToken.None);
                    Console.WriteLine("Perfis iniciais conferidos; vínculos existentes preservados.");
                    break;
                case "ChangeSubscriptionStatus":
                    await mediator.Send(new ChangeSubscriptionStatusCommand(
                        Guid.Parse(config["HouseholdCode"] ?? ""),
                        Enum.Parse<SubscriptionStatus>(config["Status"] ?? "", ignoreCase: true),
                        config["Reason"] ?? "", config["Actor"] ?? ""));
                    Console.WriteLine("Situação da assinatura atualizada.");
                    break;
                default:
                    throw new InvalidOperationException("Operação administrativa desconhecida.");
            }
            return true;
        }
    }
}
