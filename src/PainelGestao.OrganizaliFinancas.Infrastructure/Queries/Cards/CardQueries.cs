using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Expenses;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Cards
{
    internal static class CardSql
    {
        private const string OpenBalance = $"o.Amount+({ExpenseSql.AdjustmentSum})-({ExpenseSql.LivePaid})";
        public const string Cards = $"""
            SELECT c.Code,c.Name,c.IsActive,c.Version,p.Code HolderPersonCode,p.Name HolderName,c.ClosingDay,c.DueDay,c.CreditLimit,c.ClosingDayRule,
                COALESCE((SELECT SUM({OpenBalance}) FROM CardInvoice i JOIN Obligation o ON o.IdCardInvoice=i.Id AND o.IdTenant=i.IdTenant
                    WHERE i.IdTenant=c.IdTenant AND i.IdCreditCard=c.Id),0) Committed
            FROM CreditCard c JOIN Person p ON p.Id=c.IdHolderPerson AND p.IdTenant=c.IdTenant
            WHERE c.IdTenant=@IdTenant ORDER BY c.IsActive DESC,c.Name
            """;
        public const string FindCard = "SELECT * FROM CreditCard WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string FindCardById = "SELECT * FROM CreditCard WHERE IdTenant=@IdTenant AND Id=@Id";
        public const string UpdateCard = """
            UPDATE CreditCard SET Name=@Name,IdHolderPerson=@IdHolderPerson,ClosingDay=@ClosingDay,DueDay=@DueDay,CreditLimit=@CreditLimit,
                ClosingDayRule=@ClosingDayRule,Version=@Version,IsActive=@IsActive,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy,DtDeleted=@DtDeleted,DeletedBy=@DeletedBy
            WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        private const string SelectInvoice = $"""
            SELECT i.Code,c.Code CardCode,c.Name CardName,i.ReferenceMonth,i.ClosingDate,i.DueDate,o.Code ExpenseCode,o.Amount,
                ({ExpenseSql.AdjustmentSum}) Adjustments,({ExpenseSql.LivePaid}) Paid,
                (SELECT COUNT(*) FROM CardItem x WHERE x.IdTenant=i.IdTenant AND x.IdCardInvoice=i.Id AND x.IsActive=1) ItemCount
            FROM CardInvoice i
            JOIN CreditCard c ON c.Id=i.IdCreditCard AND c.IdTenant=i.IdTenant
            JOIN Obligation o ON o.IdCardInvoice=i.Id AND o.IdTenant=i.IdTenant
            """;
        public const string Invoices = SelectInvoice + """
             WHERE i.IdTenant=@IdTenant AND (@CardCode IS NULL OR c.Code=@CardCode) AND i.ReferenceMonth BETWEEN @From AND @To
            ORDER BY i.ReferenceMonth,c.Name
            """;
        public const string GetInvoice = SelectInvoice + " WHERE i.IdTenant=@IdTenant AND i.Code=@Code";
        public const string FindInvoices = "SELECT * FROM CardInvoice WHERE IdTenant=@IdTenant AND IdCreditCard=@IdCard AND ReferenceMonth IN @Months";
        public const string LinkInvoice = """
            UPDATE Obligation SET IdCardInvoice=(SELECT i.Id FROM CardInvoice i WHERE i.IdTenant=@IdTenant AND i.Code=@InvoiceCode)
            WHERE IdTenant=@IdTenant AND Code=@ObligationCode
            """;
        public const string Items = """
            SELECT x.Code,p.Code PurchaseCode,p.Description,p.PurchaseDate,x.Kind,x.InstallmentNumber,p.InstallmentCount,x.Amount,c.Name CategoryName,
                i.Code InvoiceCode,i.ReferenceMonth,x.IsActive,x.Note
            FROM CardItem x
            JOIN CardPurchase p ON p.Id=x.IdCardPurchase AND p.IdTenant=x.IdTenant
            JOIN CardInvoice i ON i.Id=x.IdCardInvoice AND i.IdTenant=x.IdTenant
            LEFT JOIN Category c ON c.Id=p.IdCategory AND c.IdTenant=p.IdTenant
            WHERE x.IdTenant=@IdTenant AND i.Code=@InvoiceCode ORDER BY p.PurchaseDate,x.Id
            """;
        private const string SelectPurchase = """
            SELECT p.Code,c.Code CardCode,c.Name CardName,p.Description,p.PurchaseDate,p.TotalAmount,p.InstallmentCount,p.RefundedAmount,
                cat.Code CategoryCode,cat.Name CategoryName,b.Code BuyerPersonCode,b.Name BuyerName,t.Code ExpenseTypeCode,p.Version
            FROM CardPurchase p
            JOIN CreditCard c ON c.Id=p.IdCreditCard AND c.IdTenant=p.IdTenant
            LEFT JOIN Category cat ON cat.Id=p.IdCategory AND cat.IdTenant=p.IdTenant
            LEFT JOIN Person b ON b.Id=p.IdBuyerPerson AND b.IdTenant=p.IdTenant
            LEFT JOIN ExpenseType t ON t.Id=p.IdExpenseType AND t.IdTenant=p.IdTenant
            """;
        public const string Purchases = SelectPurchase + " WHERE p.IdTenant=@IdTenant AND p.PurchaseDate BETWEEN @From AND @To ORDER BY p.PurchaseDate DESC,p.Id DESC";
        public const string GetPurchase = SelectPurchase + " WHERE p.IdTenant=@IdTenant AND p.Code=@Code";
        public const string FindPurchase = "SELECT * FROM CardPurchase WHERE IdTenant=@IdTenant AND Code=@Code";
        public const string PurchaseItems = """
            SELECT x.Code,i.Code InvoiceCode,i.ClosingDate,x.Kind,x.InstallmentNumber,x.Amount
            FROM CardItem x JOIN CardInvoice i ON i.Id=x.IdCardInvoice AND i.IdTenant=x.IdTenant
            WHERE x.IdTenant=@IdTenant AND x.IdCardPurchase=@IdPurchase AND x.IsActive=1 ORDER BY i.ReferenceMonth,x.Id
            """;
        public const string UpdatePurchase = """
            UPDATE CardPurchase SET RefundedAmount=@RefundedAmount,Version=@Version,DtUpdated=@DtUpdated,UpdatedBy=@UpdatedBy WHERE IdTenant=@IdTenant AND Id=@Id
            """;
        public const string AddItem = """
            INSERT INTO CardItem (Code,IdTenant,IdCardPurchase,IdCardInvoice,Kind,InstallmentNumber,Amount,Competence,Note,IsActive,DtCreated,CreatedBy)
            SELECT @Code,@IdTenant,p.Id,i.Id,@Kind,@Number,@Amount,@Competence,@Note,1,UTC_TIMESTAMP(6),@Actor
            FROM CardPurchase p JOIN CardInvoice i ON i.IdTenant=p.IdTenant AND i.Code=@InvoiceCode
            WHERE p.IdTenant=@IdTenant AND p.Code=@PurchaseCode
            """;
        public const string DeactivateItem = """
            UPDATE CardItem SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor WHERE IdTenant=@IdTenant AND Code=@Code AND IsActive=1
            """;
        public const string RecalculateInvoice = """
            UPDATE Obligation o JOIN CardInvoice i ON i.Id=o.IdCardInvoice AND i.IdTenant=o.IdTenant
            SET o.Amount=(SELECT COALESCE(SUM(x.Amount),0) FROM CardItem x WHERE x.IdTenant=i.IdTenant AND x.IdCardInvoice=i.Id AND x.IsActive=1),
                o.Version=o.Version+1,o.DtUpdated=UTC_TIMESTAMP(6),o.UpdatedBy=@Actor
            WHERE o.IdTenant=@IdTenant AND i.Code=@InvoiceCode
            """;
    }
}
