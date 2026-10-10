using System.Security.Claims;
using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Cards;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class CardEndpoints
    {
        private const string KeyHelp = "Exige cabeçalho `Idempotency-Key` (UUID). Repetir a mesma chave e o mesmo corpo devolve o resultado original; outro corpo devolve 409.";
        private static Guid IdempotencyKey(HttpContext context) =>
            Guid.TryParse(context.Request.Headers["Idempotency-Key"].ToString(), out var key) ? key : Guid.Empty;
        private static IResult Ok<T>(HttpContext context, string title, string detail, T data) =>
            Results.Ok(new SuccessDetails<T>(200, title, detail, context.Request.Path, data));
        private static IResult Created<T>(HttpContext context, string title, string detail, T data) =>
            Results.Json(new SuccessDetails<T>(201, title, detail, context.Request.Path, data), statusCode: 201);

        public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var cards = endpoints.MapGroup("/api/cards").WithTags("Cartões").RequireAuthorization();
            cards.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Cartões", "Limite, comprometido e disponível estimado.", await mediator.Send(new ListCardsQuery(user.SessionIdentity()), ct)))
                .WithSummary("Lista cartões com o comprometido (faturas em aberto, inclusive parcelas futuras) e o disponível estimado.")
                .Produces<SuccessDetails<IReadOnlyList<CardDto>>>();
            const string cardHelp = "| Propriedade | Descrição |\n|---|---|\n| name | Apelido, sem o número do cartão (nunca PAN/CVV). |\n| closingDay / dueDay | Dias de fechamento e vencimento (1–31; meses curtos usam o último dia). |\n| closingDayRule | `next` (padrão): compra no dia do fechamento entra na próxima fatura; `current`: entra na fatura que fecha no dia. |\n| creditLimit | Opcional, texto decimal invariável. |";
            cards.MapPost("", async (CardPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Cartão cadastrado", "Cartão salvo.", await mediator.Send(new SaveCardCommand(user.SessionIdentity(), null, payload), ct)))
                .WithSummary("Cadastra cartão com titular, fechamento, vencimento e limite. Titular ou editor.").WithDescription(cardHelp)
                .Produces<SuccessDetails<CardDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            cards.MapPut("/{code:guid}", async (Guid code, CardPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Cartão atualizado", "Novos dias valem para as próximas faturas.", await mediator.Send(new SaveCardCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Altera o cartão; faturas já criadas mantêm datas e itens.").WithDescription(cardHelp)
                .Produces<SuccessDetails<CardDto>>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(409);

            var invoices = endpoints.MapGroup("/api/card-invoices").WithTags("Faturas").RequireAuthorization();
            invoices.MapGet("", async (Guid? cardCode, DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Faturas", "Valor, pago e em aberto por fatura.", await mediator.Send(new ListInvoicesQuery(user.SessionIdentity(), cardCode, from, to), ct)))
                .WithSummary("Lista faturas por mês de vencimento (padrão: dois meses atrás até 12 à frente). O pagamento usa `POST /api/payments` com o `expenseCode` da fatura.")
                .Produces<SuccessDetails<IReadOnlyList<InvoiceDto>>>().ProducesProblem(400);
            invoices.MapGet("/{code:guid}", async (Guid code, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Fatura", "Itens da fatura.", await mediator.Send(new GetInvoiceQuery(user.SessionIdentity(), code), ct)))
                .WithSummary("Fatura com compras, parcelas e créditos de estorno.").Produces<SuccessDetails<InvoiceDetailDto>>().ProducesProblem(404);

            var purchases = endpoints.MapGroup("/api/card-purchases").WithTags("Compras no cartão").RequireAuthorization();
            purchases.MapGet("", async (DateOnly? from, DateOnly? to, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Compras", "Compras no cartão.", await mediator.Send(new ListPurchasesQuery(user.SessionIdentity(), from, to), ct)))
                .WithSummary("Lista compras (padrão: dois meses anteriores até hoje).").Produces<SuccessDetails<IReadOnlyList<PurchaseDto>>>().ProducesProblem(400);
            purchases.MapPost("/preview", async (PurchasePreviewPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Prévia", "Nada foi gravado.", await mediator.Send(new PreviewPurchaseQuery(user.SessionIdentity(), payload), ct)))
                .WithSummary("Mostra em quais faturas caem as parcelas; `firstInvoiceShift` −1/0/+1 corrige a primeira fatura.")
                .Produces<SuccessDetails<PurchasePreviewDto>>().ProducesProblem(400).ProducesProblem(404);
            purchases.MapPost("", async (PurchasePayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Compra lançada", "Parcelas incluídas nas faturas; nenhuma conta foi movimentada.",
                    await mediator.Send(new CreatePurchaseCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Lança compra à vista ou parcelada no cartão. Não reduz saldo bancário; a fatura é paga depois, uma única vez.").WithDescription(KeyHelp)
                .Produces<SuccessDetails<PurchaseDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            purchases.MapPost("/{code:guid}/refunds", async (Guid code, RefundPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Estorno registrado", "Parcelas canceladas ou crédito lançado na fatura.",
                    await mediator.Send(new RefundPurchaseCommand(user.SessionIdentity(), IdempotencyKey(context), code, payload), ct)))
                .WithSummary("Estorno do estabelecimento (total ou parcial): cancela parcelas ainda abertas e credita o restante; sem entrada bancária.").WithDescription(KeyHelp)
                .Produces<SuccessDetails<PurchaseDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            return endpoints;
        }
    }
}
