using Foundation.Domain.Abstractions;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class PasswordRecovery : Entity
    {
        private PasswordRecovery() { }
        public int IdUser { get; private set; }
        public int SessionVersion { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime? UsedAt { get; private set; }
    }
}
