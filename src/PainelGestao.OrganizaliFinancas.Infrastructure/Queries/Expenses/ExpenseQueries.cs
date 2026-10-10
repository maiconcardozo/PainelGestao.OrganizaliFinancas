namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Expenses
{
    internal static class ExpenseSql
    {
        // Only allocations of non-reversed payments reduce the open balance.
        public const string LivePaid = """
            SELECT COALESCE(SUM(a.Amount),0) FROM PaymentAllocation a JOIN Payment p ON p.Id=a.IdPayment AND p.IdTenant=a.IdTenant
            WHERE a.IdTenant=o.IdTenant AND a.IdObligation=o.Id AND p.DtReversed IS NULL
            """;
        public const string AdjustmentSum = "SELECT COALESCE(SUM(j.Amount),0) FROM ObligationAdjustment j WHERE j.IdTenant=o.IdTenant AND j.IdObligation=o.Id";
        private const string SelectObligation = $"""
            SELECT o.Code,o.Description,o.Competence,o.DueDate,o.Amount,({AdjustmentSum}) Adjustments,({LivePaid}) Paid,o.Status,o.IsEstimated,
                o.CancelReason,t.Code ExpenseTypeCode,t.Name ExpenseTypeName,o.ExpenseTypeVersion,c.Code CategoryCode,c.Name CategoryName,
                cr.Code CreditorCode,cr.Name CreditorName,pm.Code PaymentMethodCode,pp.Code PayerPersonCode,pp.Name PayerPersonName,
                r.Code RecurrenceCode,o.IsDetached,ip.Code InstallmentPlanCode,o.InstallmentNumber,o.InstallmentCount,o.FieldValuesJson,o.SplitMode,o.Version,ci.Code CardInvoiceCode
            FROM Obligation o
            LEFT JOIN ExpenseType t ON t.Id=o.IdExpenseType AND t.IdTenant=o.IdTenant
            LEFT JOIN Category c ON c.Id=o.IdCategory AND c.IdTenant=o.IdTenant
            LEFT JOIN Creditor cr ON cr.Id=o.IdCreditor AND cr.IdTenant=o.IdTenant
            LEFT JOIN PaymentMethod pm ON pm.Id=o.IdPaymentMethod AND pm.IdTenant=o.IdTenant
            LEFT JOIN Person pp ON pp.Id=o.IdPayerPerson AND pp.IdTenant=o.IdTenant
            LEFT JOIN RecurringExpense r ON r.Id=o.IdRecurringExpense AND r.IdTenant=o.IdTenant
            LEFT JOIN InstallmentPlan ip ON ip.Id=o.IdInstallmentPlan AND ip.IdTenant=o.IdTenant
            LEFT JOIN CardInvoice ci ON ci.Id=o.IdCardInvoice AND ci.IdTenant=o.IdTenant
            """;
        public const string List = SelectObligation + $"""
             WHERE o.IdTenant=@IdTenant AND o.IsActive=1 AND (o.DueDate BETWEEN @From AND @To
                OR (@IncludeOverdue AND o.DueDate<@From AND o.Status='open' AND o.Amount+({AdjustmentSum})-({LivePaid})>0))
            ORDER BY o.DueDate,o.Id
            """;
        public const string Get = SelectObligation + " WHERE o.IdTenant=@IdTenant AND o.Code=@Code";
        public const string Find = "SELECT * FROM Obligation WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string FindMany = "SELECT * FROM Obligation WHERE IdTenant=@IdTenant AND Code IN @Codes";
        public const string Totals = $"SELECT o.Id,({AdjustmentSum}) Adjustments,({LivePaid}) Paid FROM Obligation o WHERE o.IdTenant=@IdTenant AND o.Id IN @Ids";
        public const string Shares = """
            SELECT o.Code ObligationCode,p.Code PersonCode,p.Name PersonName,s.Amount,s.Percent
            FROM ObligationShare s
            JOIN Obligation o ON o.Id=s.IdObligation AND o.IdTenant=s.IdTenant
            JOIN Person p ON p.Id=s.IdPerson AND p.IdTenant=s.IdTenant
            WHERE s.IdTenant=@IdTenant AND s.IsActive=1 AND o.Code IN @Codes ORDER BY p.Name,p.Code
            """;
        public const string Adjustments = """
            SELECT Code,Kind,Amount,AdjustmentDate,Reason,CreatedBy FROM ObligationAdjustment
            WHERE IdTenant=@IdTenant AND IdObligation=@IdObligation ORDER BY AdjustmentDate,Id
            """;
        public const string Update = """
            UPDATE Obligation SET Description=@Description,IdExpenseType=@IdExpenseType,ExpenseTypeVersion=@ExpenseTypeVersion,IdCategory=@IdCategory,
                IdCreditor=@IdCreditor,IdPaymentMethod=@IdPaymentMethod,IdPayerPerson=@IdPayerPerson,FieldValuesJson=@FieldValuesJson,SplitMode=@SplitMode,
                Competence=@Competence,DueDate=@DueDate,Amount=@Amount,IsEstimated=@IsEstimated,Status=@Status,CancelReason=@CancelReason,
                IsDetached=@IsDetached,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy
            WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string DeactivateShares = """
            UPDATE ObligationShare s JOIN Obligation o ON o.Id=s.IdObligation AND o.IdTenant=s.IdTenant
            SET s.IsActive=0,s.DtDeleted=UTC_TIMESTAMP(6),s.DeletedBy=@Actor
            WHERE s.IdTenant=@IdTenant AND o.Code=@ObligationCode AND s.IsActive=1
            """;
        public const string AddShare = """
            INSERT INTO ObligationShare (Code,IdTenant,IdObligation,IdPerson,Amount,Percent,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,o.Id,p.Id,@Amount,@Percent,1,UTC_TIMESTAMP(6),@Actor
            FROM Obligation o JOIN Person p ON p.IdTenant=o.IdTenant AND p.Code=@PersonCode
            WHERE o.IdTenant=@IdTenant AND o.Code=@ObligationCode
            """;
        public const string LinkInstallment = """
            UPDATE Obligation SET IdInstallmentPlan=(SELECT ip.Id FROM InstallmentPlan ip WHERE ip.IdTenant=@IdTenant AND ip.Code=@PlanCode)
            WHERE IdTenant=@IdTenant AND Code=@ObligationCode
            """;

        public const string FindPayment = "SELECT * FROM Payment WHERE IdTenant=@IdTenant AND Code=@Code";
        private const string SelectPayment = """
            SELECT p.Code,a.Code AccountCode,a.Name AccountName,pp.Code PayerPersonCode,pp.Name PayerPersonName,pm.Code PaymentMethodCode,
                p.PaymentDate,p.Amount,p.Description,p.DtReversed,p.ReversalReason
            FROM Payment p
            JOIN FinancialAccount a ON a.Id=p.IdFinancialAccount AND a.IdTenant=p.IdTenant
            LEFT JOIN Person pp ON pp.Id=p.IdPayerPerson AND pp.IdTenant=p.IdTenant
            LEFT JOIN PaymentMethod pm ON pm.Id=p.IdPaymentMethod AND pm.IdTenant=p.IdTenant
            """;
        public const string GetPayment = SelectPayment + " WHERE p.IdTenant=@IdTenant AND p.Code=@Code";
        public const string Payments = SelectPayment + " WHERE p.IdTenant=@IdTenant AND p.PaymentDate BETWEEN @From AND @To ORDER BY p.PaymentDate DESC,p.Id DESC";
        private const string SelectAllocation = """
            SELECT p.Code PaymentCode,o.Code ExpenseCode,o.Description ExpenseDescription,x.Amount,p.PaymentDate,a.Name AccountName,(p.DtReversed IS NOT NULL) IsReversed
            FROM PaymentAllocation x
            JOIN Payment p ON p.Id=x.IdPayment AND p.IdTenant=x.IdTenant
            JOIN Obligation o ON o.Id=x.IdObligation AND o.IdTenant=x.IdTenant
            JOIN FinancialAccount a ON a.Id=p.IdFinancialAccount AND a.IdTenant=p.IdTenant
            """;
        public const string AllocationsByPayment = SelectAllocation + " WHERE x.IdTenant=@IdTenant AND p.Code IN @Codes ORDER BY x.Id";
        public const string AllocationsByObligation = SelectAllocation + " WHERE x.IdTenant=@IdTenant AND x.IdObligation=@IdObligation ORDER BY p.PaymentDate,x.Id";
        public const string AddAllocation = """
            INSERT INTO PaymentAllocation (Code,IdTenant,IdPayment,IdObligation,Amount,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,p.Id,o.Id,@Amount,1,UTC_TIMESTAMP(6),@Actor
            FROM Payment p JOIN Obligation o ON o.IdTenant=p.IdTenant AND o.Code=@ObligationCode
            WHERE p.IdTenant=@IdTenant AND p.Code=@PaymentCode
            """;
        public const string MarkPaymentReversed = """
            UPDATE Payment SET DtReversed=@DtReversed,ReversedBy=@ReversedBy,ReversalReason=@ReversalReason
            WHERE IdTenant=@IdTenant AND Id=@Id AND DtReversed IS NULL
            """;

        public const string Recurrences = """
            SELECT r.Code,r.Description,r.IsActive,r.Version,v.ValidFrom,v.EndDate,v.Frequency,v.Amount,v.IsEstimated,t.Code ExpenseTypeCode,r.ExpenseTypeVersion,
                c.Code CategoryCode,c.Name CategoryName,cr.Code CreditorCode,pm.Code PaymentMethodCode,pp.Code PayerPersonCode,pp.Name PayerPersonName,
                r.FieldValuesJson,r.SplitMode,r.SharesJson
            FROM RecurringExpense r
            JOIN RecurringExpenseVersion v ON v.IdRecurringExpense=r.Id AND v.IdTenant=r.IdTenant AND v.Version=r.Version
            LEFT JOIN ExpenseType t ON t.Id=r.IdExpenseType AND t.IdTenant=r.IdTenant
            LEFT JOIN Category c ON c.Id=r.IdCategory AND c.IdTenant=r.IdTenant
            LEFT JOIN Creditor cr ON cr.Id=r.IdCreditor AND cr.IdTenant=r.IdTenant
            LEFT JOIN PaymentMethod pm ON pm.Id=r.IdPaymentMethod AND pm.IdTenant=r.IdTenant
            LEFT JOIN Person pp ON pp.Id=r.IdPayerPerson AND pp.IdTenant=r.IdTenant
            WHERE r.IdTenant=@IdTenant ORDER BY r.IsActive DESC,r.Description
            """;
        public const string FindRecurrence = "SELECT * FROM RecurringExpense WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string RecurrenceVersions = """
            SELECT Version,ValidFrom,EndDate,Frequency,Amount,IsEstimated FROM RecurringExpenseVersion
            WHERE IdTenant=@IdTenant AND IdRecurringExpense=@IdRule ORDER BY Version
            """;
        public const string UpdateRecurrence = """
            UPDATE RecurringExpense SET Description=@Description,IdExpenseType=@IdExpenseType,ExpenseTypeVersion=@ExpenseTypeVersion,IdCategory=@IdCategory,
                IdCreditor=@IdCreditor,IdPaymentMethod=@IdPaymentMethod,IdPayerPerson=@IdPayerPerson,FieldValuesJson=@FieldValuesJson,SplitMode=@SplitMode,
                SharesJson=@SharesJson,Version=@Version,IsActive=@IsActive,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,DtDeleted=@DtDeleted,DeletedBy=@DeletedBy
            WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string AddRecurrenceVersion = """
            INSERT INTO RecurringExpenseVersion (Code,IdTenant,IdRecurringExpense,Version,ValidFrom,EndDate,Frequency,Amount,IsEstimated,IsActive,DtCreated,CreatedBy)
            SELECT @VersionCode,@IdTenant,r.Id,@Version,@ValidFrom,@EndDate,@Frequency,@Amount,@IsEstimated,1,UTC_TIMESTAMP(6),@Actor
            FROM RecurringExpense r WHERE r.IdTenant=@IdTenant AND r.Code=@RuleCode
            """;
        public const string OccurrenceStates = $"""
            SELECT o.Code,o.OccurrenceDate,o.IsActive,(o.Status<>'open' OR o.IsDetached OR ({LivePaid})>0
                OR EXISTS(SELECT 1 FROM ObligationAdjustment j WHERE j.IdTenant=o.IdTenant AND j.IdObligation=o.Id)) Locked,
                o.RecurrenceVersion,o.Amount
            FROM Obligation o WHERE o.IdTenant=@IdTenant AND o.IdRecurringExpense=@IdRule
            """;
        public const string InsertOccurrence = """
            INSERT INTO Obligation (Code,IdTenant,Description,IdExpenseType,ExpenseTypeVersion,IdCategory,IdCreditor,IdPaymentMethod,IdPayerPerson,
                FieldValuesJson,SplitMode,Competence,DueDate,Amount,IsEstimated,Status,IdRecurringExpense,RecurrenceVersion,OccurrenceDate,IsDetached,
                Version,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,r.Description,r.IdExpenseType,r.ExpenseTypeVersion,r.IdCategory,r.IdCreditor,r.IdPaymentMethod,r.IdPayerPerson,
                r.FieldValuesJson,r.SplitMode,@Competence,@OccurrenceDate,@Amount,@IsEstimated,'open',r.Id,@RecurrenceVersion,@OccurrenceDate,0,
                0,1,UTC_TIMESTAMP(6),@Actor
            FROM RecurringExpense r WHERE r.IdTenant=@IdTenant AND r.Code=@RuleCode
            """;
        public const string UpdateOccurrence = """
            UPDATE Obligation o JOIN RecurringExpense r ON r.Id=o.IdRecurringExpense AND r.IdTenant=o.IdTenant
            SET o.Description=r.Description,o.IdExpenseType=r.IdExpenseType,o.ExpenseTypeVersion=r.ExpenseTypeVersion,o.IdCategory=r.IdCategory,
                o.IdCreditor=r.IdCreditor,o.IdPaymentMethod=r.IdPaymentMethod,o.IdPayerPerson=r.IdPayerPerson,o.FieldValuesJson=r.FieldValuesJson,
                o.SplitMode=r.SplitMode,o.Amount=@Amount,o.IsEstimated=@IsEstimated,o.RecurrenceVersion=@RecurrenceVersion,o.IsActive=1,
                o.DtDeleted=NULL,o.DeletedBy=NULL,o.Version=o.Version+1,o.DtUpdated=UTC_TIMESTAMP(6),o.UpdatedBy=@Actor
            WHERE o.IdTenant=@IdTenant AND o.Code=@Code AND o.Status='open' AND o.IsDetached=0
            """;
        public const string CancelOccurrence = """
            UPDATE Obligation SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor,Version=Version+1
            WHERE IdTenant=@IdTenant AND Code=@Code AND Status='open' AND IsDetached=0
            """;

        public const string ResponsibleTotals = """
            SELECT p.Code PersonCode,p.Name PersonName,SUM(s.Amount) Total
            FROM ObligationShare s
            JOIN Obligation o ON o.Id=s.IdObligation AND o.IdTenant=s.IdTenant
            JOIN Person p ON p.Id=s.IdPerson AND p.IdTenant=s.IdTenant
            WHERE s.IdTenant=@IdTenant AND s.IsActive=1 AND o.IsActive=1 AND o.Status='open' AND o.Competence>=@From AND o.Competence<@ToExclusive
            GROUP BY p.Code,p.Name ORDER BY p.Name
            """;
        public const string PaidBySplitPayer = """
            SELECT pp.Code PersonCode,pp.Name PersonName,SUM(x.Amount) Total
            FROM PaymentAllocation x
            JOIN Payment p ON p.Id=x.IdPayment AND p.IdTenant=x.IdTenant
            JOIN Obligation o ON o.Id=x.IdObligation AND o.IdTenant=x.IdTenant
            JOIN Person pp ON pp.Id=p.IdPayerPerson AND pp.IdTenant=p.IdTenant
            WHERE x.IdTenant=@IdTenant AND p.DtReversed IS NULL AND o.IsActive=1 AND o.Competence>=@From AND o.Competence<@ToExclusive
                AND EXISTS(SELECT 1 FROM ObligationShare s WHERE s.IdTenant=o.IdTenant AND s.IdObligation=o.Id AND s.IsActive=1)
            GROUP BY pp.Code,pp.Name ORDER BY pp.Name
            """;
    }
}
