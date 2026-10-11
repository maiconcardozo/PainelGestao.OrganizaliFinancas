using Foundation.Dapper.Sql;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Pilot
{
    internal sealed class FindPilotQuery(Guid key) : SqlQuery
    {
        public Guid Key { get; } = key;
        public override string GetSql() => """
            SELECT t.Code AS HouseholdCode, u.Code AS UserCode, s.Code AS SubscriptionCode,
                t.Name AS HouseholdName, u.Name AS OwnerName, u.Email, s.Status
            FROM Subscription s
            JOIN Tenant t ON t.Id = s.IdTenant AND t.IsActive = 1
            JOIN User u ON u.Id = s.IdOwnerUser AND u.IdTenant = s.IdTenant AND u.IsActive = 1
            WHERE s.ProvisioningKey = @Key AND s.IsActive = 1
            """;
    }

    internal static class PilotSql
    {
        public const string LockOwner = """
            SELECT u.Code FROM Tenant t JOIN Subscription s ON s.IdTenant=t.Id
            JOIN User u ON u.Id=s.IdOwnerUser AND u.IdTenant=t.Id
            WHERE t.Code=@HouseholdCode FOR UPDATE
            """;
        public const string InsertTenant = """
            INSERT INTO Tenant (Code, Name, IsActive, DtCreated, CreatedBy)
            VALUES (@HouseholdCode, @HouseholdName, 1, UTC_TIMESTAMP(6), @Actor)
            """;
        public const string InsertOwner = """
            INSERT INTO User (Code, IdTenant, Name, Email, PasswordHash, IsBlocked, SessionVersion, IsActive, DtCreated, CreatedBy)
            SELECT @UserCode, Id, @OwnerName, @Email, @PasswordHash, 0, 0, 1, UTC_TIMESTAMP(6), @Actor
            FROM Tenant WHERE Code = @HouseholdCode AND IsActive = 1
            """;
        public const string InsertSubscription = """
            INSERT INTO Subscription (Code, IdTenant, IdOwnerUser, ProvisioningKey, Status, Version, IsActive, DtCreated, CreatedBy)
            SELECT @SubscriptionCode, t.Id, u.Id, @Key, 0, 0, 1, UTC_TIMESTAMP(6), @Actor
            FROM Tenant t JOIN User u ON u.IdTenant = t.Id AND u.Code = @UserCode AND u.IsActive = 1
            WHERE t.Code = @HouseholdCode AND t.IsActive = 1
            """;
        public const string InsertInitialEvent = """
            INSERT INTO SubscriptionEvent (Code, IdTenant, IdSubscription, Status, Reason, IsActive, DtCreated, CreatedBy)
            SELECT @EventCode, IdTenant, Id, 0, 'Provisionamento do piloto', 1, UTC_TIMESTAMP(6), @Actor
            FROM Subscription WHERE Code = @SubscriptionCode AND IsActive = 1
            """;
        public const string LockSubscription = """
            SELECT s.* FROM Subscription s
            JOIN Tenant t ON t.Id = s.IdTenant AND t.IsActive = 1
            WHERE t.Code = @HouseholdCode AND s.IsActive = 1 FOR UPDATE
            """;
        public const string UpdateStatus = """
            UPDATE Subscription SET Status = @Status, Version = @Version,
                DtUpdated = @DtUpdated, UpdatedBy = @UpdatedBy
            WHERE Id = @Id AND IdTenant = @IdTenant AND IsActive = 1
            """;
        public const string RevokeHouseholdSessions = """
            UPDATE User SET SessionVersion=SessionVersion+1, DtUpdated=UTC_TIMESTAMP(6), UpdatedBy=@Actor
            WHERE IdTenant=@IdTenant AND IsActive=1
            """;
        public const string InsertEvent = """
            INSERT INTO SubscriptionEvent (Code, IdTenant, IdSubscription, Status, Reason, IsActive, DtCreated, CreatedBy)
            VALUES (@Code, @IdTenant, @IdSubscription, @Status, @Reason, 1, UTC_TIMESTAMP(6), @Actor)
            """;
        public const string ActiveHouseholds = "SELECT IdTenant FROM Subscription WHERE Status = @Status ORDER BY IdTenant";
    }
}
