using System.Globalization;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Application.Planning
{
    /// <summary>
    /// Read-only view of one household's plan for "today": budget, cash projection, agenda and alert conditions.
    /// Every figure is recomputed from the ledger, obligations and forecasts; nothing here writes or moves money.
    /// </summary>
    internal sealed class HouseholdPlanner
    {
        public const int ProjectionDays = 60;
        private readonly IUnitOfWork work;
        private readonly int idTenant;
        public DateOnly Today { get; }
        public AlertPreferenceRecord Preference { get; }
        private HouseholdPlanner(IUnitOfWork work, int idTenant, DateOnly today, AlertPreferenceRecord preference) =>
            (this.work, this.idTenant, Today, Preference) = (work, idTenant, today, preference);

        public static async Task<HouseholdPlanner> CreateAsync(IUnitOfWork work, int idTenant, CancellationToken ct) =>
            new(work, idTenant, await HouseholdCalendar.TodayAsync(work, idTenant, ct), await work.PlanningRepository.PreferenceAsync(idTenant, ct) ?? new AlertPreferenceRecord());

        public DateOnly MonthOf(DateOnly? requested) => Obligation.Month(requested ?? Today);
        private static string Format(decimal? value) => MoneyRules.Format(value) ?? "0.00";

        public async Task<(BudgetDto Dto, BudgetSummary Summary)> BudgetAsync(DateOnly month, CancellationToken ct)
        {
            var categories = await work.PlanningRepository.CategoriesAsync(idTenant, ct);
            var limits = (await work.PlanningRepository.BudgetsAsync(idTenant, HouseholdCalendar.Day(month), ct)).ToDictionary(x => x.CategoryCode, x => x.Amount);
            var spend = await work.PlanningRepository.SpendAsync(idTenant, HouseholdCalendar.Day(month), ct);
            var summary = BudgetEvaluation.Evaluate(categories.Select(x => new CategoryNode(x.Code, x.Name, x.ParentCode, x.IsActive)).ToArray(), limits,
                spend.Select(x => new CategorySpend(x.CategoryCode, x.Bills, x.Card, x.Paid)).ToArray(), Preference.WarningPercent, Preference.LimitPercent);
            var dto = new BudgetDto(month.ToString("yyyy-MM"), Preference.WarningPercent, Preference.LimitPercent, Money.Format(summary.TotalLimit),
                Money.Format(summary.TotalCommitted), Money.Format(summary.TotalPaid), Money.Format(summary.Unbudgeted),
                summary.Lines.Select(x => new BudgetLineDto(x.CategoryCode, x.Name, x.ParentCode, x.Depth, MoneyRules.Format(x.Limit), Money.Format(x.Committed),
                    Money.Format(x.Paid), Money.Format(x.Card), x.UsedPercent?.ToString("0.##", CultureInfo.InvariantCulture), x.Band, Money.Format(x.Excess), x.CountsInTotal)).ToArray());
            return (dto, summary);
        }

        /// <summary>Open obligations (bills and invoices) and open fixed income forecasts up to the date; overdue ones are kept with their due date.</summary>
        public async Task<IReadOnlyList<(CashEvent Event, bool Overdue, string Link)>> OpenItemsAsync(DateOnly until, CancellationToken ct)
        {
            var items = new List<(CashEvent, bool, string)>();
            foreach (var x in await work.ExpenseRepository.ListAsync(idTenant, HouseholdCalendar.Day(Today), HouseholdCalendar.Day(until), true, ct))
            {
                var state = ObligationBalance.Of(x.Amount, x.Adjustments, x.Paid, x.Status == "cancelled", DateOnly.FromDateTime(x.DueDate), Today);
                if (state.OpenBalance <= 0) continue;
                var kind = x.CardInvoiceCode is not null ? "invoice" : "bill";
                items.Add((new CashEvent(DateOnly.FromDateTime(x.DueDate), -state.OpenBalance, kind, x.Description, x.Code, x.IsEstimated), state.IsOverdue,
                    kind == "invoice" ? "/cards" : "/expenses"));
            }
            foreach (var x in await work.IncomeRepository.ListForecastsAsync(idTenant, HouseholdCalendar.Day(Obligation.Month(Today).AddMonths(-12)), HouseholdCalendar.Day(Obligation.Month(until)), ct))
            {
                var date = DateOnly.FromDateTime(x.OccurrenceDate);
                if (x.Status != "open" || x.ExpectedAmount is not decimal expected || date > until) continue;
                var residual = expected - x.ReceivedAmount;
                if (residual <= 0) continue;
                items.Add((new CashEvent(date, residual, "income", $"{x.SourceName} · {x.PersonName}", x.Code, false), date < Today, "/income"));
            }
            return items;
        }

        /// <summary>Late income is not assumed to arrive: only income dated today or later enters the projection; overdue bills are placed on today.</summary>
        public async Task<(CashProjection Projection, ProjectionDto Dto)> ProjectionAsync(DateOnly until, CancellationToken ct)
        {
            var start = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(Today.AddDays(1)), ct);
            var events = (await OpenItemsAsync(until, ct)).Where(x => x.Event.Amount < 0 || !x.Overdue).Select(x => x.Event).ToArray();
            var projection = CashProjector.Project(start, Today, until, events);
            CashEventDto Map(CashEvent x) => new(x.Kind, x.Description, Money.Format(x.Amount), x.IsEstimate, x.Code);
            var causes = projection.FirstDeficit is DateOnly deficit
                ? projection.Days.Where(x => x.Date <= deficit).SelectMany(x => x.Events).Where(x => x.Amount < 0).OrderBy(x => x.Amount).Take(5).Select(Map).ToArray()
                : [];
            return (projection, new ProjectionDto(projection.From, projection.To, Money.Format(projection.Start), Money.Format(projection.End), projection.FirstDeficit,
                Money.Format(projection.Lowest), projection.LowestDate, causes,
                projection.Days.Select(x => new ProjectionDayDto(x.Date, Money.Format(x.Inflow), Money.Format(x.Outflow), Money.Format(x.Balance), x.Events.Select(Map).ToArray())).ToArray()));
        }

        public async Task<AgendaDto> AgendaAsync(CancellationToken ct)
        {
            var items = (await OpenItemsAsync(Today.AddDays(90), ct)).Select(x => new AgendaItemDto(x.Event.Date, x.Event.Kind, x.Event.Description,
                Money.Format(Math.Abs(x.Event.Amount)), x.Event.IsEstimate, x.Overdue, x.Event.Code!.Value, x.Link)).OrderBy(x => x.Date).ToArray();
            return new AgendaDto(items.Where(x => x.IsOverdue).ToArray(), items.Where(x => !x.IsOverdue && x.Date == Today).ToArray(),
                items.Where(x => x.Date > Today && x.Date <= Today.AddDays(7)).ToArray(), items.Where(x => x.Date > Today.AddDays(7) && x.Date <= Today.AddDays(30)).ToArray(),
                items.Where(x => x.Date > Today.AddDays(30)).ToArray());
        }

        /// <summary>Alert conditions with a fingerprint per relevant state: budget band, deficit date, obligation version or residual.</summary>
        public async Task<IReadOnlyList<ActiveAlert>> AlertsAsync(CancellationToken ct)
        {
            var conditions = new List<AlertCondition>();
            var month = Obligation.Month(Today);
            var (_, budget) = await BudgetAsync(month, ct);
            foreach (var line in budget.Lines.Where(x => x.CategoryCode is not null && x.Band is "warning" or "limit" or "exceeded"))
            {
                var detail = line.Band == "exceeded"
                    ? $"Excesso de {Brl(line.Excess)} ({Percent(line.Excess, line.Limit!.Value)}): {Brl(line.Committed)} comprometidos para limite de {Brl(line.Limit.Value)}."
                    : $"{Brl(line.Committed)} comprometidos de {Brl(line.Limit!.Value)} ({line.UsedPercent?.ToString("0.#", Brazil)}%).";
                conditions.Add(new AlertCondition($"budget:{line.CategoryCode}:{month:yyyy-MM}", line.Band, "budget", line.Band == "warning" ? "warning" : "critical",
                    line.Band == "warning" ? $"{line.Name} perto do orçamento" : line.Band == "limit" ? $"{line.Name} atingiu o orçamento" : $"{line.Name} passou do orçamento",
                    detail, "/budget", Today));
            }
            var (projection, _) = await ProjectionAsync(Today.AddDays(ProjectionDays), ct);
            if (projection.FirstDeficit is DateOnly deficit)
                conditions.Add(new AlertCondition("deficit", deficit.ToString("yyyy-MM-dd"), "deficit", "critical", $"Saldo projetado negativo em {deficit:dd/MM/yyyy}",
                    $"Pelas contas e rendas previstas, o saldo da casa chega a {Brl(projection.Lowest)} em {projection.LowestDate:dd/MM/yyyy}.", "/dashboard", deficit));
            foreach (var (item, overdue, link) in await OpenItemsAsync(Today.AddDays(30), ct))
            {
                var value = Brl(Math.Abs(item.Amount));
                if (item.Amount > 0)
                {
                    if (overdue)
                        conditions.Add(new AlertCondition($"income-late:{item.Code}", item.Amount.ToString(CultureInfo.InvariantCulture), "income", "warning",
                            $"Renda em atraso: {item.Description}", $"Faltam {value} previstos para {item.Date:dd/MM/yyyy}.", link, item.Date));
                    continue;
                }
                if (overdue)
                    conditions.Add(new AlertCondition($"overdue:{item.Code}", item.Amount.ToString(CultureInfo.InvariantCulture), "overdue", "critical",
                        $"Vencida: {item.Description}", $"{value} em aberto desde {item.Date:dd/MM/yyyy}.", link, item.Date));
                else if (item.Date <= Today.AddDays(Preference.DueSoonDays))
                    conditions.Add(new AlertCondition($"due:{item.Code}", $"{item.Date:yyyy-MM-dd}:{item.Amount.ToString(CultureInfo.InvariantCulture)}", item.Kind == "invoice" ? "invoice" : "due",
                        "warning", item.Kind == "invoice" ? $"Fatura vence em {item.Date:dd/MM/yyyy}" : $"Vence em {item.Date:dd/MM/yyyy}: {item.Description}",
                        $"{value} a pagar.", link, item.Date));
                else if (item.IsEstimate)
                    conditions.Add(new AlertCondition($"estimate:{item.Code}", item.Amount.ToString(CultureInfo.InvariantCulture), "estimate", "info",
                        $"Confirme o valor: {item.Description}", $"Valor estimado de {value} para {item.Date:dd/MM/yyyy}.", link, item.Date));
            }
            var memory = (await work.PlanningRepository.AlertStatesAsync(idTenant, ct))
                .Select(x => new AlertMemory(x.AlertKey, x.Fingerprint, x.Status, x.SnoozedUntil is DateTime until ? DateOnly.FromDateTime(until) : null)).ToArray();
            return AlertCenter.Merge(conditions, memory, Today);
        }
        public static AlertDto Map(ActiveAlert x) => new(x.Condition.Key, x.Condition.Kind, x.Condition.Severity, x.Condition.Title, x.Condition.Detail,
            x.Condition.Link, x.Condition.Date, x.Status);

        // Fixed BRL format, independent of the ICU data available in the container.
        private static readonly NumberFormatInfo Brazil = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = ".", NumberGroupSizes = [3] };
        public static string Brl(decimal value) => (value < 0 ? "-R$ " : "R$ ") + Math.Abs(value).ToString("N2", Brazil);
        private static string Percent(decimal part, decimal total) => total == 0 ? "sem limite" : (part * 100 / total).ToString("0.#", Brazil) + "%";
    }
}
