using PainelGestao.OrganizaliFinancas.Domain.Entities;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security
{
    public sealed record IssuedToken(string Value, DateTime ExpiresAt);
    public interface ITokenService
    {
        IssuedToken CreateAccessToken(User user);
        (string Value, string Hash, DateTime ExpiresAt) CreateRefreshToken();
        string ComputeHash(string value);
    }
    public interface IRecoveryDelivery
    {
        bool IsConfigured { get; }
        Task DeliverAsync(string email, string token, CancellationToken cancellationToken);
    }
}
