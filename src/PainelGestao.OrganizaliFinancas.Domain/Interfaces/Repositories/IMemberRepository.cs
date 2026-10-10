using PainelGestao.OrganizaliFinancas.Domain.Entities;

namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record MemberRecord(Guid Code, string Name, string Email, bool IsBlocked, string RoleKey, string RoleName);
    public sealed record InvitationRecord(Guid Code, string Name, string Email, string RoleName, DateTime ExpiresAt);
    public sealed record NewInvitation(Guid Code, int IdTenant, int IdRole, string Name, string Email, string TokenHash, DateTime ExpiresAt, string Actor);
    public interface IMemberRepository
    {
        Task LockHouseholdAsync(int idTenant, CancellationToken ct);
        Task<string[]> GetRoleKeysAsync(int idTenant, int idUser, CancellationToken ct);
        Task<IReadOnlyList<MemberRecord>> ListAsync(int idTenant, CancellationToken ct);
        Task<IReadOnlyList<InvitationRecord>> ListInvitationsAsync(int idTenant, CancellationToken ct);
        Task<Role?> FindRoleAsync(int idTenant, string key, CancellationToken ct);
        Task<User?> FindUserAsync(int idTenant, Guid code, CancellationToken ct);
        Task<int> CountOwnersAsync(int idTenant, CancellationToken ct);
        Task<MemberInvitation?> FindInvitationAsync(string hash, CancellationToken ct);
        Task<bool> EmailExistsAsync(string email, CancellationToken ct);
        void AddInvitation(NewInvitation invitation);
        void RevokeInvitation(int idTenant, Guid code, string actor);
        void AcceptInvitation(MemberInvitation invitation, Guid userCode, string passwordHash);
        void UpdateMember(User user, Role role, bool blocked, string actor);
    }
}
