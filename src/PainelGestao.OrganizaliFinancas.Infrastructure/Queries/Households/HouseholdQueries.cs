namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Households
{
    internal static class HouseholdSql
    {
        public const string People = "SELECT * FROM Person WHERE IdTenant=@IdTenant ORDER BY IsActive DESC,Name,Id";
        public const string Person = "SELECT * FROM Person WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string Settings = "SELECT * FROM HouseholdSettings WHERE IdTenant=@IdTenant";
        public const string UpdatePerson = """
            UPDATE Person SET Name=@Name,IsActive=@IsActive,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,
                DtDeleted=@DtDeleted,DeletedBy=@DeletedBy WHERE Id=@Id AND IdTenant=@IdTenant
            """;
        public const string UpdateSettings = """
            UPDATE HouseholdSettings SET TimeZone=@TimeZone,StartDate=@StartDate,Version=@Version,
                DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy WHERE Id=@Id AND IdTenant=@IdTenant
            """;
    }
}
