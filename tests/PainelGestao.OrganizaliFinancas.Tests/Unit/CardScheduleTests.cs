using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class CardScheduleTests
    {
        [Fact]
        public void Cycle_DueAfterClosingInSameOrNextMonth()
        {
            CardSchedule.ClosingIn(new DateOnly(2026, 10, 1), 25, 5).Should().Be(new CardCycle(new DateOnly(2026, 10, 25), new DateOnly(2026, 11, 5)));
            CardSchedule.ClosingIn(new DateOnly(2026, 10, 1), 3, 10).ReferenceMonth.Should().Be(new DateOnly(2026, 10, 1));
            CardSchedule.ClosingIn(new DateOnly(2026, 2, 1), 31, 10).ClosingDate.Should().Be(new DateOnly(2026, 2, 28), "short months close on their last day");
        }

        [Theory]
        [InlineData(24, "next", 10)]
        [InlineData(25, "next", 11)]
        [InlineData(25, "current", 10)]
        [InlineData(26, "current", 11)]
        public void ClosingDayRule_DecidesTheInvoiceOfThatDay(int day, string rule, int closingMonth) =>
            CardSchedule.ClosingMonthFor(new DateOnly(2026, 10, day), 25, rule).Month.Should().Be(closingMonth);

        [Fact]
        public void Plan_SpreadsInstallmentsOverConsecutiveInvoicesWithExactSum()
        {
            var items = CardSchedule.Plan(300m, 3, new DateOnly(2026, 10, 10), 25, 5, "next");
            items.Select(x => (x.Cycle.ReferenceMonth.Month, x.Amount)).Should().Equal((11, 100m), (12, 100m), (1, 100m));
            CardSchedule.Plan(100m, 3, new DateOnly(2026, 10, 10), 25, 5, "next").Sum(x => x.Amount).Should().Be(100m);
            CardSchedule.Plan(300m, 1, new DateOnly(2026, 10, 10), 25, 5, "next").Should().ContainSingle().Which.Amount.Should().Be(300m);
            CardSchedule.Plan(300m, 1, new DateOnly(2026, 10, 10), 25, 5, "next", 1).Single().Cycle.ReferenceMonth.Should().Be(new DateOnly(2026, 12, 1));
            ((Action)(() => CardSchedule.Plan(300m, 1, new DateOnly(2026, 10, 10), 25, 5, "next", 2))).Should().Throw<InputValidationException>();
        }

        [Fact]
        public void Card_RefusesCardNumberAsNickname()
        {
            var holder = Person.Create(1, "Ana", "test");
            ((Action)(() => CreditCard.Create(holder, "4111 1111 1111 1111", 25, 5, null, "next", "test"))).Should().Throw<InputValidationException>();
            CreditCard.Create(holder, "Nubank 2025", 25, 5, 1000m, "next", "test").Name.Should().Be("Nubank 2025");
        }

        [Fact]
        public void Refund_CannotExceedRemainingAmount()
        {
            var card = CreditCard.Create(Person.Create(1, "Ana", "test"), "Visa", 25, 5, null, "next", "test");
            var purchase = CardPurchase.Create(card, null, new ObligationDetails("Mercado", null, null, null, null, null, null, "{}", null), new DateOnly(2026, 10, 10), 300m, 1, "test");
            purchase.Refund(50m, "test");
            ((Action)(() => purchase.Refund(250.01m, "test"))).Should().Throw<ConflictException>();
            purchase.Refund(250m, "test");
            purchase.IsFullyRefunded().Should().BeTrue();
        }
    }
}
