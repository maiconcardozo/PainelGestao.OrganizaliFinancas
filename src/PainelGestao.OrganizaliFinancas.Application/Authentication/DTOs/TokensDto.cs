namespace PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs
{
    public sealed record TokensDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public sealed record AuthenticationResultDto(TokensDto Tokens);
    public sealed record SessionMenuItemDto(string Key, string Name, string Route, string Icon);
    public sealed record SessionDto(Guid UserCode, string Name, string Email, string[] Roles, string[] Permissions, SessionMenuItemDto[] Menu);
}
