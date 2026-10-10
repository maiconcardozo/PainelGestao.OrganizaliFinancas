namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class PaymentMethod : TenantEntity, Interfaces.Repositories.ICatalogItem
    {
        private PaymentMethod() { }
        private PaymentMethod(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public int Version { get; private set; }
        public static PaymentMethod Create(int idTenant, string name, string actor)
        {
            var item = new PaymentMethod(idTenant);
            item.Rename(name); item.SetCreatedBy(actor);
            return item;
        }
        private void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160) throw new ArgumentException("Nome excede 160 caracteres.", nameof(name));
            Name = name.Trim();
        }
        public void Update(string name, bool active, string actor)
        {
            Rename(name);
            if (active) Activate(actor); else SoftDelete(actor);
            Version++; UpdateAuditInfo(actor);
        }
    }
}

