using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Application.Income
{
    public sealed record IncomeSourcePayload(Guid PersonCode, Guid? IncomeTypeCode, string Name, bool IsActive, int Version, DateOnly ValidFrom,
        DateOnly? EndDate, string Frequency, string AmountMode, string? ExpectedNetAmount, string? GrossAmount);
    public sealed record IncomeSourceDto(Guid Code, string Name, bool IsActive, int Version, Guid PersonCode, string PersonName, Guid? IncomeTypeCode,
        DateOnly ValidFrom, DateOnly? EndDate, string Frequency, string AmountMode, string? ExpectedNetAmount, string? GrossAmount, string? MonthlyAverage);
    public sealed record IncomeSourceVersionDto(int Version, DateOnly ValidFrom, DateOnly? EndDate, string Frequency, string AmountMode,
        string? ExpectedNetAmount, string? GrossAmount, DateTime CreatedAt);
    public sealed record ForecastGenerationDto(int Inserted, int Updated, int Cancelled, DateOnly From, DateOnly To);
    /// <summary>Target is the expected amount, or for variable income the labeled average estimate (D24). Excess is only a comparison (D25).</summary>
    public sealed record ForecastDto(Guid Code, Guid SourceCode, string SourceName, Guid PersonCode, string PersonName, string AmountMode,
        DateOnly OccurrenceDate, string Competence, string? ExpectedAmount, string? EstimatedAmount, string ReceivedAmount, string? Difference,
        string? Residual, string Status, string? CloseReason, int SourceVersion, int Version);
    public sealed record CloseForecastPayload(string Reason, int Version);
    public sealed record AllocationPayload(Guid ForecastCode, string Amount);
    public sealed record ReceiptPayload(Guid AccountCode, Guid PersonCode, Guid? IncomeSourceCode, Guid? IncomeTypeCode, DateOnly ReceiptDate,
        string Amount, string? Description, AllocationPayload[]? Allocations);
    public sealed record ReceiptDto(Guid Code, Guid AccountCode, string AccountName, Guid PersonCode, string PersonName, Guid? SourceCode,
        string? SourceName, DateOnly ReceiptDate, string Amount, string AllocatedAmount, string Description, bool IsReversed, string? ReversalReason);
    public sealed record IncomeAverageDto(Guid? Code, string Name, string? PersonName, string Total, string Average);
    public sealed record MonthComparisonDto(string Month, Guid PersonCode, string PersonName, string Expected, string Received, string Difference);
    public sealed record IncomeSummaryDto(int Year, DateOnly AverageFrom, DateOnly AverageTo, int AverageMonths, string HouseholdTotal,
        string HouseholdAverage, IReadOnlyList<IncomeAverageDto> People, IReadOnlyList<IncomeAverageDto> Sources, IReadOnlyList<MonthComparisonDto> Months);

    public sealed record ListIncomeSourcesQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<IncomeSourceDto>>;
    public sealed record ListIncomeSourceVersionsQuery(SessionIdentity Identity, Guid Code) : IRequest<IReadOnlyList<IncomeSourceVersionDto>>;
    public sealed record SaveIncomeSourceCommand(SessionIdentity Identity, Guid? Code, IncomeSourcePayload Payload) : IRequest<IncomeSourceDto>;
    public sealed record GenerateForecastsCommand(SessionIdentity Identity) : IRequest<ForecastGenerationDto>;
    public sealed record ListForecastsQuery(SessionIdentity Identity, DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<ForecastDto>>;
    public sealed record CloseForecastCommand(SessionIdentity Identity, Guid Code, CloseForecastPayload Payload) : IRequest<IReadOnlyList<ForecastDto>>;
    public sealed record ListReceiptsQuery(SessionIdentity Identity, DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<ReceiptDto>>;
    public sealed record CreateReceiptCommand(SessionIdentity Identity, Guid Key, ReceiptPayload Payload) : IRequest<ReceiptDto>;
    public sealed record ReverseReceiptCommand(SessionIdentity Identity, Guid Key, Guid Code, ReversalPayload Payload) : IRequest<ReceiptDto>;
    public sealed record GetIncomeSummaryQuery(SessionIdentity Identity, int? Year) : IRequest<IncomeSummaryDto>;

    public class SaveIncomeSourceValidator : AbstractValidator<SaveIncomeSourceCommand>
    {
        public SaveIncomeSourceValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null).WithMessage("Uma nova fonte deve estar ativa.");
            RuleFor(x => x.Payload.PersonCode).NotEmpty();
            RuleFor(x => x.Payload.Frequency).Must(IncomeFrequency.IsValid).WithMessage("Periodicidade inválida.");
            RuleFor(x => x.Payload.AmountMode).Must(mode => mode is "fixed" or "variable").WithMessage("Informe se o valor é fixo ou variável.");
            RuleFor(x => x.Payload.ExpectedNetAmount).PositiveMoney().When(x => x.Payload.ExpectedNetAmount is not null);
            RuleFor(x => x.Payload.GrossAmount).PositiveMoney().When(x => x.Payload.GrossAmount is not null);
        }
    }
    public class CloseForecastValidator : AbstractValidator<CloseForecastCommand>
    {
        public CloseForecastValidator()
        {
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe o motivo do encerramento.");
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
        }
    }
    public class CreateReceiptValidator : AbstractValidator<CreateReceiptCommand>
    {
        public CreateReceiptValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Amount).PositiveMoney();
            RuleFor(x => x.Payload.Description).MaximumLength(200);
            RuleFor(x => x.Payload.Allocations).Must(items => items is null || items.Length <= 24).WithMessage("Use até 24 alocações por recebimento.")
                .Must(items => items is null || items.Select(x => x.ForecastCode).Distinct().Count() == items.Length).WithMessage("Cada previsão aparece uma única vez.");
            RuleForEach(x => x.Payload.Allocations).ChildRules(item => item.RuleFor(x => x.Amount).PositiveMoney());
        }
    }
    public class ReverseReceiptValidator : AbstractValidator<ReverseReceiptCommand>
    {
        public ReverseReceiptValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe a justificativa do estorno.");
        }
    }

    /// <summary>Applies the idempotent forecast plan of one source within the buffered transaction.</summary>
    internal static class ForecastGenerator
    {
        public static async Task<ForecastGenerationDto> SyncAsync(IUnitOfWork work, IncomeSource source, IReadOnlyList<IncomeTermsVersion> versions,
            bool isNew, DateOnly today, int horizonMonths, string actor, CancellationToken ct)
        {
            var (from, to) = IncomeSchedule.Window(today, horizonMonths);
            var expected = IncomeSchedule.Expected(versions, from, to);
            // An inactive source keeps its past forecasts and stops projecting the future.
            if (!source.IsActive) expected = expected.Where(x => x.Date < today).ToArray();
            var existing = isNew ? [] : (await work.IncomeRepository.ListForecastStatesAsync(source.IdTenant, source.Id, ct)).Select(x => x.ToState()).ToArray();
            var plan = IncomeSchedule.Plan(existing, expected, from, to);
            foreach (var occurrence in plan.Inserts) work.IncomeRepository.InsertForecast(source, occurrence, actor);
            foreach (var (code, occurrence) in plan.Updates) work.IncomeRepository.UpdateForecast(source, code, occurrence, actor);
            foreach (var code in plan.Cancels) work.IncomeRepository.CancelForecast(source.IdTenant, code, actor);
            return new(plan.Inserts.Count, plan.Updates.Count, plan.Cancels.Count, from, to);
        }
    }

    internal static class IncomeAverages
    {
        public static async Task<(AverageWindow Window, IReadOnlyList<IncomeTotalRecord> Totals)> ReadAsync(IUnitOfWork work, int idTenant, DateOnly today, CancellationToken ct)
        {
            var window = IncomeAverage.Window(today);
            return (window, await work.IncomeRepository.ReceivedTotalsAsync(idTenant, HouseholdCalendar.Day(window.From), HouseholdCalendar.Day(window.ToExclusive), ct));
        }
        public static Dictionary<Guid, decimal> BySource(AverageWindow window, IReadOnlyList<IncomeTotalRecord> totals) =>
            totals.Where(x => x.SourceCode is not null).GroupBy(x => x.SourceCode!.Value)
                .ToDictionary(group => group.Key, group => IncomeAverage.Of(group.Sum(x => x.Total), window));
    }

    public class ListIncomeSourcesHandler(IUnitOfWork work) : IRequestHandler<ListIncomeSourcesQuery, IReadOnlyList<IncomeSourceDto>>
    {
        public async Task<IReadOnlyList<IncomeSourceDto>> Handle(ListIncomeSourcesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var (window, totals) = await IncomeAverages.ReadAsync(work, request.Identity.IdTenant, today, ct);
            var averages = IncomeAverages.BySource(window, totals);
            return (await work.IncomeRepository.ListSourcesAsync(request.Identity.IdTenant, ct)).Select(x => new IncomeSourceDto(
                x.Code, x.Name, x.IsActive, x.Version, x.PersonCode, x.PersonName, x.IncomeTypeCode, DateOnly.FromDateTime(x.ValidFrom),
                x.EndDate is DateTime end ? DateOnly.FromDateTime(end) : null, x.Frequency, x.AmountMode, MoneyRules.Format(x.ExpectedNetAmount),
                MoneyRules.Format(x.GrossAmount), Money.Format(averages.GetValueOrDefault(x.Code)))).ToArray();
        }
    }
    public class ListIncomeSourceVersionsHandler(IUnitOfWork work) : IRequestHandler<ListIncomeSourceVersionsQuery, IReadOnlyList<IncomeSourceVersionDto>>
    {
        public async Task<IReadOnlyList<IncomeSourceVersionDto>> Handle(ListIncomeSourceVersionsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var source = await work.IncomeRepository.FindSourceAsync(request.Identity.IdTenant, request.Code, ct) ?? throw new NotFoundException("Fonte de renda não encontrada.");
            return (await work.IncomeRepository.ListSourceVersionsAsync(source.IdTenant, source.Id, ct)).Select(x => new IncomeSourceVersionDto(
                x.Version, DateOnly.FromDateTime(x.ValidFrom), x.EndDate is DateTime end ? DateOnly.FromDateTime(end) : null, x.Frequency, x.AmountMode,
                MoneyRules.Format(x.ExpectedNetAmount), MoneyRules.Format(x.GrossAmount), x.DtCreated)).ToArray();
        }
    }
    public class SaveIncomeSourceHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<SaveIncomeSourceCommand, IncomeSourceDto>
    {
        public Task<IncomeSourceDto> Handle(SaveIncomeSourceCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var actor = user.Code.ToString();
            var payload = request.Payload;
            var terms = new IncomeTerms(payload.ValidFrom, payload.EndDate, payload.Frequency, payload.AmountMode,
                payload.ExpectedNetAmount is null ? null : Money.Parse(payload.ExpectedNetAmount), payload.GrossAmount is null ? null : Money.Parse(payload.GrossAmount));
            terms.Validate();
            var person = await work.HouseholdRepository.FindPersonAsync(user.IdTenant, payload.PersonCode, ct) ?? throw new NotFoundException("Morador não encontrado nesta casa.");
            int? idType = null;
            if (payload.IncomeTypeCode is Guid typeCode)
            {
                var type = await work.CatalogRepository.FindAsync(CatalogKind.IncomeTypes, user.IdTenant, typeCode, ct) ?? throw new NotFoundException("Tipo de renda não encontrado.");
                if (!type.IsActive && payload.IsActive) throw new ConflictException("Escolha um tipo de renda ativo.");
                idType = type.Id;
            }
            IncomeSource source;
            List<IncomeTermsVersion> versions;
            if (request.Code is Guid code)
            {
                source = await work.IncomeRepository.FindSourceAsync(user.IdTenant, code, ct) ?? throw new NotFoundException("Fonte de renda não encontrada.");
                if (source.Version != payload.Version) throw new ConflictException("Fonte alterada. Atualize a página antes de salvar.");
                if (source.IdPerson != person.Id) throw new ConflictException("O morador de uma fonte não pode ser trocado; cadastre outra fonte.");
                versions = (await work.IncomeRepository.ListSourceVersionsAsync(user.IdTenant, source.Id, ct)).Select(x => x.ToTerms()).ToList();
                if (terms.ValidFrom < versions.MaxBy(x => x.Version)!.Terms.ValidFrom)
                    throw new ConflictException("A nova vigência não pode começar antes da vigência atual; o histórico não é reescrito.");
                source.Update(payload.Name, idType, payload.IsActive, actor);
                work.IncomeRepository.UpdateSource(source);
            }
            else
            {
                source = IncomeSource.Create(person, idType, payload.Name, actor);
                work.IncomeRepository.AddSource(source);
                versions = [];
            }
            work.IncomeRepository.AddSourceVersion(source, terms, actor);
            versions.Add(new IncomeTermsVersion(source.Version, terms));
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            await ForecastGenerator.SyncAsync(work, source, versions, request.Code is null, today, settings.ForecastHorizonMonths, actor, ct);
            return new IncomeSourceDto(source.Code, source.Name, source.IsActive, source.Version, person.Code, person.Name, payload.IncomeTypeCode,
                terms.ValidFrom, terms.EndDate, terms.Frequency, terms.AmountMode, MoneyRules.Format(terms.ExpectedNetAmount), MoneyRules.Format(terms.GrossAmount), null);
        }, ct);
    }
    /// <summary>Rolls the D23 horizon forward for every source; repeating it changes nothing.</summary>
    public class GenerateForecastsHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<GenerateForecastsCommand, ForecastGenerationDto>
    {
        public Task<ForecastGenerationDto> Handle(GenerateForecastsCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            var (from, to) = IncomeSchedule.Window(today, settings.ForecastHorizonMonths);
            int inserted = 0, updated = 0, cancelled = 0;
            foreach (var record in await work.IncomeRepository.ListSourcesAsync(user.IdTenant, ct))
            {
                var source = (await work.IncomeRepository.FindSourceAsync(user.IdTenant, record.Code, ct))!;
                var versions = (await work.IncomeRepository.ListSourceVersionsAsync(user.IdTenant, source.Id, ct)).Select(x => x.ToTerms()).ToArray();
                var result = await ForecastGenerator.SyncAsync(work, source, versions, false, today, settings.ForecastHorizonMonths, user.Code.ToString(), ct);
                inserted += result.Inserted; updated += result.Updated; cancelled += result.Cancelled;
            }
            return new ForecastGenerationDto(inserted, updated, cancelled, from, to);
        }, ct);
    }
    public class ListForecastsHandler(IUnitOfWork work) : IRequestHandler<ListForecastsQuery, IReadOnlyList<ForecastDto>>
    {
        public async Task<IReadOnlyList<ForecastDto>> Handle(ListForecastsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var month = new DateOnly(today.Year, today.Month, 1);
            var from = request.From is DateOnly start ? new DateOnly(start.Year, start.Month, 1) : month;
            var to = request.To is DateOnly end ? new DateOnly(end.Year, end.Month, 1) : month.AddMonths(2);
            if (from > to || to > from.AddMonths(24))
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até 24 meses."] });
            return await ReadAsync(work, request.Identity.IdTenant, today, from, to, ct);
        }
        internal static async Task<IReadOnlyList<ForecastDto>> ReadAsync(IUnitOfWork work, int idTenant, DateOnly today, DateOnly from, DateOnly to, CancellationToken ct)
        {
            var (window, totals) = await IncomeAverages.ReadAsync(work, idTenant, today, ct);
            var averages = IncomeAverages.BySource(window, totals);
            return (await work.IncomeRepository.ListForecastsAsync(idTenant, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct)).Select(x =>
            {
                decimal? estimate = x.AmountMode == "variable" ? averages.GetValueOrDefault(x.SourceCode) : null;
                var target = x.ExpectedAmount ?? estimate;
                var residual = target is decimal value ? (x.Status == "closed" ? 0 : Math.Max(0, value - x.ReceivedAmount)) : (decimal?)null;
                return new ForecastDto(x.Code, x.SourceCode, x.SourceName, x.PersonCode, x.PersonName, x.AmountMode, DateOnly.FromDateTime(x.OccurrenceDate),
                    x.Competence.ToString("yyyy-MM"), MoneyRules.Format(x.ExpectedAmount), MoneyRules.Format(estimate), Money.Format(x.ReceivedAmount),
                    MoneyRules.Format(x.ReceivedAmount - target), MoneyRules.Format(residual), x.Status, x.CloseReason, x.SourceVersion, x.Version);
            }).ToArray();
        }
    }
    public class CloseForecastHandler(IUnitOfWork work) : IRequestHandler<CloseForecastCommand, IReadOnlyList<ForecastDto>>
    {
        public async Task<IReadOnlyList<ForecastDto>> Handle(CloseForecastCommand request, CancellationToken ct)
        {
            var month = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var forecast = await work.IncomeRepository.FindForecastAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Previsão não encontrada.");
                if (forecast.Version != request.Payload.Version) throw new ConflictException("Previsão alterada. Atualize a página antes de encerrar.");
                forecast.Close(request.Payload.Reason, user.Code.ToString());
                work.IncomeRepository.CloseForecast(forecast);
                return DateOnly.FromDateTime(forecast.Competence);
            }, ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            return (await ListForecastsHandler.ReadAsync(work, request.Identity.IdTenant, today, month, month, ct)).Where(x => x.Code == request.Code).ToArray();
        }
    }

    public class ListReceiptsHandler(IUnitOfWork work) : IRequestHandler<ListReceiptsQuery, IReadOnlyList<ReceiptDto>>
    {
        public async Task<IReadOnlyList<ReceiptDto>> Handle(ListReceiptsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var from = request.From ?? new DateOnly(today.Year, 1, 1);
            var to = request.To ?? today;
            if (from > to || to.DayNumber - from.DayNumber > 731)
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até dois anos."] });
            return (await work.IncomeRepository.ListReceiptsAsync(request.Identity.IdTenant, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct)).Select(Map).ToArray();
        }
        internal static ReceiptDto Map(ReceiptRecord x) => new(x.Code, x.AccountCode, x.AccountName, x.PersonCode, x.PersonName, x.SourceCode, x.SourceName,
            DateOnly.FromDateTime(x.ReceiptDate), Money.Format(x.Amount), Money.Format(x.AllocatedAmount), x.Description, x.DtReversed is not null, x.ReversalReason);
    }
    public class CreateReceiptHandler(IUnitOfWork work) : IRequestHandler<CreateReceiptCommand, ReceiptDto>
    {
        private const string Operation = "receipts.create";
        public Task<ReceiptDto> Handle(CreateReceiptCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var payload = request.Payload;
            var hash = Idempotency.Hash(payload);
            if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing)
                return ListReceiptsHandler.Map((await work.IncomeRepository.GetReceiptAsync(user.IdTenant, existing, ct))!);
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            HouseholdCalendar.EnsureNotFuture(payload.ReceiptDate, today, "ReceiptDate");
            var account = await work.AccountRepository.FindAsync(user.IdTenant, payload.AccountCode, ct) ?? throw new NotFoundException("Conta não encontrada.");
            var person = await work.HouseholdRepository.FindPersonAsync(user.IdTenant, payload.PersonCode, ct) ?? throw new NotFoundException("Morador não encontrado nesta casa.");
            var allocations = payload.Allocations ?? [];
            var forecasts = await work.IncomeRepository.FindForecastsAsync(user.IdTenant, allocations.Select(x => x.ForecastCode).ToArray(), ct);
            if (forecasts.Count != allocations.Length) throw new NotFoundException("Previsão não encontrada nesta casa.");
            foreach (var forecast in forecasts) forecast.EnsureAcceptsAllocation();
            // One receipt belongs to at most one source, so per-source averages never split a deposit.
            var sourceIds = forecasts.Select(x => x.IdIncomeSource).Distinct().ToArray();
            if (sourceIds.Length > 1) throw new ConflictException("Registre um recebimento por fonte de renda.");
            IncomeSource? source = null;
            if (payload.IncomeSourceCode is Guid sourceCode)
                source = await work.IncomeRepository.FindSourceAsync(user.IdTenant, sourceCode, ct) ?? throw new NotFoundException("Fonte de renda não encontrada.");
            else if (sourceIds.Length == 1)
                source = await work.IncomeRepository.FindSourceByIdAsync(user.IdTenant, sourceIds[0], ct);
            if (sourceIds.Length == 1 && source!.Id != sourceIds[0]) throw new ConflictException("As previsões escolhidas pertencem a outra fonte de renda.");
            int? idType = null;
            if (payload.IncomeTypeCode is Guid typeCode)
                idType = (await work.CatalogRepository.FindAsync(CatalogKind.IncomeTypes, user.IdTenant, typeCode, ct) ?? throw new NotFoundException("Tipo de renda não encontrado.")).Id;
            var actor = user.Code.ToString();
            var amount = Money.Parse(payload.Amount);
            var amounts = allocations.Select(x => Money.Parse(x.Amount)).ToArray();
            var receipt = Receipt.Create(account, person, source, idType, payload.ReceiptDate, amount, amounts, payload.Description, actor);
            work.IncomeRepository.AddReceipt(receipt);
            var label = string.IsNullOrEmpty(receipt.Description) ? $"Recebimento {source?.Name ?? person.Name}" : receipt.Description;
            work.AccountRepository.AddMovement(AccountMovement.Create(account, payload.ReceiptDate, amount, "receipt", label, actor), receiptCode: receipt.Code);
            foreach (var (allocation, value) in allocations.Zip(amounts))
                work.IncomeRepository.AddAllocation(receipt, forecasts.Single(x => x.Code == allocation.ForecastCode), value, actor);
            Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, receipt.Code, actor);
            return new ReceiptDto(receipt.Code, account.Code, account.Name, person.Code, person.Name, source?.Code, source?.Name, payload.ReceiptDate,
                Money.Format(amount), Money.Format(amounts.Sum()), receipt.Description, false, null);
        }, ct);
    }
    public class ReverseReceiptHandler(IUnitOfWork work) : IRequestHandler<ReverseReceiptCommand, ReceiptDto>
    {
        private const string Operation = "receipts.reverse";
        public Task<ReceiptDto> Handle(ReverseReceiptCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "payments.reverse", ct);
            var hash = Idempotency.Hash(request.Code, request.Payload);
            if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is not null)
                return ListReceiptsHandler.Map((await work.IncomeRepository.GetReceiptAsync(user.IdTenant, request.Code, ct))!);
            var receipt = await work.IncomeRepository.FindReceiptAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Recebimento não encontrado.");
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            var date = request.Payload.ReversalDate ?? today;
            HouseholdCalendar.EnsureNotFuture(date, today, "ReversalDate");
            var actor = user.Code.ToString();
            receipt.Reverse(request.Payload.Reason, actor);
            work.IncomeRepository.MarkReceiptReversed(receipt);
            foreach (var original in await work.AccountRepository.FindOriginalMovementsAsync(user.IdTenant, null, receipt.Id, null, ct))
                work.AccountRepository.AddMovement(original.Compensate(date, $"Estorno: {receipt.ReversalReason}", actor));
            Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, receipt.Code, actor);
            var record = (await work.IncomeRepository.GetReceiptAsync(user.IdTenant, receipt.Code, ct))!;
            return ListReceiptsHandler.Map(record with { DtReversed = receipt.DtReversed, ReversalReason = receipt.ReversalReason });
        }, ct);
    }

    public class GetIncomeSummaryHandler(IUnitOfWork work) : IRequestHandler<GetIncomeSummaryQuery, IncomeSummaryDto>
    {
        public async Task<IncomeSummaryDto> Handle(GetIncomeSummaryQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var year = request.Year ?? today.Year;
            if (year is < 2000 or > 2100) throw new InputValidationException(new Dictionary<string, string[]> { ["Year"] = ["Ano inválido."] });
            var (window, totals) = await IncomeAverages.ReadAsync(work, idTenant, today, ct);
            var people = (await work.HouseholdRepository.ListPeopleAsync(idTenant, ct))
                .Select(person => (person.Code, person.Name, person.IsActive, Total: totals.Where(x => x.PersonCode == person.Code).Sum(x => x.Total)))
                .Where(x => x.IsActive || x.Total > 0)
                .Select(x => new IncomeAverageDto(x.Code, x.Name, null, Money.Format(x.Total), Money.Format(IncomeAverage.Of(x.Total, window)))).ToArray();
            var sources = totals.GroupBy(x => (x.SourceCode, x.PersonCode)).Select(group =>
            {
                var total = group.Sum(x => x.Total);
                var first = group.First();
                return new IncomeAverageDto(first.SourceCode, first.SourceName ?? "Entradas sem fonte", first.PersonName, Money.Format(total),
                    Money.Format(IncomeAverage.Of(total, window)));
            }).OrderBy(x => x.PersonName).ThenBy(x => x.Name).ToArray();
            var start = new DateTime(year, 1, 1);
            var received = await work.IncomeRepository.ReceivedTotalsAsync(idTenant, start, start.AddYears(1), ct);
            var expected = await work.IncomeRepository.ExpectedTotalsAsync(idTenant, start, start.AddYears(1), ct);
            // Future months have nothing received yet; listing them would read as a shortfall.
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var months = received.Concat(expected).Select(x => (x.Month, x.PersonCode, x.PersonName)).Distinct().Where(x => x.Month <= currentMonth)
                .OrderBy(x => x.Month).ThenBy(x => x.PersonName)
                .Select(key =>
                {
                    var got = received.Where(x => x.Month == key.Month && x.PersonCode == key.PersonCode).Sum(x => x.Total);
                    var planned = expected.Where(x => x.Month == key.Month && x.PersonCode == key.PersonCode).Sum(x => x.Total);
                    return new MonthComparisonDto(key.Month.ToString("yyyy-MM"), key.PersonCode, key.PersonName, Money.Format(planned), Money.Format(got), Money.Format(got - planned));
                }).ToArray();
            var householdTotal = totals.Sum(x => x.Total);
            return new IncomeSummaryDto(year, window.From, window.ToExclusive.AddDays(-1), window.Months, Money.Format(householdTotal),
                Money.Format(IncomeAverage.Of(householdTotal, window)), people, sources, months);
        }
    }
}
