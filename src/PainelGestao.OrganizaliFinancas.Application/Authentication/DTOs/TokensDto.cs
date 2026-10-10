namespace PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs
{
    public sealed record TokensDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);
    public sealed record AuthenticationResultDto(TokensDto Tokens);
    public sealed record SessionDto(Guid UserCode, string Name, string Email);
}
