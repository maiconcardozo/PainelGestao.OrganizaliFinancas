using System.Globalization;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Application.Planning
{
    /// <summary>
    /// An open item of the household as the plan sees it. <see cref="Event"/> is dated when the cash really leaves or arrives: a bill the
    /// household pays with a credit card (A22) sits on that card's invoice due date, not on its own <see cref="DueDate"/>, which the agenda
    /// and the due-date alerts keep using.
    /// </summary>
    internal sealed record OpenItem(CashEvent Event, bool Overdue, string Link, DateOnly DueDate, string? CardName, DateOnly? InvoiceMonth, string? CategoryName);
    /// <summary>One month of the series: opening + inflow + outflow + other = closing. Other holds opening balances of accounts created in the month.</summary>
    internal sealed record MonthFlow(DateOnly Month, decimal Opening, decimal Inflow, decimal Outflow, decimal Other, decimal Closing, decimal Bills, decimal Invoices);

    /// <summary>
    /// Read-only view of one household's plan for "today": budget, cash projection, agenda and alert conditions.
    /// Every figure is recomputed from the ledger, obligations and forecasts; nothing here writes or moves money.
    /// </summary>
    internal sealed class HouseholdPlanner
    {
        public const int ProjectionDays = 60;
        public const int MarginMonths = 6;
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

        /// <summary>
        /// Open obligations (bills and invoices) and open fixed income forecasts up to the date; overdue ones are kept with their due date.
        /// A bill whose default card (A22) is active is dated on the due date of the invoice a purchase on its due date (or today, when overdue)
        /// would land in; the bill itself keeps its due date for the agenda. An inactive card counts as a bank payment.
        /// </summary>
        public async Task<IReadOnlyList<OpenItem>> OpenItemsAsync(DateOnly until, CancellationToken ct)
        {
            var items = new List<OpenItem>();
            var cards = (await work.CardRepository.ListCardsAsync(idTenant, ct)).Where(x => x.IsActive).ToDictionary(x => x.Code);
            foreach (var x in await work.ExpenseRepository.ListAsync(idTenant, HouseholdCalendar.Day(Today), HouseholdCalendar.Day(until), true, ct))
            {
                var due = DateOnly.FromDateTime(x.DueDate);
                var state = ObligationBalance.Of(x.Amount, x.Adjustments, x.Paid, x.Status == "cancelled", due, Today);
                if (state.OpenBalance <= 0) continue;
                var kind = x.CardInvoiceCode is not null ? "invoice" : "bill";
                var (cashDate, cardName, invoiceMonth) = (due, (string?)null, (DateOnly?)null);
                if (kind == "bill" && x.PayCardCode is Guid cardCode && cards.TryGetValue(cardCode, out var card))
                {
                    var landing = CardSchedule.LandingFor(due < Today ? Today : due, card.ClosingDay, card.DueDay, card.ClosingDayRule);
                    (cashDate, cardName, invoiceMonth) = (landing.Cycle.DueDate, card.Name, landing.Cycle.ReferenceMonth);
                }
                items.Add(new OpenItem(new CashEvent(cashDate, -state.OpenBalance, kind, x.Description, x.Code, x.IsEstimated), state.IsOverdue,
                    kind == "invoice" ? "/cards" : "/expenses", due, cardName, invoiceMonth, x.CategoryName));
            }
            foreach (var x in await work.IncomeRepository.ListForecastsAsync(idTenant, HouseholdCalendar.Day(Obligation.Month(Today).AddMonths(-12)), HouseholdCalendar.Day(Obligation.Month(until)), ct))
            {
                var date = DateOnly.FromDateTime(x.OccurrenceDate);
                if (x.Status != "open" || x.ExpectedAmount is not decimal expected || date > until) continue;
                var residual = expected - x.ReceivedAmount;
                if (residual <= 0) continue;
                items.Add(new OpenItem(new CashEvent(date, residual, "income", $"{x.SourceName} · {x.PersonName}", x.Code, false), date < Today, "/income", date, null, null, null));
            }
            return items;
        }

        /// <summary>Late income is not assumed to arrive: only income dated today or later enters the projection; overdue bills are placed on today.</summary>
        public async Task<(CashProjection Projection, ProjectionDto Dto)> ProjectionAsync(DateOnly until, CancellationToken ct)
        {
            var start = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(Today.AddDays(1)), ct);
            var events = ProjectedEvents(await OpenItemsAsync(until, ct));
            var projection = CashProjector.Project(start, Today, until, events);
            CashEventDto Map(CashEvent x) => new(x.Kind, x.Description, Money.Format(x.Amount), x.IsEstimate, x.Code);
            var causes = projection.FirstDeficit is DateOnly deficit
                ? projection.Days.Where(x => x.Date <= deficit).SelectMany(x => x.Events).Where(x => x.Amount < 0).OrderBy(x => x.Amount).Take(5).Select(Map).ToArray()
                : [];
            return (projection, new ProjectionDto(projection.From, projection.To, Money.Format(projection.Start), Money.Format(projection.End), projection.FirstDeficit,
                Money.Format(projection.Lowest), projection.LowestDate, causes,
                projection.Days.Select(x => new ProjectionDayDto(x.Date, Money.Format(x.Inflow), Money.Format(x.Outflow), Money.Format(x.Balance), x.Events.Select(Map).ToArray())).ToArray()));
        }
        private static CashEvent[] ProjectedEvents(IEnumerable<OpenItem> items) => items.Where(x => x.Event.Amount < 0 || !x.Overdue).Select(x => x.Event).ToArray();

        /// <summary>
        /// Monthly series from realized movements up to today plus the projected events after it. For a month up to the current one the closing
        /// is the real balance at its end (or today) plus what is still projected inside it, so the series never re-applies realized movements.
        /// </summary>
        public async Task<IReadOnlyList<MonthFlow>> MonthsAsync(DateOnly first, DateOnly last, IReadOnlyList<CashEvent> projected, CancellationToken ct)
        {
            var current = Obligation.Month(Today);
            var realized = (await work.PlanningRepository.MonthlyCashAsync(idTenant, HouseholdCalendar.Day(first), HouseholdCalendar.Day(Today.AddDays(1)), ct))
                .ToDictionary(x => DateOnly.FromDateTime(x.Period));
            var placed = projected.Select(x => x.Date < Today ? x with { Date = Today } : x).ToArray();
            var opening = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(first), ct);
            var months = new List<MonthFlow>();
            for (var m = first; m <= last; m = m.AddMonths(1))
            {
                var next = m.AddMonths(1);
                var real = realized.GetValueOrDefault(m) ?? new CashTotalRecord();
                var future = placed.Where(x => x.Date >= m && x.Date < next).ToArray();
                var inflow = real.Inflow + future.Where(x => x.Amount > 0).Sum(x => x.Amount);
                var outflow = real.Outflow + future.Where(x => x.Amount < 0).Sum(x => x.Amount);
                var closing = m <= current
                    ? await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(next <= Today ? next : Today.AddDays(1)), ct) + future.Sum(x => x.Amount)
                    : opening + inflow + outflow;
                months.Add(new MonthFlow(m, opening, inflow, outflow, closing - opening - inflow - outflow, closing,
                    -future.Where(x => x.Kind == "bill").Sum(x => x.Amount), -future.Where(x => x.Kind == "invoice").Sum(x => x.Amount)));
                opening = closing;
            }
            return months;
        }

        /// <summary>
        /// A22, "Margem do mês": for the current month and the next <see cref="MarginMonths"/>, what leaves the account, what goes to a card
        /// (and leaves on the invoice), the projected closing balance and the margin left above the minimum reserve. A simulated spend
        /// (today, cash or on a card) is projected the same way so the screen can show which margins it eats.
        /// </summary>
        public async Task<MarginDto> MarginAsync(decimal? amount, Guid? cardCode, CancellationToken ct)
        {
            var settings = await work.HouseholdRepository.GetSettingsAsync(idTenant, ct);
            var reserve = settings?.MinimumReserve ?? HouseholdSettings.Create(idTenant, "system").MinimumReserve;
            var first = Obligation.Month(Today);
            var last = first.AddMonths(MarginMonths);
            var until = last.AddMonths(1).AddDays(-1);
            var items = await OpenItemsAsync(until, ct);
            var cards = (await work.CardRepository.ListCardsAsync(idTenant, ct)).Where(x => x.IsActive).ToArray();
            var baseEvents = ProjectedEvents(items);
            var toCard = items.Where(x => x.CardName is not null).ToLookup(x => Obligation.Month(x.DueDate));

            async Task<IReadOnlyList<MarginMonthDto>> SeriesAsync(IReadOnlyList<CashEvent> events) =>
                (await MonthsAsync(first, last, events, ct)).Select(m =>
                {
                    var groups = toCard[m.Month].GroupBy(x => (Name: x.CategoryName ?? x.Event.Description, x.CardName, Month: x.InvoiceMonth!.Value, x.Event.Date))
                        .Select(g => new MarginGroupDto(g.Key.Name, g.Count(), Money.Format(-g.Sum(x => x.Event.Amount)), g.Key.CardName!, g.Key.Month.ToString("yyyy-MM"), g.Key.Date))
                        .OrderBy(x => x.InvoiceDueDate).ThenByDescending(x => Money.Parse(x.Amount)).ToArray();
                    return new MarginMonthDto(m.Month.ToString("yyyy-MM"), Money.Format(m.Opening), Money.Format(m.Inflow), Money.Format(m.Outflow), Money.Format(m.Bills),
                        Money.Format(m.Invoices), Money.Format(m.Other), Money.Format(m.Closing), Money.Format(ReserveMargin.Margin(m.Closing, reserve)),
                        ReserveMargin.Status(m.Closing, reserve), Money.Format(ReserveMargin.Shortfall(m.Closing, reserve)),
                        Money.Format(groups.Sum(x => Money.Parse(x.Amount))), groups);
                }).ToArray();

            var months = await SeriesAsync(baseEvents);
            MarginSimulationDto? simulation = null;
            if (amount is decimal value && value > 0)
            {
                var (date, cardName, invoiceMonth) = (Today, (string?)null, (DateOnly?)null);
                if (cardCode is Guid code)
                {
                    var card = cards.FirstOrDefault(x => x.Code == code) ?? throw new Shared.Exceptions.NotFoundException("Cartão não encontrado ou inativo.");
                    var landing = CardSchedule.LandingFor(Today, card.ClosingDay, card.DueDay, card.ClosingDayRule);
                    (date, cardName, invoiceMonth) = (landing.Cycle.DueDate, card.Name, landing.Cycle.ReferenceMonth);
                }
                var simulated = await SeriesAsync([.. baseEvents, new CashEvent(date, -value, "simulation", "Gasto simulado", null, true)]);
                simulation = new MarginSimulationDto(Money.Format(value), date, cardName, invoiceMonth?.ToString("yyyy-MM"), cardName is null ? null : date, simulated);
            }
            var landings = cards.OrderBy(x => x.Name).Select(x =>
            {
                var (cycle, lastDay, nextMonth) = CardSchedule.LandingFor(Today, x.ClosingDay, x.DueDay, x.ClosingDayRule);
                return new CardLandingDto(x.Code, x.Name, x.ClosingDay, x.DueDay, cycle.ReferenceMonth.ToString("yyyy-MM"), cycle.ClosingDate, cycle.DueDate, lastDay, nextMonth.ToString("yyyy-MM"));
            }).ToArray();
            return new MarginDto(Today, Money.Format(reserve), ReserveMargin.AttentionThreshold.ToString("0.00", CultureInfo.InvariantCulture), months, landings, simulation);
        }

        public async Task<AgendaDto> AgendaAsync(CancellationToken ct)
        {
            // The agenda is about when each bill must be paid, so it keeps the bill's due date; a bill paid with a card says so.
            var items = (await OpenItemsAsync(Today.AddDays(90), ct)).Select(x => new AgendaItemDto(x.DueDate, x.Event.Kind,
                x.CardName is null ? x.Event.Description : $"{x.Event.Description} · no cartão {x.CardName}",
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
            foreach (var open in await OpenItemsAsync(Today.AddDays(30), ct))
            {
                // Due-date alerts follow the bill's own due date, even when its cash leaves later through a card invoice.
                var (item, overdue, link) = (open.Event with { Date = open.DueDate }, open.Overdue, open.Link);
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
