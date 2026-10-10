using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Finance;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class ExpenseEndpoints
    {
        private const string KeyHelp = "Exige cabeçalho `Idempotency-Key` (UUID). Repetir a mesma chave e o mesmo corpo devolve o resultado original; outro corpo devolve 409.";
        private const string DetailsHelp = "`details`: descrição, tipo de despesa e versão (os campos adicionais são validados pela versão), categoria, credor, forma de pagamento, pagador previsto e rateio opcional (`splitMode` value|percent e `shares` com `personCode` e `value`). Valores monetários são texto decimal invariável, por exemplo `\"1250.50\"`.";
        private static Guid IdempotencyKey(HttpContext context) =>
            Guid.TryParse(context.Request.Headers["Idempotency-Key"].ToString(), out var key) ? key : Guid.Empty;
        private static IResult Ok<T>(HttpContext context, string title, string detail, T data) =>
            Results.Ok(new SuccessDetails<T>(200, title, detail, context.Request.Path, data));
        private static IResult Created<T>(HttpContext context, string title, string detail, T data) =>
            Results.Json(new SuccessDetails<T>(201, title, detail, context.Request.Path, data), statusCode: 201);

        public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var expenses = endpoints.MapGroup("/api/expenses").WithTags("Despesas").RequireAuthorization();
            expenses.MapGet("", async (DateOnly? from, DateOnly? to, bool? includeOverdue, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Despesas", "Vencimentos do período e vencidas em aberto.", await mediator.Send(new ListExpensesQuery(user.SessionIdentity(), from, to, includeOverdue ?? true), ct)))
                .WithSummary("Lista despesas por vencimento (padrão: mês atual; até 366 dias) e, por padrão, as vencidas ainda abertas de antes do período.")
                .Produces<SuccessDetails<ExpenseListDto>>().ProducesProblem(400);
            expenses.MapGet("/{code:guid}", async (Guid code, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Despesa", "Saldo aberto, ajustes e pagamentos.", await mediator.Send(new GetExpenseQuery(user.SessionIdentity(), code), ct)))
                .WithSummary("Detalhe com juros, multa, desconto, ajustes e pagamentos (inclusive estornados).")
                .Produces<SuccessDetails<ExpenseDetailDto>>().ProducesProblem(404);
            expenses.MapPost("", async (ExpensePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Despesa lançada", "Obrigação registrada.", await mediator.Send(new CreateExpenseCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Lança conta avulsa com competência, vencimento, credor, pagador e responsáveis. Titular ou editor.")
                .WithDescription(KeyHelp + " " + DetailsHelp + " `payment` opcional registra a quitação imediata na mesma transação (exige permissão de liquidar).")
                .Produces<SuccessDetails<ExpenseDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            expenses.MapPut("/{code:guid}", async (Guid code, ExpensePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Despesa atualizada", "Somente esta ocorrência foi alterada.", await mediator.Send(new UpdateExpenseCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Altera esta despesa. Ocorrência de recorrência passa a ser individual; com pagamento, o valor principal fica travado.")
                .WithDescription(DetailsHelp).Produces<SuccessDetails<ExpenseDto>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
            expenses.MapPost("/{code:guid}/cancel", async (Guid code, CancelExpensePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Despesa cancelada", "Saldo futuro cancelado; histórico mantido.", await mediator.Send(new CancelExpenseCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Cancela despesa sem pagamento ativo, com motivo. Paga exige estornar o pagamento antes.")
                .Produces<SuccessDetails<ExpenseDto>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
            expenses.MapPost("/{code:guid}/adjustments", async (Guid code, AdjustmentPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Ajuste registrado", "Saldo aberto recalculado.", await mediator.Send(new AddAdjustmentCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Registra juros, multa, desconto ou ajuste (com sinal) com justificativa. Não movimenta conta.")
                .WithDescription("| kind | Efeito |\n|---|---|\n| interest | Soma ao saldo aberto |\n| fine | Soma ao saldo aberto |\n| discount | Valor positivo que reduz o saldo |\n| adjustment | Valor com sinal |")
                .Produces<SuccessDetails<ExpenseDetailDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            expenses.MapGet("/contributions", async (DateOnly? month, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Rateio do mês", "Responsabilidade, pagamentos e acerto sugerido.", await mediator.Send(new GetContributionsQuery(user.SessionIdentity(), month), ct)))
                .WithSummary("Por competência: cota de cada morador nas despesas rateadas, quanto cada um pagou e o acerto interno sugerido (não é nova despesa).")
                .Produces<SuccessDetails<ContributionsDto>>();

            var payments = endpoints.MapGroup("/api/payments").WithTags("Pagamentos").RequireAuthorization();
            payments.MapGet("", async (DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Pagamentos", "Saídas efetivas.", await mediator.Send(new ListPaymentsQuery(user.SessionIdentity(), from, to), ct)))
                .WithSummary("Lista pagamentos (padrão: dois meses anteriores até hoje).").Produces<SuccessDetails<IReadOnlyList<PaymentDto>>>().ProducesProblem(400);
            payments.MapPost("", async (PaymentPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Pagamento registrado", "Saída, alocações e ajustes gravados juntos.", await mediator.Send(new CreatePaymentCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Paga total ou parcialmente uma ou mais despesas a partir de uma conta. Juros, multa e desconto ficam separados. Titular ou editor.")
                .WithDescription(KeyHelp + " Cada alocação não pode superar o saldo aberto (depois dos ajustes informados). O valor do pagamento é a soma das alocações.")
                .Produces<SuccessDetails<PaymentDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            payments.MapPost("/{code:guid}/reversals", async (Guid code, ReversalPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Pagamento estornado", "Saldo da conta e da despesa restaurados; o original foi mantido.",
                    await mediator.Send(new ReversePaymentCommand(user.SessionIdentity(), IdempotencyKey(context), code, payload), ct)))
                .WithSummary("Estorna pagamento com justificativa. Somente titular.").WithDescription(KeyHelp)
                .Produces<SuccessDetails<PaymentDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

            var recurrences = endpoints.MapGroup("/api/expense-recurrences").WithTags("Despesas recorrentes").RequireAuthorization();
            recurrences.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Recorrências", "Regras vigentes.", await mediator.Send(new ListRecurrencesQuery(user.SessionIdentity()), ct)))
                .WithSummary("Lista contas recorrentes com a vigência atual.").Produces<SuccessDetails<IReadOnlyList<RecurrenceDto>>>();
            recurrences.MapPost("/preview", async (RecurrenceTermsPayload payload, Guid? code, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Prévia", "Nada foi gravado.", await mediator.Send(new PreviewRecurrenceCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Prévia de criação (sem `code`) ou de “esta e próximas” (com `code`): ocorrências novas, alteradas, canceladas e preservadas.")
                .Produces<SuccessDetails<RecurrencePlanDto>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
            recurrences.MapPost("", async (RecurrencePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Recorrência criada", "Ocorrências geradas até o horizonte.", await mediator.Send(new CreateRecurrenceCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Cria conta recorrente (valor fixo ou estimado) e gera as ocorrências até o mês atual + 12.")
                .WithDescription(KeyHelp + " " + DetailsHelp + " Rateio de recorrência é sempre por percentual.")
                .Produces<SuccessDetails<RecurrenceSaveDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            recurrences.MapPut("/{code:guid}", async (Guid code, RecurrencePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Recorrência alterada", "Esta e próximas ocorrências realinhadas.", await mediator.Send(new UpdateRecurrenceCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("“Esta e próximas”: nova vigência a partir de `terms.validFrom`. Ocorrências pagas, ajustadas, canceladas ou editadas individualmente são preservadas.")
                .Produces<SuccessDetails<RecurrenceSaveDto>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);
            recurrences.MapPost("/generate", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Recorrências atualizadas", "Geração idempotente concluída.", await mediator.Send(new GenerateRecurrencesCommand(user.SessionIdentity()), ct)))
                .WithSummary("Avança o horizonte de todas as recorrências; repetir não duplica.").Produces<SuccessDetails<RecurrencePlanDto>>().ProducesProblem(403);

            var installments = endpoints.MapGroup("/api/expense-installments").WithTags("Compras parceladas").RequireAuthorization();
            installments.MapPost("/preview", async (InstallmentPreviewPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Prévia das parcelas", "Nada foi gravado.", await mediator.Send(new PreviewInstallmentsQuery(user.SessionIdentity(), payload), ct)))
                .WithSummary("Mostra parcelas, datas e valores; o resíduo de centavos vai para a última (100 em 3 = 33,33 + 33,33 + 33,34).")
                .Produces<SuccessDetails<InstallmentPreviewDto>>().ProducesProblem(400);
            installments.MapPost("", async (InstallmentPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Compra parcelada lançada", "Uma despesa por parcela.", await mediator.Send(new CreateInstallmentsCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Cria a compra e suas parcelas, cada uma na competência do seu vencimento.").WithDescription(KeyHelp + " " + DetailsHelp)
                .Produces<SuccessDetails<InstallmentPlanDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            return endpoints;
        }
    }
}
