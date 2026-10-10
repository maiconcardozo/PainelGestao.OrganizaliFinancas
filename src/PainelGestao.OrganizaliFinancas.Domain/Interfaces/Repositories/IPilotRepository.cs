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
        Task<PilotRecord> ProvisionAsync(NewPilot pilot, CancellationToken cancellationToken);
        Task ChangeStatusAsync(Guid householdCode, SubscriptionStatus status, string reason, string actor, CancellationToken cancellationToken);
    }
}
