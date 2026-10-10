using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Card identified by a nickname only: no card number or security code is ever stored (RF23).</summary>
    public class CreditCard : TenantEntity
    {
        private CreditCard() { }
        private CreditCard(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public int IdHolderPerson { get; private set; }
        public int ClosingDay { get; private set; }
        public int DueDay { get; private set; }
        public decimal? CreditLimit { get; private set; }
        public string ClosingDayRule { get; private set; } = "next";
        public int Version { get; private set; }
        public static CreditCard Create(Person holder, string name, int closingDay, int dueDay, decimal? limit, string rule, string actor)
        {
            var card = new CreditCard(holder.IdTenant);
            card.Apply(holder, name, closingDay, dueDay, limit, rule);
            card.SetCreatedBy(actor);
            return card;
        }
        private void Apply(Person holder, string name, int closingDay, int dueDay, decimal? limit, string rule)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 60) throw new ArgumentException("Apelido excede 60 caracteres.", nameof(name));
            // A nickname that looks like a card number is refused so the PAN never reaches the database.
            if (name.Count(char.IsDigit) > 6) throw new InputValidationException(new Dictionary<string, string[]> { ["Name"] = ["Use um apelido sem o número do cartão."] });
            if (limit is < 0) throw new InputValidationException(new Dictionary<string, string[]> { ["CreditLimit"] = ["O limite não pode ser negativo."] });
            CardSchedule.ValidateDays(closingDay, dueDay, rule);
            (Name, IdHolderPerson, ClosingDay, DueDay, CreditLimit, ClosingDayRule) = (name.Trim(), holder.Id, closingDay, dueDay, limit, rule);
        }
        /// <summary>New closing or due days apply to invoices created from now on; existing invoices keep their dates and items.</summary>
        public void Update(Person holder, string name, int closingDay, int dueDay, decimal? limit, string rule, bool active, string actor)
        {
            Apply(holder, name, closingDay, dueDay, limit, rule);
            if (active) Activate(actor); else SoftDelete(actor);
            Version++;
            UpdateAuditInfo(actor);
        }
        public void EnsureAcceptsPurchases()
        {
            if (!IsActive) throw new ConflictException($"O cartão {Name} está inativo e não recebe novas compras.");
        }
    }

    /// <summary>Aggregator of a cycle's items. It is paid through its backing obligation, never counted as a second expense.</summary>
    public class CardInvoice : TenantEntity
    {
        private CardInvoice() { }
        private CardInvoice(int idTenant) : base(idTenant) { }
        public int IdCreditCard { get; private set; }
        public DateTime ReferenceMonth { get; private set; }
        public DateTime ClosingDate { get; private set; }
        public DateTime DueDate { get; private set; }
        public static CardInvoice Create(CreditCard card, CardCycle cycle, string actor)
        {
            var invoice = new CardInvoice(card.IdTenant)
            {
                IdCreditCard = card.Id, ReferenceMonth = cycle.ReferenceMonth.ToDateTime(TimeOnly.MinValue),
                ClosingDate = cycle.ClosingDate.ToDateTime(TimeOnly.MinValue), DueDate = cycle.DueDate.ToDateTime(TimeOnly.MinValue)
            };
            invoice.SetCreatedBy(actor);
            return invoice;
        }
        public DateOnly ClosingDay() => DateOnly.FromDateTime(ClosingDate);
    }

    /// <summary>Purchase on the card. It does not move any bank account; its installments are items of future invoices.</summary>
    public class CardPurchase : TenantEntity
    {
        private CardPurchase() { }
        private CardPurchase(int idTenant) : base(idTenant) { }
        public int IdCreditCard { get; private set; }
        public int? IdBuyerPerson { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public int? IdExpenseType { get; private set; }
        public int? ExpenseTypeVersion { get; private set; }
        public int? IdCategory { get; private set; }
        public string FieldValuesJson { get; private set; } = "{}";
        public DateTime PurchaseDate { get; private set; }
        public decimal TotalAmount { get; private set; }
        public int InstallmentCount { get; private set; }
        public decimal RefundedAmount { get; private set; }
        public int Version { get; private set; }
        public static CardPurchase Create(CreditCard card, int? idBuyer, ObligationDetails details, DateOnly date, decimal total, int count, string actor)
        {
            card.EnsureAcceptsPurchases();
            var normalized = details.Normalized();
            var purchase = new CardPurchase(card.IdTenant)
            {
                IdCreditCard = card.Id, IdBuyerPerson = idBuyer, Description = normalized.Description, IdExpenseType = normalized.IdExpenseType,
                ExpenseTypeVersion = normalized.ExpenseTypeVersion, IdCategory = normalized.IdCategory, FieldValuesJson = normalized.FieldValuesJson,
                PurchaseDate = date.ToDateTime(TimeOnly.MinValue), TotalAmount = total, InstallmentCount = count
            };
            purchase.SetCreatedBy(actor);
            return purchase;
        }
        /// <summary>Merchant refund: reduces the expense; money only returns to a bank account if a real refund is received later.</summary>
        public void Refund(decimal amount, string actor)
        {
            if (amount <= 0 || Money.Round(amount) != amount) throw new ArgumentException("Valor deve ser positivo em centavos.", nameof(amount));
            if (RefundedAmount + amount > TotalAmount) throw new ConflictException("O estorno supera o valor da compra ainda não estornado.");
            RefundedAmount += amount; Version++;
            UpdateAuditInfo(actor);
        }
        public bool IsFullyRefunded() => RefundedAmount == TotalAmount;
    }

    /// <summary>Installment or merchant credit (negative) inside one invoice, recognized in the invoice's reference month.</summary>
    public class CardItem : TenantEntity
    {
        private CardItem() { }
        public int IdCardPurchase { get; private set; }
        public int IdCardInvoice { get; private set; }
        public string Kind { get; private set; } = string.Empty;
        public int? InstallmentNumber { get; private set; }
        public decimal Amount { get; private set; }
        public DateTime Competence { get; private set; }
        public string? Note { get; private set; }
    }
}
