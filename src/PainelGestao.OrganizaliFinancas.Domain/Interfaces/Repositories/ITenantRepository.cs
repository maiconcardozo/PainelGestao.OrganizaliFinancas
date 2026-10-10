using Foundation.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    /// <summary>Platform provisioning contract. Never expose it as a financial repository.</summary>
    public interface ITenantRepository : IDapperRepository<Tenant>
    {
        Task<Tenant?> FindAsync(int idTenant, CancellationToken cancellationToken = default);
    }
}
