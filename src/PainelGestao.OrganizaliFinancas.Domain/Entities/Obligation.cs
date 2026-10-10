using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Descriptive data shared by an obligation and the templates (recurrence, installment plan) that generate it.</summary>
    public sealed record ObligationDetails(string Description, int? IdExpenseType, int? ExpenseTypeVersion, int? IdCategory, int? IdCreditor,
        int? IdPaymentMethod, int? IdPayerPerson, string FieldValuesJson, string? SplitMode)
    {
        public ObligationDetails Normalized()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Description);
            if (Description.Trim().Length > 200) throw new ArgumentException("Descrição excede 200 caracteres.", nameof(Description));
            return this with { Description = Description.Trim() };
        }
    }

    /// <summary>
    /// A commitment to pay: amount, competence and due date. It is not money; payments are separate ledger records.
    /// Status stores only open/cancelled; partial, paid and overdue are derived from adjustments and live payments.
    /// </summary>
    public class Obligation : TenantEntity
    {
        private Obligation() { }
        private Obligation(int idTenant) : base(idTenant) { }
        public string Description { get; private set; } = string.Empty;
        public int? IdExpenseType { get; private set; }
        public int? ExpenseTypeVersion { get; private set; }
        public int? IdCategory { get; private set; }
        public int? IdCreditor { get; private set; }
        public int? IdPaymentMethod { get; private set; }
        public int? IdPayerPerson { get; private set; }
        public string FieldValuesJson { get; private set; } = "{}";
        public string? SplitMode { get; private set; }
        public DateTime Competence { get; private set; }
        public DateTime DueDate { get; private set; }
        public decimal Amount { get; private set; }
        public bool IsEstimated { get; private set; }
        public string Status { get; private set; } = "open";
        public string? CancelReason { get; private set; }
        public int? IdRecurringExpense { get; private set; }
        public int? RecurrenceVersion { get; private set; }
        public DateTime? OccurrenceDate { get; private set; }
        public bool IsDetached { get; private set; }
        public int? IdInstallmentPlan { get; private set; }
        public int? InstallmentNumber { get; private set; }
        public int? InstallmentCount { get; private set; }
        public int Version { get; private set; }

        public static Obligation Create(int idTenant, ObligationDetails details, DateOnly competence, DateOnly dueDate, decimal amount, string actor)
        {
            var obligation = new Obligation(idTenant);
            obligation.Apply(details.Normalized(), competence, dueDate, amount);
            obligation.SetCreatedBy(actor);
            return obligation;
        }
        public static Obligation CreateInstallment(int idTenant, ObligationDetails details, PlannedInstallment installment, int count, string actor)
        {
            var obligation = Create(idTenant, details, Month(installment.DueDate), installment.DueDate, installment.Amount, actor);
            (obligation.InstallmentNumber, obligation.InstallmentCount) = (installment.Number, count);
            return obligation;
        }
        public static DateOnly Month(DateOnly date) => new(date.Year, date.Month, 1);
        private void Apply(ObligationDetails details, DateOnly competence, DateOnly dueDate, decimal amount)
        {
            if (amount <= 0 || Money.Round(amount) != amount) throw new ArgumentException("Valor deve ser positivo em centavos.", nameof(amount));
            (Description, IdExpenseType, ExpenseTypeVersion, IdCategory, IdCreditor, IdPaymentMethod, IdPayerPerson, FieldValuesJson, SplitMode) =
                (details.Description, details.IdExpenseType, details.ExpenseTypeVersion, details.IdCategory, details.IdCreditor, details.IdPaymentMethod,
                 details.IdPayerPerson, details.FieldValuesJson, details.SplitMode);
            Competence = Month(competence).ToDateTime(TimeOnly.MinValue);
            DueDate = dueDate.ToDateTime(TimeOnly.MinValue);
            Amount = amount;
        }
        public bool IsCancelled() => Status == "cancelled";
        public DateOnly DueDay() => DateOnly.FromDateTime(DueDate);

        /// <summary>
        /// Editing "this occurrence" detaches it from its recurrence so regeneration never overwrites it.
        /// The principal is frozen once a live payment depends on it; corrections then go through adjustments.
        /// </summary>
        public void Edit(ObligationDetails details, DateOnly competence, DateOnly dueDate, decimal amount, decimal livePaid, string actor)
        {
            EnsureOpen();
            if (livePaid > 0 && amount != Amount)
                throw new ConflictException("Esta despesa já tem pagamento; corrija o valor com juros, multa, desconto ou ajuste.");
            Apply(details.Normalized(), competence, dueDate, amount);
            IsEstimated = false;
            if (IdRecurringExpense is not null) IsDetached = true;
            Version++;
            UpdateAuditInfo(actor);
        }
        /// <summary>Cancels the future balance and keeps history. A paid obligation needs its payments reversed first (RF22).</summary>
        public void Cancel(string reason, decimal livePaid, string actor)
        {
            EnsureOpen();
            if (livePaid > 0) throw new ConflictException("Esta despesa tem pagamento ativo. Estorne o pagamento antes de cancelar.");
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            Status = "cancelled"; CancelReason = reason.Trim(); Version++;
            UpdateAuditInfo(actor);
        }
        public void EnsureOpen()
        {
            if (IsCancelled()) throw new ConflictException($"A despesa {Description} está cancelada.");
        }
    }

    public class ObligationShare : TenantEntity
    {
        private ObligationShare() { }
        public int IdObligation { get; private set; }
        public int IdPerson { get; private set; }
        public decimal Amount { get; private set; }
        public decimal? Percent { get; private set; }
    }

    /// <summary>Interest, fine, discount or a signed adjustment, always with a reason. Payments never hide these components.</summary>
    public class ObligationAdjustment : TenantEntity
    {
        public static readonly string[] Kinds = ["interest", "fine", "discount", "adjustment"];
        private ObligationAdjustment() { }
        private ObligationAdjustment(int idTenant) : base(idTenant) { }
        public int IdObligation { get; private set; }
        public string Kind { get; private set; } = string.Empty;
        public decimal Amount { get; private set; }
        public DateTime AdjustmentDate { get; private set; }
        public string Reason { get; private set; } = string.Empty;
        /// <summary>Discount is informed as a positive value and stored negative; "adjustment" keeps the informed sign.</summary>
        public static ObligationAdjustment Create(Obligation obligation, string kind, decimal value, DateOnly date, string reason, decimal openBalance, string actor)
        {
            obligation.EnsureOpen();
            if (!Kinds.Contains(kind)) throw new ArgumentException("Tipo de ajuste inválido.", nameof(kind));
            if (value == 0 || Money.Round(value) != value || (kind != "adjustment" && value < 0))
                throw new ConflictException("Informe juros, multa e desconto como valores positivos em centavos.");
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            var signed = kind == "discount" ? -value : value;
            if (openBalance + signed < 0) throw new ConflictException("O desconto ou ajuste deixaria a despesa com saldo negativo.");
            var adjustment = new ObligationAdjustment(obligation.IdTenant)
            {
                IdObligation = obligation.Id, Kind = kind, Amount = signed, AdjustmentDate = date.ToDateTime(TimeOnly.MinValue),
                Reason = reason.Trim().Length > 300 ? reason.Trim()[..300] : reason.Trim()
            };
            adjustment.SetCreatedBy(actor);
            return adjustment;
        }
    }

    /// <summary>Effective cash out of one account, allocated to one or more obligations. Reversal keeps it and compensates the movement.</summary>
    public class Payment : TenantEntity
    {
        private Payment() { }
        private Payment(int idTenant) : base(idTenant) { }
        public int IdFinancialAccount { get; private set; }
        public int? IdPayerPerson { get; private set; }
        public int? IdPaymentMethod { get; private set; }
        public DateTime PaymentDate { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public DateTime? DtReversed { get; private set; }
        public string? ReversedBy { get; private set; }
        public string? ReversalReason { get; private set; }
        public bool IsReversed() => DtReversed is not null;
        public static Payment Create(FinancialAccount account, Person? payer, int? idPaymentMethod, DateOnly date, decimal amount, string? description, string actor)
        {
            if (amount <= 0) throw new ArgumentException("Valor deve ser positivo.", nameof(amount));
            if (payer is not null && payer.IdTenant != account.IdTenant) throw new ArgumentException("Pagador de outra casa.");
            account.EnsureAccepts(date);
            var payment = new Payment(account.IdTenant)
            {
                IdFinancialAccount = account.Id, IdPayerPerson = payer?.Id, IdPaymentMethod = idPaymentMethod, PaymentDate = date.ToDateTime(TimeOnly.MinValue),
                Amount = amount, Description = description?.Trim() ?? string.Empty
            };
            payment.SetCreatedBy(actor);
            return payment;
        }
        public void Reverse(string reason, string actor)
        {
            if (IsReversed()) throw new ConflictException("Este pagamento já foi estornado.");
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            (DtReversed, ReversedBy, ReversalReason) = (DateTime.UtcNow, actor, reason.Trim());
        }
    }
    public class PaymentAllocation : TenantEntity
    {
        private PaymentAllocation() { }
        public int IdPayment { get; private set; }
        public int IdObligation { get; private set; }
        public decimal Amount { get; private set; }
    }

    /// <summary>Template of a recurring bill; amounts and dates live in immutable <see cref="RecurringExpenseVersion"/> rows.</summary>
    public class RecurringExpense : TenantEntity
    {
        private RecurringExpense() { }
        private RecurringExpense(int idTenant) : base(idTenant) { }
        public string Description { get; private set; } = string.Empty;
        public int? IdExpenseType { get; private set; }
        public int? ExpenseTypeVersion { get; private set; }
        public int? IdCategory { get; private set; }
        public int? IdCreditor { get; private set; }
        public int? IdPaymentMethod { get; private set; }
        public int? IdPayerPerson { get; private set; }
        public string FieldValuesJson { get; private set; } = "{}";
        public string? SplitMode { get; private set; }
        public string SharesJson { get; private set; } = "[]";
        public int Version { get; private set; } = 1;
        public static RecurringExpense Create(int idTenant, ObligationDetails details, string sharesJson, string actor)
        {
            var rule = new RecurringExpense(idTenant);
            rule.Apply(details.Normalized(), sharesJson);
            rule.SetCreatedBy(actor);
            return rule;
        }
        private void Apply(ObligationDetails details, string sharesJson)
        {
            (Description, IdExpenseType, ExpenseTypeVersion, IdCategory, IdCreditor, IdPaymentMethod, IdPayerPerson, FieldValuesJson, SplitMode) =
                (details.Description, details.IdExpenseType, details.ExpenseTypeVersion, details.IdCategory, details.IdCreditor, details.IdPaymentMethod,
                 details.IdPayerPerson, details.FieldValuesJson, details.SplitMode);
            SharesJson = sharesJson;
        }
        public ObligationDetails Details() => new(Description, IdExpenseType, ExpenseTypeVersion, IdCategory, IdCreditor, IdPaymentMethod, IdPayerPerson, FieldValuesJson, SplitMode);
        /// <summary>"This and next": a new version governs the dates from its start; earlier and locked occurrences are kept.</summary>
        public void Update(ObligationDetails details, string sharesJson, bool active, string actor)
        {
            Apply(details.Normalized(), sharesJson);
            Version++;
            if (active) Activate(actor); else SoftDelete(actor);
            UpdateAuditInfo(actor);
        }
    }
    public class RecurringExpenseVersion : TenantEntity
    {
        private RecurringExpenseVersion() { }
        public int IdRecurringExpense { get; private set; }
        public int Version { get; private set; }
        public DateTime ValidFrom { get; private set; }
        public DateTime? EndDate { get; private set; }
        public string Frequency { get; private set; } = string.Empty;
        public decimal Amount { get; private set; }
        public bool IsEstimated { get; private set; }
    }

    /// <summary>A purchase split into installments; each installment is an ordinary obligation with its own competence.</summary>
    public class InstallmentPlan : TenantEntity
    {
        private InstallmentPlan() { }
        private InstallmentPlan(int idTenant) : base(idTenant) { }
        public string Description { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }
        public int InstallmentCount { get; private set; }
        public DateTime FirstDueDate { get; private set; }
        public string Frequency { get; private set; } = string.Empty;
        public static InstallmentPlan Create(int idTenant, string description, decimal total, int count, DateOnly firstDueDate, string frequency, string actor)
        {
            var plan = new InstallmentPlan(idTenant)
            {
                Description = description.Trim(), TotalAmount = total, InstallmentCount = count, FirstDueDate = firstDueDate.ToDateTime(TimeOnly.MinValue), Frequency = frequency
            };
            plan.SetCreatedBy(actor);
            return plan;
        }
    }
}
