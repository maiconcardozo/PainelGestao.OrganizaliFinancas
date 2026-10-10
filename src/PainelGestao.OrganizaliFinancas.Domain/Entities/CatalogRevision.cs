namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Append-only label snapshot; referenced financial records can retain their original version.</summary>
    public class CatalogRevision : TenantEntity
    {
        private CatalogRevision() { }
        private CatalogRevision(int idTenant) : base(idTenant) { }
        public string Catalog { get; private set; } = string.Empty;
        public Guid ItemCode { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Guid? ParentCode { get; private set; }
        public bool ItemActive { get; private set; }
        public int Version { get; private set; }
        public static CatalogRevision Create(int idTenant, string catalog, Guid itemCode, string name, Guid? parentCode, bool active, int version, string actor)
        {
            var revision = new CatalogRevision(idTenant) { Catalog = catalog, ItemCode = itemCode, Name = name, ParentCode = parentCode, ItemActive = active, Version = version };
            revision.SetCreatedBy(actor);
            return revision;
        }
    }
}
