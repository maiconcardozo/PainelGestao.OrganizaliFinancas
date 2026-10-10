using System.Net;
using System.Net.Http.Json;
using Dapper;
using FluentAssertions;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Accounts;
using PainelGestao.OrganizaliFinancas.Application.Cards;
using PainelGestao.OrganizaliFinancas.Application.ExpenseTypes;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using static PainelGestao.OrganizaliFinancas.Tests.Helpers.FinanceAssertions;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>M6 acceptance: purchase 300 + payment 300 is never an expense of 600; partial invoice payment; installments; refunds; no PAN.</summary>
    internal static class CardAssertions
    {
        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader, string connection)
        {
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, settings.TimeZone));
            var month = new DateOnly(today.Year, today.Month, 1);
            var account = await Create<AccountDto>(owner, "/api/accounts", new AccountPayload("Conta do cartão", true, 0, month.AddMonths(-1), "2000.00"));
            var holder = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Titular do cartão", true, 0));
            ExpenseDetailsPayload Details(string description, ExpenseTypeDto? type = null) => new(description, type?.Code, type?.Version, null, null, null, null, null, null, null);

            (await owner.PostAsJsonAsync("/api/cards", new CardPayload(holder.Code, "4111 1111 1111 1111", 25, 5, "1000.00", "next", true, 0)))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest, "a card number is never stored");
            var card = await Create<CardDto>(owner, "/api/cards", new CardPayload(holder.Code, "Visa da casa", 25, 5, "1000.00", "next", true, 0));
            (await reader.PostAsJsonAsync("/api/cards", new CardPayload(holder.Code, "Negado", 25, 5, null, "next", true, 0))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await other.GetFromJsonAsync<SuccessDetails<CardDto[]>>("/api/cards"))!.Data.Should().BeEmpty();

            // Acceptance: purchase 300 does not touch the bank; paying the invoice reduces cash once; the expense is 300, not 600.
            var key = Guid.NewGuid();
            var single = new PurchasePayload(card.Code, holder.Code, Details("Mercado do mês"), today, "300.00", 1, 0);
            var purchase = await Post<PurchaseDto>(owner, "/api/card-purchases", single, key, HttpStatusCode.Created);
            (await Post<PurchaseDto>(owner, "/api/card-purchases", single, key, HttpStatusCode.Created)).Code.Should().Be(purchase.Code);
            await PostStatus(reader, "/api/card-purchases", single, Guid.NewGuid(), HttpStatusCode.Forbidden);
            await PostStatus(owner, "/api/card-purchases", single with { PurchaseDate = today.AddDays(1) }, Guid.NewGuid(), HttpStatusCode.BadRequest);
            (await Balance(owner, account.Code)).Should().Be("2000.00", "a card purchase never moves a bank account");
            var cycle = CardSchedule.Plan(300m, 1, today, 25, 5, "next").Single().Cycle;
            var invoice = (await Invoices(owner, card.Code)).Single(x => x.ReferenceMonth == cycle.ReferenceMonth.ToString("yyyy-MM"));
            (invoice.Amount, invoice.DueDate, invoice.ClosingDate).Should().Be(("300.00", cycle.DueDate, cycle.ClosingDate));
            (await Cards(owner)).Single(x => x.Code == card.Code).Should().Match<CardDto>(x => x.Committed == "300.00" && x.Available == "700.00");
            (await owner.PutAsJsonAsync($"/api/expenses/{invoice.ExpenseCode}", new ExpensePayload(Details("Fatura"), null, cycle.DueDate, "1.00", 1, null)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict, "an invoice amount comes only from its items");
            await Post<PaymentDto>(owner, "/api/payments", new PaymentPayload(account.Code, today, holder.Code, null, null,
                [new PaymentAllocationPayload(invoice.ExpenseCode, "300.00", null, null, null)], null), Guid.NewGuid(), HttpStatusCode.Created);
            (await Balance(owner, account.Code)).Should().Be("1700.00");
            invoice = (await Invoices(owner, card.Code)).Single(x => x.Code == invoice.Code);
            (invoice.OpenBalance, invoice.Paid).Should().Be(("0.00", "300.00"));
            (await Cards(owner)).Single(x => x.Code == card.Code).Available.Should().Be("1000.00");
            await using var database = new MySqlConnection(connection);
            (await database.QuerySingleAsync<decimal>("""
                SELECT COALESCE(SUM(o.Amount),0) FROM Obligation o JOIN CardInvoice i ON i.Id=o.IdCardInvoice JOIN CreditCard c ON c.Id=i.IdCreditCard WHERE c.Code=@Code
                """, new { card.Code })).Should().Be(300m, "the invoice is the only obligation; the purchase is not a second expense");

            // Partial payment of 100 on an invoice of 300 leaves 200 open.
            var second = await Create<CardDto>(owner, "/api/cards", new CardPayload(holder.Code, "Master", 10, 20, null, "current", true, 0));
            await Post<PurchaseDto>(owner, "/api/card-purchases", new PurchasePayload(second.Code, null, Details("Farmácia"), today, "300.00", 1, 0), Guid.NewGuid(), HttpStatusCode.Created);
            var partial = (await Invoices(owner, second.Code)).Single();
            await Post<PaymentDto>(owner, "/api/payments", new PaymentPayload(account.Code, today, null, null, null,
                [new PaymentAllocationPayload(partial.ExpenseCode, "100.00", null, null, null)], null), Guid.NewGuid(), HttpStatusCode.Created);
            (await Invoices(owner, second.Code)).Single().OpenBalance.Should().Be("200.00");

            // Installments land in consecutive invoices; a full refund cancels the ones still open.
            var preview = await PostOk<PurchasePreviewDto>(owner, "/api/card-purchases/preview", new PurchasePreviewPayload(card.Code, today, "300.00", 3, 0));
            preview.Items.Select(x => x.Amount).Should().Equal("100.00", "100.00", "100.00");
            preview.Items.Select(x => x.ReferenceMonth).Distinct().Should().HaveCount(3);
            var tv = await Post<PurchaseDto>(owner, "/api/card-purchases", new PurchasePayload(card.Code, null, Details("Televisão"), today, "300.00", 3, 0), Guid.NewGuid(), HttpStatusCode.Created);
            var invoices = await Invoices(owner, card.Code);
            invoices.Where(x => preview.Items.Any(p => p.ReferenceMonth == x.ReferenceMonth)).Sum(x => decimal.Parse(x.Amount, System.Globalization.CultureInfo.InvariantCulture))
                .Should().Be(600m, "300 already in the first invoice plus the three installments");
            var refunded = await Post<PurchaseDto>(owner, $"/api/card-purchases/{tv.Code}/refunds", new RefundPayload(null, "Produto devolvido", null), Guid.NewGuid(), HttpStatusCode.Created);
            refunded.Refunded.Should().Be("300.00");
            (await database.QuerySingleAsync<decimal>("SELECT COALESCE(SUM(x.Amount),0) FROM CardItem x JOIN CardPurchase p ON p.Id=x.IdCardPurchase WHERE p.Code=@Code AND x.IsActive=1",
                new { tv.Code })).Should().Be(0m);
            await PostStatus(owner, $"/api/card-purchases/{tv.Code}/refunds", new RefundPayload("1.00", "De novo", null), Guid.NewGuid(), HttpStatusCode.Conflict);

            // A partial refund of an already paid invoice becomes a credit: the invoice shows 250 with 50 in favor of the household.
            await Post<PurchaseDto>(owner, $"/api/card-purchases/{purchase.Code}/refunds", new RefundPayload("50.00", "Desconto devolvido", null), Guid.NewGuid(), HttpStatusCode.Created);
            invoice = (await Invoices(owner, card.Code)).Single(x => x.Code == invoice.Code);
            (invoice.Amount, invoice.OpenBalance).Should().Be(("250.00", "-50.00"));
            (await Balance(owner, account.Code)).Should().Be("1600.00", "a merchant refund is not a bank entry");
            var detail = (await owner.GetFromJsonAsync<SuccessDetails<InvoiceDetailDto>>($"/api/card-invoices/{invoice.Code}"))!.Data;
            detail.Items.Should().Contain(x => x.Kind == "credit" && x.Amount == "-50.00" && x.Note == "Desconto devolvido");
            (await other.GetAsync($"/api/card-invoices/{invoice.Code}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Expense types with another behavior are refused on cards; card types are accepted.
            var single2 = await Create<ExpenseTypeDto>(owner, "/api/expense-types", new ExpenseTypePayload("Conta comum", true, 0, "single", null, []));
            await PostStatus(owner, "/api/card-purchases", single with { Details = Details("Errado", single2) }, Guid.NewGuid(), HttpStatusCode.Conflict);
            var cardType = await Create<ExpenseTypeDto>(owner, "/api/expense-types", new ExpenseTypePayload("Streaming", true, 0, "card", null, []));
            await PostStatus(owner, "/api/card-purchases", single with { Details = Details("Assinatura de vídeo", cardType), Total = "39.90" }, Guid.NewGuid(), HttpStatusCode.Created);
        }

        private static async Task<IReadOnlyList<InvoiceDto>> Invoices(HttpClient client, Guid card) =>
            (await client.GetFromJsonAsync<SuccessDetails<InvoiceDto[]>>($"/api/card-invoices?cardCode={card}"))!.Data;
        private static async Task<IReadOnlyList<CardDto>> Cards(HttpClient client) => (await client.GetFromJsonAsync<SuccessDetails<CardDto[]>>("/api/cards"))!.Data;
        private static async Task<string> Balance(HttpClient client, Guid account) =>
            (await client.GetFromJsonAsync<SuccessDetails<AccountsOverviewDto>>("/api/accounts"))!.Data.Accounts.Single(x => x.Code == account).Balance;
        private static async Task<T> PostOk<T>(HttpClient client, string path, object payload)
        {
            var response = await client.PostAsJsonAsync(path, payload);
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<T>>())!.Data;
        }
    }
}
