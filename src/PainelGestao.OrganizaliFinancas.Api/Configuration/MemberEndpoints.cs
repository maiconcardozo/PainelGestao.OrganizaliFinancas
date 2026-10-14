using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Members;
using PainelGestao.OrganizaliFinancas.Shared.Constants;

namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class MemberEndpoints
    {
        public static SessionIdentity SessionIdentity(this ClaimsPrincipal principal) => new(
            Guid.Parse(principal.FindFirst(TokenClaims.UserCode)!.Value),
            int.Parse(principal.FindFirst(TokenClaims.IdTenant)!.Value),
            int.Parse(principal.FindFirst(TokenClaims.SessionVersion)!.Value));

        public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/members").WithTags("Integrantes").RequireAuthorization();
            group.AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                return await next(context);
            });
            group.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<MembersDto>(200, "Integrantes", "Integrantes e convites da casa.", context.Request.Path,
                    await mediator.Send(new ListMembersQuery(user.SessionIdentity()), ct))))
                .WithSummary("Lista integrantes e convites. Somente titular.")
                .Produces<SuccessDetails<MembersDto>>().ProducesProblem(401).ProducesProblem(403);
            group.MapPost("/invitations", async (InviteMemberPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Json(new SuccessDetails<InvitationDto>(201, "Convite criado", "Compartilhe o link com a pessoa convidada.", context.Request.Path,
                    await mediator.Send(new InviteMemberCommand(user.SessionIdentity(), payload), ct)), statusCode: 201))
                .WithSummary("Cria convite manual de uso único, válido por 48 horas.")
                .WithDescription("| Propriedade | Tipo | Descrição |\n|---|---|---|\n| name | string | Nome, até 160 caracteres. |\n| email | string | E-mail do convidado. |\n| roleKey | string | owner, editor ou reader. |")
                .Produces<SuccessDetails<InvitationDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(409);
            group.MapPut("/{code:guid}", async (Guid code, UpdateMemberPayload payload, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new UpdateMemberCommand(user.SessionIdentity(), code, payload), ct);
                return Results.NoContent();
            }).WithSummary("Altera perfil ou bloqueia acesso, revogando sessões. Preserva o último titular.")
                .Produces(204).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            group.MapPost("/{code:guid}/recovery", async (Guid code, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Json(new SuccessDetails<InvitationDto>(201, "Link gerado", "Entregue o link à pessoa; ele vale por 24 horas e só pode ser usado uma vez.", context.Request.Path,
                    await mediator.Send(new CreateRecoveryLinkCommand(user.SessionIdentity(), code), ct)), statusCode: 201))
                .WithSummary("Titular gera link de nova senha para um integrante, sem depender de e-mail.")
                .Produces<SuccessDetails<InvitationDto>>(201).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            group.MapDelete("/invitations/{code:guid}", async (Guid code, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new RevokeInvitationCommand(user.SessionIdentity(), code), ct);
                return Results.NoContent();
            }).WithSummary("Revoga um convite da casa.").Produces(204).ProducesProblem(403);
            endpoints.MapPost("/api/auth/accept-invitation", async (AcceptInvitationPayload payload, IMediator mediator, HttpContext context, CancellationToken ct) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                await mediator.Send(new AcceptInvitationCommand(payload), ct);
                return Results.NoContent();
            }).WithTags("Acesso").RequireRateLimiting("authentication")
                .WithSummary("Aceita convite e define senha de 12 a 128 caracteres.")
                .Produces(204).ProducesProblem(400).ProducesProblem(401).ProducesProblem(409);
            return endpoints;
        }
    }
}
