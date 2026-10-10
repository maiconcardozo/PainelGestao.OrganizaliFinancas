using Foundation.Domain.Abstractions;

namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>The house owning a subscription. Platform entity without an IdTenant parent.</summary>
    public class Tenant : Entity
    {
        private Tenant() { }
        public string Name { get; private set; } = string.Empty;

        public static Tenant Create(string name, string createdBy = SystemActor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160)
                throw new ArgumentException("O nome da casa deve ter até 160 caracteres.", nameof(name));

            var tenant = new Tenant { Name = name.Trim() };
            tenant.SetCreatedBy(createdBy);
            return tenant;
        }
    }
}
