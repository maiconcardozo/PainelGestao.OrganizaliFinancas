namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class User : TenantEntity
    {
        private User() { }
        private User(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public bool IsBlocked { get; private set; }
        public int SessionVersion { get; private set; }

        public static User Create(int idTenant, string name, string email, string passwordHash, string actor)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(idTenant);
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(email);
            ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
            var user = new User(idTenant) { Name = name.Trim(), Email = email.Trim().ToLowerInvariant(), PasswordHash = passwordHash };
            user.SetCreatedBy(actor);
            return user;
        }

        public void Block(string actor) { IsBlocked = true; SessionVersion++; UpdateAuditInfo(actor); }
        public void RevokeSessions(string actor) { SessionVersion++; UpdateAuditInfo(actor); }
        public void ChangePassword(string hash, string actor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(hash);
            PasswordHash = hash;
            RevokeSessions(actor);
        }
    }
}
