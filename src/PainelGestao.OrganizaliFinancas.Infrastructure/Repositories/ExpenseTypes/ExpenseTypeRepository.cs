using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.ExpenseTypes;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.ExpenseTypes
{
    public class ExpenseTypeRepository(IDapperUnitOfWork work) : IExpenseTypeRepository
    {
        public async Task<IReadOnlyList<ExpenseTypeRecord>> ListAsync(int idTenant, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<ExpenseTypeRecord>(new CommandDefinition(
                ExpenseTypeSql.List, new { IdTenant = idTenant }, transaction, cancellationToken: ct)), ct)).ToArray();
        public Task<ExpenseType?> FindAsync(int idTenant, Guid code, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<ExpenseType>(new CommandDefinition(
                ExpenseTypeSql.Find, new { IdTenant = idTenant, Code = code }, transaction, cancellationToken: ct)), ct);
        public Task<ExpenseTypeRecord?> FindVersionAsync(int idTenant, Guid code, int version, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<ExpenseTypeRecord>(new CommandDefinition(
                ExpenseTypeSql.FindVersion, new { IdTenant = idTenant, Code = code, Version = version }, transaction, cancellationToken: ct)), ct);
        public void Save(ExpenseType type, bool exists, string behavior, int? idCategory, string fieldsJson, string actor)
        {
            if (exists) work.AddPendingWrite(ExpenseTypeSql.Update, type);
            else new DapperRepository<ExpenseType>(work).Add(type);
            work.AddPendingWrite(ExpenseTypeSql.AddVersion, new { type.Code, type.IdTenant, type.Version, type.Name,
                VersionCode = Guid.CreateVersion7(), Behavior = behavior, IdCategory = idCategory, FieldsJson = fieldsJson, Actor = actor });
        }
    }
}
