using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Complements;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Complements
{
    internal static class Reads
    {
        public static async Task<IReadOnlyList<T>> List<T>(IDapperUnitOfWork work, string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();
        public static Task<T?> Single<T>(IDapperUnitOfWork work, string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
    }

    public class GoalRepository(IDapperUnitOfWork work) : IGoalRepository
    {
        public Task<IReadOnlyList<GoalRecord>> ListAsync(int idTenant, DateTime asOf, CancellationToken ct) =>
            Reads.List<GoalRecord>(work, GoalSql.List, new { IdTenant = idTenant, AsOf = asOf }, ct);
        public Task<SavingsGoal?> FindAsync(int idTenant, Guid code, CancellationToken ct) =>
            Reads.Single<SavingsGoal>(work, GoalSql.Find, new { IdTenant = idTenant, Code = code }, ct);
        public void Add(SavingsGoal goal) => new DapperRepository<SavingsGoal>(work).Add(goal);
        public void Update(SavingsGoal goal) => work.AddPendingWrite(GoalSql.Update, goal);
    }

    public class AttachmentRepository(IDapperUnitOfWork work) : IAttachmentRepository
    {
        public Task<IReadOnlyList<AttachmentRecord>> ListAsync(int idTenant, string kind, Guid target, CancellationToken ct) =>
            Reads.List<AttachmentRecord>(work, AttachmentSql.List, new { IdTenant = idTenant, Kind = kind, Target = target }, ct);
        public Task<AttachmentRecord?> FindAsync(int idTenant, Guid code, CancellationToken ct) =>
            Reads.Single<AttachmentRecord>(work, AttachmentSql.Find, new { IdTenant = idTenant, Code = code }, ct);
        public void Add(Attachment attachment) => new DapperRepository<Attachment>(work).Add(attachment);
        public void Remove(int idTenant, Guid code, string actor) => work.AddPendingWrite(AttachmentSql.Remove, new { IdTenant = idTenant, Code = code, Actor = actor });
    }

    public class ImportRepository(IDapperUnitOfWork work) : IImportRepository
    {
        public Task<IReadOnlyList<ImportBatchRecord>> ListAsync(int idTenant, CancellationToken ct) =>
            Reads.List<ImportBatchRecord>(work, ImportSql.List, new { IdTenant = idTenant }, ct);
        public Task<IReadOnlyList<string>> AppliedFingerprintsAsync(int idTenant, IReadOnlyCollection<string> fingerprints, CancellationToken ct) =>
            fingerprints.Count == 0 ? Task.FromResult<IReadOnlyList<string>>([])
                : Reads.List<string>(work, ImportSql.AppliedFingerprints, new { IdTenant = idTenant, Fingerprints = fingerprints.ToArray() }, ct);
        public void AddBatch(ImportBatch batch) => new DapperRepository<ImportBatch>(work).Add(batch);
        public void AddLine(int idTenant, Guid batchCode, ImportedLine line, string actor) => work.AddPendingWrite(ImportSql.AddLine, new
        {
            line.Code, IdTenant = idTenant, BatchCode = batchCode, line.Fingerprint, LineDate = line.Date.ToDateTime(TimeOnly.MinValue), line.Amount,
            line.Description, line.ExternalId, line.Action, line.ResultCode, Actor = actor
        });
    }
}
