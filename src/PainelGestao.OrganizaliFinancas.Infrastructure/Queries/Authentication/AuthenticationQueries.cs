namespace PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Authentication
{
    internal static class AuthenticationSql
    {
        private const string ActiveUser = """
            FROM User u
            JOIN Tenant t ON t.Id=u.IdTenant AND t.IsActive=1
            JOIN Subscription s ON s.IdTenant=t.Id AND s.IsActive=1 AND s.Status=1
            """;
        public const string FindByEmail = "SELECT u.* " + ActiveUser +
            " WHERE u.Email=@Email AND u.IsActive=1 AND u.IsBlocked=0";
        public const string FindAuthorized = "SELECT u.* " + ActiveUser +
            " WHERE u.Code=@UserCode AND u.IdTenant=@IdTenant AND u.SessionVersion=@Version AND u.IsActive=1 AND u.IsBlocked=0";
        public const string LockRefresh = "SELECT u.* " + ActiveUser + " " + """
            JOIN RefreshToken r ON r.IdUser=u.Id AND r.SessionVersion=u.SessionVersion
            WHERE r.TokenHash=@Hash AND r.IsActive=1 AND r.RevokedAt IS NULL AND r.ExpiresAt>UTC_TIMESTAMP(6)
                AND u.IsActive=1 AND u.IsBlocked=0 FOR UPDATE
            """;
        public const string LockRecovery = "SELECT u.* " + ActiveUser + " " + """
            JOIN PasswordRecovery r ON r.IdUser=u.Id AND r.SessionVersion=u.SessionVersion
            WHERE r.TokenHash=@Hash AND r.IsActive=1 AND r.UsedAt IS NULL AND r.ExpiresAt>UTC_TIMESTAMP(6)
                AND u.IsActive=1 AND u.IsBlocked=0 FOR UPDATE
            """;
        public const string RevokeRefresh = """
            UPDATE RefreshToken SET RevokedAt=UTC_TIMESTAMP(6), IsActive=0
            WHERE TokenHash=@Hash AND IsActive=1
            """;
        public const string RevokeSessions = """
            UPDATE User SET SessionVersion=SessionVersion+1, DtUpdated=UTC_TIMESTAMP(6), UpdatedBy=@Actor
            WHERE Id=@IdUser AND IdTenant=@IdTenant AND IsActive=1
            """;
        public const string AddRecovery = """
            INSERT INTO PasswordRecovery (Code,IdUser,SessionVersion,TokenHash,ExpiresAt,IsActive,DtCreated,CreatedBy)
            VALUES (@Code,@IdUser,@SessionVersion,@Hash,@ExpiresAt,1,UTC_TIMESTAMP(6),'Recovery')
            """;
        public const string ResetPassword = """
            UPDATE User SET PasswordHash=@PasswordHash, SessionVersion=SessionVersion+1,
                DtUpdated=UTC_TIMESTAMP(6),UpdatedBy=@Actor
            WHERE Id=@IdUser AND IdTenant=@IdTenant AND IsActive=1
            """;
        public const string ConsumeRecovery = """
            UPDATE PasswordRecovery SET UsedAt=UTC_TIMESTAMP(6),IsActive=0 WHERE TokenHash=@Hash AND IsActive=1
            """;
    }
}
