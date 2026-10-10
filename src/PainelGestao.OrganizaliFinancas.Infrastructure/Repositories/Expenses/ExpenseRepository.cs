using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Expenses;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Expenses
{
    public class ExpenseRepository(IDapperUnitOfWork work) : IExpenseRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();

        public Task<IReadOnlyList<ObligationRecord>> ListAsync(int idTenant, DateTime from, DateTime to, bool includeOverdue, CancellationToken ct) =>
            List<ObligationRecord>(ExpenseSql.List, new { IdTenant = idTenant, From = from, To = to, IncludeOverdue = includeOverdue }, ct);
        public Task<ObligationRecord?> GetAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<ObligationRecord>(ExpenseSql.Get, new { IdTenant = idTenant, Code = code }, ct);
        public Task<Obligation?> FindAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<Obligation>(ExpenseSql.Find, new { IdTenant = idTenant, Code = code }, ct);
        public async Task<IReadOnlyList<Obligation>> FindManyAsync(int idTenant, IReadOnlyCollection<Guid> codes, CancellationToken ct) =>
            codes.Count == 0 ? [] : await List<Obligation>(ExpenseSql.FindMany, new { IdTenant = idTenant, Codes = codes.ToArray() }, ct);
        public async Task<IReadOnlyList<ObligationTotalsRecord>> TotalsAsync(int idTenant, IReadOnlyCollection<int> ids, CancellationToken ct) =>
            ids.Count == 0 ? [] : await List<ObligationTotalsRecord>(ExpenseSql.Totals, new { IdTenant = idTenant, Ids = ids.ToArray() }, ct);
        public async Task<IReadOnlyList<ShareRecord>> ListSharesAsync(int idTenant, IReadOnlyCollection<Guid> obligationCodes, CancellationToken ct) =>
            obligationCodes.Count == 0 ? [] : await List<ShareRecord>(ExpenseSql.Shares, new { IdTenant = idTenant, Codes = obligationCodes.ToArray() }, ct);
        public Task<IReadOnlyList<AdjustmentRecord>> ListAdjustmentsAsync(int idTenant, int idObligation, CancellationToken ct) =>
            List<AdjustmentRecord>(ExpenseSql.Adjustments, new { IdTenant = idTenant, IdObligation = idObligation }, ct);
        public void Add(Obligation obligation) => new DapperRepository<Obligation>(work).Add(obligation);
        public void Update(Obligation obligation) => work.AddPendingWrite(ExpenseSql.Update, obligation);
        public void ReplaceShares(int idTenant, Guid obligationCode, IReadOnlyList<ResolvedShare> shares, string actor)
        {
            work.AddPendingWrite(ExpenseSql.DeactivateShares, new { IdTenant = idTenant, ObligationCode = obligationCode, Actor = actor });
            foreach (var share in shares)
                work.AddPendingWrite(ExpenseSql.AddShare, new
                {
                    Code = Guid.CreateVersion7(), IdTenant = idTenant, ObligationCode = obligationCode, share.PersonCode, share.Amount, share.Percent, Actor = actor
                });
        }
        public void AddAdjustment(ObligationAdjustment adjustment) => new DapperRepository<ObligationAdjustment>(work).Add(adjustment);
        public void AddInstallmentPlan(InstallmentPlan plan) => new DapperRepository<InstallmentPlan>(work).Add(plan);
        public void LinkInstallment(int idTenant, Guid obligationCode, Guid planCode) =>
            work.AddPendingWrite(ExpenseSql.LinkInstallment, new { IdTenant = idTenant, ObligationCode = obligationCode, PlanCode = planCode });

        public Task<Payment?> FindPaymentAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<Payment>(ExpenseSql.FindPayment, new { IdTenant = idTenant, Code = code }, ct);
        public Task<PaymentRecord?> GetPaymentAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<PaymentRecord>(ExpenseSql.GetPayment, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IReadOnlyList<PaymentRecord>> ListPaymentsAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct) =>
            List<PaymentRecord>(ExpenseSql.Payments, new { IdTenant = idTenant, From = from, To = to }, ct);
        public async Task<IReadOnlyList<PaymentAllocationRecord>> ListAllocationsByPaymentAsync(int idTenant, IReadOnlyCollection<Guid> paymentCodes, CancellationToken ct) =>
            paymentCodes.Count == 0 ? [] : await List<PaymentAllocationRecord>(ExpenseSql.AllocationsByPayment, new { IdTenant = idTenant, Codes = paymentCodes.ToArray() }, ct);
        public Task<IReadOnlyList<PaymentAllocationRecord>> ListAllocationsByObligationAsync(int idTenant, int idObligation, CancellationToken ct) =>
            List<PaymentAllocationRecord>(ExpenseSql.AllocationsByObligation, new { IdTenant = idTenant, IdObligation = idObligation }, ct);
        public void AddPayment(Payment payment) => new DapperRepository<Payment>(work).Add(payment);
        public void AddAllocation(int idTenant, Guid paymentCode, Guid obligationCode, decimal amount, string actor) =>
            work.AddPendingWrite(ExpenseSql.AddAllocation, new
            {
                Code = Guid.CreateVersion7(), IdTenant = idTenant, PaymentCode = paymentCode, ObligationCode = obligationCode, Amount = amount, Actor = actor
            });
        public void MarkPaymentReversed(Payment payment) => work.AddPendingWrite(ExpenseSql.MarkPaymentReversed, payment);

        public Task<IReadOnlyList<RecurringExpenseRecord>> ListRecurrencesAsync(int idTenant, CancellationToken ct) =>
            List<RecurringExpenseRecord>(ExpenseSql.Recurrences, new { IdTenant = idTenant }, ct);
        public Task<RecurringExpense?> FindRecurrenceAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<RecurringExpense>(ExpenseSql.FindRecurrence, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IReadOnlyList<RecurringVersionRecord>> ListRecurrenceVersionsAsync(int idTenant, int idRule, CancellationToken ct) =>
            List<RecurringVersionRecord>(ExpenseSql.RecurrenceVersions, new { IdTenant = idTenant, IdRule = idRule }, ct);
        public void AddRecurrence(RecurringExpense rule) => new DapperRepository<RecurringExpense>(work).Add(rule);
        public void UpdateRecurrence(RecurringExpense rule) => work.AddPendingWrite(ExpenseSql.UpdateRecurrence, rule);
        public void AddRecurrenceVersion(RecurringExpense rule, ExpenseTerms terms, string actor) =>
            work.AddPendingWrite(ExpenseSql.AddRecurrenceVersion, new
            {
                VersionCode = Guid.CreateVersion7(), rule.IdTenant, rule.Version, RuleCode = rule.Code,
                ValidFrom = terms.ValidFrom.ToDateTime(TimeOnly.MinValue), EndDate = terms.EndDate?.ToDateTime(TimeOnly.MinValue),
                terms.Frequency, terms.Amount, terms.IsEstimated, Actor = actor
            });
        public Task<IReadOnlyList<OccurrenceStateRecord>> ListOccurrenceStatesAsync(int idTenant, int idRule, CancellationToken ct) =>
            List<OccurrenceStateRecord>(ExpenseSql.OccurrenceStates, new { IdTenant = idTenant, IdRule = idRule }, ct);
        public void InsertOccurrence(RecurringExpense rule, Guid code, PlannedOccurrence occurrence, bool isEstimated, string actor) =>
            work.AddPendingWrite(ExpenseSql.InsertOccurrence, new
            {
                Code = code, rule.IdTenant, RuleCode = rule.Code, RecurrenceVersion = occurrence.Version, OccurrenceDate = occurrence.Date.ToDateTime(TimeOnly.MinValue),
                Competence = new DateTime(occurrence.Date.Year, occurrence.Date.Month, 1), occurrence.Amount, IsEstimated = isEstimated, Actor = actor
            });
        public void UpdateOccurrence(RecurringExpense rule, Guid code, PlannedOccurrence occurrence, bool isEstimated, string actor) =>
            work.AddPendingWrite(ExpenseSql.UpdateOccurrence, new
            {
                rule.IdTenant, Code = code, RecurrenceVersion = occurrence.Version, occurrence.Amount, IsEstimated = isEstimated, Actor = actor
            });
        public void CancelOccurrence(int idTenant, Guid code, string actor) =>
            work.AddPendingWrite(ExpenseSql.CancelOccurrence, new { IdTenant = idTenant, Code = code, Actor = actor });

        public Task<IReadOnlyList<PersonTotalRecord>> ResponsibleTotalsAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<PersonTotalRecord>(ExpenseSql.ResponsibleTotals, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
        public Task<IReadOnlyList<PersonTotalRecord>> PaidBySplitPayerAsync(int idTenant, DateTime from, DateTime toExclusive, CancellationToken ct) =>
            List<PersonTotalRecord>(ExpenseSql.PaidBySplitPayer, new { IdTenant = idTenant, From = from, ToExclusive = toExclusive }, ct);
    }
}
