using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    // Read models use init properties: Dapper converts computed SQL types (BIGINT flags, SUM) by name instead of by constructor signature.
    public sealed record IncomeSourceRecord
    {
        public Guid Code { get; init; } public string Name { get; init; } = string.Empty; public bool IsActive { get; init; } public int Version { get; init; }
        public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty; public Guid? IncomeTypeCode { get; init; }
        public DateTime ValidFrom { get; init; } public DateTime? EndDate { get; init; } public string Frequency { get; init; } = string.Empty;
        public string AmountMode { get; init; } = string.Empty; public decimal? ExpectedNetAmount { get; init; } public decimal? GrossAmount { get; init; }
    }
    public sealed record IncomeSourceVersionRecord
    {
        public int Version { get; init; } public DateTime ValidFrom { get; init; } public DateTime? EndDate { get; init; }
        public string Frequency { get; init; } = string.Empty; public string AmountMode { get; init; } = string.Empty;
        public decimal? ExpectedNetAmount { get; init; } public decimal? GrossAmount { get; init; }
        public DateTime DtCreated { get; init; } public string CreatedBy { get; init; } = string.Empty;
        public IncomeTermsVersion ToTerms() => new(Version, new IncomeTerms(DateOnly.FromDateTime(ValidFrom),
            EndDate is DateTime end ? DateOnly.FromDateTime(end) : null, Frequency, AmountMode, ExpectedNetAmount, GrossAmount));
    }
    public sealed record ForecastStateRecord
    {
        public Guid Code { get; init; } public DateTime OccurrenceDate { get; init; } public bool IsActive { get; init; } public bool Locked { get; init; }
        public int SourceVersion { get; init; } public decimal? ExpectedAmount { get; init; }
        public ForecastState ToState() => new(Code, DateOnly.FromDateTime(OccurrenceDate), IsActive, Locked, SourceVersion, ExpectedAmount);
    }
    public sealed record ForecastRecord
    {
        public Guid Code { get; init; } public Guid SourceCode { get; init; } public string SourceName { get; init; } = string.Empty;
        public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty; public string AmountMode { get; init; } = string.Empty;
        public DateTime OccurrenceDate { get; init; } public DateTime Competence { get; init; } public decimal? ExpectedAmount { get; init; }
        public decimal ReceivedAmount { get; init; } public string Status { get; init; } = string.Empty; public string? CloseReason { get; init; }
        public int SourceVersion { get; init; } public int Version { get; init; }
    }
    public sealed record ReceiptRecord
    {
        public Guid Code { get; init; } public Guid AccountCode { get; init; } public string AccountName { get; init; } = string.Empty;
        public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty; public Guid? SourceCode { get; init; }
        public string? SourceName { get; init; } public DateTime ReceiptDate { get; init; } public decimal Amount { get; init; }
        public decimal AllocatedAmount { get; init; } public string Description { get; init; } = string.Empty;
        public DateTime? DtReversed { get; init; } public string? ReversalReason { get; init; }
    }
    /// <summary>Monthly totals; SourceCode is null for income registered without a source.</summary>
    public sealed record IncomeTotalRecord
    {
        public DateTime Month { get; init; } public Guid PersonCode { get; init; } public string PersonName { get; init; } = string.Empty;
        public Guid? SourceCode { get; init; } public string? SourceName { get; init; } public decimal Total { get; init; }
    }

    public interface IIncomeRepository
    {
        Task<IReadOnlyList<IncomeSourceRecord>> ListSourcesAsync(int idTenant, CancellationToken ct);
        Task<IncomeSource?> FindSourceAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IncomeSource?> FindSourceByIdAsync(int idTenant, int id, CancellationToken ct);
        Task<IReadOnlyList<IncomeSourceVersionRecord>> ListSourceVersionsAsync(int idTenant, int idSource, CancellationToken ct);
        void AddSource(IncomeSource source);
        void UpdateSource(IncomeSource source);
        void AddSourceVersion(IncomeSource source, IncomeTerms terms, string actor);

        Task<IReadOnlyList<ForecastStateRecord>> ListForecastStatesAsync(int idTenant, int idSource, CancellationToken ct);
        void InsertForecast(IncomeSource source, PlannedOccurrence occurrence, string actor);
        void UpdateForecast(IncomeSource source, Guid code, PlannedOccurrence occurrence, string actor);
        void CancelForecast(int idTenant, Guid code, string actor);
        Task<IReadOnlyList<ForecastRecord>> ListForecastsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct);
        Task<IncomeForecast?> FindForecastAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<IncomeForecast>> FindForecastsAsync(int idTenant, IReadOnlyCollection<Guid> codes, CancellationToken ct);
        void CloseForecast(IncomeForecast forecast);

        Task<Receipt?> FindReceiptAsync(int idTenant, Guid code, CancellationToken ct);
        Task<ReceiptRecord?> GetReceiptAsync(int idTenant, Guid code, CancellationToken ct);
        Task<IReadOnlyList<ReceiptRecord>> ListReceiptsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct);
        void AddReceipt(Receipt receipt);
        void AddAllocation(Receipt receipt, IncomeForecast forecast, decimal amount, string actor);
        void MarkReceiptReversed(Receipt receipt);

        /// <summary>Effective (non-reversed) receipts grouped by receipt month, person and source.</summary>
        Task<IReadOnlyList<IncomeTotalRecord>> ReceivedTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
        /// <summary>Active fixed-amount forecasts grouped by competence, person and source.</summary>
        Task<IReadOnlyList<IncomeTotalRecord>> ExpectedTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
    }
}
