using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Catalogs;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class CatalogEndpoints
    {
        public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
        {
            foreach (var (route, kind, label) in new[] {
                ("categories", CatalogKind.Categories, "Categorias"), ("income-types", CatalogKind.IncomeTypes, "Tipos de renda"),
                ("creditors", CatalogKind.Creditors, "Credores"), ("payment-methods", CatalogKind.PaymentMethods, "Formas de pagamento") })
            {
                var group = endpoints.MapGroup($"/api/catalogs/{route}").WithTags(label).RequireAuthorization();
                group.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                    Results.Ok(new SuccessDetails<IReadOnlyList<CatalogItemDto>>(200, label, "Cadastros da casa.", context.Request.Path,
                        await mediator.Send(new ListCatalogQuery(user.SessionIdentity(), kind), ct))))
                    .WithSummary($"Lista {label.ToLowerInvariant()} ativos e inativos.").Produces<SuccessDetails<IReadOnlyList<CatalogItemDto>>>().ProducesProblem(403);
                group.MapPost("", async (CatalogPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                    Results.Json(new SuccessDetails<CatalogItemDto>(201, "Cadastro criado", "Cadastro salvo.", context.Request.Path,
                        await mediator.Send(new SaveCatalogCommand(user.SessionIdentity(), kind, null, payload), ct)), statusCode: 201))
                    .WithSummary($"Cria registro de {label.ToLowerInvariant()}. Titular ou editor.")
                    .WithDescription("| Propriedade | Tipo | Descrição |\n|---|---|---|\n| name | string | Nome até 160 caracteres. |\n| isActive | bool | true na criação. |\n| version | int | Zero na criação. |\n| parentCode | UUID? | Somente categorias; mesma casa. |")
                    .Produces<SuccessDetails<CatalogItemDto>>(201).ProducesProblem(400).ProducesProblem(403);
                group.MapPut("/{code:guid}", async (Guid code, CatalogPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                    Results.Ok(new SuccessDetails<CatalogItemDto>(200, "Cadastro atualizado", "Nova versão salva.", context.Request.Path,
                        await mediator.Send(new SaveCatalogCommand(user.SessionIdentity(), kind, code, payload), ct))))
                    .WithSummary($"Edita ou inativa registro de {label.ToLowerInvariant()}, preservando versões anteriores.")
                    .Produces<SuccessDetails<CatalogItemDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            }
            endpoints.MapPost("/api/catalogs/defaults", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<DefaultCatalogResultDto>(200, "Catálogo padrão", "Cadastros padrão conferidos; só o que faltava foi criado.", context.Request.Path,
                    await mediator.Send(new SeedDefaultCatalogCommand(user.SessionIdentity()), ct))))
                .WithTags("Categorias").RequireAuthorization()
                .WithSummary("Carrega o catálogo padrão (categorias, tipos de renda, credores, formas de pagamento e tipos de despesa) sem duplicar nomes. Titular ou editor.")
                .Produces<SuccessDetails<DefaultCatalogResultDto>>().ProducesProblem(403);
            return endpoints;
        }
    }
}
