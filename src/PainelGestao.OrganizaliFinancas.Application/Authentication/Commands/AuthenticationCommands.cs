using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs;
namespace PainelGestao.OrganizaliFinancas.Application.Authentication.Commands
{
    public sealed record AuthenticateUserCommand(string Email, string Password) : IRequest<AuthenticationResultDto>;
    public sealed record RenewSessionCommand(string RefreshToken) : IRequest<AuthenticationResultDto>;
    public sealed record SignOutCommand(Guid UserCode, int IdTenant, int Version) : IRequest;
    public sealed record GetSessionQuery(Guid UserCode, int IdTenant, int Version) : IRequest<SessionDto>;
    public sealed record RequestRecoveryCommand(string Email) : IRequest;
    public sealed record ResetPasswordCommand(string Token, string Password) : IRequest;
}
