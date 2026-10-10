using PainelGestao.OrganizaliFinancas.Domain.Enums;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class SubscriptionEvent : TenantEntity
    {
        private SubscriptionEvent() { }
        public int IdSubscription { get; private set; }
        public SubscriptionStatus Status { get; private set; }
        public string Reason { get; private set; } = string.Empty;
    }
}
