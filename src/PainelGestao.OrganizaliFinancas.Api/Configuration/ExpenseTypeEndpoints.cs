using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.ExpenseTypes;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class ExpenseTypeEndpoints
    {
        public static IEndpointRouteBuilder MapExpenseTypeEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/expense-types").WithTags("Tipos de despesa").RequireAuthorization();
            group.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<IReadOnlyList<ExpenseTypeDto>>(200, "Tipos de despesa", "Definições atuais.", context.Request.Path,
                    await mediator.Send(new ListExpenseTypesQuery(user.SessionIdentity()), ct))))
                .WithSummary("Lista tipos parametrizados da casa.").Produces<SuccessDetails<IReadOnlyList<ExpenseTypeDto>>>();
            group.MapGet("/{code:guid}/versions/{version:int}", async (Guid code, int version, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<ExpenseTypeDto>(200, "Versão", "Definição preservada.", context.Request.Path,
                    await mediator.Send(new GetExpenseTypeVersionQuery(user.SessionIdentity(), code, version), ct))))
                .WithSummary("Consulta uma definição histórica, sem reinterpretá-la.").Produces<SuccessDetails<ExpenseTypeDto>>().ProducesProblem(404);
            group.MapPost("", async (ExpenseTypePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Json(new SuccessDetails<ExpenseTypeDto>(201, "Tipo criado", "Definição salva.", context.Request.Path,
                    await mediator.Send(new SaveExpenseTypeCommand(user.SessionIdentity(), null, payload), ct)), statusCode: 201))
                .WithSummary("Cria tipo com campos tipados. Não gera obrigações financeiras nesta etapa.")
                .WithDescription("| Propriedade | Descrição |\n|---|---|\n| name | Nome até 160 caracteres. |\n| behavior | single, recurring, installments ou card. |\n| fields | Até 30 campos text, decimal, date, boolean ou list. |\n| defaultCategoryCode | Categoria da mesma casa, opcional. |")
                .Produces<SuccessDetails<ExpenseTypeDto>>(201).ProducesProblem(400).ProducesProblem(403);
            group.MapPut("/{code:guid}", async (Guid code, ExpenseTypePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Results.Ok(new SuccessDetails<ExpenseTypeDto>(200, "Tipo atualizado", "Nova versão salva.", context.Request.Path,
                    await mediator.Send(new SaveExpenseTypeCommand(user.SessionIdentity(), code, payload), ct))))
                .WithSummary("Cria nova versão de definição; exige a versão atual.").Produces<SuccessDetails<ExpenseTypeDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(409);
            group.MapPost("/{code:guid}/validate", async (Guid code, ValidateFieldsPayload payload, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new ValidateFieldsCommand(user.SessionIdentity(), code, payload), ct); return Results.NoContent();
            }).WithSummary("Valida campos contra a versão informada, sem criar despesa.").Produces(204).ProducesProblem(400).ProducesProblem(404);
            return endpoints;
        }
    }
}
