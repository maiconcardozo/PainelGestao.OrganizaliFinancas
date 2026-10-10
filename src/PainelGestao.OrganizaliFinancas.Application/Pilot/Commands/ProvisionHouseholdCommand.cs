using MediatR;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Commands
{
    public sealed record ProvisionHouseholdCommand(Guid Key, string HouseholdName, string OwnerName,
        string Email, string Password, string Actor) : IRequest<PilotRecord>;
}
