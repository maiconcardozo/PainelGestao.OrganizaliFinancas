using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Application.Authentication.Services
{
    public sealed record SessionIdentity(Guid UserCode, int IdTenant, int Version);

    /// <summary>Approved pilot matrix. No platform role can be assigned through household operations.</summary>
    public static class HouseholdAccess
    {
        public static string[] Permissions(IEnumerable<string> roles)
        {
            var keys = roles.ToHashSet(StringComparer.Ordinal);
            if (keys.Contains("owner")) return ["household.read", "household.manage", "finance.write", "payments.settle", "payments.reverse", "budgets.write", "members.manage"];
            if (keys.Contains("editor")) return ["household.read", "finance.write", "payments.settle", "budgets.write"];
            if (keys.Contains("reader")) return ["household.read"];
            return [];
        }

        public static async Task<User> RequireAsync(IUnitOfWork work, SessionIdentity identity, string permission, CancellationToken ct)
        {
            var user = await work.AuthenticationRepository.FindAuthorizedAsync(identity.UserCode, identity.IdTenant, identity.Version, ct)
                ?? throw new UnauthenticatedException("Sessão expirada. Entre novamente.");
            var roles = await work.MemberRepository.GetRoleKeysAsync(user.IdTenant, user.Id, ct);
            if (!Permissions(roles).Contains(permission)) throw new ForbiddenException("Seu perfil não permite esta operação.");
            return user;
        }
    }
}
