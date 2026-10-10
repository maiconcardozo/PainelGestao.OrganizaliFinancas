namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class MemberInvitation : TenantEntity
    {
        private MemberInvitation() { }
        public int IdRole { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string TokenHash { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime? AcceptedAt { get; private set; }
    }
}
