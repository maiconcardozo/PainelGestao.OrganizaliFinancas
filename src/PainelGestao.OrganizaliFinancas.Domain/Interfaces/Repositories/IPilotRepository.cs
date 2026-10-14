using PainelGestao.OrganizaliFinancas.Domain.Enums;
namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories
{
    public sealed record PilotRecord(Guid HouseholdCode, Guid UserCode, Guid SubscriptionCode,
        string HouseholdName, string OwnerName, string Email, SubscriptionStatus Status);
    public sealed record NewPilot(Guid Key, Guid HouseholdCode, Guid UserCode, Guid SubscriptionCode,
        string HouseholdName, string OwnerName, string Email, string PasswordHash, string Actor);

    /// <summary>Platform provisioning only. Does not expose household financial data.</summary>
    public interface IPilotRepository
    {
        Task<PilotRecord?> FindAsync(Guid key, CancellationToken cancellationToken);
        /// <summary>Internal tenant id of a household, for operator routines that act on one household.</summary>
        Task<int?> FindTenantIdAsync(Guid householdCode, CancellationToken cancellationToken);
        Task<PilotRecord> ProvisionAsync(NewPilot pilot, CancellationToken cancellationToken);
        Task InitializeAccessAsync(Guid householdCode, string actor, CancellationToken cancellationToken);
        Task ChangeStatusAsync(Guid householdCode, SubscriptionStatus status, string reason, string actor, CancellationToken cancellationToken);
        /// <summary>Households whose subscription is active, for platform routines that keep their own data current.</summary>
        Task<IReadOnlyList<int>> ActiveHouseholdIdsAsync(CancellationToken cancellationToken);
    }
}
