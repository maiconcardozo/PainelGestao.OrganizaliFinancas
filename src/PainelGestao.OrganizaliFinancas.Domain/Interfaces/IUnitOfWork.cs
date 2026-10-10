using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;

namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces
{
    /// <summary>Foundation transaction contract; repositories are added with their use cases.</summary>
    public interface IUnitOfWork : IDapperUnitOfWork
    {
        ITenantRepository TenantRepository { get; }
        IPilotRepository PilotRepository { get; }
    }
}
