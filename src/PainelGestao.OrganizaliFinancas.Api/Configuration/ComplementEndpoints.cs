using System.Security.Claims;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Attachments;
using PainelGestao.OrganizaliFinancas.Application.Goals;
using PainelGestao.OrganizaliFinancas.Application.Imports;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    /// <summary>M9/M10 complements: reserve goals, private receipts and statement import with preview.</summary>
    public static class ComplementEndpoints
    {
        private static Guid IdempotencyKey(HttpContext context) =>
            Guid.TryParse(context.Request.Headers["Idempotency-Key"].ToString(), out var key) ? key : Guid.Empty;
        private static IResult Ok<T>(HttpContext context, string title, string detail, T data) =>
            Results.Ok(new SuccessDetails<T>(200, title, detail, context.Request.Path, data));
        private static IResult Created<T>(HttpContext context, string title, string detail, T data) =>
            Results.Json(new SuccessDetails<T>(201, title, detail, context.Request.Path, data), statusCode: 201);

        public static IEndpointRouteBuilder MapComplementEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var goals = endpoints.MapGroup("/api/goals").WithTags("Metas de reserva").RequireAuthorization();
            goals.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Metas", "Progresso pelo saldo da conta vinculada.", await mediator.Send(new ListGoalsQuery(user.SessionIdentity()), ct)))
                .WithSummary("Metas de reserva com valor alvo, prazo, conta vinculada e progresso. Reservar não cria renda.").Produces<SuccessDetails<IReadOnlyList<GoalDto>>>();
            goals.MapPost("", async (GoalPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Meta criada", "Meta de reserva salva.", await mediator.Send(new SaveGoalCommand(user.SessionIdentity(), null, payload), ct)))
                .WithSummary("Cria meta de reserva. Titular ou editor.")
                .WithDescription("| Propriedade | Tipo | Descrição |\n|---|---|---|\n| name | string | Nome até 160. |\n| targetAmount | string | Valor alvo, ex. \"5000.00\". |\n| targetDate | date? | Prazo opcional. |\n| accountCode | UUID | Conta onde a reserva fica. |\n| note | string? | Observação até 300. |")
                .Produces<SuccessDetails<GoalDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404);
            goals.MapPut("/{code:guid}", async (Guid code, GoalPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Meta atualizada", "Meta de reserva salva.", await mediator.Send(new SaveGoalCommand(user.SessionIdentity(), code, payload), ct)))
                .WithSummary("Edita ou encerra a meta; exige a versão atual.").Produces<SuccessDetails<GoalDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

            var attachments = endpoints.MapGroup("/api/attachments").WithTags("Comprovantes").RequireAuthorization();
            attachments.MapGet("", async (string kind, Guid target, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Comprovantes", "Arquivos do lançamento.", await mediator.Send(new ListAttachmentsQuery(user.SessionIdentity(), kind, target), ct)))
                .WithSummary("Lista comprovantes de uma despesa (`expense`), pagamento (`payment`) ou recebimento (`receipt`).").Produces<SuccessDetails<IReadOnlyList<AttachmentDto>>>();
            attachments.MapPost("", async (string kind, Guid target, IFormFile file, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();
                return Created(context, "Comprovante anexado", "Arquivo guardado de forma privada.", await mediator.Send(
                    new UploadAttachmentCommand(user.SessionIdentity(), kind, target, file.FileName, file.ContentType, file.Length, stream), ct));
            }).DisableAntiforgery().WithSummary("Anexa PDF ou imagem de até 10 MB (multipart, campo `file`). Titular ou editor.")
                .Produces<SuccessDetails<AttachmentDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            attachments.MapGet("/{code:guid}/content", async (Guid code, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(new GetAttachmentContentQuery(user.SessionIdentity(), code), ct);
                return Results.File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: false);
            }).WithSummary("Baixa o arquivo; só a casa dona do lançamento alcança o comprovante.").Produces(200).ProducesProblem(404);
            attachments.MapDelete("/{code:guid}", async (Guid code, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteAttachmentCommand(user.SessionIdentity(), code), ct);
                return Results.NoContent();
            }).WithSummary("Remove o comprovante (exclusão lógica do registro e do arquivo). Titular ou editor.").Produces(204).ProducesProblem(403).ProducesProblem(404);

            var imports = endpoints.MapGroup("/api/imports").WithTags("Importação de extrato").RequireAuthorization();
            imports.MapGet("", async (ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Ok(context, "Importações", "Arquivos já importados.", await mediator.Send(new ListImportsQuery(user.SessionIdentity()), ct)))
                .WithSummary("Histórico das importações da casa (últimas 50).").Produces<SuccessDetails<IReadOnlyList<ImportBatchDto>>>();
            imports.MapPost("/preview", async ([FromForm] Guid accountCode, IFormFile file, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
            {
                if (file.Length > 4 * 1024 * 1024) return Results.Problem(statusCode: 400, title: "Arquivo grande", detail: "O extrato precisa ter até 4 MB.");
                string content;
                await using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    content = await reader.ReadToEndAsync(ct);
                if (content.Contains('�'))
                {
                    // Bank exports in Windows-1252 (common for OFX) are re-read with that code page.
                    await using var again = file.OpenReadStream();
                    using var latin = new StreamReader(again, Encoding.Latin1);
                    content = await latin.ReadToEndAsync(ct);
                }
                return Ok(context, "Prévia", "Nada foi gravado. Revise linha a linha e confirme.", await mediator.Send(new PreviewImportCommand(user.SessionIdentity(), accountCode, file.FileName, content), ct));
            }).DisableAntiforgery().WithSummary("Lê um extrato OFX ou CSV (multipart: `file` e `accountCode`) e devolve a prévia com duplicidades e sugestões. Nada é gravado.")
                .Produces<SuccessDetails<ImportPreviewDto>>().ProducesProblem(400).ProducesProblem(403).ProducesProblem(404);
            imports.MapPost("", async (ImportPayload payload, ClaimsPrincipal user, IMediator mediator, HttpContext context, CancellationToken ct) =>
                Created(context, "Importação concluída", "Lançamentos gravados conforme a confirmação.", await mediator.Send(new ApplyImportCommand(user.SessionIdentity(), IdempotencyKey(context), payload), ct)))
                .WithSummary("Confirma a prévia: cada linha vira renda, despesa paga, pagamento de despesa existente ou é ignorada. Tudo ou nada; exige `Idempotency-Key`.")
                .Produces<SuccessDetails<ImportResultDto>>(201).ProducesProblem(400).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
            return endpoints;
        }
    }
}
