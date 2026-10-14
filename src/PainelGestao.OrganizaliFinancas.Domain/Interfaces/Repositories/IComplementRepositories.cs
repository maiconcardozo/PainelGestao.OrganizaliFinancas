using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record GoalRecord
    {
        public Guid Code { get; init; } public string Name { get; init; } = string.Empty; public decimal TargetAmount { get; init; } public DateTime? TargetDate { get; init; }
        public string? Note { get; init; } public bool IsActive { get; init; } public int Version { get; init; }
        public Guid AccountCode { get; init; } public string AccountName { get; init; } = string.Empty; public bool AccountActive { get; init; } public decimal Balance { get; init; }
    }
    public interface IGoalRepository
    {
        /// <summary>Goals with the balance of the linked account up to the given day.</summary>
        Task<IReadOnlyList<GoalRecord>> ListAsync(int idTenant, DateTime asOf, CancellationToken ct);
        Task<SavingsGoal?> FindAsync(int idTenant, Guid code, CancellationToken ct);
        void Add(SavingsGoal goal);
        void Update(SavingsGoal goal);
    }

    public sealed record AttachmentRecord
    {
        public Guid Code { get; init; } public string TargetKind { get; init; } = string.Empty; public Guid TargetCode { get; init; } public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty; public long Size { get; init; } public string StorageKey { get; init; } = string.Empty; public DateTime DtCreated { get; init; }
    }
    public interface IAttachmentRepository
    {
        Task<IReadOnlyList<AttachmentRecord>> ListAsync(int idTenant, string kind, Guid target, CancellationToken ct);
        Task<AttachmentRecord?> FindAsync(int idTenant, Guid code, CancellationToken ct);
        void Add(Attachment attachment);
        void Remove(int idTenant, Guid code, string actor);
    }

    public sealed record ImportBatchRecord
    {
        public Guid Code { get; init; } public string FileName { get; init; } = string.Empty; public string Format { get; init; } = string.Empty; public string AccountName { get; init; } = string.Empty;
        public int LineCount { get; init; } public int AppliedCount { get; init; } public int IgnoredCount { get; init; } public DateTime DtCreated { get; init; }
    }
    public interface IImportRepository
    {
        Task<IReadOnlyList<ImportBatchRecord>> ListAsync(int idTenant, CancellationToken ct);
        /// <summary>Fingerprints among the given ones that were already applied (not ignored) in this household.</summary>
        Task<IReadOnlyList<string>> AppliedFingerprintsAsync(int idTenant, IReadOnlyCollection<string> fingerprints, CancellationToken ct);
        void AddBatch(ImportBatch batch);
        /// <summary>The batch is created in the same transaction and resolved by its public code.</summary>
        void AddLine(int idTenant, Guid batchCode, ImportedLine line, string actor);
    }
}
