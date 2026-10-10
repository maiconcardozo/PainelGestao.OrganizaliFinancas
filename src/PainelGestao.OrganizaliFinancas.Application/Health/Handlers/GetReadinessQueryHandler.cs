using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Health.Queries;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
namespace PainelGestao.OrganizaliFinancas.Application.Health.Handlers
{
    public class GetReadinessQueryHandler(IDatabaseReadiness database) : IRequestHandler<GetReadinessQuery, bool>
    {
        public Task<bool> Handle(GetReadinessQuery request, CancellationToken cancellationToken) =>
            database.IsReadyAsync(cancellationToken);
    }
}
