using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Households;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class HouseholdEndpoints
    {
        public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var people = endpoints.MapGroup("/api/people").WithTags("Pessoas").RequireAuthorization();
            people.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<IReadOnlyList<PersonDto>>(200, "Pessoas", "Moradores da casa.", context.Request.Path,
                    await mediator.Send(new ListPeopleQuery(user.SessionIdentity()), ct))))
                .WithSummary("Lista pessoas ativas e inativas, sem exigir login para o morador.")
                .Produces<SuccessDetails<IReadOnlyList<PersonDto>>>().ProducesProblem(401).ProducesProblem(403);
            people.MapPost("", async (PersonPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Json(new SuccessDetails<PersonDto>(201, "Pessoa criada", "Cadastro salvo.", context.Request.Path,
                    await mediator.Send(new SavePersonCommand(user.SessionIdentity(), null, payload), ct)), statusCode: 201))
                .WithSummary("Cadastra morador. Somente titular.").Produces<SuccessDetails<PersonDto>>(201).ProducesProblem(400).ProducesProblem(403);
            people.MapPut("/{code:guid}", async (Guid code, PersonPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<PersonDto>(200, "Pessoa atualizada", "Cadastro salvo.", context.Request.Path,
                    await mediator.Send(new SavePersonCommand(user.SessionIdentity(), code, payload), ct))))
                .WithSummary("Edita ou inativa sem excluir histórico; exige versão atual.")
                .Produces<SuccessDetails<PersonDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            var settings = endpoints.MapGroup("/api/household-settings").WithTags("Casa").RequireAuthorization();
            settings.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<SettingsDto>(200, "Configurações", "Preferências da casa.", context.Request.Path,
                    await mediator.Send(new GetSettingsQuery(user.SessionIdentity()), ct))))
                .WithSummary("Consulta fuso, moeda, idioma e início de uso.").Produces<SuccessDetails<SettingsDto>>();
            settings.MapPut("", async (SettingsPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<SettingsDto>(200, "Configurações salvas", "Preferências atualizadas.", context.Request.Path,
                    await mediator.Send(new SaveSettingsCommand(user.SessionIdentity(), payload), ct))))
                .WithSummary("Titular configura fuso IANA e início de uso; português e BRL no piloto.")
                .Produces<SuccessDetails<SettingsDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(409);
            return endpoints;
        }
    }
}
