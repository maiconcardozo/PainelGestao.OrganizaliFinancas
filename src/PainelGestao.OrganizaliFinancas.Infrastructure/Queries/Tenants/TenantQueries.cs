using Foundation.Dapper.Sql;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Tenants
{
    internal sealed class FindTenantQuery(int idTenant) : SqlQuery
    {
        public int IdTenant { get; } = idTenant;
        public override string GetSql() => """
            SELECT Id, Code, Name, IsActive, DtCreated, CreatedBy, DtUpdated, UpdatedBy, DtDeleted, DeletedBy
            FROM Tenant
            WHERE Id = @IdTenant AND IsActive = 1
            """;
    }
}
