using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Imports
{
    public sealed record ImportCandidateDto(Guid Code, string Description, DateOnly DueDate, string OpenBalance);
    /// <summary>One statement line with what the system found: already imported, a movement with the same date and amount, and a suggestion.</summary>
    public sealed record ImportLineDto(int Index, DateOnly Date, string Amount, string Description, string? ExternalId, string Fingerprint, bool AlreadyImported,
        bool SimilarMovement, string SuggestedAction, Guid? SuggestedExpenseCode);
    public sealed record ImportPreviewDto(Guid AccountCode, string AccountName, string FileName, string Format, IReadOnlyList<ImportLineDto> Lines, IReadOnlyList<ImportCandidateDto> OpenExpenses);
    public sealed record ImportLinePayload(DateOnly Date, string Amount, string Description, string? ExternalId, string Fingerprint, string Action, Guid? ExpenseCode,
        Guid? PersonCode, Guid? CategoryCode, Guid? IncomeTypeCode);
    public sealed record ImportPayload(Guid AccountCode, string FileName, string Format, ImportLinePayload[] Lines);
    public sealed record ImportResultDto(Guid Code, int Applied, int Ignored, int Receipts, int Payments, int Expenses, string ReceiptsTotal, string PaymentsTotal, string ExpensesTotal);
    public sealed record ImportBatchDto(Guid Code, string FileName, string Format, string AccountName, int LineCount, int AppliedCount, int IgnoredCount, DateTime CreatedAt);

    public sealed record PreviewImportCommand(SessionIdentity Identity, Guid AccountCode, string FileName, string Content) : IRequest<ImportPreviewDto>;
    public sealed record ApplyImportCommand(SessionIdentity Identity, Guid Key, ImportPayload Payload) : IRequest<ImportResultDto>;
    public sealed record ListImportsQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<ImportBatchDto>>;

    public static class ImportActions
    {
        public const string Ignore = "ignore", Receipt = "receipt", Expense = "expense", Pay = "pay";
        public static readonly string[] All = [Ignore, Receipt, Expense, Pay];
    }
    public class PreviewImportValidator : AbstractValidator<PreviewImportCommand>
    {
        public PreviewImportValidator()
        {
            RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Content).NotEmpty().WithMessage("Envie um arquivo OFX ou CSV.").Must(x => x.Length <= 4 * 1024 * 1024).WithMessage("O arquivo precisa ter até 4 MB.");
        }
    }
    public class ApplyImportValidator : AbstractValidator<ApplyImportCommand>
    {
        public ApplyImportValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.FileName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Payload.Format).Must(x => x is "ofx" or "csv").WithMessage("Formato inválido.");
            RuleFor(x => x.Payload.Lines).NotEmpty().WithMessage("Nenhuma linha para importar.")
                .Must(lines => lines is null || lines.Length <= StatementParser.MaxLines).WithMessage($"Importe até {StatementParser.MaxLines} linhas por vez.");
            RuleForEach(x => x.Payload.Lines).ChildRules(line =>
            {
                line.RuleFor(x => x.Amount).SignedMoney().Must(x => x != "0" && x != "0.00").WithMessage("Valor inválido.");
                line.RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
                line.RuleFor(x => x.Fingerprint).NotEmpty().MaximumLength(40);
                line.RuleFor(x => x.Action).Must(ImportActions.All.Contains).WithMessage("Escolha ignorar, renda, despesa paga ou pagar despesa.");
                line.RuleFor(x => x.PersonCode).NotNull().When(x => x.Action == ImportActions.Receipt).WithMessage("Informe quem recebeu.");
                line.RuleFor(x => x.ExpenseCode).NotNull().When(x => x.Action == ImportActions.Pay).WithMessage("Escolha a despesa a pagar.");
            });
        }
    }

    internal static class ImportSupport
    {
        /// <summary>Open bills around today, as candidates for matching statement debits.</summary>
        public static async Task<List<(ObligationRecord Record, decimal Open)>> OpenExpensesAsync(IUnitOfWork work, int idTenant, DateOnly today, CancellationToken ct)
        {
            var records = await work.ExpenseRepository.ListAsync(idTenant, HouseholdCalendar.Day(today.AddDays(-90)), HouseholdCalendar.Day(today.AddDays(60)), true, ct);
            var result = new List<(ObligationRecord, decimal)>();
            foreach (var record in records)
            {
                if (record.Status == "cancelled") continue;
                var state = ObligationBalance.Of(record.Amount, record.Adjustments, record.Paid, false, DateOnly.FromDateTime(record.DueDate), today);
                if (state.OpenBalance > 0) result.Add((record, state.OpenBalance));
            }
            return result;
        }
    }

    public class PreviewImportHandler(IUnitOfWork work) : IRequestHandler<PreviewImportCommand, ImportPreviewDto>
    {
        public async Task<ImportPreviewDto> Handle(PreviewImportCommand request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var account = await work.AccountRepository.FindAsync(user.IdTenant, request.AccountCode, ct) ?? throw new NotFoundException("Conta não encontrada nesta casa.");
            var format = StatementParser.DetectFormat(request.FileName, request.Content);
            var parsed = StatementParser.Parse(format, request.Content);
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            var fingerprints = parsed.Select(x => StatementParser.Fingerprint(account.Code, x.Date, x.Amount, x.Description, x.ExternalId)).ToArray();
            var imported = (await work.ImportRepository.AppliedFingerprintsAsync(user.IdTenant, fingerprints.Distinct().ToArray(), ct)).ToHashSet(StringComparer.Ordinal);
            var first = parsed.Min(x => x.Date); var last = parsed.Max(x => x.Date);
            var movements = await work.AccountRepository.ListMovementsAsync(user.IdTenant, account.Id, HouseholdCalendar.Day(first), HouseholdCalendar.Day(last), ct);
            var open = await ImportSupport.OpenExpensesAsync(work, user.IdTenant, today, ct);
            var lines = parsed.Select((line, index) =>
            {
                var date = line.Date;
                var similar = movements.Any(m => m.Kind != "reversal" && DateOnly.FromDateTime(m.MovementDate) == date && m.Amount == line.Amount);
                Guid? suggestedExpense = null;
                var action = line.Amount > 0 ? ImportActions.Receipt : ImportActions.Expense;
                if (line.Amount < 0)
                {
                    var match = open.Where(x => x.Open == -line.Amount && Math.Abs(DateOnly.FromDateTime(x.Record.DueDate).DayNumber - date.DayNumber) <= 15)
                        .OrderBy(x => Math.Abs(DateOnly.FromDateTime(x.Record.DueDate).DayNumber - date.DayNumber)).FirstOrDefault();
                    if (match.Record is not null) { action = ImportActions.Pay; suggestedExpense = match.Record.Code; }
                }
                if (imported.Contains(fingerprints[index]) || (similar && date > today)) action = ImportActions.Ignore;
                else if (similar) action = ImportActions.Ignore;
                return new ImportLineDto(index, date, Money.Format(line.Amount), line.Description, line.ExternalId, fingerprints[index], imported.Contains(fingerprints[index]), similar, action, suggestedExpense);
            }).ToArray();
            var candidates = open.OrderBy(x => x.Record.DueDate).Select(x => new ImportCandidateDto(x.Record.Code, x.Record.Description, DateOnly.FromDateTime(x.Record.DueDate), Money.Format(x.Open))).ToArray();
            return new ImportPreviewDto(account.Code, account.Name, Path.GetFileName(request.FileName), format, lines, candidates);
        }
    }

    /// <summary>All or nothing: every line is validated first, then receipts, paid expenses and payments are written in one transaction with the batch.</summary>
    public class ApplyImportHandler(IUnitOfWork work) : IRequestHandler<ApplyImportCommand, ImportResultDto>
    {
        private const string Operation = "imports.apply";
        public Task<ImportResultDto> Handle(ApplyImportCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var payload = request.Payload;
            var hash = Idempotency.Hash(payload);
            if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing)
            {
                var previous = (await work.ImportRepository.ListAsync(user.IdTenant, ct)).FirstOrDefault(x => x.Code == existing) ?? throw new NotFoundException("Importação não encontrada.");
                return new ImportResultDto(previous.Code, previous.AppliedCount, previous.IgnoredCount, 0, 0, 0, "0.00", "0.00", "0.00");
            }
            if (payload.Lines.Any(x => x.Action is ImportActions.Receipt or ImportActions.Pay or ImportActions.Expense))
                await HouseholdAccess.RequireAsync(work, request.Identity, "payments.settle", ct);
            var actor = user.Code.ToString();
            var idTenant = user.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var account = await work.AccountRepository.FindAsync(idTenant, payload.AccountCode, ct) ?? throw new NotFoundException("Conta não encontrada nesta casa.");
            var errors = new Dictionary<string, string[]>();
            var active = payload.Lines.Where(x => x.Action != ImportActions.Ignore).ToArray();
            var duplicates = (await work.ImportRepository.AppliedFingerprintsAsync(idTenant, active.Select(x => x.Fingerprint).Distinct().ToArray(), ct)).ToHashSet(StringComparer.Ordinal);
            var open = await ImportSupport.OpenExpensesAsync(work, idTenant, today, ct);
            var allocated = new Dictionary<Guid, decimal>();
            var people = (await work.HouseholdRepository.ListPeopleAsync(idTenant, ct)).Where(x => x.IsActive).ToDictionary(x => x.Code);
            for (var index = 0; index < payload.Lines.Length; index++)
            {
                var line = payload.Lines[index];
                if (line.Action == ImportActions.Ignore) continue;
                var amount = Money.Parse(line.Amount);
                if (line.Date > today) errors[$"Lines[{index}]"] = ["Lançamento com data futura só pode ser ignorado."];
                else if (duplicates.Contains(line.Fingerprint)) errors[$"Lines[{index}]"] = ["Esta linha já foi importada antes."];
                else if (payload.Lines.Take(index).Any(x => x.Action != ImportActions.Ignore && x.Fingerprint == line.Fingerprint)) errors[$"Lines[{index}]"] = ["Linha repetida no arquivo."];
                else if (line.Action == ImportActions.Receipt && (amount <= 0 || line.PersonCode is null || !people.ContainsKey(line.PersonCode.Value)))
                    errors[$"Lines[{index}]"] = ["Entrada de renda exige valor positivo e um morador ativo."];
                else if (line.Action == ImportActions.Expense && amount >= 0) errors[$"Lines[{index}]"] = ["Despesa paga exige valor negativo (saída da conta)."];
                else if (line.Action == ImportActions.Pay)
                {
                    var target = open.FirstOrDefault(x => x.Record.Code == line.ExpenseCode);
                    if (amount >= 0 || target.Record is null) errors[$"Lines[{index}]"] = ["Escolha uma despesa em aberto para uma saída da conta."];
                    else
                    {
                        var already = allocated.GetValueOrDefault(target.Record.Code);
                        if (-amount + already > target.Open) errors[$"Lines[{index}]"] = [$"O valor supera o saldo em aberto de {target.Record.Description} ({Money.Format(target.Open - already)})."];
                        else allocated[target.Record.Code] = already + -amount;
                    }
                }
                if (line.Date < account.OpeningDay()) errors[$"Lines[{index}]"] = [$"Data anterior à abertura da conta ({account.OpeningDay():dd/MM/yyyy})."];
            }
            if (errors.Count > 0) throw new InputValidationException(errors);

            int receipts = 0, payments = 0, expenses = 0, ignored = 0; decimal receiptsTotal = 0, paymentsTotal = 0, expensesTotal = 0;
            var recorded = new List<ImportedLine>();
            foreach (var line in payload.Lines)
            {
                var amount = Money.Parse(line.Amount);
                Guid? result = null;
                switch (line.Action)
                {
                    case ImportActions.Ignore: ignored++; break;
                    case ImportActions.Receipt:
                    {
                        int? idType = null;
                        if (line.IncomeTypeCode is Guid typeCode)
                            idType = (await work.CatalogRepository.FindAsync(CatalogKind.IncomeTypes, idTenant, typeCode, ct) ?? throw new NotFoundException("Tipo de renda não encontrado.")).Id;
                        var receipt = Receipt.Create(account, people[line.PersonCode!.Value], null, idType, line.Date, amount, [], line.Description, actor);
                        work.IncomeRepository.AddReceipt(receipt);
                        work.AccountRepository.AddMovement(AccountMovement.Create(account, line.Date, amount, "receipt", line.Description, actor), receiptCode: receipt.Code);
                        result = receipt.Code; receipts++; receiptsTotal += amount;
                        break;
                    }
                    case ImportActions.Expense:
                    {
                        int? idCategory = null;
                        if (line.CategoryCode is Guid categoryCode)
                            idCategory = (await work.CatalogRepository.FindAsync(CatalogKind.Categories, idTenant, categoryCode, ct) ?? throw new NotFoundException("Categoria não encontrada.")).Id;
                        var details = new ObligationDetails(line.Description, null, null, idCategory, null, null, null, "{}", null);
                        var obligation = Obligation.Create(idTenant, details, new DateOnly(line.Date.Year, line.Date.Month, 1), line.Date, -amount, actor);
                        work.ExpenseRepository.Add(obligation);
                        work.ExpenseRepository.ReplaceShares(idTenant, obligation.Code, [], actor);
                        await PaymentWriter.PayAsync(work, idTenant, account.Code, line.Date, null, null, line.Description, [(obligation.Code, -amount)], actor, ct);
                        result = obligation.Code; expenses++; expensesTotal += -amount;
                        break;
                    }
                    case ImportActions.Pay:
                    {
                        var payment = await PaymentWriter.PayAsync(work, idTenant, account.Code, line.Date, null, null, line.Description, [(line.ExpenseCode!.Value, -amount)], actor, ct);
                        result = payment.Code; payments++; paymentsTotal += -amount;
                        break;
                    }
                }
                recorded.Add(new ImportedLine(Guid.CreateVersion7(), line.Fingerprint, line.Date, amount, line.Description, line.ExternalId, line.Action, result));
            }
            var batch = ImportBatch.Create(account, payload.FileName, payload.Format, payload.Lines.Length, payload.Lines.Length - ignored, ignored, actor);
            // The batch code is generated by the entity; lines reference it by code inside the same transaction.
            work.ImportRepository.AddBatch(batch);
            foreach (var line in recorded) work.ImportRepository.AddLine(idTenant, batch.Code, line, actor);
            Idempotency.Record(work, idTenant, Operation, request.Key, hash, batch.Code, actor);
            return new ImportResultDto(batch.Code, payload.Lines.Length - ignored, ignored, receipts, payments, expenses, Money.Format(receiptsTotal), Money.Format(paymentsTotal), Money.Format(expensesTotal));
        }, ct);
    }

    public class ListImportsHandler(IUnitOfWork work) : IRequestHandler<ListImportsQuery, IReadOnlyList<ImportBatchDto>>
    {
        public async Task<IReadOnlyList<ImportBatchDto>> Handle(ListImportsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.ImportRepository.ListAsync(request.Identity.IdTenant, ct))
                .Select(x => new ImportBatchDto(x.Code, x.FileName, x.Format, x.AccountName, x.LineCount, x.AppliedCount, x.IgnoredCount, x.DtCreated)).ToArray();
        }
    }
}
