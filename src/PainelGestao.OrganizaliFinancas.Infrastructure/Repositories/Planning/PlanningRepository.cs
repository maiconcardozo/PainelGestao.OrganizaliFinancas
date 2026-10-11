using Dapper;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Planning;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Planning
{
    public class PlanningRepository(IDapperUnitOfWork work) : IPlanningRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();

        public Task<IReadOnlyList<CategoryRecord>> CategoriesAsync(int idTenant, CancellationToken ct) =>
            List<CategoryRecord>(PlanningSql.Categories, new { IdTenant = idTenant }, ct);
        public Task<IReadOnlyList<SpendRecord>> SpendAsync(int idTenant, DateTime month, CancellationToken ct) =>
            List<SpendRecord>(PlanningSql.Spend, new { IdTenant = idTenant, Month = month }, ct);
        public Task<IReadOnlyList<BudgetRecord>> BudgetsAsync(int idTenant, DateTime month, CancellationToken ct) =>
            List<BudgetRecord>(PlanningSql.Budgets, new { IdTenant = idTenant, Month = month }, ct);
        public void SaveBudget(int idTenant, int idCategory, DateTime month, decimal? amount, string actor) =>
            work.AddPendingWrite(PlanningSql.SaveBudget, new
            {
                Code = Guid.CreateVersion7(), IdTenant = idTenant, IdCategory = idCategory, Month = month, Amount = amount ?? 0, IsActive = amount is not null, Actor = actor
            });

        public Task<AlertPreferenceRecord?> PreferenceAsync(int idTenant, CancellationToken ct) =>
            Single<AlertPreferenceRecord>(PlanningSql.Preference, new { IdTenant = idTenant }, ct);
        public void SavePreference(int idTenant, int warning, int limit, int dueSoonDays, int version, string actor) =>
            work.AddPendingWrite(PlanningSql.SavePreference, new
            {
                Code = Guid.CreateVersion7(), IdTenant = idTenant, Warning = warning, Limit = limit, DueSoonDays = dueSoonDays, Actor = actor
            });
        public Task<IReadOnlyList<AlertStateRecord>> AlertStatesAsync(int idTenant, CancellationToken ct) =>
            List<AlertStateRecord>(PlanningSql.AlertStates, new { IdTenant = idTenant }, ct);
        public void SaveAlertState(int idTenant, string key, string fingerprint, string status, DateTime? snoozedUntil, string actor) =>
            work.AddPendingWrite(PlanningSql.SaveAlertState, new
            {
                Code = Guid.CreateVersion7(), IdTenant = idTenant, Key = key, Fingerprint = fingerprint, Status = status, SnoozedUntil = snoozedUntil, Actor = actor
            });

        public Task<decimal> BalanceBeforeAsync(int idTenant, DateTime date, CancellationToken ct) =>
            Single<decimal>(PlanningSql.BalanceBefore, new { IdTenant = idTenant, Date = date }, ct);
        public Task<IReadOnlyList<CashTotalRecord>> MonthlyCashAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<CashTotalRecord>(PlanningSql.MonthlyCash, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
        public Task<IReadOnlyList<CashTotalRecord>> DailyCashAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<CashTotalRecord>(PlanningSql.DailyCash, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
        public Task<IReadOnlyList<AnalysisLineRecord>> AnalysisLinesAsync(int idTenant, DateTime month, CancellationToken ct) =>
            List<AnalysisLineRecord>(PlanningSql.AnalysisLines, new { IdTenant = idTenant, Month = month }, ct);
    }
}
