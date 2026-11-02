using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    /// <summary>Obligation with its derived totals: adjustments and payments of non-reversed payments.</summary>
    public sealed record ObligationRecord
    {
        public Guid Code { get; init; } public string Description { get; init; } = string.Empty; public DateTime Competence { get; init; }
        public DateTime DueDate { get; init; } public decimal Amount { get; init; } public decimal Adjustments { get; init; } public decimal Paid { get; init; }
        public string Status { get; init; } = string.Empty; public bool IsEstimated { get; init; } public string? CancelReason { get; init; }
        public Guid? ExpenseTypeCode { get; init; } public string? ExpenseTypeName { get; init; } public int? ExpenseTypeVersion { get; init; }
        public Guid? CategoryCode { get; init; } public string? CategoryName { get; init; } public Guid? CreditorCode { get; init; } public string? CreditorName { get; init; }
        public Guid? PaymentMethodCode { get; init; } public Guid? PayerPersonCode { get; init; } public string? PayerPersonName { get; init; }
        public Guid? RecurrenceCode { get; init; } public bool IsDetached { get; init; } public Guid? InstallmentPlanCode { get; init; }
        public int? InstallmentNumber { get; init; } public int? InstallmentCount { get; init; } public string FieldValuesJson { get; init; } = "{}";
        public string? SplitMode { get; init; } public int Version { get; init; } public Guid? CardInvoiceCode { get; init; }
        /// <summary>Card invoice only: the card's name and how many active items (purchases and installments) the invoice holds (A19).</summary>
        public string? CardName { get; init; } public int CardItemCount { get; init; }
        /// <summary>
        /// Amount of this obligation settled by live payments on a credit card (A17), with the card and the invoice that holds it (A21): that amount already
        /// sits inside the invoice line, so Despesas shows it as "Paga · cartão X · fatura m/aa" without counting it again in the month.
        /// </summary>
        public decimal CardPaid { get; init; } public string? PaidCardName { get; init; } public DateTime? PaidInvoiceMonth { get; init; }
        public DateTime? LastPaymentDate { get; init; }
    }
    public sealed record ShareRecord
    {
        public Guid ObligationCode { get; init; } public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty;
        public decimal Amount { get; init; } public decimal? Percent { get; init; }
    }
    public sealed record AdjustmentRecord
    {
        public Guid Code { get; init; } public string Kind { get; init; } = string.Empty; public decimal Amount { get; init; }
        public DateTime AdjustmentDate { get; init; } public string Reason { get; init; } = string.Empty; public string CreatedBy { get; init; } = string.Empty;
    }
    public sealed record ObligationTotalsRecord
    {
        public int Id { get; init; } public decimal Adjustments { get; init; } public decimal Paid { get; init; }
    }
    /// <summary>Account fields are null for a payment made on a credit card; card fields are null for a bank payment.</summary>
    public sealed record PaymentRecord
    {
        public Guid Code { get; init; } public Guid? AccountCode { get; init; } public string? AccountName { get; init; }
        public Guid? CardCode { get; init; } public string? CardName { get; init; } public Guid? PurchaseCode { get; init; }
        public Guid? PayerPersonCode { get; init; } public string? PayerPersonName { get; init; } public Guid? PaymentMethodCode { get; init; }
        public DateTime PaymentDate { get; init; } public decimal Amount { get; init; } public string Description { get; init; } = string.Empty;
        public DateTime? DtReversed { get; init; } public string? ReversalReason { get; init; }
    }
    public sealed record PaymentAllocationRecord
    {
        public Guid PaymentCode { get; init; } public Guid ExpenseCode { get; init; } public string ExpenseDescription { get; init; } = string.Empty;
        public decimal Amount { get; init; } public DateTime PaymentDate { get; init; } public string? AccountName { get; init; } public string? CardName { get; init; }
        public bool IsReversed { get; init; }
    }
    public sealed record RecurringExpenseRecord
    {
        public Guid Code { get; init; } public string Description { get; init; } = string.Empty; public bool IsActive { get; init; } public int Version { get; init; }
        public DateTime ValidFrom { get; init; } public DateTime? EndDate { get; init; } public string Frequency { get; init; } = string.Empty;
        public decimal Amount { get; init; } public bool IsEstimated { get; init; } public Guid? ExpenseTypeCode { get; init; } public int? ExpenseTypeVersion { get; init; }
        public Guid? CategoryCode { get; init; } public string? CategoryName { get; init; } public Guid? CreditorCode { get; init; } public Guid? PaymentMethodCode { get; init; }
        public Guid? PayerPersonCode { get; init; } public string? PayerPersonName { get; init; } public string FieldValuesJson { get; init; } = "{}";
        public string? SplitMode { get; init; } public string SharesJson { get; init; } = "[]";
    }
    public sealed record RecurringVersionRecord
    {
        public int Version { get; init; } public DateTime ValidFrom { get; init; } public DateTime? EndDate { get; init; }
        public string Frequency { get; init; } = string.Empty; public decimal Amount { get; init; } public bool IsEstimated { get; init; }
        public ExpenseTermsVersion ToTerms() => new(Version, new ExpenseTerms(DateOnly.FromDateTime(ValidFrom),
            EndDate is DateTime end ? DateOnly.FromDateTime(end) : null, Frequency, Amount, IsEstimated));
    }
    /// <summary>Locked occurrences were cancelled, edited individually, adjusted or paid and are never rewritten.</summary>
    public sealed record OccurrenceStateRecord
    {
        public Guid Code { get; init; } public DateTime OccurrenceDate { get; init; } public bool IsActive { get; init; } public bool Locked { get; init; }
        public int RecurrenceVersion { get; init; } public decimal Amount { get; init; }
        public ForecastState ToState() => new(Code, DateOnly.FromDateTime(OccurrenceDate), IsActive, Locked, RecurrenceVersion, Amount);
    }
    public sealed record PersonTotalRecord
    {
        public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty; public decimal Total { get; init; }
    }

    public interface IExpenseRepository
    {
        /// <summary>Obligations due in the period plus, when requested, open ones that were already overdue before it.</summary>
        Task<IReadOnlyList<ObligationRecord>> ListAsync(int idTenant, DateTime from, DateTime to, bool includeOverdue, CancellationToken ct);
        Task<ObligationRecord?> GetAsync(int idTenant, Guid code, CancellationToken ct);
        Task<Obligation?> FindAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<Obligation>> FindManyAsync(int idTenant, IReadOnlyCollection<Guid> codes, CancellationToken ct);
        Task<IReadOnlyList<ObligationTotalsRecord>> TotalsAsync(int idTenant, IReadOnlyCollection<int> ids, CancellationToken ct);
        Task<IReadOnlyList<ShareRecord>> ListSharesAsync(int idTenant, IReadOnlyCollection<Guid> obligationCodes, CancellationToken ct);
        Task<IReadOnlyList<AdjustmentRecord>> ListAdjustmentsAsync(int idTenant, int idObligation, CancellationToken ct);
        void Add(Obligation obligation);
        void Update(Obligation obligation);
        /// <summary>Previous shares are deactivated, not deleted, so the split history remains.</summary>
        void ReplaceShares(int idTenant, Guid obligationCode, IReadOnlyList<ResolvedShare> shares, string actor);
        void AddAdjustment(ObligationAdjustment adjustment);
        void AddInstallmentPlan(InstallmentPlan plan);
        void LinkInstallment(int idTenant, Guid obligationCode, Guid planCode);
        /// <summary>Active receipts attached to the obligation; they block a physical delete so no stored file is orphaned.</summary>
        Task<int> CountAttachmentsAsync(int idTenant, Guid obligationCode, CancellationToken ct);
        /// <summary>
        /// Hard delete of an obligation nobody paid: shares, adjustments, allocations of reversed payments and the row itself go;
        /// an installment plan left without installments goes with it. The caller checked <see cref="Obligation.EnsureRemovable"/>.
        /// </summary>
        void Remove(Obligation obligation);
        /// <summary>Hides a recurring occurrence and detaches it, so lists stop showing it and the recurrence never recreates it.</summary>
        void HideOccurrence(Obligation obligation, string actor);

        Task<Payment?> FindPaymentAsync(int idTenant, Guid code, CancellationToken ct);
        Task<PaymentRecord?> GetPaymentAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<PaymentRecord>> ListPaymentsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct);
        Task<IReadOnlyList<PaymentAllocationRecord>> ListAllocationsByPaymentAsync(int idTenant, IReadOnlyCollection<Guid> paymentCodes, CancellationToken ct);
        Task<IReadOnlyList<PaymentAllocationRecord>> ListAllocationsByObligationAsync(int idTenant, int idObligation, CancellationToken ct);
        void AddPayment(Payment payment);
        /// <summary>Payment and obligation may both be new in the transaction; they are resolved by Code.</summary>
        void AddAllocation(int idTenant, Guid paymentCode, Guid obligationCode, decimal amount, string actor);
        void MarkPaymentReversed(Payment payment);
        /// <summary>Rewrites only the reason of a payment already reversed.</summary>
        void UpdateReversalReason(Payment payment);
        /// <summary>Links a card payment to the purchase that carries it into the invoice; both rows may be new in the transaction and are resolved by Code.</summary>
        void LinkPaymentToPurchase(int idTenant, Guid paymentCode, Guid purchaseCode);

        Task<IReadOnlyList<RecurringExpenseRecord>> ListRecurrencesAsync(int idTenant, CancellationToken ct);
        Task<RecurringExpense?> FindRecurrenceAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<RecurringVersionRecord>> ListRecurrenceVersionsAsync(int idTenant, int idRule, CancellationToken ct);
        void AddRecurrence(RecurringExpense rule);
        void UpdateRecurrence(RecurringExpense rule);
        void AddRecurrenceVersion(RecurringExpense rule, ExpenseTerms terms, string actor);
        Task<IReadOnlyList<OccurrenceStateRecord>> ListOccurrenceStatesAsync(int idTenant, int idRule, CancellationToken ct);
        /// <summary>Copies the template's current descriptive data into the occurrence.</summary>
        void InsertOccurrence(RecurringExpense rule, Guid code, PlannedOccurrence occurrence, bool isEstimated, string actor);
        void UpdateOccurrence(RecurringExpense rule, Guid code, PlannedOccurrence occurrence, bool isEstimated, string actor);
        void CancelOccurrence(int idTenant, Guid code, string actor);

        /// <summary>Active shares of open obligations whose competence is in the period, grouped by person.</summary>
        Task<IReadOnlyList<PersonTotalRecord>> ResponsibleTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
        /// <summary>Live payments, grouped by the effective payer, of split obligations whose competence is in the period.</summary>
        Task<IReadOnlyList<PersonTotalRecord>> PaidBySplitPayerAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
    }
}
