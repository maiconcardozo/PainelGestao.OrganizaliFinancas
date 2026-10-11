namespace PainelGestao.OrganizaliFinancas.Shared.Settings
{
    /// <summary>
    /// SMTP transport for password recovery (D16 still open: any provider with SMTP works). Without Host, From and AppOrigin
    /// the recovery stays unavailable (503) instead of silently dropping the message.
    /// </summary>
    public class MailSettings
    {
        public const string SectionName = "Mail";
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        /// <summary>Public web origin used in the link, for example https://financas.example.</summary>
        public string AppOrigin { get; set; } = string.Empty;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(From) && Uri.TryCreate(AppOrigin, UriKind.Absolute, out _);
    }

    /// <summary>Background routines and protection limits of the running API.</summary>
    public class JobSettings
    {
        public const string SectionName = "Jobs";
        /// <summary>Rolls income forecasts and recurring expenses forward to the D23 horizon for every active household.</summary>
        public bool HorizonEnabled { get; set; }
        public int HorizonIntervalHours { get; set; } = 24;
        /// <summary>Requests per minute for one session (or one address without session). Login keeps its own stricter limit.</summary>
        public int ApiRequestsPerMinute { get; set; } = 600;
        /// <summary>Networks of the reverse proxies whose X-Forwarded-For is trusted, in CIDR (for example 172.16.0.0/12).</summary>
        public string[] TrustedProxyNetworks { get; set; } = [];
    }
}
