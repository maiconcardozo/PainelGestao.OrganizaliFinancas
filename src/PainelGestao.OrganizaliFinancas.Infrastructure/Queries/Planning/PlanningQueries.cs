using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Expenses;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Planning
{
    internal static class PlanningSql
    {
        public const string Categories = """
            SELECT c.Id,c.Code,c.Name,p.Code ParentCode,c.IsActive FROM Category c
            LEFT JOIN Category p ON p.Id=c.IdParentCategory AND p.IdTenant=c.IdTenant
            WHERE c.IdTenant=@IdTenant ORDER BY c.Name
            """;
        // Invoice obligations are excluded: their card items carry the categories, so nothing is counted twice.
        public const string Spend = $"""
            SELECT s.CategoryCode,SUM(s.Bills) Bills,SUM(s.Card) Card,SUM(s.Paid) Paid FROM (
                SELECT c.Code CategoryCode,o.Amount+({ExpenseSql.AdjustmentSum}) Bills,0.00 Card,({ExpenseSql.LivePaid}) Paid
                FROM Obligation o LEFT JOIN Category c ON c.Id=o.IdCategory AND c.IdTenant=o.IdTenant
                WHERE o.IdTenant=@IdTenant AND o.IsActive=1 AND o.Status<>'cancelled' AND o.IdCardInvoice IS NULL AND o.Competence=@Month
                UNION ALL
                SELECT c.Code,0.00,x.Amount,0.00
                FROM CardItem x JOIN CardPurchase p ON p.Id=x.IdCardPurchase AND p.IdTenant=x.IdTenant
                LEFT JOIN Category c ON c.Id=p.IdCategory AND c.IdTenant=p.IdTenant
                WHERE x.IdTenant=@IdTenant AND x.IsActive=1 AND x.Competence=@Month
            ) s GROUP BY s.CategoryCode
            """;
        public const string Budgets = """
            SELECT c.Code CategoryCode,b.Amount,b.Version FROM BudgetLimit b JOIN Category c ON c.Id=b.IdCategory AND c.IdTenant=b.IdTenant
            WHERE b.IdTenant=@IdTenant AND b.Month=@Month AND b.IsActive=1
            """;
        public const string SaveBudget = """
            INSERT INTO BudgetLimit (Code,IdTenant,IdCategory,Month,Amount,Version,IsActive,DtCreated,CreatedBy,DtDeleted,DeletedBy)
            VALUES (@Code,@IdTenant,@IdCategory,@Month,@Amount,0,@IsActive,UTC_TIMESTAMP(6),@Actor,IF(@IsActive,NULL,UTC_TIMESTAMP(6)),IF(@IsActive,NULL,@Actor))
            ON DUPLICATE KEY UPDATE Amount=VALUES(Amount),IsActive=VALUES(IsActive),Version=Version+1,DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor,
                DtDeleted=IF(VALUES(IsActive),NULL,UTC_TIMESTAMP(6)),DeletedBy=IF(VALUES(IsActive),NULL,@Actor)
            """;
        public const string Preference = "SELECT WarningPercent,LimitPercent,DueSoonDays,Version FROM AlertPreference WHERE IdTenant=@IdTenant";
        public const string SavePreference = """
            INSERT INTO AlertPreference (Code,IdTenant,WarningPercent,LimitPercent,DueSoonDays,Version,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdTenant,@Warning,@Limit,@DueSoonDays,1,1,UTC_TIMESTAMP(6),@Actor)
            ON DUPLICATE KEY UPDATE WarningPercent=VALUES(WarningPercent),LimitPercent=VALUES(LimitPercent),DueSoonDays=VALUES(DueSoonDays),
                Version=Version+1,DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            """;
        public const string AlertStates = "SELECT AlertKey,Fingerprint,Status,SnoozedUntil FROM AlertState WHERE IdTenant=@IdTenant";
        public const string SaveAlertState = """
            INSERT INTO AlertState (Code,IdTenant,AlertKey,Fingerprint,Status,SnoozedUntil,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdTenant,@Key,@Fingerprint,@Status,@SnoozedUntil,1,UTC_TIMESTAMP(6),@Actor)
            ON DUPLICATE KEY UPDATE Fingerprint=VALUES(Fingerprint),Status=VALUES(Status),SnoozedUntil=VALUES(SnoozedUntil),DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            """;
        public const string BalanceBefore = """
            SELECT COALESCE((SELECT SUM(OpeningBalance) FROM FinancialAccount WHERE IdTenant=@IdTenant AND OpeningDate<@Date),0)
                 + COALESCE((SELECT SUM(Amount) FROM AccountMovement WHERE IdTenant=@IdTenant AND MovementDate<@Date),0)
            """;
        // Reversals stay on the side of the original movement, so a reversed receipt reduces inflows instead of inflating outflows.
        private const string Flows = """
            SUM(CASE WHEN Kind='receipt' OR (Kind='reversal' AND IdReceipt IS NOT NULL) THEN Amount ELSE 0 END) Inflow,
            SUM(CASE WHEN Kind='payment' OR (Kind='reversal' AND IdPayment IS NOT NULL) THEN Amount ELSE 0 END) Outflow
            FROM AccountMovement WHERE IdTenant=@IdTenant AND IdTransfer IS NULL AND MovementDate>=@From AND MovementDate<@ToExclusive
            """;
        public const string MonthlyCash = "SELECT CAST(DATE_FORMAT(MovementDate,'%Y-%m-01') AS DATE) Period," + Flows + " GROUP BY Period ORDER BY Period";
        public const string DailyCash = "SELECT MovementDate Period," + Flows + " GROUP BY MovementDate ORDER BY MovementDate";
        public const string AnalysisLines = $"""
            SELECT 'bill' Source,o.Code,o.Description,c.Code CategoryCode,c.Name CategoryName,o.DueDate,o.Amount+({ExpenseSql.AdjustmentSum}) Amount,
                ({ExpenseSql.LivePaid}) Paid,o.Status,CAST(NULL AS CHAR(60)) CardName,CAST(NULL AS CHAR(36)) BuyerCode,
                (SELECT GROUP_CONCAT(DISTINCT a.Code) FROM PaymentAllocation x JOIN Payment p ON p.Id=x.IdPayment AND p.IdTenant=x.IdTenant
                    JOIN FinancialAccount a ON a.Id=p.IdFinancialAccount AND a.IdTenant=p.IdTenant
                    WHERE x.IdTenant=o.IdTenant AND x.IdObligation=o.Id AND p.DtReversed IS NULL) AccountCodes
            FROM Obligation o LEFT JOIN Category c ON c.Id=o.IdCategory AND c.IdTenant=o.IdTenant
            WHERE o.IdTenant=@IdTenant AND o.IsActive=1 AND o.Status<>'cancelled' AND o.IdCardInvoice IS NULL AND o.Competence=@Month
            UNION ALL
            SELECT 'card',x.Code,p.Description,c.Code,c.Name,i.DueDate,x.Amount,0.00,'card',cc.Name,b.Code,NULL
            FROM CardItem x
            JOIN CardPurchase p ON p.Id=x.IdCardPurchase AND p.IdTenant=x.IdTenant
            JOIN CardInvoice i ON i.Id=x.IdCardInvoice AND i.IdTenant=x.IdTenant
            JOIN CreditCard cc ON cc.Id=p.IdCreditCard AND cc.IdTenant=p.IdTenant
            LEFT JOIN Category c ON c.Id=p.IdCategory AND c.IdTenant=p.IdTenant
            LEFT JOIN Person b ON b.Id=p.IdBuyerPerson AND b.IdTenant=p.IdTenant
            WHERE x.IdTenant=@IdTenant AND x.IsActive=1 AND x.Competence=@Month
            ORDER BY DueDate,Description
            """;
    }
}
