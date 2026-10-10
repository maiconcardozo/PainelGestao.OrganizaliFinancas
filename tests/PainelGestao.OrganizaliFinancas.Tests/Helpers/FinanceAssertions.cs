using System.Net;
using System.Net.Http.Json;
using Dapper;
using FluentAssertions;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Accounts;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Application.Income;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>M4 acceptance: accounts, transfers, forecasts, receipts, reversals, idempotency and the D24/D25 rules.</summary>
    internal static class FinanceAssertions
    {
        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader, string connection)
        {
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, settings.TimeZone));
            var month = new DateOnly(today.Year, today.Month, 1);
            var window = IncomeAverage.Window(today);
            var opening = window.From.AddYears(-1);

            var checking = await Create<AccountDto>(owner, "/api/accounts", new AccountPayload("Conta corrente", true, 0, opening, "1000.00"));
            var savings = await Create<AccountDto>(owner, "/api/accounts", new AccountPayload("Poupança", true, 0, opening, "0.00"));
            var maicon = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Maicon", true, 0));
            var spouse = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Esposa", true, 0));
            (await other.GetAsync($"/api/accounts/{checking.Code}/statement")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Acceptance: forecast 4000, receipts 2000 + 1800 => realized 3800, residual 200.
            var salary = await Create<IncomeSourceDto>(owner, "/api/income-sources", new IncomeSourcePayload(maicon.Code, null, "Salário", true, 0,
                month, null, "monthly", "fixed", "4000.00", "5000.00"));
            var forecast = await CurrentForecast(owner, salary.Code, month);
            forecast.ExpectedAmount.Should().Be("4000.00");
            var key = Guid.NewGuid();
            var first = new ReceiptPayload(checking.Code, maicon.Code, null, null, today, "2000.00", null, [new(forecast.Code, "2000.00")]);
            var receipt = await Post<ReceiptDto>(owner, "/api/receipts", first, key, HttpStatusCode.Created);
            (await Post<ReceiptDto>(owner, "/api/receipts", first, key, HttpStatusCode.Created)).Code.Should().Be(receipt.Code, "repeating the request returns the original");
            await PostStatus(owner, "/api/receipts", first with { Amount = "2100.00" }, key, HttpStatusCode.Conflict);
            await PostStatus(owner, "/api/receipts", first, null, HttpStatusCode.BadRequest);
            receipt.SourceName.Should().Be("Salário", "the source is derived from the allocated forecast");
            var second = await Post<ReceiptDto>(owner, "/api/receipts", first with { Amount = "1800.00", Allocations = [new(forecast.Code, "1800.00")] }, Guid.NewGuid(), HttpStatusCode.Created);
            forecast = await CurrentForecast(owner, salary.Code, month);
            (forecast.ReceivedAmount, forecast.Difference, forecast.Residual).Should().Be(("3800.00", "-200.00", "200.00"));
            await PostStatus(owner, "/api/receipts", first with { Allocations = [new(forecast.Code, "2000.01")] }, Guid.NewGuid(), HttpStatusCode.Conflict);
            await PostStatus(owner, "/api/receipts", first with { ReceiptDate = today.AddDays(1) }, Guid.NewGuid(), HttpStatusCode.BadRequest);
            await PostStatus(owner, "/api/receipts", first with { ReceiptDate = opening.AddDays(-1), Allocations = null }, Guid.NewGuid(), HttpStatusCode.Conflict);
            await PostStatus(reader, "/api/receipts", first, Guid.NewGuid(), HttpStatusCode.Forbidden);
            await PostStatus(other, "/api/receipts", first, Guid.NewGuid(), HttpStatusCode.NotFound);
            (await Total(owner)).Should().Be("4800.00");

            // Transfer of 500 keeps the household total; reversal restores both accounts once.
            var transferKey = Guid.NewGuid();
            var transfer = await Post<TransferDto>(owner, "/api/transfers", new TransferPayload(checking.Code, savings.Code, today, "500.00", null), transferKey, HttpStatusCode.Created);
            await Post<TransferDto>(owner, "/api/transfers", new TransferPayload(checking.Code, savings.Code, today, "500.00", null), transferKey, HttpStatusCode.Created);
            var overview = (await owner.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data;
            overview.Total.Should().Be("4800.00");
            overview.Accounts.Single(x => x.Code == savings.Code).Balance.Should().Be("500.00");
            await PostStatus(owner, "/api/transfers", new TransferPayload(checking.Code, checking.Code, today, "1.00", null), Guid.NewGuid(), HttpStatusCode.BadRequest);
            await PostStatus(reader, $"/api/transfers/{transfer.Code}/reversals", new ReversalPayload("Lançada errada", null), Guid.NewGuid(), HttpStatusCode.Forbidden);
            (await Post<TransferDto>(owner, $"/api/transfers/{transfer.Code}/reversals", new ReversalPayload("Lançada errada", null), Guid.NewGuid(), HttpStatusCode.Created))
                .IsReversed.Should().BeTrue();
            await PostStatus(owner, $"/api/transfers/{transfer.Code}/reversals", new ReversalPayload("De novo", null), Guid.NewGuid(), HttpStatusCode.Conflict);
            (await owner.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data.Accounts.Single(x => x.Code == savings.Code).Balance.Should().Be("0.00");

            // Reversing a receipt keeps the original movement and recomposes balance and forecast.
            await PostStatus(owner, $"/api/receipts/{second.Code}/reversals", new ReversalPayload("Depósito devolvido", null), Guid.NewGuid(), HttpStatusCode.Created);
            forecast = await CurrentForecast(owner, salary.Code, month);
            (forecast.ReceivedAmount, forecast.Residual).Should().Be(("2000.00", "2000.00"));
            (await Total(owner)).Should().Be("3000.00");
            var statement = (await owner.GetFromJsonAsync<SuccessDetails<StatementDto>>($"/api/accounts/{checking.Code}/statement?from={today.AddDays(-365):yyyy-MM-dd}&to={today:yyyy-MM-dd}"))!.Data;
            statement.OpeningBalance.Should().Be("1000.00");
            statement.ClosingBalance.Should().Be("3000.00");
            statement.Lines.Should().Contain(x => x.ReceiptCode == second.Code && x.Kind == "receipt").And.Contain(x => x.ReceiptCode == second.Code && x.Kind == "reversal");
            (await owner.PutAsJsonAsync($"/api/accounts/{checking.Code}", new AccountPayload("Conta corrente", true, 0, opening.AddDays(1), "1000.00")))
                .StatusCode.Should().Be(HttpStatusCode.Conflict, "the opening base is frozen after movements");

            // D25: PJ base 14000 receiving 16000 is one receipt; the excess is a comparison only.
            var pj = await Create<IncomeSourceDto>(owner, "/api/income-sources", new IncomeSourcePayload(maicon.Code, null, "PJ", true, 0, month, null, "monthly", "fixed", "14000.00", null));
            var pjForecast = await CurrentForecast(owner, pj.Code, month);
            await Post<ReceiptDto>(owner, "/api/receipts", new ReceiptPayload(checking.Code, maicon.Code, pj.Code, null, today, "16000.00", "Nota fiscal", [new(pjForecast.Code, "16000.00")]), Guid.NewGuid(), HttpStatusCode.Created);
            pjForecast = await CurrentForecast(owner, pj.Code, month);
            (pjForecast.ReceivedAmount, pjForecast.Difference, pjForecast.Residual).Should().Be(("16000.00", "2000.00", "0.00"));
            (await owner.PutAsJsonAsync($"/api/income-sources/{pj.Code}", new IncomeSourcePayload(maicon.Code, null, "PJ", true, 1, month, null, "monthly", "fixed", "15000.00", null)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await CurrentForecast(owner, pj.Code, month)).ExpectedAmount.Should().Be("14000.00", "a forecast with receipts keeps its original version");
            (await Forecasts(owner, month.AddMonths(1))).Single(x => x.SourceCode == pj.Code).ExpectedAmount.Should().Be("15000.00");
            var generation = (await (await owner.PostAsync("/api/income-forecasts/generate", null)).Content.ReadFromJsonAsync<SuccessDetails<ForecastGenerationDto>>())!.Data;
            (generation.Inserted, generation.Updated, generation.Cancelled).Should().Be((0, 0, 0), "generation is idempotent");

            // D24: variable commission has no expected amount; the average of closed months becomes its labeled estimate.
            var commission = await Create<IncomeSourceDto>(owner, "/api/income-sources", new IncomeSourcePayload(spouse.Code, null, "Comissões", true, 0, month, null, "monthly", "variable", null, null));
            await Post<ReceiptDto>(owner, "/api/receipts", new ReceiptPayload(savings.Code, spouse.Code, commission.Code, null, window.From, "9000.00", null, null), Guid.NewGuid(), HttpStatusCode.Created);
            var expectedAverage = Money.Format(IncomeAverage.Of(9000m, window));
            var estimate = (await Forecasts(owner, month)).Single(x => x.SourceCode == commission.Code);
            (estimate.ExpectedAmount, estimate.EstimatedAmount).Should().Be((null, expectedAverage));
            var summary = (await owner.GetFromJsonAsync<SuccessDetails<IncomeSummaryDto>>("/api/income/summary"))!.Data;
            summary.AverageMonths.Should().Be(window.Months);
            summary.People.Single(x => x.Code == spouse.Code).Average.Should().Be(expectedAverage);
            summary.Sources.Single(x => x.Code == commission.Code).Total.Should().Be("9000.00");
            summary.Months.Should().Contain(x => x.PersonCode == maicon.Code && x.Month == month.ToString("yyyy-MM") && x.Expected == "18000.00" && x.Received == "18000.00");
            (await reader.GetAsync("/api/income/summary")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await other.GetFromJsonAsync<SuccessDetails<IncomeSourceDto[]>>("/api/income-sources"))!.Data.Should().BeEmpty();

            await using var database = new MySqlConnection(connection);
            (await database.QuerySingleAsync<long>("SELECT COUNT(*) FROM Receipt WHERE Code=@Code", new { receipt.Code })).Should().Be(1);
            (await database.QuerySingleAsync<long>("SELECT COUNT(*) FROM AccountMovement m JOIN Receipt r ON r.Id=m.IdReceipt WHERE r.Code=@Code", new { second.Code }))
                .Should().Be(2, "the original movement and its compensation coexist");
            (await database.QuerySingleAsync<long>("SELECT COUNT(*) FROM IncomeForecast f JOIN IncomeSource s ON s.Id=f.IdIncomeSource WHERE s.Code=@Code GROUP BY f.OccurrenceDate HAVING COUNT(*)>1 UNION ALL SELECT 0", new { salary.Code }))
                .Should().Be(0);
        }

        private static async Task<IReadOnlyList<ForecastDto>> Forecasts(HttpClient client, DateOnly month) =>
            (await client.GetFromJsonAsync<SuccessDetails<ForecastDto[]>>($"/api/income-forecasts?from={month:yyyy-MM-dd}&to={month:yyyy-MM-dd}"))!.Data;
        private static async Task<ForecastDto> CurrentForecast(HttpClient client, Guid source, DateOnly month) =>
            (await Forecasts(client, month)).Single(x => x.SourceCode == source);
        private static async Task<string> Total(HttpClient client) =>
            (await client.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data.Total;
        private static async Task<T> Create<T>(HttpClient client, string path, object payload)
        {
            var response = await client.PostAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data;
        }
        private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object payload, Guid? key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
            if (key is Guid value) request.Headers.Add("Idempotency-Key", value.ToString());
            return await client.SendAsync(request);
        }
        private static async Task PostStatus(HttpClient client, string path, object payload, Guid? key, HttpStatusCode expected)
        {
            var response = await Send(client, path, payload, key);
            response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
        }
        private static async Task<T> Post<T>(HttpClient client, string path, object payload, Guid key, HttpStatusCode expected)
        {
            var response = await Send(client, path, payload, key);
            response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data;
        }
    }
}
