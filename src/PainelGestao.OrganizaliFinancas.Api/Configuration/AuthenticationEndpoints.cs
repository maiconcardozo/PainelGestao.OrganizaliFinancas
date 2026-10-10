using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Commands;
using PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs;
using PainelGestao.OrganizaliFinancas.Shared.Constants;

namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class AuthenticationEndpoints
    {
        private static (Guid UserCode, int IdTenant, int Version) Identity(ClaimsPrincipal principal) =>
            (Guid.Parse(principal.FindFirst(TokenClaims.UserCode)!.Value),
             int.Parse(principal.FindFirst(TokenClaims.IdTenant)!.Value),
             int.Parse(principal.FindFirst(TokenClaims.SessionVersion)!.Value));

        public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/auth").WithTags("Acesso");
            group.AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
            group.MapPost("/login", async (AuthenticateUserCommand command, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<AuthenticationResultDto>(200,"Autenticado","Sessão iniciada.",context.Request.Path,await mediator.Send(command,ct))))
                .RequireRateLimiting("authentication").WithSummary("Autentica na casa.")
                .WithDescription("| Propriedade | Tipo | Descrição |\n|---|---|---|\n| email | string | E-mail do usuário. |\n| password | string | Senha, até 128 caracteres. |")
                .Produces<SuccessDetails<AuthenticationResultDto>>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(429);
            group.MapPost("/refresh", async (RenewSessionCommand command, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<AuthenticationResultDto>(200,"Renovado","Sessão renovada.",context.Request.Path,await mediator.Send(command,ct))))
                .RequireRateLimiting("authentication").WithSummary("Renova e rotaciona o refresh token.")
                .Produces<SuccessDetails<AuthenticationResultDto>>().ProducesProblem(401);
            group.MapPost("/logout", async (IMediator mediator, ClaimsPrincipal principal, CancellationToken ct) =>
            {
                var identity=Identity(principal);
                await mediator.Send(new SignOutCommand(identity.UserCode,identity.IdTenant,identity.Version),ct);
                return Results.NoContent();
            }).RequireAuthorization().WithSummary("Encerra todas as sessões do usuário.").Produces(204).ProducesProblem(401);
            group.MapGet("/me", async (IMediator mediator, ClaimsPrincipal principal, HttpContext context, CancellationToken ct) =>
            {
                var identity=Identity(principal);
                return Results.Ok(new SuccessDetails<SessionDto>(200,"Sessão","Usuário autenticado.",context.Request.Path,
                    await mediator.Send(new GetSessionQuery(identity.UserCode,identity.IdTenant,identity.Version),ct)));
            }).RequireAuthorization().WithSummary("Consulta a sessão atual.").Produces<SuccessDetails<SessionDto>>().ProducesProblem(401);
            group.MapPost("/recovery", async (RequestRecoveryCommand command, IMediator mediator, HttpContext context, CancellationToken ct) =>
            {
                await mediator.Send(command,ct);
                return Results.Json(new SuccessDetails<string>(202,"Solicitação recebida",
                    "Se houver uma conta elegível, você receberá as instruções.",context.Request.Path,"Accepted"),statusCode:202);
            }).RequireRateLimiting("authentication").WithSummary("Solicita recuperação de senha.")
                .WithDescription("Transporte externo ainda não configurado: responde 503 para qualquer endereço até a escolha do provedor.")
                .Produces<SuccessDetails<string>>(202).ProducesProblem(400).ProducesProblem(503);
            group.MapPost("/reset-password", async (ResetPasswordCommand command, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(command,ct); return Results.NoContent();
            }).RequireRateLimiting("authentication").WithSummary("Usa o link uma vez e revoga sessões anteriores.")
                .Produces(204).ProducesProblem(400).ProducesProblem(401);
            return endpoints;
        }
    }
}
