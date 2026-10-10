using MediatR;
namespace PainelGestao.OrganizaliFinancas.Application.Health.Queries
{
    public sealed record GetReadinessQuery : IRequest<bool>;
}
