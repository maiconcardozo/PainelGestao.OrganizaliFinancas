using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Application.Expenses
{
    internal static class RecurrenceTerms
    {
        public static ExpenseTerms From(RecurrenceTermsPayload payload)
        {
            var terms = new ExpenseTerms(payload.ValidFrom, payload.EndDate, payload.Frequency, Money.Parse(payload.Amount), payload.IsEstimated);
            terms.Validate();
            return terms;
        }
        /// <summary>"This and next" never starts before the current version: earlier occurrences keep the rule they were generated from.</summary>
        public static void EnsureForward(IReadOnlyList<ExpenseTermsVersion> versions, ExpenseTerms terms)
        {
            if (terms.ValidFrom < versions.MaxBy(x => x.Version)!.Terms.ValidFrom)
                throw new ConflictException("A alteração não pode começar antes da vigência atual; o histórico não é reescrito.");
        }
        public static RecurrenceDto Map(RecurringExpenseRecord x) => new(x.Code, x.Description, x.IsActive, x.Version, DateOnly.FromDateTime(x.ValidFrom),
            x.EndDate is DateTime end ? DateOnly.FromDateTime(end) : null, x.Frequency, Money.Format(x.Amount), x.IsEstimated, x.ExpenseTypeCode,
            x.ExpenseTypeVersion, x.CategoryCode, x.CategoryName, x.CreditorCode, x.PaymentMethodCode, x.PayerPersonCode, x.PayerPersonName, x.SplitMode,
            System.Text.Json.JsonSerializer.Deserialize<SharePayload[]>(x.SharesJson) ?? [], ExpenseMapper.Fields(x.FieldValuesJson));
        public static async Task<RecurrenceDto> ReadAsync(IUnitOfWork work, int idTenant, Guid code, CancellationToken ct) =>
            Map((await work.ExpenseRepository.ListRecurrencesAsync(idTenant, ct)).Single(x => x.Code == code));
    }

    public class ListRecurrencesHandler(IUnitOfWork work) : IRequestHandler<ListRecurrencesQuery, IReadOnlyList<RecurrenceDto>>
    {
        public async Task<IReadOnlyList<RecurrenceDto>> Handle(ListRecurrencesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.ExpenseRepository.ListRecurrencesAsync(request.Identity.IdTenant, ct)).Select(RecurrenceTerms.Map).ToArray();
        }
    }
    /// <summary>Shows what saving would generate, change, cancel and preserve, without writing anything.</summary>
    public class PreviewRecurrenceHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<PreviewRecurrenceCommand, RecurrencePlanDto>
    {
        public async Task<RecurrencePlanDto> Handle(PreviewRecurrenceCommand request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var terms = RecurrenceTerms.From(request.Terms);
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            if (request.Code is not Guid code)
            {
                var draft = RecurringExpense.Create(user.IdTenant, new ObligationDetails("Prévia", null, null, null, null, null, null, "{}", null), "[]", user.Code.ToString());
                return await RecurrenceGenerator.SyncAsync(work, draft, [new ExpenseTermsVersion(1, terms)], true, today, settings.ForecastHorizonMonths, user.Code.ToString(), false, ct);
            }
            var rule = await work.ExpenseRepository.FindRecurrenceAsync(user.IdTenant, code, ct) ?? throw new NotFoundException("Recorrência não encontrada.");
            var versions = (await work.ExpenseRepository.ListRecurrenceVersionsAsync(user.IdTenant, rule.Id, ct)).Select(x => x.ToTerms()).ToList();
            RecurrenceTerms.EnsureForward(versions, terms);
            rule.Update(rule.Details(), rule.SharesJson, request.Terms.IsActive, user.Code.ToString());
            versions.Add(new ExpenseTermsVersion(rule.Version, terms));
            return await RecurrenceGenerator.SyncAsync(work, rule, versions, false, today, settings.ForecastHorizonMonths, user.Code.ToString(), false, ct);
        }
    }
    /// <summary>RF17: generates the occurrences up to the D23 horizon; the unique key (rule, date) prevents duplicates.</summary>
    public class CreateRecurrenceHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<CreateRecurrenceCommand, RecurrenceSaveDto>
    {
        private const string Operation = "recurrences.create";
        public async Task<RecurrenceSaveDto> Handle(CreateRecurrenceCommand request, CancellationToken ct)
        {
            var (code, plan) = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var hash = Idempotency.Hash(payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing)
                    return (existing, new RecurrencePlanDto(0, 0, 0, 0, []));
                var actor = user.Code.ToString();
                var terms = RecurrenceTerms.From(payload.Terms);
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "recurring", null, ct);
                Shares.Resolve(terms.Amount, resolved.Details.SplitMode, resolved.Shares);
                var rule = RecurringExpense.Create(user.IdTenant, resolved.Details, ExpenseDetailsResolver.SharesJson(resolved.Shares), actor);
                work.ExpenseRepository.AddRecurrence(rule);
                work.ExpenseRepository.AddRecurrenceVersion(rule, terms, actor);
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                var result = await RecurrenceGenerator.SyncAsync(work, rule, [new ExpenseTermsVersion(rule.Version, terms)], true, today, settings.ForecastHorizonMonths, actor, true, ct);
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, rule.Code, actor);
                return (rule.Code, result);
            }, ct);
            return new RecurrenceSaveDto(await RecurrenceTerms.ReadAsync(work, request.Identity.IdTenant, code, ct), plan);
        }
    }
    /// <summary>"This and next": a new version from the chosen date; paid, adjusted, cancelled or individually edited occurrences are preserved.</summary>
    public class UpdateRecurrenceHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<UpdateRecurrenceCommand, RecurrenceSaveDto>
    {
        public async Task<RecurrenceSaveDto> Handle(UpdateRecurrenceCommand request, CancellationToken ct)
        {
            var plan = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var rule = await work.ExpenseRepository.FindRecurrenceAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Recorrência não encontrada.");
                if (rule.Version != payload.Version) throw new ConflictException("Recorrência alterada. Atualize a página antes de salvar.");
                var actor = user.Code.ToString();
                var terms = RecurrenceTerms.From(payload.Terms);
                var versions = (await work.ExpenseRepository.ListRecurrenceVersionsAsync(user.IdTenant, rule.Id, ct)).Select(x => x.ToTerms()).ToList();
                RecurrenceTerms.EnsureForward(versions, terms);
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "recurring", rule.Details(), ct);
                Shares.Resolve(terms.Amount, resolved.Details.SplitMode, resolved.Shares);
                rule.Update(resolved.Details, ExpenseDetailsResolver.SharesJson(resolved.Shares), payload.Terms.IsActive, actor);
                work.ExpenseRepository.UpdateRecurrence(rule);
                work.ExpenseRepository.AddRecurrenceVersion(rule, terms, actor);
                versions.Add(new ExpenseTermsVersion(rule.Version, terms));
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                return await RecurrenceGenerator.SyncAsync(work, rule, versions, false, today, settings.ForecastHorizonMonths, actor, true, ct);
            }, ct);
            return new RecurrenceSaveDto(await RecurrenceTerms.ReadAsync(work, request.Identity.IdTenant, request.Code, ct), plan);
        }
    }
    /// <summary>Rolls the horizon forward for every recurrence; repeating it changes nothing.</summary>
    public class GenerateRecurrencesHandler(IUnitOfWork work, OrganizaliFinancasSettings settings) : IRequestHandler<GenerateRecurrencesCommand, RecurrencePlanDto>
    {
        public Task<RecurrencePlanDto> Handle(GenerateRecurrencesCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            return await GenerateAllAsync(work, user.IdTenant, settings.ForecastHorizonMonths, user.Code.ToString(), ct);
        }, ct);
        internal static async Task<RecurrencePlanDto> GenerateAllAsync(IUnitOfWork work, int idTenant, int horizonMonths, string actor, CancellationToken ct)
        {
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            int inserted = 0, updated = 0, cancelled = 0, preserved = 0;
            foreach (var record in await work.ExpenseRepository.ListRecurrencesAsync(idTenant, ct))
            {
                var rule = (await work.ExpenseRepository.FindRecurrenceAsync(idTenant, record.Code, ct))!;
                var versions = (await work.ExpenseRepository.ListRecurrenceVersionsAsync(idTenant, rule.Id, ct)).Select(x => x.ToTerms()).ToArray();
                var result = await RecurrenceGenerator.SyncAsync(work, rule, versions, false, today, horizonMonths, actor, true, ct);
                inserted += result.Inserted; updated += result.Updated; cancelled += result.Cancelled; preserved += result.Preserved;
            }
            return new RecurrencePlanDto(inserted, updated, cancelled, preserved, []);
        }
    }

    public class PreviewInstallmentsHandler(IUnitOfWork work) : IRequestHandler<PreviewInstallmentsQuery, InstallmentPreviewDto>
    {
        public async Task<InstallmentPreviewDto> Handle(PreviewInstallmentsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return Preview(request.Payload);
        }
        internal static InstallmentPreviewDto Preview(InstallmentPreviewPayload payload)
        {
            var items = Installments.Plan(Money.Parse(payload.Total), payload.Count, payload.FirstDueDate, payload.Frequency);
            return new InstallmentPreviewDto(Money.Format(items.Sum(x => x.Amount)), items.Count,
                items.Select(x => new InstallmentPreviewItemDto(x.Number, x.DueDate, x.DueDate.ToString("yyyy-MM"), Money.Format(x.Amount))).ToArray());
        }
    }
    /// <summary>RF18: every installment is an obligation in its own competence; the sum is exact and the future commitment is visible.</summary>
    public class CreateInstallmentsHandler(IUnitOfWork work) : IRequestHandler<CreateInstallmentsCommand, InstallmentPlanDto>
    {
        private const string Operation = "installments.create";
        public async Task<InstallmentPlanDto> Handle(CreateInstallmentsCommand request, CancellationToken ct)
        {
            var (planCode, codes) = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var hash = Idempotency.Hash(payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing) return (existing, Array.Empty<Guid>());
                var actor = user.Code.ToString();
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "installments", null, ct);
                var total = Money.Parse(payload.Plan.Total);
                var items = Installments.Plan(total, payload.Plan.Count, payload.Plan.FirstDueDate, payload.Plan.Frequency);
                var plan = InstallmentPlan.Create(user.IdTenant, resolved.Details.Normalized().Description, total, items.Count, payload.Plan.FirstDueDate, payload.Plan.Frequency, actor);
                work.ExpenseRepository.AddInstallmentPlan(plan);
                var created = new List<Guid>();
                foreach (var item in items)
                {
                    var obligation = Obligation.CreateInstallment(user.IdTenant, resolved.Details, item, items.Count, actor);
                    work.ExpenseRepository.Add(obligation);
                    work.ExpenseRepository.LinkInstallment(user.IdTenant, obligation.Code, plan.Code);
                    work.ExpenseRepository.ReplaceShares(user.IdTenant, obligation.Code, Shares.Resolve(item.Amount, resolved.Details.SplitMode, resolved.Shares), actor);
                    created.Add(obligation.Code);
                }
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, plan.Code, actor);
                return (plan.Code, created.ToArray());
            }, ct);
            var idTenant = request.Identity.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var records = new List<ObligationRecord>();
            foreach (var code in codes) records.Add((await work.ExpenseRepository.GetAsync(idTenant, code, ct))!);
            var installments = await ExpenseMapper.ReadAsync(work, idTenant, records, today, ct);
            return new InstallmentPlanDto(planCode, request.Payload.Details.Description.Trim(), request.Payload.Plan.Total, installments.Count == 0 ? request.Payload.Plan.Count : installments.Count, installments);
        }
    }
}
