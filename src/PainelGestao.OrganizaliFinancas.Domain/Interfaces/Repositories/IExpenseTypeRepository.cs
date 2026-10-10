using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record ExpenseTypeRecord(Guid Code, string Name, bool IsActive, int Version, string Behavior, Guid? DefaultCategoryCode, string FieldsJson);
    public interface IExpenseTypeRepository
    {
        Task<IReadOnlyList<ExpenseTypeRecord>> ListAsync(int idTenant, CancellationToken ct);
        Task<ExpenseType?> FindAsync(int idTenant, Guid code, CancellationToken ct);
        Task<ExpenseTypeRecord?> FindVersionAsync(int idTenant, Guid code, int version, CancellationToken ct);
        void Save(ExpenseType type, bool exists, string behavior, int? idCategory, string fieldsJson, string actor);
    }
}
