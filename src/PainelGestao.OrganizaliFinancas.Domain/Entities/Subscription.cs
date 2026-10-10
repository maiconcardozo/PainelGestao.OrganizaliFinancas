using PainelGestao.OrganizaliFinancas.Domain.Enums;
namespace PainelGestao.OrganizaliFinancas.Domain.Entities
{
    public class Subscription : TenantEntity
    {
        private Subscription() { }
        public Guid ProvisioningKey { get; private set; }
        public int IdOwnerUser { get; private set; }
        public SubscriptionStatus Status { get; private set; }
        public int Version { get; private set; }

        public void ChangeStatus(SubscriptionStatus status, string actor)
        {
            if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            Version++;
            UpdateAuditInfo(actor);
        }
    }
}
