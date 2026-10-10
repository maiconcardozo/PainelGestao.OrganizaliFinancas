namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Household profile, following RevendaPRO Role/UserRole. Pilot profiles are fixed.</summary>
    public class Role : TenantEntity
    {
        private Role() { }
        public string Key { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public bool IsSystem { get; private set; }
    }

    public class UserRole : TenantEntity
    {
        private UserRole() { }
        public int IdUser { get; private set; }
        public int IdRole { get; private set; }
    }
}
