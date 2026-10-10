using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Accounts;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Accounts
{
    public class AccountRepository(IDapperUnitOfWork work) : IAccountRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();

        public Task<IReadOnlyList<AccountRecord>> ListAsync(int idTenant, DateTime asOf, CancellationToken ct) =>
            List<AccountRecord>(AccountSql.List, new { IdTenant = idTenant, AsOf = asOf }, ct);
        public Task<FinancialAccount?> FindAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<FinancialAccount>(AccountSql.Find, new { IdTenant = idTenant, Code = code }, ct);
        public Task<FinancialAccount?> FindByIdAsync(int idTenant, int id, CancellationToken ct) =>
            Single<FinancialAccount>(AccountSql.FindById, new { IdTenant = idTenant, Id = id }, ct);
        public async Task<bool> HasMovementsAsync(int idTenant, int idAccount, CancellationToken ct) =>
            await Single<long>(AccountSql.CountMovements, new { IdTenant = idTenant, IdAccount = idAccount }, ct) > 0;
        public Task<decimal> SumBeforeAsync(int idTenant, int idAccount, DateTime date, CancellationToken ct) =>
            Single<decimal>(AccountSql.SumBefore, new { IdTenant = idTenant, IdAccount = idAccount, Date = date }, ct);
        public Task<IReadOnlyList<MovementRecord>> ListMovementsAsync(int idTenant, int idAccount, DateTime from, DateTime to, CancellationToken ct) =>
            List<MovementRecord>(AccountSql.Movements, new { IdTenant = idTenant, IdAccount = idAccount, From = from, To = to }, ct);
        public Task<IReadOnlyList<AccountMovement>> FindOriginalMovementsAsync(int idTenant, int? idTransfer, int? idReceipt, CancellationToken ct) =>
            List<AccountMovement>(AccountSql.OriginalMovements, new { IdTenant = idTenant, IdTransfer = idTransfer, IdReceipt = idReceipt }, ct);
        public Task<Transfer?> FindTransferAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<Transfer>(AccountSql.FindTransfer, new { IdTenant = idTenant, Code = code }, ct);
        public Task<TransferRecord?> GetTransferAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<TransferRecord>(AccountSql.GetTransfer, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IReadOnlyList<TransferRecord>> ListTransfersAsync(int idTenant, CancellationToken ct) =>
            List<TransferRecord>(AccountSql.ListTransfers, new { IdTenant = idTenant }, ct);

        public void Add(FinancialAccount account) => new DapperRepository<FinancialAccount>(work).Add(account);
        public void Update(FinancialAccount account) => work.AddPendingWrite(AccountSql.Update, account);
        public void AddTransfer(Transfer transfer) => new DapperRepository<Transfer>(work).Add(transfer);
        public void MarkTransferReversed(Transfer transfer) => work.AddPendingWrite(AccountSql.MarkTransferReversed, transfer);
        public void AddMovement(AccountMovement movement, Guid? transferCode = null, Guid? receiptCode = null) =>
            work.AddPendingWrite(AccountSql.AddMovement, new
            {
                movement.Code, movement.IdTenant, movement.IdFinancialAccount, movement.MovementDate, movement.Amount, movement.Kind,
                movement.Description, movement.IdTransfer, movement.IdReceipt, movement.IdReversedMovement, movement.DtCreated,
                movement.CreatedBy, TransferCode = transferCode, ReceiptCode = receiptCode
            });
    }
    public class IdempotencyRepository(IDapperUnitOfWork work) : IIdempotencyRepository
    {
        public Task<IdempotencyRecord?> FindAsync(int idTenant, string operation, Guid key, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<IdempotencyRecord>(new CommandDefinition(
                AccountSql.FindIdempotency, new { IdTenant = idTenant, Operation = operation, Key = key }, transaction, cancellationToken: ct)), ct);
        public void Add(IdempotencyRecord record) => new DapperRepository<IdempotencyRecord>(work).Add(record);
    }
}
