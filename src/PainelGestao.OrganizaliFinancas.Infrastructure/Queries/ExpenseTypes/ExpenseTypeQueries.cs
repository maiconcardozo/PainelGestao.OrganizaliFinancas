namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.ExpenseTypes
{
    internal static class ExpenseTypeSql
    {
        private const string SelectVersion = """
            SELECT t.Code,v.Name,t.IsActive,v.Version,v.Behavior,c.Code DefaultCategoryCode,v.FieldsJson
            FROM ExpenseType t JOIN ExpenseTypeVersion v ON v.IdExpenseType=t.Id AND v.IdTenant=t.IdTenant
            LEFT JOIN Category c ON c.Id=v.IdDefaultCategory AND c.IdTenant=t.IdTenant
            """;
        public const string List = SelectVersion + " WHERE t.IdTenant=@IdTenant AND v.Version=t.Version ORDER BY t.IsActive DESC,t.Name";
        public const string FindVersion = SelectVersion + " WHERE t.IdTenant=@IdTenant AND t.Code=@Code AND v.Version=@Version";
        public const string Find = "SELECT * FROM ExpenseType WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string Update = """
            UPDATE ExpenseType SET Name=@Name,Version=@Version,IsActive=@IsActive,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,
                DtDeleted=@DtDeleted,DeletedBy=@DeletedBy WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string AddVersion = """
            INSERT INTO ExpenseTypeVersion (Code,IdTenant,IdExpenseType,Version,Name,Behavior,IdDefaultCategory,FieldsJson,IsActive,DtCreated,CreatedBy)
            SELECT @VersionCode,@IdTenant,t.Id,@Version,@Name,@Behavior,@IdCategory,@FieldsJson,1,UTC_TIMESTAMP(6),@Actor
            FROM ExpenseType t WHERE t.IdTenant=@IdTenant AND t.Code=@Code
            """;
    }
}
