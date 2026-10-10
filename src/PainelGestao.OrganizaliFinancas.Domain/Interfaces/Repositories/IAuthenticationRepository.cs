using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public interface IAuthenticationRepository
    {
        Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
        Task<User?> FindAuthorizedAsync(Guid userCode, int idTenant, int version, CancellationToken cancellationToken);
        Task<User?> LockRefreshUserAsync(string hash, CancellationToken cancellationToken);
        Task<User?> LockRecoveryUserAsync(string hash, CancellationToken cancellationToken);
        void AddRefresh(RefreshToken token);
        void RevokeRefresh(string hash);
        void RevokeSessions(int idUser, int idTenant, string actor);
        void AddRecovery(User user, string hash, DateTime expiresAt);
        void ResetPassword(User user, string recoveryHash, string passwordHash);
    }
}
