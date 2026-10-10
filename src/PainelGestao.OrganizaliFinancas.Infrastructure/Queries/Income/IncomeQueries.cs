namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Income
{
    internal static class IncomeSql
    {
        public const string Sources = """
            SELECT s.Code,s.Name,s.IsActive,s.Version,p.Code PersonCode,p.Name PersonName,it.Code IncomeTypeCode,
                v.ValidFrom,v.EndDate,v.Frequency,v.AmountMode,v.ExpectedNetAmount,v.GrossAmount
            FROM IncomeSource s
            JOIN Person p ON p.Id=s.IdPerson AND p.IdTenant=s.IdTenant
            JOIN IncomeSourceVersion v ON v.IdIncomeSource=s.Id AND v.IdTenant=s.IdTenant AND v.Version=s.Version
            LEFT JOIN IncomeType it ON it.Id=s.IdIncomeType AND it.IdTenant=s.IdTenant
            WHERE s.IdTenant=@IdTenant ORDER BY s.IsActive DESC,p.Name,s.Name
            """;
        public const string FindSource = "SELECT * FROM IncomeSource WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string FindSourceById = "SELECT * FROM IncomeSource WHERE IdTenant=@IdTenant AND Id=@Id";
        public const string Versions = """
            SELECT Version,ValidFrom,EndDate,Frequency,AmountMode,ExpectedNetAmount,GrossAmount,DtCreated,CreatedBy
            FROM IncomeSourceVersion WHERE IdTenant=@IdTenant AND IdIncomeSource=@IdSource ORDER BY Version
            """;
        public const string UpdateSource = """
            UPDATE IncomeSource SET Name=@Name,IdIncomeType=@IdIncomeType,Version=@Version,IsActive=@IsActive,DtUpdated=@DtUpdated,
                UpdatedBy=@UpdatedBy,DtDeleted=@DtDeleted,DeletedBy=@DeletedBy WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string AddVersion = """
            INSERT INTO IncomeSourceVersion (Code,IdTenant,IdIncomeSource,Version,ValidFrom,EndDate,Frequency,AmountMode,ExpectedNetAmount,
                GrossAmount,IsActive,DtCreated,CreatedBy)
            SELECT @VersionCode,@IdTenant,s.Id,@Version,@ValidFrom,@EndDate,@Frequency,@AmountMode,@ExpectedNetAmount,@GrossAmount,1,UTC_TIMESTAMP(6),@Actor
            FROM IncomeSource s WHERE s.IdTenant=@IdTenant AND s.Code=@SourceCode
            """;
        // Only allocations of non-reversed receipts lock a forecast against regeneration.
        private const string LiveAllocated = """
            SELECT COALESCE(SUM(a.Amount),0) FROM ReceiptAllocation a JOIN Receipt r ON r.Id=a.IdReceipt AND r.IdTenant=a.IdTenant
            WHERE a.IdTenant=f.IdTenant AND a.IdIncomeForecast=f.Id AND r.DtReversed IS NULL
            """;
        public const string ForecastStates = $"""
            SELECT f.Code,f.OccurrenceDate,f.IsActive,(f.Status<>'open' OR ({LiveAllocated})>0) Locked,f.SourceVersion,f.ExpectedAmount
            FROM IncomeForecast f WHERE f.IdTenant=@IdTenant AND f.IdIncomeSource=@IdSource
            """;
        public const string InsertForecast = """
            INSERT INTO IncomeForecast (Code,IdTenant,IdIncomeSource,IdIncomeSourceVersion,SourceVersion,OccurrenceDate,Competence,ExpectedAmount,
                Status,Version,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,s.Id,v.Id,@SourceVersion,@OccurrenceDate,@Competence,@ExpectedAmount,'open',0,1,UTC_TIMESTAMP(6),@Actor
            FROM IncomeSource s JOIN IncomeSourceVersion v ON v.IdIncomeSource=s.Id AND v.IdTenant=s.IdTenant AND v.Version=@SourceVersion
            WHERE s.IdTenant=@IdTenant AND s.Code=@SourceCode
            """;
        public const string UpdateForecast = """
            UPDATE IncomeForecast f
            JOIN IncomeSourceVersion v ON v.IdIncomeSource=f.IdIncomeSource AND v.IdTenant=f.IdTenant AND v.Version=@SourceVersion
            SET f.IdIncomeSourceVersion=v.Id,f.SourceVersion=@SourceVersion,f.ExpectedAmount=@ExpectedAmount,f.IsActive=1,f.DtDeleted=NULL,
                f.DeletedBy=NULL,f.Version=f.Version+1,f.DtUpdated=UTC_TIMESTAMP(6),f.UpdatedBy=@Actor
            WHERE f.IdTenant=@IdTenant AND f.Code=@Code AND f.Status='open'
            """;
        public const string CancelForecast = """
            UPDATE IncomeForecast SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor,Version=Version+1
            WHERE IdTenant=@IdTenant AND Code=@Code AND Status='open'
            """;
        public const string Forecasts = $"""
            SELECT f.Code,s.Code SourceCode,s.Name SourceName,p.Code PersonCode,p.Name PersonName,v.AmountMode,f.OccurrenceDate,f.Competence,
                f.ExpectedAmount,({LiveAllocated}) ReceivedAmount,f.Status,f.CloseReason,f.SourceVersion,f.Version
            FROM IncomeForecast f
            JOIN IncomeSource s ON s.Id=f.IdIncomeSource AND s.IdTenant=f.IdTenant
            JOIN Person p ON p.Id=s.IdPerson AND p.IdTenant=s.IdTenant
            JOIN IncomeSourceVersion v ON v.Id=f.IdIncomeSourceVersion AND v.IdTenant=f.IdTenant
            WHERE f.IdTenant=@IdTenant AND f.IsActive=1 AND f.Competence BETWEEN @From AND @To
            ORDER BY f.OccurrenceDate,p.Name,s.Name
            """;
        public const string FindForecast = "SELECT * FROM IncomeForecast WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string FindForecasts = "SELECT * FROM IncomeForecast WHERE IdTenant=@IdTenant AND Code IN @Codes";
        public const string CloseForecast = """
            UPDATE IncomeForecast SET Status=@Status,CloseReason=@CloseReason,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy
            WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string FindReceipt = "SELECT * FROM Receipt WHERE IdTenant=@IdTenant AND Code=@Code";
        private const string SelectReceipt = """
            SELECT r.Code,a.Code AccountCode,a.Name AccountName,p.Code PersonCode,p.Name PersonName,s.Code SourceCode,s.Name SourceName,
                r.ReceiptDate,r.Amount,COALESCE((SELECT SUM(x.Amount) FROM ReceiptAllocation x WHERE x.IdTenant=r.IdTenant AND x.IdReceipt=r.Id),0) AllocatedAmount,
                r.Description,r.DtReversed,r.ReversalReason
            FROM Receipt r
            JOIN FinancialAccount a ON a.Id=r.IdFinancialAccount AND a.IdTenant=r.IdTenant
            JOIN Person p ON p.Id=r.IdPerson AND p.IdTenant=r.IdTenant
            LEFT JOIN IncomeSource s ON s.Id=r.IdIncomeSource AND s.IdTenant=r.IdTenant
            """;
        public const string GetReceipt = SelectReceipt + " WHERE r.IdTenant=@IdTenant AND r.Code=@Code";
        public const string Receipts = SelectReceipt + " WHERE r.IdTenant=@IdTenant AND r.ReceiptDate BETWEEN @From AND @To ORDER BY r.ReceiptDate DESC,r.Id DESC";
        public const string AddAllocation = """
            INSERT INTO ReceiptAllocation (Code,IdTenant,IdReceipt,IdIncomeForecast,Amount,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,r.Id,@IdForecast,@Amount,1,UTC_TIMESTAMP(6),@Actor FROM Receipt r WHERE r.IdTenant=@IdTenant AND r.Code=@ReceiptCode
            """;
        public const string MarkReceiptReversed = """
            UPDATE Receipt SET DtReversed=@DtReversed,ReversedBy=@ReversedBy,ReversalReason=@ReversalReason
            WHERE IdTenant=@IdTenant AND Id=@Id AND DtReversed IS NULL
            """;
        public const string ReceivedTotals = """
            SELECT CAST(DATE_FORMAT(r.ReceiptDate,'%Y-%m-01') AS DATE) Month,p.Code PersonCode,p.Name PersonName,s.Code SourceCode,s.Name SourceName,
                SUM(r.Amount) Total
            FROM Receipt r
            JOIN Person p ON p.Id=r.IdPerson AND p.IdTenant=r.IdTenant
            LEFT JOIN IncomeSource s ON s.Id=r.IdIncomeSource AND s.IdTenant=r.IdTenant
            WHERE r.IdTenant=@IdTenant AND r.DtReversed IS NULL AND r.ReceiptDate>=@From AND r.ReceiptDate<@ToExclusive
            GROUP BY Month,p.Code,p.Name,s.Code,s.Name ORDER BY Month,p.Name
            """;
        public const string ExpectedTotals = """
            SELECT f.Competence Month,p.Code PersonCode,p.Name PersonName,s.Code SourceCode,s.Name SourceName,SUM(f.ExpectedAmount) Total
            FROM IncomeForecast f
            JOIN IncomeSource s ON s.Id=f.IdIncomeSource AND s.IdTenant=f.IdTenant
            JOIN Person p ON p.Id=s.IdPerson AND p.IdTenant=s.IdTenant
            WHERE f.IdTenant=@IdTenant AND f.IsActive=1 AND f.ExpectedAmount IS NOT NULL AND f.Competence>=@From AND f.Competence<@ToExclusive
            GROUP BY f.Competence,p.Code,p.Name,s.Code,s.Name ORDER BY f.Competence,p.Name
            """;
    }
}
