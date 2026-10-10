using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record AccountRecord
    {
        public Guid Code { get; init; } public string Name { get; init; } = string.Empty; public bool IsActive { get; init; } public int Version { get; init; }
        public DateTime OpeningDate { get; init; } public decimal OpeningBalance { get; init; } public decimal Balance { get; init; } public bool HasMovements { get; init; }
    }
    public sealed record MovementRecord
    {
        public Guid Code { get; init; } public DateTime MovementDate { get; init; } public decimal Amount { get; init; }
        public string Kind { get; init; } = string.Empty; public string Description { get; init; } = string.Empty;
        public Guid? TransferCode { get; init; } public Guid? ReceiptCode { get; init; } public Guid? PaymentCode { get; init; } public Guid? ReversedMovementCode { get; init; }
    }
    public sealed record TransferRecord
    {
        public Guid Code { get; init; } public Guid SourceAccountCode { get; init; } public string SourceAccountName { get; init; } = string.Empty;
        public Guid TargetAccountCode { get; init; } public string TargetAccountName { get; init; } = string.Empty; public DateTime TransferDate { get; init; }
        public decimal Amount { get; init; } public string Description { get; init; } = string.Empty;
        public DateTime? DtReversed { get; init; } public string? ReversalReason { get; init; }
    }
    public interface IAccountRepository
    {
        Task<IReadOnlyList<AccountRecord>> ListAsync(int idTenant, DateTime asOf, CancellationToken ct);
        Task<FinancialAccount?> FindAsync(int idTenant, Guid code, CancellationToken ct);
        Task<FinancialAccount?> FindByIdAsync(int idTenant, int id, CancellationToken ct);
        Task<bool> HasMovementsAsync(int idTenant, int idAccount, CancellationToken ct);
        Task<decimal> SumBeforeAsync(int idTenant, int idAccount, DateTime date, CancellationToken ct);
        Task<IReadOnlyList<MovementRecord>> ListMovementsAsync(int idTenant, int idAccount, DateTime from, DateTime to, CancellationToken ct);
        /// <summary>Original (non-compensating) movements of a transfer, receipt or payment.</summary>
        Task<IReadOnlyList<AccountMovement>> FindOriginalMovementsAsync(int idTenant, int? idTransfer, int? idReceipt, int? idPayment, CancellationToken ct);
        Task<Transfer?> FindTransferAsync(int idTenant, Guid code, CancellationToken ct);
        Task<TransferRecord?> GetTransferAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<TransferRecord>> ListTransfersAsync(int idTenant, CancellationToken ct);
        void Add(FinancialAccount account);
        void Update(FinancialAccount account);
        void AddTransfer(Transfer transfer);
        void MarkTransferReversed(Transfer transfer);
        /// <summary>Parents created in the same transaction are resolved by public Code; no intermediate commit.</summary>
        void AddMovement(AccountMovement movement, Guid? transferCode = null, Guid? receiptCode = null, Guid? paymentCode = null);
    }
    public interface IIdempotencyRepository
    {
        Task<IdempotencyRecord?> FindAsync(int idTenant, string operation, Guid key, CancellationToken ct);
        void Add(IdempotencyRecord record);
    }
}
