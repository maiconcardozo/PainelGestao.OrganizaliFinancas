namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Complements
{
    internal static class GoalSql
    {
        // Balance = opening balance (if the account was already open) + movements up to the day, the same rule as the accounts screen.
        public const string List = """
            SELECT g.Code,g.Name,g.TargetAmount,g.TargetDate,g.Note,g.IsActive,g.Version,a.Code AccountCode,a.Name AccountName,a.IsActive AccountActive,
                IF(a.OpeningDate<=@AsOf,a.OpeningBalance,0)+COALESCE((SELECT SUM(m.Amount) FROM AccountMovement m
                    WHERE m.IdTenant=a.IdTenant AND m.IdFinancialAccount=a.Id AND m.MovementDate<=@AsOf),0) Balance
            FROM SavingsGoal g JOIN FinancialAccount a ON a.Id=g.IdFinancialAccount AND a.IdTenant=g.IdTenant
            WHERE g.IdTenant=@IdTenant ORDER BY g.IsActive DESC,g.TargetDate IS NULL,g.TargetDate,g.Name
            """;
        public const string Find = "SELECT * FROM SavingsGoal WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string Update = """
            UPDATE SavingsGoal SET Name=@Name,TargetAmount=@TargetAmount,TargetDate=@TargetDate,IdFinancialAccount=@IdFinancialAccount,Note=@Note,
                IsActive=@IsActive,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,DtDeleted=@DtDeleted,DeletedBy=@DeletedBy
            WHERE Id=@Id AND IdTenant=@IdTenant
            """;
    }
    internal static class AttachmentSql
    {
        public const string List = """
            SELECT Code,TargetKind,TargetCode,FileName,ContentType,Size,StorageKey,DtCreated FROM Attachment
            WHERE IdTenant=@IdTenant AND TargetKind=@Kind AND TargetCode=@Target AND IsActive=1 ORDER BY DtCreated
            """;
        public const string Find = "SELECT Code,TargetKind,TargetCode,FileName,ContentType,Size,StorageKey,DtCreated FROM Attachment WHERE IdTenant=@IdTenant AND Code=@Code AND IsActive=1";
        public const string Remove = "UPDATE Attachment SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor WHERE IdTenant=@IdTenant AND Code=@Code AND IsActive=1";
    }
    internal static class ImportSql
    {
        public const string List = """
            SELECT b.Code,b.FileName,b.Format,a.Name AccountName,b.LineCount,b.AppliedCount,b.IgnoredCount,b.DtCreated
            FROM ImportBatch b JOIN FinancialAccount a ON a.Id=b.IdFinancialAccount AND a.IdTenant=b.IdTenant
            WHERE b.IdTenant=@IdTenant ORDER BY b.DtCreated DESC LIMIT 50
            """;
        public const string AppliedFingerprints = "SELECT DISTINCT Fingerprint FROM ImportLine WHERE IdTenant=@IdTenant AND Action<>'ignore' AND Fingerprint IN @Fingerprints";
        public const string AddLine = """
            INSERT INTO ImportLine (Code,IdTenant,IdImportBatch,Fingerprint,LineDate,Amount,Description,ExternalId,Action,ResultCode,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdTenant,(SELECT Id FROM ImportBatch WHERE IdTenant=@IdTenant AND Code=@BatchCode),@Fingerprint,@LineDate,@Amount,@Description,@ExternalId,@Action,@ResultCode,1,UTC_TIMESTAMP(6),@Actor)
            """;
    }
}
