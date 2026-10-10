using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Income;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Income
{
    public class IncomeRepository(IDapperUnitOfWork work) : IIncomeRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();

        public Task<IReadOnlyList<IncomeSourceRecord>> ListSourcesAsync(int idTenant, CancellationToken ct) =>
            List<IncomeSourceRecord>(IncomeSql.Sources, new { IdTenant = idTenant }, ct);
        public Task<IncomeSource?> FindSourceAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<IncomeSource>(IncomeSql.FindSource, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IncomeSource?> FindSourceByIdAsync(int idTenant, int id, CancellationToken ct) =>
            Single<IncomeSource>(IncomeSql.FindSourceById, new { IdTenant = idTenant, Id = id }, ct);
        public Task<IReadOnlyList<IncomeSourceVersionRecord>> ListSourceVersionsAsync(int idTenant, int idSource, CancellationToken ct) =>
            List<IncomeSourceVersionRecord>(IncomeSql.Versions, new { IdTenant = idTenant, IdSource = idSource }, ct);
        public void AddSource(IncomeSource source) => new DapperRepository<IncomeSource>(work).Add(source);
        public void UpdateSource(IncomeSource source) => work.AddPendingWrite(IncomeSql.UpdateSource, source);
        public void AddSourceVersion(IncomeSource source, IncomeTerms terms, string actor) =>
            work.AddPendingWrite(IncomeSql.AddVersion, new
            {
                VersionCode = Guid.CreateVersion7(), source.IdTenant, source.Version, SourceCode = source.Code,
                ValidFrom = terms.ValidFrom.ToDateTime(TimeOnly.MinValue), EndDate = terms.EndDate?.ToDateTime(TimeOnly.MinValue),
                terms.Frequency, terms.AmountMode, terms.ExpectedNetAmount, terms.GrossAmount, Actor = actor
            });

        public Task<IReadOnlyList<ForecastStateRecord>> ListForecastStatesAsync(int idTenant, int idSource, CancellationToken ct) =>
            List<ForecastStateRecord>(IncomeSql.ForecastStates, new { IdTenant = idTenant, IdSource = idSource }, ct);
        public void InsertForecast(IncomeSource source, PlannedOccurrence occurrence, string actor) =>
            work.AddPendingWrite(IncomeSql.InsertForecast, new
            {
                Code = Guid.CreateVersion7(), source.IdTenant, SourceCode = source.Code, SourceVersion = occurrence.Version,
                OccurrenceDate = occurrence.Date.ToDateTime(TimeOnly.MinValue),
                Competence = new DateTime(occurrence.Date.Year, occurrence.Date.Month, 1), ExpectedAmount = occurrence.Amount, Actor = actor
            });
        public void UpdateForecast(IncomeSource source, Guid code, PlannedOccurrence occurrence, string actor) =>
            work.AddPendingWrite(IncomeSql.UpdateForecast, new
            {
                source.IdTenant, Code = code, SourceVersion = occurrence.Version, ExpectedAmount = occurrence.Amount, Actor = actor
            });
        public void CancelForecast(int idTenant, Guid code, string actor) =>
            work.AddPendingWrite(IncomeSql.CancelForecast, new { IdTenant = idTenant, Code = code, Actor = actor });
        public Task<IReadOnlyList<ForecastRecord>> ListForecastsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct) =>
            List<ForecastRecord>(IncomeSql.Forecasts, new { IdTenant = idTenant, From = from, To = to }, ct);
        public Task<IncomeForecast?> FindForecastAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<IncomeForecast>(IncomeSql.FindForecast, new { IdTenant = idTenant, Code = code }, ct);
        public async Task<IReadOnlyList<IncomeForecast>> FindForecastsAsync(int idTenant, IReadOnlyCollection<Guid> codes, CancellationToken ct) =>
            codes.Count == 0 ? [] : await List<IncomeForecast>(IncomeSql.FindForecasts, new { IdTenant = idTenant, Codes = codes.ToArray() }, ct);
        public void CloseForecast(IncomeForecast forecast) => work.AddPendingWrite(IncomeSql.CloseForecast, forecast);

        public Task<Receipt?> FindReceiptAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<Receipt>(IncomeSql.FindReceipt, new { IdTenant = idTenant, Code = code }, ct);
        public Task<ReceiptRecord?> GetReceiptAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<ReceiptRecord>(IncomeSql.GetReceipt, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IReadOnlyList<ReceiptRecord>> ListReceiptsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct) =>
            List<ReceiptRecord>(IncomeSql.Receipts, new { IdTenant = idTenant, From = from, To = to }, ct);
        public void AddReceipt(Receipt receipt) => new DapperRepository<Receipt>(work).Add(receipt);
        public void AddAllocation(Receipt receipt, IncomeForecast forecast, decimal amount, string actor) =>
            work.AddPendingWrite(IncomeSql.AddAllocation, new
            {
                Code = Guid.CreateVersion7(), receipt.IdTenant, ReceiptCode = receipt.Code, IdForecast = forecast.Id, Amount = amount, Actor = actor
            });
        public void MarkReceiptReversed(Receipt receipt) => work.AddPendingWrite(IncomeSql.MarkReceiptReversed, receipt);

        public Task<IReadOnlyList<IncomeTotalRecord>> ReceivedTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<IncomeTotalRecord>(IncomeSql.ReceivedTotals, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
        public Task<IReadOnlyList<IncomeTotalRecord>> ExpectedTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<IncomeTotalRecord>(IncomeSql.ExpectedTotals, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
    }
}
