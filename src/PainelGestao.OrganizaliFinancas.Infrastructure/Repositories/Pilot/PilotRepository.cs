using Dapper;
using Foundation.Domain.Interfaces.UnitOfWork;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Pilot;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Members;
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
            SeedAccess(pilot.HouseholdCode, pilot.UserCode, pilot.Actor);
            try { await work.CommitAsync(cancellationToken); }
            catch (MySqlException exception) when (exception.Number == 1062)
            {
                var existing = await FindAsync(pilot.Key, cancellationToken);
                if (existing is not null) return existing;
                throw new ConflictException("Já existe um usuário com esse e-mail.");
            }
            return (await FindAsync(pilot.Key, cancellationToken))!;
        }

        private void SeedAccess(Guid householdCode, Guid userCode, string actor)
        {
            foreach (var (key, name) in new[] { ("owner", "Titular"), ("editor", "Editor"), ("reader", "Leitor") })
                work.AddPendingWrite(MemberSql.SeedRole, new { RoleCode = Guid.CreateVersion7(), RoleKey = key,
                    RoleName = name, HouseholdCode = householdCode, Actor = actor });
            work.AddPendingWrite(MemberSql.SeedOwner, new { LinkCode = Guid.CreateVersion7(), HouseholdCode = householdCode, UserCode = userCode, Actor = actor });
        }

        public Task InitializeAccessAsync(Guid householdCode, string actor, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actor);
            return work.ExecuteInTransactionAsync(async () =>
            {
                var owner = await work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<Guid>(
                    new CommandDefinition(PilotSql.LockOwner, new { HouseholdCode = householdCode }, transaction, cancellationToken: cancellationToken)), cancellationToken);
                if (owner == Guid.Empty) throw new NotFoundException("Casa não encontrada.");
                SeedAccess(householdCode, owner, actor);
                return true;
            }, cancellationToken);
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
                if (status != SubscriptionStatus.Active)
                    work.AddPendingWrite(PilotSql.RevokeHouseholdSessions, new { subscription.IdTenant, Actor = actor });
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
