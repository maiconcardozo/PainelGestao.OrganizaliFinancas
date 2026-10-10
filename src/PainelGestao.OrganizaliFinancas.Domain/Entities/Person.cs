namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>A resident can have income and obligations without an account or e-mail.</summary>
    public class Person : TenantEntity
    {
        private Person() { }
        private Person(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public int Version { get; private set; }
        public static Person Create(int idTenant, string name, string actor)
        {
            var person = new Person(idTenant);
            person.Rename(name);
            person.SetCreatedBy(actor);
            return person;
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
            Version++;
            UpdateAuditInfo(actor);
        }
    }
}
