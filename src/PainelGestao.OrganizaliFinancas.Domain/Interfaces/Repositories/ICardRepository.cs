using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    /// <summary>Committed is the open balance of every invoice of the card, future installments included.</summary>
    public sealed record CardRecord
    {
        public Guid Code { get; init; } public string Name { get; init; } = string.Empty; public bool IsActive { get; init; } public int Version { get; init; }
        public Guid HolderPersonCode { get; init; } public string HolderName { get; init; } = string.Empty; public int ClosingDay { get; init; } public int DueDay { get; init; }
        public decimal? CreditLimit { get; init; } public string ClosingDayRule { get; init; } = string.Empty; public decimal Committed { get; init; }
    }
    public sealed record InvoiceRecord
    {
        public Guid Code { get; init; } public Guid CardCode { get; init; } public string CardName { get; init; } = string.Empty; public DateTime ReferenceMonth { get; init; }
        public DateTime ClosingDate { get; init; } public DateTime DueDate { get; init; } public Guid ExpenseCode { get; init; } public decimal Amount { get; init; }
        public decimal Adjustments { get; init; } public decimal Paid { get; init; } public int ItemCount { get; init; }
    }
    public sealed record CardItemRecord
    {
        public Guid Code { get; init; } public Guid PurchaseCode { get; init; } public string Description { get; init; } = string.Empty; public DateTime PurchaseDate { get; init; }
        public string Kind { get; init; } = string.Empty; public int? InstallmentNumber { get; init; } public int InstallmentCount { get; init; } public decimal Amount { get; init; }
        public string? CategoryName { get; init; } public Guid InvoiceCode { get; init; } public DateTime ReferenceMonth { get; init; } public bool IsActive { get; init; }
        public string? Note { get; init; }
    }
    /// <summary>Active item of a purchase with the closing date of its invoice, used to decide what a refund can cancel.</summary>
    public sealed record PurchaseItemState
    {
        public Guid Code { get; init; } public Guid InvoiceCode { get; init; } public DateTime ClosingDate { get; init; } public string Kind { get; init; } = string.Empty;
        public int? InstallmentNumber { get; init; } public decimal Amount { get; init; }
    }
    public sealed record PurchaseRecord
    {
        public Guid Code { get; init; } public Guid CardCode { get; init; } public string CardName { get; init; } = string.Empty; public string Description { get; init; } = string.Empty;
        public DateTime PurchaseDate { get; init; } public decimal TotalAmount { get; init; } public int InstallmentCount { get; init; } public decimal RefundedAmount { get; init; }
        public Guid? CategoryCode { get; init; } public string? CategoryName { get; init; } public Guid? BuyerPersonCode { get; init; } public string? BuyerName { get; init; }
        public Guid? ExpenseTypeCode { get; init; } public int? ExpenseTypeVersion { get; init; } public string FieldValuesJson { get; init; } = "{}"; public int Version { get; init; }
    }
    /// <summary>What stands in the way of removing a card: purchases still registered and invoices with a payment, adjustment, share or receipt.</summary>
    public sealed record CardUsage(int Purchases, int LockedInvoices);

    public interface ICardRepository
    {
        Task<IReadOnlyList<CardRecord>> ListCardsAsync(int idTenant, CancellationToken ct);
        Task<CreditCard?> FindCardAsync(int idTenant, Guid code, CancellationToken ct);
        Task<CreditCard?> FindCardByIdAsync(int idTenant, int id, CancellationToken ct);
        void AddCard(CreditCard card);
        void UpdateCard(CreditCard card);
        Task<CardUsage> GetCardUsageAsync(int idTenant, int idCard, CancellationToken ct);
        /// <summary>Hard delete; only valid after <see cref="RemoveEmptyInvoices"/> left the card without invoices.</summary>
        void RemoveCard(int idTenant, int idCard);

        Task<IReadOnlyList<InvoiceRecord>> ListInvoicesAsync(int idTenant, Guid? cardCode, DateTime fromMonth, DateTime toMonth, CancellationToken ct);
        Task<InvoiceRecord?> GetInvoiceAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<CardInvoice>> FindInvoicesAsync(int idTenant, int idCard, IReadOnlyCollection<DateTime> referenceMonths, CancellationToken ct);
        /// <summary>The invoice and its backing obligation are created together and linked by Code in the same transaction.</summary>
        void AddInvoice(CardInvoice invoice, Obligation obligation);
        Task<IReadOnlyList<CardItemRecord>> ListItemsAsync(int idTenant, Guid invoiceCode, CancellationToken ct);

        Task<IReadOnlyList<PurchaseRecord>> ListPurchasesAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct);
        Task<PurchaseRecord?> GetPurchaseAsync(int idTenant, Guid code, CancellationToken ct);
        Task<CardPurchase?> FindPurchaseAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<PurchaseItemState>> ListPurchaseItemsAsync(int idTenant, int idPurchase, CancellationToken ct);
        void AddPurchase(CardPurchase purchase);
        void UpdatePurchase(CardPurchase purchase);
        void AddItem(int idTenant, Guid purchaseCode, Guid invoiceCode, string kind, int? number, decimal amount, DateTime competence, string? note, string actor);
        void DeactivateItem(int idTenant, Guid itemCode, string actor);
        /// <summary>The invoice obligation amount is always recomputed from its active items, after the item writes of the transaction.</summary>
        void RecalculateInvoice(int idTenant, Guid invoiceCode, string actor);
        void RecalculateCardInvoices(int idTenant, int idCard, string actor);
        /// <summary>True when an invoice holding an item of the purchase received a payment, reversed or not.</summary>
        Task<bool> PurchaseHasPaidInvoiceAsync(int idTenant, int idPurchase, CancellationToken ct);
        void UpdatePurchaseDetails(CardPurchase purchase);
        /// <summary>Deletes the purchase and every item of it; the caller recalculates the card's invoices afterwards.</summary>
        void RemovePurchase(int idTenant, int idPurchase);
        /// <summary>Deletes the card's invoices (with their obligations) that no longer have items, payments, adjustments, shares or receipts.</summary>
        void RemoveEmptyInvoices(int idTenant, int idCard);
    }
}
