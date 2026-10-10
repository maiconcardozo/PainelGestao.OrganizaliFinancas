using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Accounts;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Application.Income;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class FinanceEndpoints
    {
        private const string KeyHelp = "Exige cabeçalho `Idempotency-Key` (UUID). Repetir a mesma chave e o mesmo corpo devolve o resultado original; outro corpo devolve 409.";
        private const string MoneyHelp = "Valores monetários trafegam como texto decimal invariável, por exemplo `\"1250.50\"`.";
        /// <summary>Missing or malformed keys become Guid.Empty and are rejected by the validators.</summary>
        private static Guid IdempotencyKey(this HttpContext context) =>
            Guid.TryParse(context.Request.Headers["Idempotency-Key"].ToString(), out var key) ? key : Guid.Empty;
        private static IResult Ok<T>(HttpContext context, string title, string detail, T data) =>
            Results.Ok(new SuccessDetails<T>(200, title, detail, context.Request.Path, data));
        private static IResult Created<T>(HttpContext context, string title, string detail, T data) =>
            Results.Json(new SuccessDetails<T>(201, title, detail, context.Request.Path, data), statusCode: 201);

        public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var accounts = endpoints.MapGroup("/api/accounts").WithTags("Contas").RequireAuthorization();
            accounts.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Contas", "Saldos recompostos pelo extrato.", await mediator.Send(new ListAccountsQuery(user.SessionIdentity()), ct)))
                .WithSummary("Lista contas com saldo atual (saldo inicial + movimentos efetivos até hoje) e total consolidado da casa.")
                .Produces<SuccessDetails<AccountsOverviewDto>>().ProducesProblem(403);
            accounts.MapPost("", async (AccountPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Conta criada", "Conta salva.", await mediator.Send(new SaveAccountCommand(user.SessionIdentity(), null, payload), ct)))
                .WithSummary("Cria conta com data e saldo inicial. Titular ou editor.")
                .WithDescription("| Propriedade | Tipo | Descrição |\n|---|---|---|\n| name | string | Até 160 caracteres. |\n| openingDate | date | Movimentos anteriores são recusados. |\n| openingBalance | string | Pode ser negativo. " + MoneyHelp + " |\n| isActive | bool | true na criação. |\n| version | int | Zero na criação. |")
                .Produces<SuccessDetails<AccountDto>>(201).ProducesProblem(400).ProducesProblem(403);
            accounts.MapPut("/{code:guid}", async (Guid code, AccountPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Conta atualizada", "Conta salva.", await mediator.Send(new SaveAccountCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Renomeia ou inativa; data e saldo inicial só mudam enquanto não houver movimentos.")
                .Produces<SuccessDetails<AccountDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            accounts.MapGet("/{code:guid}/statement", async (Guid code, DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Extrato", "Saldo anterior, movimentos e saldo final.", await mediator.Send(new GetStatementQuery(user.SessionIdentity(), code, from, to), ct)))
                .WithSummary("Extrato do período (padrão: mês atual; até 366 dias) com saldo corrido.")
                .Produces<SuccessDetails<StatementDto>>().ProducesProblem(400).ProducesProblem(404);

            var transfers = endpoints.MapGroup("/api/transfers").WithTags("Transferências").RequireAuthorization();
            transfers.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Transferências", "Últimas 200 transferências.", await mediator.Send(new ListTransfersQuery(user.SessionIdentity()), ct)))
                .WithSummary("Lista transferências entre contas da casa.").Produces<SuccessDetails<IReadOnlyList<TransferDto>>>();
            transfers.MapPost("", async (TransferPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Transferência registrada", "Débito e crédito gravados juntos.",
                    await mediator.Send(new CreateTransferCommand(user.SessionIdentity(), context.IdempotencyKey(), payload), ct)))
                .WithSummary("Transfere entre contas da casa: não é renda nem despesa e não altera o total consolidado.")
                .WithDescription(KeyHelp + " " + MoneyHelp)
                .Produces<SuccessDetails<TransferDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            transfers.MapPost("/{code:guid}/reversals", async (Guid code, ReversalPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Transferência estornada", "Movimentos compensatórios gravados; os originais foram mantidos.",
                    await mediator.Send(new ReverseTransferCommand(user.SessionIdentity(), context.IdempotencyKey(), code, payload), ct)))
                .WithSummary("Estorna transferência com justificativa. Somente titular.").WithDescription(KeyHelp)
                .Produces<SuccessDetails<TransferDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

            var sources = endpoints.MapGroup("/api/income-sources").WithTags("Fontes de renda").RequireAuthorization();
            sources.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Fontes de renda", "Vigência atual e média mensal.", await mediator.Send(new ListIncomeSourcesQuery(user.SessionIdentity()), ct)))
                .WithSummary("Lista fontes com a vigência atual e a média mensal do ano (meses fechados).")
                .Produces<SuccessDetails<IReadOnlyList<IncomeSourceDto>>>();
            sources.MapGet("/{code:guid}/versions", async (Guid code, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Vigências", "Histórico imutável.", await mediator.Send(new ListIncomeSourceVersionsQuery(user.SessionIdentity(), code), ct)))
                .WithSummary("Histórico de vigências da fonte.").Produces<SuccessDetails<IReadOnlyList<IncomeSourceVersionDto>>>().ProducesProblem(404);
            const string sourceHelp = "| Propriedade | Tipo | Descrição |\n|---|---|---|\n| personCode | UUID | Morador da casa. |\n| frequency | string | weekly, biweekly (14 dias), monthly, quarterly, semiannual, annual. |\n| amountMode | string | fixed (líquido esperado) ou variable (média dos recebimentos). |\n| expectedNetAmount | string? | Obrigatório em fixed. |\n| grossAmount | string? | Opcional; maior ou igual ao líquido. |\n| validFrom | date | Primeira ocorrência e início da vigência. |";
            sources.MapPost("", async (IncomeSourcePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Fonte criada", "Previsões geradas até o horizonte.", await mediator.Send(new SaveIncomeSourceCommand(user.SessionIdentity(), null, payload), ct)))
                .WithSummary("Cria fonte de renda e gera as previsões. Titular ou editor.").WithDescription(sourceHelp)
                .Produces<SuccessDetails<IncomeSourceDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404);
            sources.MapPut("/{code:guid}", async (Guid code, IncomeSourcePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Nova vigência", "Previsões futuras sem recebimento foram realinhadas.", await mediator.Send(new SaveIncomeSourceCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Grava nova vigência; não reescreve previsões anteriores nem as que já têm recebimento.").WithDescription(sourceHelp)
                .Produces<SuccessDetails<IncomeSourceDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

            var forecasts = endpoints.MapGroup("/api/income-forecasts").WithTags("Previsões de renda").RequireAuthorization();
            forecasts.MapGet("", async (DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Previsões", "Previsto, realizado, diferença e residual.", await mediator.Send(new ListForecastsQuery(user.SessionIdentity(), from, to), ct)))
                .WithSummary("Lista previsões por competência (padrão: mês atual e dois seguintes; até 24 meses).")
                .Produces<SuccessDetails<IReadOnlyList<ForecastDto>>>().ProducesProblem(400);
            forecasts.MapPost("/generate", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Previsões atualizadas", "Geração idempotente concluída.", await mediator.Send(new GenerateForecastsCommand(user.SessionIdentity()), ct)))
                .WithSummary("Avança o horizonte (mês atual + 12) para todas as fontes; repetir não duplica.")
                .Produces<SuccessDetails<ForecastGenerationDto>>().ProducesProblem(403);
            forecasts.MapPost("/{code:guid}/close", async (Guid code, CloseForecastPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Previsão encerrada", "Residual retirado da projeção.", await mediator.Send(new CloseForecastCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Encerra previsão realizada a menor, com motivo.")
                .Produces<SuccessDetails<IReadOnlyList<ForecastDto>>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);

            var receipts = endpoints.MapGroup("/api/receipts").WithTags("Recebimentos").RequireAuthorization();
            receipts.MapGet("", async (DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Recebimentos", "Entradas efetivas.", await mediator.Send(new ListReceiptsQuery(user.SessionIdentity(), from, to), ct)))
                .WithSummary("Lista recebimentos (padrão: ano atual até hoje).").Produces<SuccessDetails<IReadOnlyList<ReceiptDto>>>().ProducesProblem(400);
            receipts.MapPost("", async (ReceiptPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Recebimento registrado", "Movimento, alocações e chave gravados juntos.",
                    await mediator.Send(new CreateReceiptCommand(user.SessionIdentity(), context.IdempotencyKey(), payload), ct)))
                .WithSummary("Registra entrada real em uma conta, com ou sem previsão. A soma alocada não supera o recebido.")
                .WithDescription(KeyHelp + " " + MoneyHelp + " Valor acima do previsto é aceito e aparece como diferença favorável.")
                .Produces<SuccessDetails<ReceiptDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            receipts.MapPost("/{code:guid}/reversals", async (Guid code, ReversalPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Recebimento estornado", "Movimento compensatório gravado; o original foi mantido.",
                    await mediator.Send(new ReverseReceiptCommand(user.SessionIdentity(), context.IdempotencyKey(), code, payload), ct)))
                .WithSummary("Estorna recebimento com justificativa. Somente titular.").WithDescription(KeyHelp)
                .Produces<SuccessDetails<ReceiptDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

            endpoints.MapGet("/api/income/summary", async (int? year, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Resumo de renda", "Média anual e comparativo mensal.", await mediator.Send(new GetIncomeSummaryQuery(user.SessionIdentity(), year), ct)))
                .WithTags("Recebimentos").RequireAuthorization()
                .WithSummary("Média mensal (ano civil, meses fechados) por pessoa, fonte e casa, e previsto x recebido por mês.")
                .Produces<SuccessDetails<IncomeSummaryDto>>().ProducesProblem(400);
            return endpoints;
        }
    }
}
