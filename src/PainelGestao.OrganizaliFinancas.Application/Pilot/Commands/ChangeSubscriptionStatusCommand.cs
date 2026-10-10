using MediatR;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Commands
{
    public sealed record ChangeSubscriptionStatusCommand(Guid HouseholdCode, SubscriptionStatus Status,
        string Reason, string Actor) : IRequest;
}
