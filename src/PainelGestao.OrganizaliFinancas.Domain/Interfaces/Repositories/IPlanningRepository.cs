namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record CategoryRecord
    {
        public int Id { get; init; } public Guid Code { get; init; } public string Name { get; init; } = string.Empty; public Guid? ParentCode { get; init; } public bool IsActive { get; init; }
    }
    public sealed record SpendRecord
    {
        public Guid? CategoryCode { get; init; } public decimal Bills { get; init; } public decimal Card { get; init; } public decimal Paid { get; init; }
    }
    public sealed record BudgetRecord
    {
        public Guid CategoryCode { get; init; } public decimal Amount { get; init; } public int Version { get; init; }
    }
    public sealed record AlertPreferenceRecord
    {
        public int WarningPercent { get; init; } = 80; public int LimitPercent { get; init; } = 100; public int DueSoonDays { get; init; } = 5; public int Version { get; init; }
    }
    public sealed record AlertStateRecord
    {
        public string AlertKey { get; init; } = string.Empty; public string Fingerprint { get; init; } = string.Empty; public string Status { get; init; } = string.Empty;
        public DateTime? SnoozedUntil { get; init; }
    }
    /// <summary>Realized household cash by period; transfers between the household's own accounts are excluded.</summary>
    public sealed record CashTotalRecord
    {
        public DateTime Period { get; init; } public decimal Inflow { get; init; } public decimal Outflow { get; init; }
    }
    /// <summary>One expense of a competence for analysis: a bill (obligation) or a card item, with the data needed by every filter.</summary>
    public sealed record AnalysisLineRecord
    {
        public string Source { get; init; } = string.Empty; public Guid Code { get; init; } public string Description { get; init; } = string.Empty;
        public Guid? CategoryCode { get; init; } public string? CategoryName { get; init; } public DateTime DueDate { get; init; } public decimal Amount { get; init; }
        public decimal Paid { get; init; } public string Status { get; init; } = string.Empty; public string? CardName { get; init; } public Guid? BuyerCode { get; init; }
        public string? AccountCodes { get; init; }
    }

    public interface IPlanningRepository
    {
        Task<IReadOnlyList<CategoryRecord>> CategoriesAsync(int idTenant, CancellationToken ct);
        /// <summary>Non-cancelled bills (invoice obligations excluded, their items are counted instead) and card items of the competence.</summary>
        Task<IReadOnlyList<SpendRecord>> SpendAsync(int idTenant, DateTime month, CancellationToken ct);
        Task<IReadOnlyList<BudgetRecord>> BudgetsAsync(int idTenant, DateTime month, CancellationToken ct);
        void SaveBudget(int idTenant, int idCategory, DateTime month, decimal? amount, string actor);

        Task<AlertPreferenceRecord?> PreferenceAsync(int idTenant, CancellationToken ct);
        void SavePreference(int idTenant, int warning, int limit, int dueSoonDays, int version, string actor);
        Task<IReadOnlyList<AlertStateRecord>> AlertStatesAsync(int idTenant, CancellationToken ct);
        void SaveAlertState(int idTenant, string key, string fingerprint, string status, DateTime? snoozedUntil, string actor);

        /// <summary>Opening balances of accounts opened before the date plus movements before it.</summary>
        Task<decimal> BalanceBeforeAsync(int idTenant, DateTime date, CancellationToken ct);
        Task<IReadOnlyList<CashTotalRecord>> MonthlyCashAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
        Task<IReadOnlyList<CashTotalRecord>> DailyCashAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct);
        Task<IReadOnlyList<AnalysisLineRecord>> AnalysisLinesAsync(int idTenant, DateTime month, CancellationToken ct);
    }
}
