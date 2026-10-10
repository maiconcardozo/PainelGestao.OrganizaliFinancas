using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class IncomeRulesTests
    {
        private static IncomeTerms Fixed(string frequency, DateOnly from, decimal amount = 14000m, DateOnly? end = null) =>
            new(from, end, frequency, "fixed", amount, null);

        [Theory]
        [InlineData("weekly", 5)]
        [InlineData("biweekly", 3)]
        [InlineData("monthly", 1)]
        public void Occurrences_FollowFrequencyWithinRange(string frequency, int expected)
        {
            var terms = Fixed(frequency, new DateOnly(2026, 1, 1));
            terms.Occurrences(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Should().HaveCount(expected);
        }

        [Fact]
        public void MonthlyOnDay31_UsesLastDayAndReturnsToDayBase()
        {
            var dates = Fixed("monthly", new DateOnly(2026, 1, 31)).Occurrences(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)).ToArray();
            dates.Should().Equal(new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31));
        }

        [Theory]
        [InlineData("quarterly", 4)]
        [InlineData("semiannual", 2)]
        [InlineData("annual", 1)]
        public void LongFrequencies_SpreadOverYear(string frequency, int expected) =>
            Fixed(frequency, new DateOnly(2026, 1, 10)).Occurrences(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Should().HaveCount(expected);

        [Fact]
        public void Terms_RejectInconsistentAmounts()
        {
            var noAmount = () => new IncomeTerms(new DateOnly(2026, 1, 1), null, "monthly", "fixed", null, null).Validate();
            noAmount.Should().Throw<InputValidationException>();
            var variableWithAmount = () => new IncomeTerms(new DateOnly(2026, 1, 1), null, "monthly", "variable", 10m, null).Validate();
            variableWithAmount.Should().Throw<InputValidationException>();
            var grossBelowNet = () => new IncomeTerms(new DateOnly(2026, 1, 1), null, "monthly", "fixed", 100m, 90m).Validate();
            grossBelowNet.Should().Throw<InputValidationException>();
            new IncomeTerms(new DateOnly(2026, 1, 1), null, "monthly", "variable", null, null).Invoking(x => x.Validate()).Should().NotThrow();
        }

        [Fact]
        public void NewVersion_AffectsOnlyDatesFromItsValidity()
        {
            var versions = new[]
            {
                new IncomeTermsVersion(1, Fixed("monthly", new DateOnly(2026, 1, 5), 14000m)),
                new IncomeTermsVersion(2, Fixed("monthly", new DateOnly(2026, 4, 5), 15000m))
            };
            var expected = IncomeSchedule.Expected(versions, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
            expected.Select(x => x.Amount).Should().Equal(14000m, 14000m, 14000m, 15000m, 15000m, 15000m);
            expected.Select(x => x.Version).Should().Equal(1, 1, 1, 2, 2, 2);
        }

        [Fact]
        public void Plan_IsIdempotentAndNeverRewritesLockedForecasts()
        {
            var from = new DateOnly(2026, 1, 1); var to = new DateOnly(2026, 3, 31);
            var v1 = IncomeSchedule.Expected([new IncomeTermsVersion(1, Fixed("monthly", from, 4000m))], from, to);
            var first = IncomeSchedule.Plan([], v1, from, to);
            first.Inserts.Should().HaveCount(3);
            var stored = v1.Select((x, i) => new ForecastState(Guid.NewGuid(), x.Date, true, i == 0, x.Version, x.Amount)).ToArray();
            IncomeSchedule.Plan(stored, v1, from, to).IsEmpty.Should().BeTrue("repeating the generation must not duplicate");

            var v2 = IncomeSchedule.Expected([new IncomeTermsVersion(1, Fixed("monthly", from, 4000m)), new IncomeTermsVersion(2, Fixed("monthly", from, 4500m))], from, to);
            var plan = IncomeSchedule.Plan(stored, v2, from, to);
            plan.Updates.Select(x => x.Code).Should().BeEquivalentTo(stored.Skip(1).Select(x => x.Code), "the received January forecast is locked");
            plan.Inserts.Should().BeEmpty();

            var weekly = IncomeSchedule.Expected([new IncomeTermsVersion(2, Fixed("weekly", new DateOnly(2026, 3, 2), 1000m))], new DateOnly(2026, 3, 1), to);
            IncomeSchedule.Plan(stored, weekly, new DateOnly(2026, 3, 1), to).Cancels.Should().ContainSingle().Which.Should().Be(stored[2].Code);
        }

        [Theory]
        [InlineData(2026, 10, 15, 2026, 1, 9)]
        [InlineData(2027, 1, 20, 2026, 1, 12)]
        [InlineData(2026, 2, 1, 2026, 1, 1)]
        public void AverageWindow_UsesClosedMonthsOfCalendarYear(int y, int m, int d, int fromYear, int fromMonth, int months)
        {
            var window = IncomeAverage.Window(new DateOnly(y, m, d));
            window.From.Should().Be(new DateOnly(fromYear, fromMonth, 1));
            window.Months.Should().Be(months);
            IncomeAverage.Of(135000m, IncomeAverage.Window(new DateOnly(2026, 10, 10))).Should().Be(15000m);
        }

        [Theory]
        [InlineData("1250.50", true, 1250.50)]
        [InlineData("-150.00", true, -150)]
        [InlineData("1.234,50", false, 0)]
        [InlineData("10.555", false, 0)]
        [InlineData("1e3", false, 0)]
        public void Money_AcceptsOnlyInvariantCents(string text, bool valid, decimal amount)
        {
            Money.TryParse(text, out var parsed).Should().Be(valid);
            if (valid) parsed.Should().Be(amount);
            Money.Format(3800m).Should().Be("3800.00");
        }

        [Fact]
        public void Receipt_AllocationsCannotExceedReceivedAmount()
        {
            var account = FinancialAccount.Create(1, "Corrente", new DateOnly(2026, 1, 1), 0, "test");
            var person = Person.Create(1, "Ana", "test");
            var over = () => Receipt.Create(account, person, null, null, new DateOnly(2026, 5, 1), 1000m, [600m, 500m], null, "test");
            over.Should().Throw<ConflictException>();
            var before = () => Receipt.Create(account, person, null, null, new DateOnly(2025, 12, 31), 10m, [], null, "test");
            before.Should().Throw<ConflictException>("movements before the opening date are refused");
            Receipt.Create(account, person, null, null, new DateOnly(2026, 5, 1), 16000m, [16000m], null, "test").Amount.Should().Be(16000m);
        }

        [Fact]
        public void Account_OpeningBaseIsFrozenOnceMovementsExist()
        {
            var account = FinancialAccount.Create(1, "Corrente", new DateOnly(2026, 1, 1), 100m, "test");
            account.Invoking(x => x.Update("Corrente", true, new DateOnly(2026, 2, 1), 100m, true, "test")).Should().Throw<ConflictException>();
            account.Update("Conta principal", true, new DateOnly(2026, 1, 1), 100m, true, "test");
            account.Name.Should().Be("Conta principal");
        }
    }
}
