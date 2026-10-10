namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Members
{
    internal static class MemberSql
    {
        public const string RoleKeys = """
            SELECT r.`Key` FROM UserRole ur
            JOIN Role r ON r.Id=ur.IdRole AND r.IdTenant=ur.IdTenant AND r.IsActive=1
            JOIN User u ON u.Id=ur.IdUser AND u.IdTenant=ur.IdTenant AND u.IsActive=1 AND u.IsBlocked=0
            WHERE ur.IdTenant=@IdTenant AND ur.IdUser=@IdUser AND ur.IsActive=1
            """;
        public const string LockHousehold = "SELECT Id FROM Tenant WHERE Id=@IdTenant AND IsActive=1 FOR UPDATE";
        public const string List = """
            SELECT u.Code,u.Name,u.Email,u.IsBlocked,r.`Key` RoleKey,r.Name RoleName
            FROM User u JOIN UserRole ur ON ur.IdUser=u.Id AND ur.IdTenant=u.IdTenant AND ur.IsActive=1
            JOIN Role r ON r.Id=ur.IdRole AND r.IdTenant=u.IdTenant AND r.IsActive=1
            WHERE u.IdTenant=@IdTenant AND u.IsActive=1 ORDER BY u.Name
            """;
        public const string Invitations = """
            SELECT i.Code,i.Name,i.Email,r.Name RoleName,i.ExpiresAt
            FROM MemberInvitation i JOIN Role r ON r.Id=i.IdRole AND r.IdTenant=i.IdTenant
            WHERE i.IdTenant=@IdTenant AND i.IsActive=1 AND i.AcceptedAt IS NULL AND i.ExpiresAt>UTC_TIMESTAMP(6)
            ORDER BY i.DtCreated DESC
            """;
        public const string FindRole = "SELECT * FROM Role WHERE IdTenant=@IdTenant AND `Key`=@Key AND IsActive=1";
        public const string FindUser = "SELECT * FROM User WHERE IdTenant=@IdTenant AND Code=@Code AND IsActive=1";
        public const string CountOwners = """
            SELECT COUNT(DISTINCT u.Id) FROM User u
            JOIN UserRole ur ON ur.IdUser=u.Id AND ur.IdTenant=u.IdTenant AND ur.IsActive=1
            JOIN Role r ON r.Id=ur.IdRole AND r.IdTenant=u.IdTenant AND r.IsActive=1 AND r.`Key`='owner'
            WHERE u.IdTenant=@IdTenant AND u.IsActive=1 AND u.IsBlocked=0
            """;
        public const string FindInvitation = """
            SELECT i.* FROM MemberInvitation i
            JOIN Subscription s ON s.IdTenant=i.IdTenant AND s.IsActive=1 AND s.Status=1
            JOIN Tenant t ON t.Id=i.IdTenant AND t.IsActive=1
            WHERE i.TokenHash=@Hash AND i.IsActive=1 AND i.AcceptedAt IS NULL AND i.ExpiresAt>UTC_TIMESTAMP(6)
            FOR UPDATE
            """;
        public const string EmailExists = "SELECT COUNT(1) FROM User WHERE Email=@Email";
        public const string AddInvitation = """
            INSERT INTO MemberInvitation (Code,IdTenant,IdRole,Name,Email,TokenHash,ExpiresAt,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdTenant,@IdRole,@Name,@Email,@TokenHash,@ExpiresAt,1,UTC_TIMESTAMP(6),@Actor)
            """;
        public const string RevokeInvitation = """
            UPDATE MemberInvitation SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor
            WHERE Code=@Code AND IdTenant=@IdTenant AND IsActive=1
            """;
        public const string ConsumeInvitation = """
            UPDATE MemberInvitation SET AcceptedAt=UTC_TIMESTAMP(6),IsActive=0,DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            WHERE Id=@Id AND IdTenant=@IdTenant AND IsActive=1
            """;
        public const string InsertUser = """
            INSERT INTO User (Code,IdTenant,Name,Email,PasswordHash,IsBlocked,SessionVersion,IsActive,DtCreated,CreatedBy)
            VALUES (@UserCode,@IdTenant,@Name,@Email,@PasswordHash,0,0,1,UTC_TIMESTAMP(6),@Actor)
            """;
        public const string AssignRole = """
            INSERT INTO UserRole (Code,IdTenant,IdUser,IdRole,IsActive,DtCreated,CreatedBy)
            SELECT @LinkCode,@IdTenant,u.Id,@IdRole,1,UTC_TIMESTAMP(6),@Actor FROM User u
            WHERE u.Code=@UserCode AND u.IdTenant=@IdTenant
            ON DUPLICATE KEY UPDATE IsActive=1,DtDeleted=NULL,DeletedBy=NULL,DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            """;
        public const string ClearRoles = """
            UPDATE UserRole SET IsActive=0,DtDeleted=UTC_TIMESTAMP(6),DeletedBy=@Actor
            WHERE IdTenant=@IdTenant AND IdUser=@IdUser AND IsActive=1
            """;
        public const string UpdateUser = """
            UPDATE User SET IsBlocked=@Blocked,SessionVersion=SessionVersion+1,DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            WHERE IdTenant=@IdTenant AND Id=@IdUser
            """;
        public const string SeedRole = """
            INSERT INTO Role (Code,IdTenant,`Key`,Name,IsSystem,IsActive,DtCreated,CreatedBy)
            SELECT @RoleCode,Id,@RoleKey,@RoleName,1,1,UTC_TIMESTAMP(6),@Actor FROM Tenant WHERE Code=@HouseholdCode
                AND NOT EXISTS (SELECT 1 FROM Role WHERE IdTenant=Tenant.Id AND `Key`=@RoleKey)
            """;
        public const string SeedOwner = """
            INSERT INTO UserRole (Code,IdTenant,IdUser,IdRole,IsActive,DtCreated,CreatedBy)
            SELECT @LinkCode,t.Id,u.Id,r.Id,1,UTC_TIMESTAMP(6),@Actor FROM Tenant t
            JOIN User u ON u.IdTenant=t.Id AND u.Code=@UserCode
            JOIN Role r ON r.IdTenant=t.Id AND r.`Key`='owner'
            WHERE t.Code=@HouseholdCode
                AND NOT EXISTS (SELECT 1 FROM UserRole WHERE IdTenant=t.Id)
            """;
    }
}
