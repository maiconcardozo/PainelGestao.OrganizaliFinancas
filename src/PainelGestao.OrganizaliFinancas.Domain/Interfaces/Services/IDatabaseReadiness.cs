namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services
{
    public interface IDatabaseReadiness
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken);
    }
}
