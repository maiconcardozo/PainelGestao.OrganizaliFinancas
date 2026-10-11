using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Accounts;
using PainelGestao.OrganizaliFinancas.Application.Catalogs;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Application.Planning;
using static PainelGestao.OrganizaliFinancas.Tests.Helpers.FinanceAssertions;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>M7 acceptance: budget bands and roll-up, deduplicated alerts, projection with first deficit, cash flow continuity, agenda and filters.</summary>
    internal static class PlanningAssertions
    {
        private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader)
        {
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, settings.TimeZone));
            var month = new DateOnly(today.Year, today.Month, 1);
            var account = await Create<AccountDto>(owner, "/api/accounts", new AccountPayload("Conta do orçamento", true, 0, month.AddMonths(-1), "3000.00"));
            var market = await Create<CatalogItemDto>(owner, "/api/catalogs/categories", new CatalogPayload("Mercado M7", true, 0, null));
            var greens = await Create<CatalogItemDto>(owner, "/api/catalogs/categories", new CatalogPayload("Hortifrúti M7", true, 0, market.Code));
            ExpenseDetailsPayload Details(string description, Guid category) => new(description, null, null, category, null, null, null, null, null, null);

            // Rule 8 examples: 900 of 1000 approaches; 1100 exceeds by 100 (10%). Children roll up into the parent.
            (await Put<BudgetDto>(reader, $"/api/budgets/{month:yyyy-MM-dd}", new SaveBudgetsPayload([new(market.Code, "1000.00")]), HttpStatusCode.Forbidden)).Should().BeNull();
            var budget = (await Put<BudgetDto>(owner, $"/api/budgets/{month:yyyy-MM-dd}", new SaveBudgetsPayload([new(market.Code, "1000.00")])))!;
            budget.Lines.Single(x => x.CategoryCode == market.Code).Band.Should().Be("ok");
            var first = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Feira", greens.Code), month, today.AddDays(3), "900.00", 0, null), Guid.NewGuid(), HttpStatusCode.Created);
            var line = (await Budget(owner, month)).Lines.Single(x => x.CategoryCode == market.Code);
            (line.Committed, line.Band, line.UsedPercent).Should().Be(("900.00", "warning", "90"));
            var key = $"budget:{market.Code}:{month:yyyy-MM}";
            (await Alerts(owner)).Single(x => x.Key == key).Should().Match<AlertDto>(x => x.Status == "new" && x.Severity == "warning");
            (await Alerts(reader)).Should().Contain(x => x.Key == key, "every member sees the household alerts");
            (await reader.PostAsJsonAsync("/api/alerts/actions", new AlertActionPayload(key, "read", null))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await Act(owner, new AlertActionPayload(key, "read", null))).Single(x => x.Key == key).Status.Should().Be("read", "reading does not resolve the cause");
            (await Act(owner, new AlertActionPayload(key, "resolve", null))).Should().NotContain(x => x.Key == key);
            (await Alerts(owner)).Should().NotContain(x => x.Key == key, "a resolved condition is not repeated while it stays the same");
            await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Supermercado", market.Code), month, today.AddDays(4), "200.00", 0, null), Guid.NewGuid(), HttpStatusCode.Created);
            var exceeded = (await Alerts(owner)).Single(x => x.Key == key);
            (exceeded.Status, exceeded.Severity).Should().Be(("new", "critical"), "a new band brings the alert back");
            exceeded.Detail.Should().Contain("Excesso de R$ 100,00 (10%)");
            line = (await Budget(owner, month)).Lines.Single(x => x.CategoryCode == market.Code);
            (line.Committed, line.Band, line.Excess).Should().Be(("1100.00", "exceeded", "100.00"));
            budget = (await Put<BudgetDto>(owner, $"/api/budgets/{month:yyyy-MM-dd}", new SaveBudgetsPayload([new(market.Code, null), new(greens.Code, "0.00")])))!;
            budget.Lines.Single(x => x.CategoryCode == market.Code).Should().Match<BudgetLineDto>(x => x.Limit == null && x.Band == "none");
            budget.Lines.Single(x => x.CategoryCode == greens.Code).Band.Should().Be("exceeded", "a zero limit is valid and any spending exceeds it");
            (await other.GetFromJsonAsync<SuccessDetails<BudgetDto>>("/api/budgets"))!.Data.Lines.Should().NotContain(x => x.CategoryCode == market.Code);

            var preference = (await owner.GetFromJsonAsync<SuccessDetails<AlertPreferenceDto>>("/api/alert-preferences"))!.Data;
            preference.Should().Be(new AlertPreferenceDto(80, 100, 5, 0));
            (await Put<AlertPreferenceDto>(owner, "/api/alert-preferences", new AlertPreferencePayload(70, 100, 7, 0)))!.Version.Should().Be(1);
            await Put<AlertPreferenceDto>(owner, "/api/alert-preferences", new AlertPreferencePayload(70, 100, 7, 0), HttpStatusCode.Conflict);
            await Put<AlertPreferenceDto>(owner, "/api/alert-preferences", new AlertPreferencePayload(90, 80, 7, 1), HttpStatusCode.BadRequest);

            // RF30: the projection starts at the real balance and finds the first deficit with its cause; the real balance never changes.
            var total = (await owner.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data.Total;
            var huge = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Reforma", market.Code), month, today.AddDays(2), "9000000.00", 0, null),
                Guid.NewGuid(), HttpStatusCode.Created);
            var projection = (await owner.GetFromJsonAsync<SuccessDetails<ProjectionDto>>("/api/projection"))!.Data;
            projection.Start.Should().Be(total);
            projection.FirstDeficit.Should().NotBeNull().And.BeOnOrBefore(today.AddDays(2));
            projection.DeficitCauses.Should().Contain(x => x.Code == huge.Code);
            (await Alerts(owner)).Should().Contain(x => x.Kind == "deficit" && x.Severity == "critical");
            var dashboard = (await owner.GetFromJsonAsync<SuccessDetails<DashboardDto>>("/api/dashboard"))!.Data;
            dashboard.Balance.Should().Be(total, "the projection does not alter the real balance");
            dashboard.Agenda.Next7.Should().Contain(x => x.Code == huge.Code);
            dashboard.Budget.Lines.Should().Contain(x => x.CategoryCode == market.Code);
            dashboard.Alerts.Should().Contain(x => x.Kind == "deficit");
            await Ok<ExpenseDto>(owner, $"/api/expenses/{huge.Code}/cancel", new CancelExpensePayload("Teste de projeção", huge.Version));

            // RF27: monthly series is continuous and balanced; transfers between own accounts are not flows.
            var flow = (await owner.GetFromJsonAsync<SuccessDetails<CashFlowDto>>("/api/cash-flow"))!.Data;
            flow.Months.Should().HaveCount(12);
            for (var index = 0; index < flow.Months.Count; index++)
            {
                var m = flow.Months[index];
                (Number(m.Opening) + Number(m.Inflow) + Number(m.Outflow) + Number(m.Other)).Should().Be(Number(m.Closing));
                if (index > 0) m.Opening.Should().Be(flow.Months[index - 1].Closing);
                m.IsProjected.Should().Be(string.CompareOrdinal(m.Month, month.ToString("yyyy-MM")) >= 0);
            }
            flow.Days.Should().Contain(x => x.Date == today);
            (await owner.GetAsync("/api/cash-flow?month=2000-01-01")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // RF32: the same filtered lines feed the chart and the list.
            await Post<PaymentDto>(owner, "/api/payments", new PaymentPayload(account.Code, today, null, null, null,
                [new PaymentAllocationPayload(first.Code, "900.00", null, null, null)], null), Guid.NewGuid(), HttpStatusCode.Created);
            var analysis = await Analysis(owner, $"month={month:yyyy-MM-dd}&categoryCode={market.Code}");
            (analysis.Total, analysis.Lines.Count).Should().Be(("1100.00", 2), "the parent filter includes subcategories");
            analysis.ByCategory.Sum(x => Number(x.Amount)).Should().Be(Number(analysis.Total));
            (await Analysis(owner, $"month={month:yyyy-MM-dd}&categoryCode={market.Code}&status=paid")).Total.Should().Be("900.00");
            (await Analysis(owner, $"month={month:yyyy-MM-dd}&accountCode={account.Code}")).Lines.Should().ContainSingle(x => x.Code == first.Code);
            (await owner.GetAsync("/api/analysis?status=late")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static async Task<BudgetDto> Budget(HttpClient client, DateOnly month) =>
            (await client.GetFromJsonAsync<SuccessDetails<BudgetDto>>($"/api/budgets?month={month:yyyy-MM-dd}"))!.Data;
        private static async Task<IReadOnlyList<AlertDto>> Alerts(HttpClient client) => (await client.GetFromJsonAsync<SuccessDetails<AlertDto[]>>("/api/alerts"))!.Data;
        private static async Task<AnalysisDto> Analysis(HttpClient client, string query) =>
            (await client.GetFromJsonAsync<SuccessDetails<AnalysisDto>>($"/api/analysis?{query}"))!.Data;
        private static async Task<IReadOnlyList<AlertDto>> Act(HttpClient client, AlertActionPayload payload) => await Ok<AlertDto[]>(client, "/api/alerts/actions", payload);
        private static async Task<T> Ok<T>(HttpClient client, string path, object payload)
        {
            var response = await client.PostAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data;
        }
        private static async Task<T?> Put<T>(HttpClient client, string path, object payload, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var response = await client.PutAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
            return expected == HttpStatusCode.OK ? (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data : default;
        }
    }
}
