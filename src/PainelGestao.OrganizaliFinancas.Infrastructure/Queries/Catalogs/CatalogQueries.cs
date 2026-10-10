using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Catalogs
{
    internal static class CatalogSql
    {
        // Table names are selected from code, never interpolated from a URL or request.
        private static string Table(CatalogKind kind) => kind switch
        {
            CatalogKind.Categories => "Category", CatalogKind.IncomeTypes => "IncomeType",
            CatalogKind.Creditors => "Creditor", CatalogKind.PaymentMethods => "PaymentMethod",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        public static string List(CatalogKind kind) => $"SELECT * FROM {Table(kind)} WHERE IdTenant=@IdTenant ORDER BY IsActive DESC,Name,Id";
        public static string Find(CatalogKind kind) => $"SELECT * FROM {Table(kind)} WHERE IdTenant=@IdTenant AND Code=@Code";
        public static string Update(CatalogKind kind) => $"""
            UPDATE {Table(kind)} SET Name=@Name,IsActive=@IsActive,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,
                DtDeleted=@DtDeleted,DeletedBy=@DeletedBy{(kind == CatalogKind.Categories ? ",IdParentCategory=@IdParentCategory" : "")}
            WHERE Id=@Id AND IdTenant=@IdTenant
            """;
    }
}
