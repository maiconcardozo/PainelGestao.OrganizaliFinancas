using Dapper;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Members;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Members
{
    public class MemberRepository(IDapperUnitOfWork work) : IMemberRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();
        public async Task LockHouseholdAsync(int idTenant, CancellationToken ct)
        {
            if (await Single<int>(MemberSql.LockHousehold, new { IdTenant = idTenant }, ct) == 0)
                throw new NotFoundException("Casa não encontrada.");
        }
        public async Task<string[]> GetRoleKeysAsync(int idTenant, int idUser, CancellationToken ct) =>
            (await List<string>(MemberSql.RoleKeys, new { IdTenant = idTenant, IdUser = idUser }, ct)).ToArray();
        public Task<IReadOnlyList<MemberRecord>> ListAsync(int idTenant, CancellationToken ct) =>
            List<MemberRecord>(MemberSql.List, new { IdTenant = idTenant }, ct);
        public Task<IReadOnlyList<InvitationRecord>> ListInvitationsAsync(int idTenant, CancellationToken ct) =>
            List<InvitationRecord>(MemberSql.Invitations, new { IdTenant = idTenant }, ct);
        public Task<Role?> FindRoleAsync(int idTenant, string key, CancellationToken ct) =>
            Single<Role>(MemberSql.FindRole, new { IdTenant = idTenant, Key = key }, ct);
        public Task<User?> FindUserAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<User>(MemberSql.FindUser, new { IdTenant = idTenant, Code = code }, ct);
        public Task<int> CountOwnersAsync(int idTenant, CancellationToken ct) =>
            Single<int>(MemberSql.CountOwners, new { IdTenant = idTenant }, ct);
        public Task<MemberInvitation?> FindInvitationAsync(string hash, CancellationToken ct) =>
            Single<MemberInvitation>(MemberSql.FindInvitation, new { Hash = hash }, ct);
        public async Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
            await Single<int>(MemberSql.EmailExists, new { Email = email }, ct) > 0;
        public void AddInvitation(NewInvitation invitation) => work.AddPendingWrite(MemberSql.AddInvitation, invitation);
        public void RevokeInvitation(int idTenant, Guid code, string actor) =>
            work.AddPendingWrite(MemberSql.RevokeInvitation, new { IdTenant = idTenant, Code = code, Actor = actor });
        public void AcceptInvitation(MemberInvitation invitation, Guid userCode, string passwordHash)
        {
            var parameters = new { invitation.Id, invitation.IdTenant, invitation.IdRole, invitation.Name, invitation.Email,
                UserCode = userCode, PasswordHash = passwordHash, Actor = userCode.ToString(), LinkCode = Guid.CreateVersion7() };
            work.AddPendingWrite(MemberSql.InsertUser, parameters);
            work.AddPendingWrite(MemberSql.AssignRole, parameters);
            work.AddPendingWrite(MemberSql.ConsumeInvitation, parameters);
        }
        public void UpdateMember(User user, Role role, bool blocked, string actor)
        {
            var parameters = new { user.IdTenant, IdUser = user.Id, UserCode = user.Code, IdRole = role.Id,
                Blocked = blocked, Actor = actor, LinkCode = Guid.CreateVersion7() };
            work.AddPendingWrite(MemberSql.ClearRoles, parameters);
            work.AddPendingWrite(MemberSql.AssignRole, parameters);
            work.AddPendingWrite(MemberSql.UpdateUser, parameters);
        }
    }
}
