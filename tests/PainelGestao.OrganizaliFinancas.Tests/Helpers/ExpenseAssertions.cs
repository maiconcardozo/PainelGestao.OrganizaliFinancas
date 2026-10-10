using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using FluentAssertions;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Accounts;
using PainelGestao.OrganizaliFinancas.Application.ExpenseTypes;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using static PainelGestao.OrganizaliFinancas.Tests.Helpers.FinanceAssertions;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>M5 acceptance: obligations, adjustments, partial payments, concurrency, reversal, cancellation, installments, recurrence and split.</summary>
    internal static class ExpenseAssertions
    {
        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader, string connection)
        {
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, settings.TimeZone));
            var month = new DateOnly(today.Year, today.Month, 1);
            var account = await Create<AccountDto>(owner, "/api/accounts", new AccountPayload("Conta das despesas", true, 0, month.AddMonths(-1), "5000.00"));
            var ana = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Ana", true, 0));
            var bruno = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Bruno", true, 0));
            var light = await Create<ExpenseTypeDto>(owner, "/api/expense-types", new ExpenseTypePayload("Luz", true, 0, "single", null,
                [new CustomFieldDefinition("installation", "Instalação", "text", true, 20, null, null, null)]));
            ExpenseDetailsPayload Details(string description, ExpenseTypeDto? type = null, Dictionary<string, JsonElement>? fields = null,
                string? mode = null, SharePayload[]? shares = null) =>
                new(description, type?.Code, type?.Version, null, null, null, null, fields, mode, shares);
            var installation = new Dictionary<string, JsonElement> { ["installation"] = JsonSerializer.SerializeToElement("12345") };

            // RF16: competence, due date and type fields validated by the type version; idempotent creation.
            await PostStatus(owner, "/api/expenses", new ExpensePayload(Details("Luz", light), month, today, "250.00", 0, null), Guid.NewGuid(), HttpStatusCode.BadRequest);
            var key = Guid.NewGuid();
            var bill = new ExpensePayload(Details("Luz", light, installation), month, today, "250.00", 0, null);
            var expense = await Post<ExpenseDto>(owner, "/api/expenses", bill, key, HttpStatusCode.Created);
            (await Post<ExpenseDto>(owner, "/api/expenses", bill, key, HttpStatusCode.Created)).Code.Should().Be(expense.Code);
            (expense.Status, expense.OpenBalance, expense.Competence).Should().Be(("open", "250.00", month.ToString("yyyy-MM")));
            await PostStatus(reader, "/api/expenses", bill, Guid.NewGuid(), HttpStatusCode.Forbidden);
            (await other.GetAsync($"/api/expenses/{expense.Code}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Rule example: 250 + 10 interest − 5 discount − 100 paid = 155; the account drops only 100; reversal restores 255.
            await Adjust(owner, expense.Code, "interest", "10.00", HttpStatusCode.Created);
            (await Adjust(owner, expense.Code, "discount", "5.00", HttpStatusCode.Created))!.Expense.OpenBalance.Should().Be("255.00");
            await Adjust(owner, expense.Code, "discount", "300.00", HttpStatusCode.Conflict);
            var payment = await Post<PaymentDto>(owner, "/api/payments", Pay(account.Code, today, expense.Code, "100.00"), Guid.NewGuid(), HttpStatusCode.Created);
            var detail = await Detail(owner, expense.Code);
            (detail.Expense.Status, detail.Expense.OpenBalance, detail.Expense.Paid).Should().Be(("partial", "155.00", "100.00"));
            (await Balance(owner, account.Code)).Should().Be("4900.00");
            await PostStatus(owner, "/api/payments", Pay(account.Code, today, expense.Code, "155.01"), Guid.NewGuid(), HttpStatusCode.Conflict);
            await PostStatus(owner, "/api/payments", Pay(account.Code, today.AddDays(1), expense.Code, "1.00"), Guid.NewGuid(), HttpStatusCode.BadRequest);
            await PostStatus(reader, "/api/payments", Pay(account.Code, today, expense.Code, "1.00"), Guid.NewGuid(), HttpStatusCode.Forbidden);
            await PostStatus(owner, "/api/payments", Pay(account.Code, today, expense.Code, "1.00"), null, HttpStatusCode.BadRequest);
            await PostStatus(owner, $"/api/expenses/{expense.Code}/cancel", new CancelExpensePayload("Paga", detail.Expense.Version), null, HttpStatusCode.Conflict);
            await PostStatus(reader, $"/api/payments/{payment.Code}/reversals", new ReversalPayload("Erro", null), Guid.NewGuid(), HttpStatusCode.Forbidden);
            (await Post<PaymentDto>(owner, $"/api/payments/{payment.Code}/reversals", new ReversalPayload("Débito em duplicidade", null), Guid.NewGuid(), HttpStatusCode.Created))
                .IsReversed.Should().BeTrue();
            await PostStatus(owner, $"/api/payments/{payment.Code}/reversals", new ReversalPayload("De novo", null), Guid.NewGuid(), HttpStatusCode.Conflict);
            detail = await Detail(owner, expense.Code);
            (detail.Expense.Status, detail.Expense.OpenBalance).Should().Be(("open", "255.00"));
            detail.Payments.Should().ContainSingle().Which.IsReversed.Should().BeTrue();
            (await Balance(owner, account.Code)).Should().Be("5000.00");

            // Paying with interest in the same request settles the bill; the payment is the cash out.
            var settled = await Post<PaymentDto>(owner, "/api/payments", new PaymentPayload(account.Code, today, ana.Code, null, null,
                [new PaymentAllocationPayload(expense.Code, "257.00", "2.00", null, null)], "Atraso de um dia"), Guid.NewGuid(), HttpStatusCode.Created);
            settled.Amount.Should().Be("257.00");
            (await Detail(owner, expense.Code)).Expense.Status.Should().Be("paid");
            (await Balance(owner, account.Code)).Should().Be("4743.00");

            // Concurrent payments never settle more than the open balance.
            var shared = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Internet"), null, today, "100.00", 0, null), Guid.NewGuid(), HttpStatusCode.Created);
            var race = await Task.WhenAll(
                Send(owner, "/api/payments", Pay(account.Code, today, shared.Code, "60.00"), Guid.NewGuid()),
                Send(owner, "/api/payments", Pay(account.Code, today, shared.Code, "60.00"), Guid.NewGuid()));
            race.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
            (await Detail(owner, shared.Code)).Expense.OpenBalance.Should().Be("40.00");

            // RF22: an unpaid obligation is cancelled with a reason and kept in history.
            var unpaid = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Assinatura"), null, today, "30.00", 0, null), Guid.NewGuid(), HttpStatusCode.Created);
            var cancelled = await Ok<ExpenseDto>(owner, $"/api/expenses/{unpaid.Code}/cancel", new CancelExpensePayload("Serviço cancelado", unpaid.Version));
            (cancelled.Status, cancelled.CancelReason).Should().Be(("cancelled", "Serviço cancelado"));
            await PostStatus(owner, "/api/payments", Pay(account.Code, today, unpaid.Code, "1.00"), Guid.NewGuid(), HttpStatusCode.Conflict);

            // RF18: 100 in 3 installments sums exactly 100, each in its own competence.
            var preview = await Ok<InstallmentPreviewDto>(owner, "/api/expense-installments/preview", new InstallmentPreviewPayload("100.00", 3, today, "monthly"));
            preview.Items.Select(x => x.Amount).Should().Equal("33.33", "33.33", "33.34");
            var purchase = await Post<InstallmentPlanDto>(owner, "/api/expense-installments", new InstallmentPayload(Details("Cadeira"),
                new InstallmentPreviewPayload("100.00", 3, today, "monthly")), Guid.NewGuid(), HttpStatusCode.Created);
            purchase.Installments.Select(x => (x.InstallmentNumber, x.Amount)).Should().Equal((1, "33.33"), (2, "33.33"), (3, "33.34"));
            purchase.Installments.Select(x => x.Competence).Distinct().Should().HaveCount(3);
            await PostStatus(owner, "/api/expense-installments", new InstallmentPayload(Details("Luz", light, installation), new InstallmentPreviewPayload("100.00", 3, today, "monthly")),
                Guid.NewGuid(), HttpStatusCode.Conflict);

            // RF17: estimated recurrence without duplicates; "this" edit detaches; "this and next" preserves paid occurrences.
            var water = await Post<RecurrenceSaveDto>(owner, "/api/expense-recurrences", new RecurrencePayload(Details("Água"),
                new RecurrenceTermsPayload(month.AddDays(9), null, "monthly", "120.00", true, true), 0), Guid.NewGuid(), HttpStatusCode.Created);
            water.Plan.Inserted.Should().Be(13, "current month plus the 12-month horizon");
            var generated = await Ok<RecurrencePlanDto>(owner, "/api/expense-recurrences/generate", null);
            (generated.Inserted, generated.Updated, generated.Cancelled).Should().Be((0, 0, 0));
            var occurrences = await Recurring(owner, water.Recurrence.Code, month, month.AddMonths(3));
            occurrences.Should().HaveCount(4).And.OnlyContain(x => x.IsEstimated && x.Amount == "120.00");
            await Post<PaymentDto>(owner, "/api/payments", Pay(account.Code, today, occurrences[0].Code, "118.40"), Guid.NewGuid(), HttpStatusCode.Created);
            var confirmed = occurrences[2];
            var edited = await Put<ExpenseDto>(owner, $"/api/expenses/{confirmed.Code}", new ExpensePayload(Details("Água"), null, confirmed.DueDate, "131.00", confirmed.Version, null));
            (edited.IsDetached, edited.IsEstimated).Should().Be((true, false));
            var preview2 = await Ok<RecurrencePlanDto>(owner, $"/api/expense-recurrences/preview?code={water.Recurrence.Code}",
                new RecurrenceTermsPayload(month.AddDays(9), null, "monthly", "150.00", false, true));
            preview2.Preserved.Should().BeGreaterThanOrEqualTo(1);
            var changed = await Put<RecurrenceSaveDto>(owner, $"/api/expense-recurrences/{water.Recurrence.Code}", new RecurrencePayload(Details("Água"),
                new RecurrenceTermsPayload(month.AddDays(9), null, "monthly", "150.00", false, true), water.Recurrence.Version));
            changed.Plan.Inserted.Should().Be(0);
            occurrences = await Recurring(owner, water.Recurrence.Code, month, month.AddMonths(3));
            occurrences.Select(x => x.Amount).Should().Equal("120.00", "150.00", "131.00", "150.00");
            await Put<RecurrenceSaveDto>(owner, $"/api/expense-recurrences/{water.Recurrence.Code}", new RecurrencePayload(Details("Água"),
                new RecurrenceTermsPayload(month.AddDays(-30), null, "monthly", "150.00", false, true), changed.Recurrence.Version), HttpStatusCode.Conflict);

            // RF41: 100 split 50/50 and paid by Ana is one household expense of 100; Bruno owes Ana 50.
            var split = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(Details("Mercado", null, null, "percent", [new(ana.Code, "50"), new(bruno.Code, "50")]),
                month, today, "100.00", 0, new ImmediatePaymentPayload(account.Code, today, ana.Code)), Guid.NewGuid(), HttpStatusCode.Created);
            (split.Status, split.Shares.Count).Should().Be(("paid", 2));
            split.Shares.Should().OnlyContain(x => x.Amount == "50.00");
            await PostStatus(owner, "/api/expenses", new ExpensePayload(Details("Errado", null, null, "value", [new(ana.Code, "50.00"), new(bruno.Code, "40.00")]),
                month, today, "100.00", 0, null), Guid.NewGuid(), HttpStatusCode.BadRequest);
            var contributions = (await owner.GetFromJsonAsync<SuccessDetails<ContributionsDto>>($"/api/expenses/contributions?month={month:yyyy-MM-dd}"))!.Data;
            contributions.People.Single(x => x.PersonCode == ana.Code).Should().Be(new ContributionDto(ana.Code, "Ana", "50.00", "100.00", "50.00"));
            contributions.Settlements.Should().ContainSingle().Which.Should().Be(new SettlementDto(bruno.Code, "Bruno", ana.Code, "Ana", "50.00"));
            var list = (await owner.GetFromJsonAsync<SuccessDetails<ExpenseListDto>>($"/api/expenses?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}"))!.Data;
            list.Items.Count(x => x.Description == "Mercado").Should().Be(1, "a split never creates a second expense");
            (await Balance(owner, account.Code)).Should().Be(Money.Format(4743m - 60m - 118.40m - 100m));

            var statement = (await owner.GetFromJsonAsync<SuccessDetails<StatementDto>>($"/api/accounts/{account.Code}/statement?from={month.AddMonths(-1):yyyy-MM-dd}&to={today:yyyy-MM-dd}"))!.Data;
            statement.Lines.Should().Contain(x => x.Kind == "payment" && x.PaymentCode == payment.Code).And.Contain(x => x.Kind == "reversal" && x.PaymentCode == payment.Code);
            await using var database = new MySqlConnection(connection);
            (await database.QuerySingleAsync<long>("SELECT COUNT(*) FROM Obligation o JOIN RecurringExpense r ON r.Id=o.IdRecurringExpense WHERE r.Code=@Code GROUP BY o.OccurrenceDate HAVING COUNT(*)>1 UNION ALL SELECT 0",
                new { water.Recurrence.Code })).Should().Be(0);
            (await database.QuerySingleAsync<decimal>("SELECT SUM(Amount) FROM Obligation o JOIN InstallmentPlan p ON p.Id=o.IdInstallmentPlan WHERE p.Code=@Code", new { purchase.Code }))
                .Should().Be(100m);
        }

        private static PaymentPayload Pay(Guid account, DateOnly date, Guid expense, string amount) =>
            new(account, date, null, null, null, [new PaymentAllocationPayload(expense, amount, null, null, null)], null);
        private static async Task<ExpenseDetailDto> Detail(HttpClient client, Guid code) =>
            (await client.GetFromJsonAsync<SuccessDetails<ExpenseDetailDto>>($"/api/expenses/{code}"))!.Data;
        private static async Task<string> Balance(HttpClient client, Guid account) =>
            (await client.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data.Accounts.Single(x => x.Code == account).Balance;
        private static async Task<ExpenseDetailDto?> Adjust(HttpClient client, Guid code, string kind, string amount, HttpStatusCode expected)
        {
            var response = await client.PostAsJsonAsync($"/api/expenses/{code}/adjustments", new AdjustmentPayload(kind, amount, "Conta de teste", null));
            response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
            return response.IsSuccessStatusCode ? (await response.Content.ReadFromJsonAsync<SuccessDetails<ExpenseDetailDto>>())!.Data : null;
        }
        private static async Task<IReadOnlyList<ExpenseDto>> Recurring(HttpClient client, Guid rule, DateOnly from, DateOnly toMonth) =>
            (await client.GetFromJsonAsync<SuccessDetails<ExpenseListDto>>($"/api/expenses?from={from:yyyy-MM-dd}&to={toMonth.AddMonths(1).AddDays(-1):yyyy-MM-dd}&includeOverdue=false"))!
                .Data.Items.Where(x => x.RecurrenceCode == rule).OrderBy(x => x.DueDate).ToArray();
        private static async Task<T> Ok<T>(HttpClient client, string path, object? payload)
        {
            var response = await client.PostAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data;
        }
        private static async Task<T> Put<T>(HttpClient client, string path, object payload, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var response = await client.PutAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
            return expected == HttpStatusCode.OK ? (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data : default!;
        }
    }
}
