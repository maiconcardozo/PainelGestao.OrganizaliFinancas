using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Cards;
using PainelGestao.OrganizaliFinancas.Application.Catalogs;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Application.Planning;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using static PainelGestao.OrganizaliFinancas.Tests.Helpers.FinanceAssertions;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    /// <summary>
    /// A22 acceptance: a bill paid with a card leaves the account on the invoice due date (projection, margin, cash flow) while the agenda keeps its
    /// due date; the margin is the projected closing balance minus the reserve; a simulated spend eats the margin of the month its cash leaves in;
    /// changing the reserve shifts every margin by the same amount.
    /// </summary>
    internal static class MarginAssertions
    {
        private static decimal Number(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader)
        {
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            settings.MinimumReserve.Should().Be("15000.00", "the reserve starts at R$ 15.000 (A22)");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, settings.TimeZone));
            var month = new DateOnly(today.Year, today.Month, 1);
            var holder = await Create<PersonDto>(owner, "/api/people", new PersonPayload("Titular da margem", true, 0));
            var card = await Create<CardDto>(owner, "/api/cards", new CardPayload(holder.Code, "Cartão da margem", 25, 5, null, "next", true, 0));
            var school = await Create<CatalogItemDto>(owner, "/api/catalogs/categories", new CatalogPayload("Escola A22", true, 0, null));
            var dueDate = today.AddDays(1);
            var landing = CardSchedule.LandingFor(dueDate, 25, 5, "next");

            // The bill names the card it is paid with: its cash leaves on that card's invoice, not on its own due date.
            var bill = await Post<ExpenseDto>(owner, "/api/expenses", new ExpensePayload(
                new ExpenseDetailsPayload("Mensalidade A22", null, null, school.Code, null, null, null, null, null, null, card.Code), month, dueDate, "500.00", 0, null),
                Guid.NewGuid(), HttpStatusCode.Created);
            (bill.PayCardCode, bill.PayCardName).Should().Be((card.Code, "Cartão da margem"));
            var projection = (await owner.GetFromJsonAsync<SuccessDetails<ProjectionDto>>("/api/projection?days=120"))!.Data;
            projection.Days.Single(day => day.Events.Any(e => e.Code == bill.Code)).Date.Should().Be(landing.Cycle.DueDate, "rule 3: the bill leaves on the invoice due date");
            var agenda = (await owner.GetFromJsonAsync<SuccessDetails<AgendaDto>>("/api/agenda"))!.Data;
            var listed = agenda.Next7.Concat(agenda.Today).Single(x => x.Code == bill.Code);
            (listed.Date, listed.Description).Should().Be((dueDate, "Mensalidade A22 · no cartão Cartão da margem"), "the agenda keeps the bill's own due date");

            var margin = await Margin(owner, "");
            margin.Reserve.Should().Be("15000.00");
            margin.Months.Should().HaveCount(HouseholdPlannerMonths);
            var current = margin.Months[0];
            current.Month.Should().Be(month.ToString("yyyy-MM"));
            var group = current.ToCardGroups.Single(x => x.CardName == "Cartão da margem");
            (group.Name, group.Count, group.Amount, group.InvoiceMonth, group.InvoiceDueDate).Should().Be(("Escola A22", 1, "500.00", landing.Cycle.ReferenceMonth.ToString("yyyy-MM"), landing.Cycle.DueDate));
            current.ToCard.Should().Be("500.00");
            foreach (var m in margin.Months)
            {
                Number(m.Margin).Should().Be(Number(m.Closing) - 15000m, "rule 5: margin = projected closing balance − reserve");
                m.Status.Should().Be(ReserveMargin.Status(Number(m.Closing), 15000m));
                Number(m.Shortfall).Should().Be(Math.Max(0, 15000m - Number(m.Closing)));
                (Number(m.Opening) + Number(m.Inflow) + Number(m.Outflow) + Number(m.Other)).Should().Be(Number(m.Closing));
            }
            var invoiceMonth = margin.Months.Single(x => x.Month == landing.Cycle.ReferenceMonth.ToString("yyyy-MM"));
            Number(invoiceMonth.Bills).Should().BeGreaterThanOrEqualTo(500m, "rule 4: the bill is part of what leaves the account in the invoice month");
            var flow = (await owner.GetFromJsonAsync<SuccessDetails<CashFlowDto>>("/api/cash-flow"))!.Data;
            flow.Months.Single(x => x.Month == invoiceMonth.Month).Closing.Should().Be(invoiceMonth.Closing, "rule 9: the cash flow uses the same dates and balances");

            // "Hoje, em qual fatura cai?"
            var todayLanding = CardSchedule.LandingFor(today, 25, 5, "next");
            var landingDto = margin.Cards.Single(x => x.CardCode == card.Code);
            (landingDto.ReferenceMonth, landingDto.ClosingDate, landingDto.DueDate, landingDto.LastDay, landingDto.NextReferenceMonth).Should().Be((
                todayLanding.Cycle.ReferenceMonth.ToString("yyyy-MM"), todayLanding.Cycle.ClosingDate, todayLanding.Cycle.DueDate, todayLanding.LastDay, todayLanding.NextReferenceMonth.ToString("yyyy-MM")));

            // Simulation: cash today eats the current month and every month after it; on the card it eats from the invoice month on.
            var cash = await Margin(owner, "?amount=1000.00");
            cash.Simulation.Should().NotBeNull();
            (cash.Simulation!.CardName, cash.Simulation.Date).Should().Be(((string?)null, today));
            for (var index = 0; index < cash.Months.Count; index++)
                Number(cash.Simulation.Months[index].Margin).Should().Be(Number(cash.Months[index].Margin) - 1000m);
            var onCard = await Margin(owner, $"?amount=1000.00&cardCode={card.Code}");
            (onCard.Simulation!.CardName, onCard.Simulation.InvoiceMonth, onCard.Simulation.Date).Should().Be(("Cartão da margem", todayLanding.Cycle.ReferenceMonth.ToString("yyyy-MM"), todayLanding.Cycle.DueDate));
            for (var index = 0; index < onCard.Months.Count; index++)
            {
                var before = string.CompareOrdinal(onCard.Months[index].Month, onCard.Simulation.InvoiceMonth) < 0;
                Number(onCard.Simulation.Months[index].Margin).Should().Be(Number(onCard.Months[index].Margin) - (before ? 0m : 1000m), "rule 7");
            }
            (await owner.GetAsync($"/api/margin?cardCode={card.Code}")).StatusCode.Should().Be(HttpStatusCode.BadRequest, "a card needs an amount");
            (await owner.GetAsync("/api/margin?amount=abc")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await owner.GetAsync($"/api/margin?amount=10.00&cardCode={Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await reader.GetAsync("/api/margin")).StatusCode.Should().Be(HttpStatusCode.OK, "every member sees the margin");
            (await Margin(other, "")).Months[0].ToCard.Should().Be("0.00", "another household has nothing on this card");

            // Rule 1: the reserve is a household setting; lowering it by 5.000 raises every margin by 5.000.
            settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            (await reader.PutAsJsonAsync("/api/household-settings", new SettingsPayload(settings.TimeZone, settings.StartDate, settings.Version, "10000.00"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload(settings.TimeZone, settings.StartDate, settings.Version, "-1.00"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var lowered = await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload(settings.TimeZone, settings.StartDate, settings.Version, "10000.00"));
            lowered.StatusCode.Should().Be(HttpStatusCode.OK, await lowered.Content.ReadAsStringAsync());
            (await lowered.Content.ReadFromJsonAsync<SuccessDetails<SettingsDto>>())!.Data.MinimumReserve.Should().Be("10000.00");
            var relaxed = await Margin(owner, "");
            relaxed.Reserve.Should().Be("10000.00");
            for (var index = 0; index < relaxed.Months.Count; index++)
                Number(relaxed.Months[index].Margin).Should().Be(Number(margin.Months[index].Margin) + 5000m);
            var kept = await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload(settings.TimeZone, settings.StartDate, settings.Version + 1));
            (await kept.Content.ReadFromJsonAsync<SuccessDetails<SettingsDto>>())!.Data.MinimumReserve.Should().Be("10000.00", "omitting the reserve keeps it");

            // Rule 2: a recurring expense names its card too, and every generated occurrence inherits it.
            var rule = await Post<RecurrenceSaveDto>(owner, "/api/expense-recurrences", new RecurrencePayload(
                new ExpenseDetailsPayload("Assinatura A22", null, null, school.Code, null, null, null, null, null, null, card.Code),
                new RecurrenceTermsPayload(today.AddDays(2), null, "monthly", "55.00", false, true), 0), Guid.NewGuid(), HttpStatusCode.Created);
            rule.Recurrence.PayCardName.Should().Be("Cartão da margem");
            var occurrences = (await owner.GetFromJsonAsync<SuccessDetails<ExpenseListDto>>($"/api/expenses?from={today.AddDays(2):yyyy-MM-dd}&to={today.AddDays(2):yyyy-MM-dd}"))!.Data;
            occurrences.Items.Single(x => x.RecurrenceCode == rule.Recurrence.Code).PayCardCode.Should().Be(card.Code);
            var withoutCard = await owner.PutAsJsonAsync($"/api/expense-recurrences/{rule.Recurrence.Code}", new RecurrencePayload(
                new ExpenseDetailsPayload("Assinatura A22", null, null, school.Code, null, null, null, null, null, null, null),
                new RecurrenceTermsPayload(today.AddDays(2), null, "monthly", "55.00", false, true), rule.Recurrence.Version));
            withoutCard.StatusCode.Should().Be(HttpStatusCode.OK, await withoutCard.Content.ReadAsStringAsync());
            (await withoutCard.Content.ReadFromJsonAsync<SuccessDetails<RecurrenceSaveDto>>())!.Data.Recurrence.PayCardCode.Should().BeNull();
            occurrences = (await owner.GetFromJsonAsync<SuccessDetails<ExpenseListDto>>($"/api/expenses?from={today.AddDays(2):yyyy-MM-dd}&to={today.AddDays(2):yyyy-MM-dd}"))!.Data;
            occurrences.Items.Single(x => x.RecurrenceCode == rule.Recurrence.Code).PayCardCode.Should().BeNull("this and next occurrences follow the rule");

            // An inactive card is refused as a new default but kept on an edit that does not change it.
            (await owner.PutAsJsonAsync($"/api/cards/{card.Code}", new CardPayload(holder.Code, "Cartão da margem", 25, 5, null, "next", false, card.Version))).StatusCode.Should().Be(HttpStatusCode.OK);
            await PostStatus(owner, "/api/expenses", new ExpensePayload(
                new ExpenseDetailsPayload("Recusada A22", null, null, school.Code, null, null, null, null, null, null, card.Code), month, dueDate, "10.00", 0, null), Guid.NewGuid(), HttpStatusCode.Conflict);
            var edited = await owner.PutAsJsonAsync($"/api/expenses/{bill.Code}", new ExpensePayload(
                new ExpenseDetailsPayload("Mensalidade A22 editada", null, null, school.Code, null, null, null, null, null, null, card.Code), month, dueDate, "500.00", bill.Version, null));
            edited.StatusCode.Should().Be(HttpStatusCode.OK, await edited.Content.ReadAsStringAsync());
            (await Margin(owner, "")).Months[0].ToCard.Should().Be("0.00", "an inactive card counts as a bank payment again");
            (await owner.GetFromJsonAsync<SuccessDetails<ProjectionDto>>("/api/projection?days=120"))!.Data.Days
                .Single(day => day.Events.Any(e => e.Code == bill.Code)).Date.Should().Be(dueDate);
        }

        private const int HouseholdPlannerMonths = 7;
        private static async Task<MarginDto> Margin(HttpClient client, string query)
        {
            var response = await client.GetAsync($"/api/margin{query}");
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<MarginDto>>())!.Data;
        }
    }
}
