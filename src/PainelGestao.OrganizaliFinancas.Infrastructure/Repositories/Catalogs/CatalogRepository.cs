using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Catalogs;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Catalogs
{
    public class CatalogRepository(IDapperUnitOfWork work) : ICatalogRepository
    {
        private async Task<IReadOnlyList<ICatalogItem>> Read<T>(string sql, object parameters, CancellationToken ct) where T : ICatalogItem =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(new CommandDefinition(
                sql, parameters, transaction, cancellationToken: ct)), ct)).Cast<ICatalogItem>().ToArray();
        private Task<IReadOnlyList<ICatalogItem>> Read(CatalogKind kind, string sql, object parameters, CancellationToken ct) => kind switch
        {
            CatalogKind.Categories => Read<Category>(sql, parameters, ct),
            CatalogKind.IncomeTypes => Read<IncomeType>(sql, parameters, ct),
            CatalogKind.Creditors => Read<Creditor>(sql, parameters, ct),
            CatalogKind.PaymentMethods => Read<PaymentMethod>(sql, parameters, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        public Task<IReadOnlyList<ICatalogItem>> ListAsync(CatalogKind kind, int idTenant, CancellationToken ct) =>
            Read(kind, CatalogSql.List(kind), new { IdTenant = idTenant }, ct);
        public async Task<ICatalogItem?> FindAsync(CatalogKind kind, int idTenant, Guid code, CancellationToken ct) =>
            (await Read(kind, CatalogSql.Find(kind), new { IdTenant = idTenant, Code = code }, ct)).SingleOrDefault();
        public void Save(ICatalogItem item, bool exists)
        {
            switch (item)
            {
                case Category value: Save(value, CatalogKind.Categories, exists); break;
                case IncomeType value: Save(value, CatalogKind.IncomeTypes, exists); break;
                case Creditor value: Save(value, CatalogKind.Creditors, exists); break;
                case PaymentMethod value: Save(value, CatalogKind.PaymentMethods, exists); break;
                default: throw new ArgumentException("Catálogo inválido.", nameof(item));
            }
        }
        private void Save<T>(T entity, CatalogKind kind, bool exists) where T : Foundation.Domain.Abstractions.Entity
        {
            if (exists) work.AddPendingWrite(CatalogSql.Update(kind), entity);
            else new DapperRepository<T>(work).Add(entity);
        }
        public void AddRevision(CatalogRevision revision) => new DapperRepository<CatalogRevision>(work).Add(revision);
    }
}
