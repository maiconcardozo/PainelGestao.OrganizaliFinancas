using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Tenants;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Tenants
{
    public class TenantRepository(IDapperUnitOfWork unitOfWork) : DapperRepository<Tenant>(unitOfWork), ITenantRepository
    {
        public Task<Tenant?> FindAsync(int idTenant, CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idTenant);
            return QuerySingleAsync(new FindTenantQuery(idTenant), cancellationToken);
        }
    }
}
