namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class HouseholdSettings : TenantEntity
    {
        private HouseholdSettings() { }
        private HouseholdSettings(int idTenant) : base(idTenant) { }
        public string TimeZone { get; private set; } = "America/Sao_Paulo";
        public string Language { get; private set; } = "pt-BR";
        public string Currency { get; private set; } = "BRL";
        public DateTime? StartDate { get; private set; }
        public int Version { get; private set; }
        public static HouseholdSettings Create(int idTenant, string actor)
        {
            var settings = new HouseholdSettings(idTenant);
            settings.SetCreatedBy(actor);
            return settings;
        }
        public void Update(string timeZone, DateOnly? startDate, string actor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            TimeZone = timeZone;
            StartDate = startDate?.ToDateTime(TimeOnly.MinValue);
            Version++;
            UpdateAuditInfo(actor);
        }
    }
}
