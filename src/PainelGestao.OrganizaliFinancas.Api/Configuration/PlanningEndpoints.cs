using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Planning;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class PlanningEndpoints
    {
        private static IResult Ok<T>(HttpContext context, string title, string detail, T data) =>
            Results.Ok(new SuccessDetails<T>(200, title, detail, context.Request.Path, data));

        public static IEndpointRouteBuilder MapPlanningEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api").WithTags("Painel, fluxo e orçamento").RequireAuthorization();
            group.MapGet("/dashboard", async (DateOnly? month, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Painel", "Saldo, mês, projeção, orçamento, agenda e alertas.", await mediator.Send(new GetDashboardQuery(user.SessionIdentity(), month), ct)))
                .WithSummary("Painel da casa: saldo real, previsto × realizado, projeção de 60 dias com primeira data de déficit, orçamento, agenda e alertas.")
                .Produces<SuccessDetails<DashboardDto>>();
            group.MapGet("/cash-flow", async (DateOnly? month, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Fluxo de caixa", "Realizado até hoje e projetado depois.", await mediator.Send(new GetCashFlowQuery(user.SessionIdentity(), month), ct)))
                .WithSummary("Fluxo diário do mês e série mensal (cinco meses atrás, seis à frente). Transferências entre contas da casa não entram.")
                .Produces<SuccessDetails<CashFlowDto>>().ProducesProblem(400);
            group.MapGet("/projection", async (int? days, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Saldo projetado", "A projeção não altera o saldo real.", await mediator.Send(new GetProjectionQuery(user.SessionIdentity(), days), ct)))
                .WithSummary("Saldo projetado dia a dia (padrão 60 dias), com eventos de origem e primeira data de déficit.")
                .Produces<SuccessDetails<ProjectionDto>>().ProducesProblem(400);
            group.MapGet("/agenda", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Agenda", "Vencidos, hoje, próximos 7 e 30 dias.", await mediator.Send(new GetAgendaQuery(user.SessionIdentity()), ct)))
                .WithSummary("Agenda financeira: contas, faturas e rendas previstas, com valores estimados sinalizados.").Produces<SuccessDetails<AgendaDto>>();
            group.MapGet("/budgets", async (DateOnly? month, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Orçamento", "Limite, comprometido e pago por categoria.", await mediator.Send(new GetBudgetQuery(user.SessionIdentity(), month), ct)))
                .WithSummary("Orçamento do mês por categoria. Categoria pai soma as filhas; o total usa só o nível mais alto com limite.")
                .Produces<SuccessDetails<BudgetDto>>();
            group.MapPut("/budgets/{month}", async (DateOnly month, SaveBudgetsPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Orçamento salvo", "Limites atualizados.", await mediator.Send(new SaveBudgetsCommand(user.SessionIdentity(), month, payload), ct)))
                .WithSummary("Grava limites do mês (`amount` nulo remove: “Sem orçamento”; zero é limite válido). Titular ou editor.")
                .Produces<SuccessDetails<BudgetDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            group.MapGet("/alert-preferences", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Preferências de alerta", "Limiares da casa.", await mediator.Send(new GetAlertPreferenceQuery(user.SessionIdentity()), ct)))
                .WithSummary("Limiares de aproximação e limite do orçamento e dias de antecedência dos vencimentos.").Produces<SuccessDetails<AlertPreferenceDto>>();
            group.MapPut("/alert-preferences", async (AlertPreferencePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Preferências salvas", "Limiares atualizados.", await mediator.Send(new SaveAlertPreferenceCommand(user.SessionIdentity(), payload), ct)))
                .WithSummary("Altera os limiares (padrão 80% e 100%). Titular ou editor.").Produces<SuccessDetails<AlertPreferenceDto>>().ProducesProblem(400).ProducesProblem(409);
            group.MapGet("/alerts", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Alertas", "Condições ativas.", await mediator.Send(new ListAlertsQuery(user.SessionIdentity()), ct)))
                .WithSummary("Central de alertas: orçamento, déficit projetado, vencidos, vencimentos e faturas próximas, renda em atraso e valores estimados.")
                .Produces<SuccessDetails<IReadOnlyList<AlertDto>>>();
            group.MapPost("/alerts/actions", async (AlertActionPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Alerta atualizado", "A causa do alerta não foi alterada.", await mediator.Send(new AlertActionCommand(user.SessionIdentity(), payload), ct)))
                .WithSummary("Marca como lido, adia (`days`) ou resolve. O alerta só volta se a condição mudar de faixa ou versão.")
                .Produces<SuccessDetails<IReadOnlyList<AlertDto>>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404);
            group.MapGet("/analysis", async (DateOnly? month, Guid? categoryCode, Guid? personCode, Guid? accountCode, string? status, ClaimsPrincipal user,
                    IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Análise", "Mesmo total no gráfico e na lista.",
                    await mediator.Send(new GetAnalysisQuery(user.SessionIdentity(), month, categoryCode, personCode, accountCode, status), ct)))
                .WithSummary("Despesas da competência filtradas por categoria (inclui subcategorias), morador responsável, conta que pagou e situação.")
                .Produces<SuccessDetails<AnalysisDto>>().ProducesProblem(400);
            return endpoints;
        }
    }
}
