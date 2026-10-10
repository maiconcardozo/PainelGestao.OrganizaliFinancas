using Foundation.Domain.Abstractions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class RefreshToken : Entity
    {
        private RefreshToken() { }
        public int IdUser { get; private set; }
        public int SessionVersion { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public static RefreshToken Create(User user, string hash, DateTime expiresAt)
        {
            return new RefreshToken { IdUser = user.Id, SessionVersion = user.SessionVersion, TokenHash = hash, ExpiresAt = expiresAt };
        }
    }
}
