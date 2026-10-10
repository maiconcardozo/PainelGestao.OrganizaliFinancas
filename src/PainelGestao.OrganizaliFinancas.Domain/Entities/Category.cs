namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class Category : TenantEntity, Interfaces.Repositories.ICatalogItem
    {
        private Category() { }
        private Category(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public int Version { get; private set; }
        public int? IdParentCategory { get; private set; }
        public static Category Create(int idTenant, string name, string actor)
        {
            var item = new Category(idTenant);
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
        public void SetParent(int? idParentCategory) => IdParentCategory = idParentCategory;
    }
}

