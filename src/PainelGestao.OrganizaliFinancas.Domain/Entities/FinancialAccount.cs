using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Balance is never stored: it is the opening balance plus effective movements.</summary>
    public class FinancialAccount : TenantEntity
    {
        private FinancialAccount() { }
        private FinancialAccount(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public DateTime OpeningDate { get; private set; }
        public decimal OpeningBalance { get; private set; }
        public int Version { get; private set; }
        public static FinancialAccount Create(int idTenant, string name, DateOnly openingDate, decimal openingBalance, string actor)
        {
            var account = new FinancialAccount(idTenant);
            account.Rename(name);
            account.SetOpening(openingDate, openingBalance);
            account.SetCreatedBy(actor);
            return account;
        }
        private void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160) throw new ArgumentException("Nome excede 160 caracteres.", nameof(name));
            Name = name.Trim();
        }
        private void SetOpening(DateOnly date, decimal balance)
        {
            OpeningDate = date.ToDateTime(TimeOnly.MinValue);
            OpeningBalance = decimal.Round(balance, 2);
        }
        public DateOnly OpeningDay() => DateOnly.FromDateTime(OpeningDate);
        /// <summary>The opening base can only change while no movement depends on it.</summary>
        public void Update(string name, bool active, DateOnly openingDate, decimal openingBalance, bool hasMovements, string actor)
        {
            if (hasMovements && (openingDate != OpeningDay() || decimal.Round(openingBalance, 2) != OpeningBalance))
                throw new ConflictException("A conta já tem movimentos; a data e o saldo inicial não podem mais ser alterados.");
            Rename(name);
            SetOpening(openingDate, openingBalance);
            if (active) Activate(actor); else SoftDelete(actor);
            Version++;
            UpdateAuditInfo(actor);
        }
        public void EnsureAccepts(DateOnly date)
        {
            if (!IsActive) throw new ConflictException($"A conta {Name} está inativa e não recebe novos movimentos.");
            if (date < OpeningDay()) throw new ConflictException($"A data é anterior à abertura da conta {Name} ({OpeningDay():dd/MM/yyyy}).");
        }
    }

    /// <summary>Immutable ledger line. Credits are positive and debits negative; corrections are compensating movements.</summary>
    public class AccountMovement : TenantEntity
    {
        public static readonly string[] Kinds = ["receipt", "transfer_in", "transfer_out", "reversal"];
        private AccountMovement() { }
        private AccountMovement(int idTenant) : base(idTenant) { }
        public int IdFinancialAccount { get; private set; }
        public DateTime MovementDate { get; private set; }
        public decimal Amount { get; private set; }
        public string Kind { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public int? IdTransfer { get; private set; }
        public int? IdReceipt { get; private set; }
        public int? IdReversedMovement { get; private set; }
        public static AccountMovement Create(FinancialAccount account, DateOnly date, decimal amount, string kind, string description, string actor)
        {
            if (amount == 0 || decimal.Round(amount, 2) != amount) throw new ArgumentException("Movimento precisa de valor em centavos diferente de zero.", nameof(amount));
            if (!Kinds.Contains(kind)) throw new ArgumentException("Tipo de movimento inválido.", nameof(kind));
            account.EnsureAccepts(date);
            var movement = new AccountMovement(account.IdTenant)
            {
                IdFinancialAccount = account.Id, MovementDate = date.ToDateTime(TimeOnly.MinValue), Amount = amount, Kind = kind,
                Description = description.Length > 200 ? description[..200] : description
            };
            movement.SetCreatedBy(actor);
            return movement;
        }
        /// <summary>Compensation keeps the original intact and links to it. Inactive accounts still accept reversals of their own history.</summary>
        public AccountMovement Compensate(DateOnly date, string description, string actor)
        {
            if (date < DateOnly.FromDateTime(MovementDate)) throw new ConflictException("O estorno não pode ser anterior ao movimento original.");
            var reversal = new AccountMovement(IdTenant)
            {
                IdFinancialAccount = IdFinancialAccount, MovementDate = date.ToDateTime(TimeOnly.MinValue), Amount = -Amount,
                Kind = "reversal", Description = description.Length > 200 ? description[..200] : description,
                IdTransfer = IdTransfer, IdReceipt = IdReceipt, IdReversedMovement = Id
            };
            reversal.SetCreatedBy(actor);
            return reversal;
        }
    }

    /// <summary>Two linked movements in one transaction; neither income nor expense, so the household total is unchanged.</summary>
    public class Transfer : TenantEntity
    {
        private Transfer() { }
        private Transfer(int idTenant) : base(idTenant) { }
        public int IdSourceAccount { get; private set; }
        public int IdTargetAccount { get; private set; }
        public DateTime TransferDate { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public DateTime? DtReversed { get; private set; }
        public string? ReversedBy { get; private set; }
        public string? ReversalReason { get; private set; }
        public bool IsReversed() => DtReversed is not null;
        public static Transfer Create(FinancialAccount source, FinancialAccount target, DateOnly date, decimal amount, string? description, string actor)
        {
            if (source.Id == target.Id) throw new ConflictException("Escolha contas diferentes para transferir.");
            if (source.IdTenant != target.IdTenant) throw new ArgumentException("Contas de casas diferentes.");
            if (amount <= 0) throw new ArgumentException("Valor deve ser positivo.", nameof(amount));
            source.EnsureAccepts(date); target.EnsureAccepts(date);
            var transfer = new Transfer(source.IdTenant)
            {
                IdSourceAccount = source.Id, IdTargetAccount = target.Id, TransferDate = date.ToDateTime(TimeOnly.MinValue),
                Amount = amount, Description = description?.Trim() ?? string.Empty
            };
            transfer.SetCreatedBy(actor);
            return transfer;
        }
        public void Reverse(string reason, string actor)
        {
            if (IsReversed()) throw new ConflictException("Esta transferência já foi estornada.");
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            (DtReversed, ReversedBy, ReversalReason) = (DateTime.UtcNow, actor, reason.Trim());
        }
    }
}
