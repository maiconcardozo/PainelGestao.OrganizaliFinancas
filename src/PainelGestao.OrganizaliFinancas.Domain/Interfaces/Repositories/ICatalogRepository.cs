using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public enum CatalogKind { Categories, IncomeTypes, Creditors, PaymentMethods }
    public interface ICatalogItem
    {
        int Id { get; }
        int IdTenant { get; }
        Guid Code { get; }
        string Name { get; }
        bool IsActive { get; }
        int Version { get; }
        void Update(string name, bool active, string actor);
    }
    public interface ICatalogRepository
    {
        Task<IReadOnlyList<ICatalogItem>> ListAsync(CatalogKind kind, int idTenant, CancellationToken ct);
        Task<ICatalogItem?> FindAsync(CatalogKind kind, int idTenant, Guid code, CancellationToken ct);
        void Save(ICatalogItem item, bool exists);
        void AddRevision(CatalogRevision revision);
    }
}
