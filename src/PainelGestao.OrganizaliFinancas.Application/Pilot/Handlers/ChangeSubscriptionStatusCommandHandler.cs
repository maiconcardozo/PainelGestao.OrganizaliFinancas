using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Handlers
{
    public class ChangeSubscriptionStatusCommandHandler(IUnitOfWork work) : IRequestHandler<ChangeSubscriptionStatusCommand>
    {
        public Task Handle(ChangeSubscriptionStatusCommand request, CancellationToken cancellationToken) =>
            work.PilotRepository.ChangeStatusAsync(request.HouseholdCode, request.Status, request.Reason, request.Actor, cancellationToken);
    }
}
