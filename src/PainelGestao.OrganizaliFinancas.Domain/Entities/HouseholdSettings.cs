using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
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
        /// <summary>A22: balance the household wants to keep untouched; the month's spending margin is the projected closing balance minus this.</summary>
        public decimal MinimumReserve { get; private set; } = 15000m;
        public int Version { get; private set; }
        public static HouseholdSettings Create(int idTenant, string actor)
        {
            var settings = new HouseholdSettings(idTenant);
            settings.SetCreatedBy(actor);
            return settings;
        }
        public void Update(string timeZone, DateOnly? startDate, decimal minimumReserve, string actor)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);
            if (minimumReserve < 0 || Money.Round(minimumReserve) != minimumReserve) throw new ArgumentException("Reserva mínima deve ser zero ou positiva, em centavos.", nameof(minimumReserve));
            MinimumReserve = minimumReserve;
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            TimeZone = timeZone;
            StartDate = startDate?.ToDateTime(TimeOnly.MinValue);
            Version++;
            UpdateAuditInfo(actor);
        }
    }
}
