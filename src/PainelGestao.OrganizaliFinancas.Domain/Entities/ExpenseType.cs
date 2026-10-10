namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class ExpenseType : TenantEntity
    {
        private ExpenseType() { }
        private ExpenseType(int idTenant) : base(idTenant) { }
        public string Name { get; private set; } = string.Empty;
        public int Version { get; private set; } = 1;
        public static ExpenseType Create(int idTenant, string name, string actor)
        {
            var type = new ExpenseType(idTenant); type.Rename(name); type.SetCreatedBy(actor); return type;
        }
        private void Rename(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            if (name.Trim().Length > 160) throw new ArgumentException("Nome excede 160 caracteres.", nameof(name));
            Name = name.Trim();
        }
        public void Update(string name, bool active, string actor)
        {
            Rename(name); Version++;
            if (active) Activate(actor); else SoftDelete(actor);
            UpdateAuditInfo(actor);
        }
    }
    public class ExpenseTypeVersion : TenantEntity
    {
        private ExpenseTypeVersion() { }
        public int IdExpenseType { get; private set; }
        public int Version { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Behavior { get; private set; } = string.Empty;
        public int? IdDefaultCategory { get; private set; }
        public string FieldsJson { get; private set; } = "[]";
    }
}
