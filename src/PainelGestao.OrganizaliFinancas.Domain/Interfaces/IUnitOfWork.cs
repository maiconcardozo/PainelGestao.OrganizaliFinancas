using Foundation.Domain.Interfaces.UnitOfWork;

namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces
{
    /// <summary>Foundation transaction contract; repositories are added with their use cases.</summary>
    public interface IUnitOfWork : IDapperUnitOfWork
    {
    }
}
