namespace PainelGestao.OrganizaliFinancas.Shared.Settings
{
    public class OrganizaliFinancasSettings
    {
        public const string SectionName = "OrganizaliFinancas";
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>D23: income forecasts are materialized through the current month plus this many months.</summary>
        public int ForecastHorizonMonths { get; set; } = 12;
    }
}
