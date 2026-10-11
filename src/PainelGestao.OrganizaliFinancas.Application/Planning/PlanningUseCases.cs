using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Planning
{
    public class GetBudgetHandler(IUnitOfWork work) : IRequestHandler<GetBudgetQuery, BudgetDto>
    {
        public async Task<BudgetDto> Handle(GetBudgetQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var planner = await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct);
            return (await planner.BudgetAsync(planner.MonthOf(request.Month), ct)).Dto;
        }
    }
    /// <summary>RF28: limits are independent from spending; copying from the previous month is done by the screen and saved only after review.</summary>
    public class SaveBudgetsHandler(IUnitOfWork work) : IRequestHandler<SaveBudgetsCommand, BudgetDto>
    {
        public async Task<BudgetDto> Handle(SaveBudgetsCommand request, CancellationToken ct)
        {
            var month = Obligation.Month(request.Month);
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "budgets.write", ct);
                var categories = (await work.PlanningRepository.CategoriesAsync(user.IdTenant, ct)).ToDictionary(x => x.Code);
                var current = (await work.PlanningRepository.BudgetsAsync(user.IdTenant, HouseholdCalendar.Day(month), ct)).ToDictionary(x => x.CategoryCode);
                foreach (var item in request.Payload.Items)
                {
                    var category = categories.GetValueOrDefault(item.CategoryCode) ?? throw new NotFoundException("Categoria não encontrada nesta casa.");
                    decimal? amount = item.Amount is null ? null : Money.Parse(item.Amount);
                    if (amount is not null && !category.IsActive && !current.ContainsKey(item.CategoryCode)) throw new ConflictException($"{category.Name} está inativa.");
                    if (current.TryGetValue(item.CategoryCode, out var existing) ? existing.Amount == amount : amount is null) continue;
                    work.PlanningRepository.SaveBudget(user.IdTenant, category.Id, HouseholdCalendar.Day(month), amount, user.Code.ToString());
                }
                return true;
            }, ct);
            var planner = await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct);
            return (await planner.BudgetAsync(month, ct)).Dto;
        }
    }
    public class GetAlertPreferenceHandler(IUnitOfWork work) : IRequestHandler<GetAlertPreferenceQuery, AlertPreferenceDto>
    {
        public async Task<AlertPreferenceDto> Handle(GetAlertPreferenceQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var x = await work.PlanningRepository.PreferenceAsync(request.Identity.IdTenant, ct) ?? new AlertPreferenceRecord();
            return new AlertPreferenceDto(x.WarningPercent, x.LimitPercent, x.DueSoonDays, x.Version);
        }
    }
    public class SaveAlertPreferenceHandler(IUnitOfWork work) : IRequestHandler<SaveAlertPreferenceCommand, AlertPreferenceDto>
    {
        public async Task<AlertPreferenceDto> Handle(SaveAlertPreferenceCommand request, CancellationToken ct)
        {
            var payload = request.Payload;
            BudgetEvaluation.ValidateThresholds(payload.WarningPercent, payload.LimitPercent);
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "budgets.write", ct);
                var current = await work.PlanningRepository.PreferenceAsync(user.IdTenant, ct);
                if ((current?.Version ?? 0) != payload.Version) throw new ConflictException("Preferências alteradas. Atualize a página antes de salvar.");
                work.PlanningRepository.SavePreference(user.IdTenant, payload.WarningPercent, payload.LimitPercent, payload.DueSoonDays, payload.Version, user.Code.ToString());
                return true;
            }, ct);
            var x = (await work.PlanningRepository.PreferenceAsync(request.Identity.IdTenant, ct))!;
            return new AlertPreferenceDto(x.WarningPercent, x.LimitPercent, x.DueSoonDays, x.Version);
        }
    }
    public class ListAlertsHandler(IUnitOfWork work) : IRequestHandler<ListAlertsQuery, IReadOnlyList<AlertDto>>
    {
        public async Task<IReadOnlyList<AlertDto>> Handle(ListAlertsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var planner = await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct);
            return (await planner.AlertsAsync(ct)).Select(HouseholdPlanner.Map).ToArray();
        }
    }
    /// <summary>Read, snooze or resolve the current state of a condition. It never changes the data that caused the alert.</summary>
    public class AlertActionHandler(IUnitOfWork work) : IRequestHandler<AlertActionCommand, IReadOnlyList<AlertDto>>
    {
        public async Task<IReadOnlyList<AlertDto>> Handle(AlertActionCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var planner = await HouseholdPlanner.CreateAsync(work, user.IdTenant, ct);
                var alert = (await planner.AlertsAsync(ct)).FirstOrDefault(x => x.Condition.Key == request.Payload.Key)
                    ?? throw new NotFoundException("Alerta não está mais ativo.");
                var status = request.Payload.Action switch { "snooze" => "snoozed", "resolve" => "resolved", _ => "read" };
                DateTime? until = status == "snoozed" ? HouseholdCalendar.Day(planner.Today.AddDays(request.Payload.Days!.Value)) : null;
                work.PlanningRepository.SaveAlertState(user.IdTenant, alert.Condition.Key, alert.Condition.Fingerprint, status, until, user.Code.ToString());
                return true;
            }, ct);
            var refreshed = await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct);
            return (await refreshed.AlertsAsync(ct)).Select(HouseholdPlanner.Map).ToArray();
        }
    }
    public class GetProjectionHandler(IUnitOfWork work) : IRequestHandler<GetProjectionQuery, ProjectionDto>
    {
        public async Task<ProjectionDto> Handle(GetProjectionQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var planner = await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct);
            return (await planner.ProjectionAsync(planner.Today.AddDays(request.Days ?? HouseholdPlanner.ProjectionDays), ct)).Dto;
        }
    }
    public class GetAgendaHandler(IUnitOfWork work) : IRequestHandler<GetAgendaQuery, AgendaDto>
    {
        public async Task<AgendaDto> Handle(GetAgendaQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return await (await HouseholdPlanner.CreateAsync(work, request.Identity.IdTenant, ct)).AgendaAsync(ct);
        }
    }
    /// <summary>
    /// RF27: daily flow of a month and monthly series (five months back, six ahead). Past days are realized movements; the rest is projected
    /// from open items. "Other" holds opening balances of accounts created in the month, so opening + flows + other = closing.
    /// </summary>
    public class GetCashFlowHandler(IUnitOfWork work) : IRequestHandler<GetCashFlowQuery, CashFlowDto>
    {
        public async Task<CashFlowDto> Handle(GetCashFlowQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var planner = await HouseholdPlanner.CreateAsync(work, idTenant, ct);
            var today = planner.Today;
            var current = Obligation.Month(today);
            var month = planner.MonthOf(request.Month);
            var first = current.AddMonths(-5);
            var last = current.AddMonths(6);
            if (month < current.AddMonths(-24) || month > last) throw new InputValidationException(new Dictionary<string, string[]> { ["Month"] = ["Escolha um mês entre dois anos atrás e seis meses à frente."] });
            var horizon = (month > last ? month : last).AddMonths(1).AddDays(-1);
            var (projection, _) = await planner.ProjectionAsync(horizon, ct);
            var projected = projection.Days.SelectMany(x => x.Events.Select(e => (Day: x.Date, e.Amount))).ToArray();

            var months = new List<MonthFlowDto>();
            var realizedFrom = month < first ? month : first;
            var realized = (await work.PlanningRepository.MonthlyCashAsync(idTenant, HouseholdCalendar.Day(realizedFrom), HouseholdCalendar.Day(today.AddDays(1)), ct))
                .ToDictionary(x => DateOnly.FromDateTime(x.Period));
            var opening = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(first), ct);
            for (var m = first; m <= last; m = m.AddMonths(1))
            {
                var next = m.AddMonths(1);
                var real = realized.GetValueOrDefault(m) ?? new CashTotalRecord();
                var future = projected.Where(x => x.Day >= m && x.Day < next).ToArray();
                var inflow = real.Inflow + future.Where(x => x.Amount > 0).Sum(x => x.Amount);
                var outflow = real.Outflow + future.Where(x => x.Amount < 0).Sum(x => x.Amount);
                var closing = m <= current
                    ? await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(next <= today ? next : today.AddDays(1)), ct) + future.Sum(x => x.Amount)
                    : opening + inflow + outflow;
                months.Add(new MonthFlowDto(m.ToString("yyyy-MM"), Money.Format(opening), Money.Format(inflow), Money.Format(outflow),
                    Money.Format(closing - opening - inflow - outflow), Money.Format(closing), m >= current));
                opening = closing;
            }

            var monthStart = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(month), ct);
            var monthEnd = month.AddMonths(1);
            var daily = (await work.PlanningRepository.DailyCashAsync(idTenant, HouseholdCalendar.Day(month), HouseholdCalendar.Day(monthEnd <= today ? monthEnd : today.AddDays(1)), ct))
                .ToDictionary(x => DateOnly.FromDateTime(x.Period));
            var openings = (await work.AccountRepository.ListAsync(idTenant, HouseholdCalendar.Day(today), ct))
                .Where(x => DateOnly.FromDateTime(x.OpeningDate) >= month && DateOnly.FromDateTime(x.OpeningDate) < monthEnd)
                .GroupBy(x => DateOnly.FromDateTime(x.OpeningDate)).ToDictionary(x => x.Key, x => x.Sum(a => a.OpeningBalance));
            var balance = monthStart;
            var days = new List<DayFlowDto>();
            for (var day = month; day < monthEnd; day = day.AddDays(1))
            {
                var isProjected = day > today;
                var real = daily.GetValueOrDefault(day) ?? new CashTotalRecord();
                var future = projected.Where(x => x.Day == day).ToArray();
                var inflow = real.Inflow + future.Where(x => x.Amount > 0).Sum(x => x.Amount);
                var outflow = real.Outflow + future.Where(x => x.Amount < 0).Sum(x => x.Amount);
                balance += inflow + outflow + openings.GetValueOrDefault(day);
                if (inflow != 0 || outflow != 0 || day == today || day == month || day == monthEnd.AddDays(-1))
                    days.Add(new DayFlowDto(day, Money.Format(inflow), Money.Format(outflow), Money.Format(balance), isProjected));
            }
            return new CashFlowDto(month.ToString("yyyy-MM"), today, Money.Format(monthStart), Money.Format(balance), days, months);
        }
    }
    /// <summary>RF26: one consistent picture of the month. Every number links to the screen that explains it.</summary>
    public class GetDashboardHandler(IUnitOfWork work) : IRequestHandler<GetDashboardQuery, DashboardDto>
    {
        public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var planner = await HouseholdPlanner.CreateAsync(work, idTenant, ct);
            var month = planner.MonthOf(request.Month);
            var balance = await work.PlanningRepository.BalanceBeforeAsync(idTenant, HouseholdCalendar.Day(planner.Today.AddDays(1)), ct);
            var forecasts = await work.IncomeRepository.ListForecastsAsync(idTenant, HouseholdCalendar.Day(month), HouseholdCalendar.Day(month), ct);
            var expected = forecasts.Where(x => x.Status != "closed" || x.ReceivedAmount > 0).Sum(x => x.ExpectedAmount ?? 0);
            var received = (await work.IncomeRepository.ReceivedTotalsAsync(idTenant, HouseholdCalendar.Day(month), HouseholdCalendar.Day(month.AddMonths(1)), ct)).Sum(x => x.Total);
            var pending = forecasts.Where(x => x.Status == "open" && x.ExpectedAmount is not null).Sum(x => Math.Max(0, x.ExpectedAmount!.Value - x.ReceivedAmount));
            var obligations = await work.ExpenseRepository.ListAsync(idTenant, HouseholdCalendar.Day(month), HouseholdCalendar.Day(month.AddMonths(1).AddDays(-1)), false, ct);
            var states = obligations.Where(x => x.Status != "cancelled").Select(x => (Record: x,
                State: ObligationBalance.Of(x.Amount, x.Adjustments, x.Paid, false, DateOnly.FromDateTime(x.DueDate), planner.Today))).ToArray();
            var (_, projection) = await planner.ProjectionAsync(planner.Today.AddDays(HouseholdPlanner.ProjectionDays), ct);
            var (budget, _) = await planner.BudgetAsync(month, ct);
            var alerts = (await planner.AlertsAsync(ct)).Select(HouseholdPlanner.Map).ToArray();
            return new DashboardDto(month.ToString("yyyy-MM"), planner.Today, Money.Format(balance),
                new MonthIncomeDto(Money.Format(expected), Money.Format(received), Money.Format(pending)),
                new MonthExpenseDto(Money.Format(states.Sum(x => x.Record.Amount + x.Record.Adjustments)), Money.Format(states.Sum(x => x.Record.Paid)),
                    Money.Format(states.Sum(x => Math.Max(0, x.State.OpenBalance))), Money.Format(states.Where(x => x.State.IsOverdue).Sum(x => x.State.OpenBalance))),
                projection, budget, await planner.AgendaAsync(ct), alerts.Take(6).ToArray(), alerts.Count(x => x.Status == "new"));
        }
    }
    /// <summary>RF32: the same filtered lines feed the chart totals and the list, so both always show the same amount.</summary>
    public class GetAnalysisHandler(IUnitOfWork work) : IRequestHandler<GetAnalysisQuery, AnalysisDto>
    {
        public async Task<AnalysisDto> Handle(GetAnalysisQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var planner = await HouseholdPlanner.CreateAsync(work, idTenant, ct);
            var month = planner.MonthOf(request.Month);
            var lines = await work.PlanningRepository.AnalysisLinesAsync(idTenant, HouseholdCalendar.Day(month), ct);
            var categories = await work.PlanningRepository.CategoriesAsync(idTenant, ct);
            var selected = request.CategoryCode is Guid root ? Descendants(categories, root) : null;
            var shares = request.PersonCode is null ? [] : await work.ExpenseRepository.ListSharesAsync(idTenant, lines.Where(x => x.Source == "bill").Select(x => x.Code).ToArray(), ct);
            string Status(AnalysisLineRecord x)
            {
                if (x.Source == "card") return "card";
                var open = x.Amount - x.Paid;
                return open <= 0 ? "paid" : DateOnly.FromDateTime(x.DueDate) < planner.Today ? "overdue" : "open";
            }
            var filtered = lines.Where(x => selected is null || x.CategoryCode is Guid code && selected.Contains(code))
                .Where(x => request.PersonCode is not Guid person || (x.Source == "bill" ? shares.Any(s => s.ObligationCode == x.Code && s.PersonCode == person) : x.BuyerCode == person))
                .Where(x => request.AccountCode is not Guid account || x.AccountCodes?.Split(',').Contains(account.ToString()) == true)
                .Where(x => request.Status is null || Status(x) == request.Status || (request.Status == "open" && Status(x) == "overdue"))
                .ToArray();
            var byCategory = filtered.GroupBy(x => (x.CategoryCode, x.CategoryName)).Select(x => new CategoryTotalDto(x.Key.CategoryCode, x.Key.CategoryName ?? "Sem categoria",
                Money.Format(x.Sum(l => l.Amount)))).OrderByDescending(x => Money.Parse(x.Amount)).ToArray();
            return new AnalysisDto(month.ToString("yyyy-MM"), request.CategoryCode, request.PersonCode, request.AccountCode, request.Status,
                Money.Format(filtered.Sum(x => x.Amount)), Money.Format(filtered.Sum(x => x.Paid)), byCategory,
                filtered.Select(x => new AnalysisLineDto(x.Source, x.Code, x.Description, x.CategoryCode, x.CategoryName, DateOnly.FromDateTime(x.DueDate),
                    Money.Format(x.Amount), Money.Format(x.Paid), Status(x), x.CardName)).ToArray());
        }
        private static HashSet<Guid> Descendants(IReadOnlyList<CategoryRecord> categories, Guid root)
        {
            var result = new HashSet<Guid> { root };
            for (var added = true; added;)
            {
                added = false;
                foreach (var category in categories.Where(x => x.ParentCode is Guid parent && result.Contains(parent) && !result.Contains(x.Code)))
                    added |= result.Add(category.Code);
            }
            return result;
        }
    }
}
