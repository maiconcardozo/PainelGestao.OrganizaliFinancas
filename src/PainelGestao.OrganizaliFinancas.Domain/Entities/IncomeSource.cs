using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Who receives and what; amounts and recurrence live in immutable <see cref="IncomeSourceVersion"/> rows.</summary>
    public class IncomeSource : TenantEntity
    {
        private IncomeSource() { }
        private IncomeSource(int idTenant) : base(idTenant) { }
        public int IdPerson { get; private set; }
        public int? IdIncomeType { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public int Version { get; private set; } = 1;
        public static IncomeSource Create(Person person, int? idIncomeType, string name, string actor)
        {
            if (!person.IsActive) throw new ConflictException("Escolha um morador ativo.");
            var source = new IncomeSource(person.IdTenant) { IdPerson = person.Id, IdIncomeType = idIncomeType };
            source.Rename(name); source.SetCreatedBy(actor);
            return source;
        }
        private void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160) throw new ArgumentException("Nome excede 160 caracteres.", nameof(name));
            Name = name.Trim();
        }
        public void Update(string name, int? idIncomeType, bool active, string actor)
        {
            Rename(name); IdIncomeType = idIncomeType; Version++;
            if (active) Activate(actor); else SoftDelete(actor);
            UpdateAuditInfo(actor);
        }
    }
    public class IncomeSourceVersion : TenantEntity
    {
        private IncomeSourceVersion() { }
        public int IdIncomeSource { get; private set; }
        public int Version { get; private set; }
        public DateTime ValidFrom { get; private set; }
        public DateTime? EndDate { get; private set; }
        public string Frequency { get; private set; } = string.Empty;
        public string AmountMode { get; private set; } = string.Empty;
        public decimal? ExpectedNetAmount { get; private set; }
        public decimal? GrossAmount { get; private set; }
    }

    /// <summary>Expectation for a date and competence; it is not money. Keeps the source version it was generated from.</summary>
    public class IncomeForecast : TenantEntity
    {
        private IncomeForecast() { }
        public int IdIncomeSource { get; private set; }
        public int IdIncomeSourceVersion { get; private set; }
        public int SourceVersion { get; private set; }
        public DateTime OccurrenceDate { get; private set; }
        public DateTime Competence { get; private set; }
        public decimal? ExpectedAmount { get; private set; }
        public string Status { get; private set; } = "open";
        public string? CloseReason { get; private set; }
        public int Version { get; private set; }
        public bool IsClosed() => Status == "closed";
        public void EnsureAcceptsAllocation()
        {
            if (!IsActive) throw new ConflictException("A previsão foi cancelada por mudança na fonte de renda.");
            if (IsClosed()) throw new ConflictException("A previsão já foi encerrada.");
        }
        /// <summary>Closing as realized below the expectation removes the residual from projections and records why.</summary>
        public void Close(string reason, string actor)
        {
            EnsureAcceptsAllocation();
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            Status = "closed"; CloseReason = reason.Trim(); Version++;
            UpdateAuditInfo(actor);
        }
    }

    public class Receipt : TenantEntity
    {
        private Receipt() { }
        private Receipt(int idTenant) : base(idTenant) { }
        public int IdFinancialAccount { get; private set; }
        public int IdPerson { get; private set; }
        public int? IdIncomeSource { get; private set; }
        public int? IdIncomeType { get; private set; }
        public DateTime ReceiptDate { get; private set; }
        public decimal Amount { get; private set; }
        public string Description { get; private set; } = string.Empty;
        public DateTime? DtReversed { get; private set; }
        public string? ReversedBy { get; private set; }
        public string? ReversalReason { get; private set; }
        public bool IsReversed() => DtReversed is not null;
        public static Receipt Create(FinancialAccount account, Person person, IncomeSource? source, int? idIncomeType, DateOnly date,
            decimal amount, IReadOnlyList<decimal> allocations, string? description, string actor)
        {
            if (amount <= 0) throw new ArgumentException("Valor deve ser positivo.", nameof(amount));
            if (source is not null && source.IdPerson != person.Id) throw new ConflictException("A fonte de renda pertence a outro morador.");
            if (allocations.Any(value => value <= 0)) throw new ConflictException("Cada valor alocado deve ser maior que zero.");
            if (allocations.Sum() > amount) throw new ConflictException("A soma alocada não pode superar o valor recebido.");
            account.EnsureAccepts(date);
            var receipt = new Receipt(account.IdTenant)
            {
                IdFinancialAccount = account.Id, IdPerson = person.Id, IdIncomeSource = source?.Id,
                IdIncomeType = idIncomeType ?? source?.IdIncomeType, ReceiptDate = date.ToDateTime(TimeOnly.MinValue),
                Amount = amount, Description = description?.Trim() ?? string.Empty
            };
            receipt.SetCreatedBy(actor);
            return receipt;
        }
        public void Reverse(string reason, string actor)
        {
            if (IsReversed()) throw new ConflictException("Este recebimento já foi estornado.");
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
            (DtReversed, ReversedBy, ReversalReason) = (DateTime.UtcNow, actor, reason.Trim());
        }
    }
    public class ReceiptAllocation : TenantEntity
    {
        private ReceiptAllocation() { }
        public int IdReceipt { get; private set; }
        public int IdIncomeForecast { get; private set; }
        public decimal Amount { get; private set; }
    }

    /// <summary>Per household, operation and client key. The same key with a different payload is a conflict.</summary>
    public class IdempotencyRecord : TenantEntity
    {
        private IdempotencyRecord() { }
        private IdempotencyRecord(int idTenant) : base(idTenant) { }
        public string Operation { get; private set; } = string.Empty;
        public Guid RequestKey { get; private set; }
        public string RequestHash { get; private set; } = string.Empty;
        public Guid ResultCode { get; private set; }
        public static IdempotencyRecord Create(int idTenant, string operation, Guid key, string hash, Guid resultCode, string actor)
        {
            var record = new IdempotencyRecord(idTenant) { Operation = operation, RequestKey = key, RequestHash = hash, ResultCode = resultCode };
            record.SetCreatedBy(actor);
            return record;
        }
    }
}
