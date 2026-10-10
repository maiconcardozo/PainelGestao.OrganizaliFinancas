namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Accounts
{
    internal static class AccountSql
    {
        // Balance is recomposed from the opening balance and effective movements; it is never a stored column.
        public const string List = """
            SELECT a.Code,a.Name,a.IsActive,a.Version,a.OpeningDate,a.OpeningBalance,
                a.OpeningBalance+COALESCE((SELECT SUM(m.Amount) FROM AccountMovement m
                    WHERE m.IdTenant=a.IdTenant AND m.IdFinancialAccount=a.Id AND m.MovementDate<=@AsOf),0) Balance,
                EXISTS(SELECT 1 FROM AccountMovement m WHERE m.IdTenant=a.IdTenant AND m.IdFinancialAccount=a.Id) HasMovements
            FROM FinancialAccount a WHERE a.IdTenant=@IdTenant ORDER BY a.IsActive DESC,a.Name,a.Id
            """;
        public const string Find = "SELECT * FROM FinancialAccount WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string FindById = "SELECT * FROM FinancialAccount WHERE IdTenant=@IdTenant AND Id=@Id";
        public const string CountMovements = "SELECT COUNT(*) FROM AccountMovement WHERE IdTenant=@IdTenant AND IdFinancialAccount=@IdAccount";
        public const string SumBefore = """
            SELECT COALESCE(SUM(Amount),0) FROM AccountMovement
            WHERE IdTenant=@IdTenant AND IdFinancialAccount=@IdAccount AND MovementDate<@Date
            """;
        public const string Movements = """
            SELECT m.Code,m.MovementDate,m.Amount,m.Kind,m.Description,t.Code TransferCode,r.Code ReceiptCode,p.Code PaymentCode,o.Code ReversedMovementCode
            FROM AccountMovement m
            LEFT JOIN Transfer t ON t.Id=m.IdTransfer AND t.IdTenant=m.IdTenant
            LEFT JOIN Receipt r ON r.Id=m.IdReceipt AND r.IdTenant=m.IdTenant
            LEFT JOIN Payment p ON p.Id=m.IdPayment AND p.IdTenant=m.IdTenant
            LEFT JOIN AccountMovement o ON o.Id=m.IdReversedMovement AND o.IdTenant=m.IdTenant
            WHERE m.IdTenant=@IdTenant AND m.IdFinancialAccount=@IdAccount AND m.MovementDate BETWEEN @From AND @To
            ORDER BY m.MovementDate,m.Id
            """;
        public const string OriginalMovements = """
            SELECT * FROM AccountMovement WHERE IdTenant=@IdTenant AND Kind<>'reversal'
                AND ((@IdTransfer IS NOT NULL AND IdTransfer=@IdTransfer) OR (@IdReceipt IS NOT NULL AND IdReceipt=@IdReceipt)
                    OR (@IdPayment IS NOT NULL AND IdPayment=@IdPayment))
            ORDER BY Id
            """;
        public const string FindTransfer = "SELECT * FROM Transfer WHERE IdTenant=@IdTenant AND Code=@Code";
        private const string SelectTransfer = """
            SELECT t.Code,sa.Code SourceAccountCode,sa.Name SourceAccountName,ta.Code TargetAccountCode,ta.Name TargetAccountName,
                t.TransferDate,t.Amount,t.Description,t.DtReversed,t.ReversalReason
            FROM Transfer t
            JOIN FinancialAccount sa ON sa.Id=t.IdSourceAccount AND sa.IdTenant=t.IdTenant
            JOIN FinancialAccount ta ON ta.Id=t.IdTargetAccount AND ta.IdTenant=t.IdTenant
            """;
        public const string GetTransfer = SelectTransfer + " WHERE t.IdTenant=@IdTenant AND t.Code=@Code";
        public const string ListTransfers = SelectTransfer + " WHERE t.IdTenant=@IdTenant ORDER BY t.TransferDate DESC,t.Id DESC LIMIT 200";
        public const string Update = """
            UPDATE FinancialAccount SET Name=@Name,OpeningDate=@OpeningDate,OpeningBalance=@OpeningBalance,IsActive=@IsActive,Version=@Version,
                DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,DtDeleted=@DtDeleted,DeletedBy=@DeletedBy
            WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string MarkTransferReversed = """
            UPDATE Transfer SET DtReversed=@DtReversed,ReversedBy=@ReversedBy,ReversalReason=@ReversalReason
            WHERE IdTenant=@IdTenant AND Id=@Id AND DtReversed IS NULL
            """;
        public const string AddMovement = """
            INSERT INTO AccountMovement (Code,IdTenant,IdFinancialAccount,MovementDate,Amount,Kind,Description,IdTransfer,IdReceipt,IdPayment,
                IdReversedMovement,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdTenant,@IdFinancialAccount,@MovementDate,@Amount,@Kind,@Description,
                COALESCE(@IdTransfer,(SELECT t.Id FROM Transfer t WHERE t.IdTenant=@IdTenant AND t.Code=@TransferCode)),
                COALESCE(@IdReceipt,(SELECT r.Id FROM Receipt r WHERE r.IdTenant=@IdTenant AND r.Code=@ReceiptCode)),
                COALESCE(@IdPayment,(SELECT p.Id FROM Payment p WHERE p.IdTenant=@IdTenant AND p.Code=@PaymentCode)),
                @IdReversedMovement,1,@DtCreated,@CreatedBy)
            """;
        public const string FindIdempotency = """
            SELECT * FROM IdempotencyRecord WHERE IdTenant=@IdTenant AND Operation=@Operation AND RequestKey=@Key
            """;
    }
}
