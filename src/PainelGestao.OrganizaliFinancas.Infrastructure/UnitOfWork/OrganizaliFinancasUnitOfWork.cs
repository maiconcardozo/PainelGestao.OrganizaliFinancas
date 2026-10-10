using Foundation.Dapper;
using Foundation.Domain.Interfaces.Data;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.UnitOfWork
{
    /// <summary>Uses the transaction implementation supplied by Foundation.Base.</summary>
    public class OrganizaliFinancasUnitOfWork(
        ISqlConnectionFactory connectionFactory,
        Func<IDapperUnitOfWork, ITenantRepository> tenantRepositoryFactory)
        : DapperUnitOfWork(connectionFactory), IUnitOfWork
    {
        private ITenantRepository? _tenantRepository;
        public ITenantRepository TenantRepository => _tenantRepository ??= tenantRepositoryFactory(this);
    }
}
