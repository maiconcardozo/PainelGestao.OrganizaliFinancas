using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Authentication;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Authentication
{
    public class AuthenticationRepository(IDapperUnitOfWork work) : IAuthenticationRepository
    {
        private Task<User?> Find(string sql, object parameters, CancellationToken cancellationToken) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<User>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken)), cancellationToken);
        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
            Find(AuthenticationSql.FindByEmail, new { Email=email }, cancellationToken);
        public Task<User?> FindAuthorizedAsync(Guid userCode, int idTenant, int version, CancellationToken cancellationToken) =>
            Find(AuthenticationSql.FindAuthorized, new { UserCode=userCode, IdTenant=idTenant, Version=version }, cancellationToken);
        public Task<User?> LockRefreshUserAsync(string hash, CancellationToken cancellationToken) =>
            Find(AuthenticationSql.LockRefresh, new { Hash=hash }, cancellationToken);
        public Task<User?> LockRecoveryUserAsync(string hash, CancellationToken cancellationToken) =>
            Find(AuthenticationSql.LockRecovery, new { Hash=hash }, cancellationToken);
        public void AddRefresh(RefreshToken token) => new DapperRepository<RefreshToken>(work).Add(token);
        public void RevokeRefresh(string hash) => work.AddPendingWrite(AuthenticationSql.RevokeRefresh, new { Hash=hash });
        public void RevokeSessions(int idUser, int idTenant, string actor) =>
            work.AddPendingWrite(AuthenticationSql.RevokeSessions, new { IdUser=idUser, IdTenant=idTenant, Actor=actor });
        public void AddRecovery(User user, string hash, DateTime expiresAt) =>
            work.AddPendingWrite(AuthenticationSql.AddRecovery,
                new { Code=Guid.CreateVersion7(), IdUser=user.Id, user.SessionVersion, Hash=hash, ExpiresAt=expiresAt });
        public void ResetPassword(User user, string recoveryHash, string passwordHash)
        {
            work.AddPendingWrite(AuthenticationSql.ResetPassword, new
            { IdUser=user.Id, user.IdTenant, PasswordHash=passwordHash, Actor=user.Code.ToString() });
            work.AddPendingWrite(AuthenticationSql.ConsumeRecovery, new { Hash=recoveryHash });
        }
    }
}
