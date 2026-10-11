namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    /// <summary>Monthly limit of one category. Removing a limit deactivates the row ("Sem orçamento"), which differs from a zero limit.</summary>
    public class BudgetLimit : TenantEntity
    {
        private BudgetLimit() { }
        public int IdCategory { get; private set; }
        public DateTime Month { get; private set; }
        public decimal Amount { get; private set; }
        public int Version { get; private set; }
    }

    /// <summary>Household alert thresholds (rule 8: 80% and 100% proposed, editable) and how many days ahead a due date warns.</summary>
    public class AlertPreference : TenantEntity
    {
        private AlertPreference() { }
        public int WarningPercent { get; private set; } = 80;
        public int LimitPercent { get; private set; } = 100;
        public int DueSoonDays { get; private set; } = 5;
        public int Version { get; private set; }
    }

    /// <summary>What a member did with an alert condition. The condition itself is always recomputed from the data.</summary>
    public class AlertState : TenantEntity
    {
        private AlertState() { }
        public string AlertKey { get; private set; } = string.Empty;
        public string Fingerprint { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;
        public DateTime? SnoozedUntil { get; private set; }
    }
}
