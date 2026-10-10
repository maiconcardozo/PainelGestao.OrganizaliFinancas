using Dapper;
using Foundation.Domain.Interfaces.UnitOfWork;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Pilot;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Pilot
{
    public class PilotRepository(IDapperUnitOfWork work) : IPilotRepository
    {
        public Task<PilotRecord?> FindAsync(Guid key, CancellationToken cancellationToken)
        {
            var query = new FindPilotQuery(key);
            return work.ExecuteReadAsync((connection, transaction) =>
                connection.QuerySingleOrDefaultAsync<PilotRecord>(
                    new CommandDefinition(query.GetSql(), query, transaction, cancellationToken: cancellationToken)), cancellationToken);
        }

        public async Task<PilotRecord> ProvisionAsync(NewPilot pilot, CancellationToken cancellationToken)
        {
            work.AddPendingWrite(PilotSql.InsertTenant, pilot);
            work.AddPendingWrite(PilotSql.InsertOwner, pilot);
            work.AddPendingWrite(PilotSql.InsertSubscription, pilot);
            work.AddPendingWrite(PilotSql.InsertInitialEvent,
                new { EventCode = Guid.CreateVersion7(), pilot.SubscriptionCode, pilot.Actor });
            try { await work.CommitAsync(cancellationToken); }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                var existing = await FindAsync(pilot.Key, cancellationToken);
                if (existing is not null) return existing;
                throw new ConflictException("Já existe um usuário com esse e-mail.");
            }
            return (await FindAsync(pilot.Key, cancellationToken))!;
        }

        public async Task ChangeStatusAsync(Guid householdCode, SubscriptionStatus status,
            string reason, string actor, CancellationToken cancellationToken)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                var subscription = await work.ExecuteReadAsync((connection, transaction) =>
                    connection.QuerySingleOrDefaultAsync<Subscription>(new CommandDefinition(
                        PilotSql.LockSubscription, new { HouseholdCode = householdCode }, transaction,
                        cancellationToken: cancellationToken)), cancellationToken)
                    ?? throw new NotFoundException("Casa não encontrada.");
                if (subscription.Status == status) return false;
                subscription.ChangeStatus(status, actor);
                work.AddPendingWrite(PilotSql.UpdateStatus, subscription);
                work.AddPendingWrite(PilotSql.InsertEvent, new
                {
                    Code = Guid.CreateVersion7(), subscription.IdTenant, IdSubscription = subscription.Id,
                    Status = status, Reason = reason.Trim(), Actor = actor
                });
                return true;
            }, cancellationToken);
        }
    }
}
