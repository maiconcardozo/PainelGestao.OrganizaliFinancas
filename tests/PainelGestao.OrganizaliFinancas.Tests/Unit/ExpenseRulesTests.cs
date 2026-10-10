using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class ExpenseRulesTests
    {
        private static readonly Guid Ana = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        private static readonly Guid Bruno = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        private static readonly Guid Carla = Guid.Parse("00000000-0000-0000-0000-00000000000c");
        private static Obligation Bill(decimal amount = 250m) => Obligation.Create(1,
            new ObligationDetails("Luz", null, null, null, null, null, null, "{}", null), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), amount, "test");

        [Fact]
        public void Installments_100In3_PutResidueOnLastAndSumExactly()
        {
            var plan = Installments.Plan(100m, 3, new DateOnly(2026, 1, 31), "monthly");
            plan.Select(x => x.Amount).Should().Equal(33.33m, 33.33m, 33.34m);
            plan.Sum(x => x.Amount).Should().Be(100m);
            plan.Select(x => x.DueDate).Should().Equal(new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31));
        }

        [Theory]
        [InlineData("0.02", 3)]
        [InlineData("100.00", 1)]
        [InlineData("100.001", 3)]
        public void Installments_RejectInvalidSplits(string total, int count) =>
            ((Action)(() => Installments.Split(decimal.Parse(total, System.Globalization.CultureInfo.InvariantCulture), count)))
                .Should().Throw<InputValidationException>();

        [Fact]
        public void Shares_PercentUsesLargestRemainderWithCodeTieBreak()
        {
            var shares = Shares.Resolve(100m, "percent", [new(Carla, 33.3333m), new(Ana, 33.3333m), new(Bruno, 33.3334m)]);
            shares.Sum(x => x.Amount).Should().Be(100m);
            shares.Single(x => x.PersonCode == Bruno).Amount.Should().Be(33.34m, "the largest remainder receives the missing cent");
            Shares.Resolve(0.01m, "percent", [new(Bruno, 50m), new(Ana, 50m)]).Single(x => x.Amount == 0.01m).PersonCode.Should().Be(Ana, "ties go to the lowest Code");
        }

        [Fact]
        public void Shares_ValueMustMatchTotalAndPercentMust100()
        {
            Shares.Resolve(100m, "value", [new(Ana, 50m), new(Bruno, 50m)]).Should().HaveCount(2);
            ((Action)(() => Shares.Resolve(100m, "value", [new(Ana, 50m), new(Bruno, 40m)]))).Should().Throw<InputValidationException>();
            ((Action)(() => Shares.Resolve(100m, "percent", [new(Ana, 50m), new(Bruno, 40m)]))).Should().Throw<InputValidationException>();
            ((Action)(() => Shares.Resolve(100m, "percent", [new(Ana, 50m), new(Ana, 50m)]))).Should().Throw<InputValidationException>();
            Shares.Resolve(100m, null, null).Should().BeEmpty("a split is optional");
        }

        [Fact]
        public void Balance_FollowsRuleExample()
        {
            // 250 + 10 interest − 5 discount − 100 paid = 155; reversing the payment restores 255.
            ObligationBalance.Of(250m, 5m, 100m, false, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 10)).Should()
                .Be(new ObligationState("partial", 155m, false));
            ObligationBalance.Of(250m, 5m, 0m, false, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 20)).Should()
                .Be(new ObligationState("open", 255m, true), "overdue is derived from the due date and the open balance");
            ObligationBalance.Of(250m, 5m, 255m, false, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 20)).Status.Should().Be("paid");
            ObligationBalance.Of(250m, 0m, 0m, true, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 20)).Should().Be(new ObligationState("cancelled", 0m, false));
        }

        [Fact]
        public void Adjustment_DiscountIsNegativeAndCannotMakeBalanceNegative()
        {
            var bill = Bill();
            ObligationAdjustment.Create(bill, "discount", 5m, new DateOnly(2026, 10, 10), "Pagamento antecipado", 250m, "test").Amount.Should().Be(-5m);
            ObligationAdjustment.Create(bill, "adjustment", -3m, new DateOnly(2026, 10, 10), "Correção", 250m, "test").Amount.Should().Be(-3m);
            ((Action)(() => ObligationAdjustment.Create(bill, "discount", 251m, new DateOnly(2026, 10, 10), "Excesso", 250m, "test"))).Should().Throw<ConflictException>();
            ((Action)(() => ObligationAdjustment.Create(bill, "interest", -1m, new DateOnly(2026, 10, 10), "Negativo", 250m, "test"))).Should().Throw<ConflictException>();
        }

        [Fact]
        public void Obligation_PaidCannotBeCancelledNorChangePrincipal()
        {
            var bill = Bill();
            ((Action)(() => bill.Cancel("Errada", 10m, "test"))).Should().Throw<ConflictException>();
            var details = new ObligationDetails("Luz", null, null, null, null, null, null, "{}", null);
            ((Action)(() => bill.Edit(details, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), 260m, 10m, "test"))).Should().Throw<ConflictException>();
            bill.Edit(details with { Description = "Luz outubro" }, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 16), 250m, 10m, "test");
            bill.Description.Should().Be("Luz outubro");
            bill.Cancel("Lançada em duplicidade", 0m, "test");
            ((Action)(() => bill.EnsureOpen())).Should().Throw<ConflictException>();
        }

        [Fact]
        public void RecurrencePlan_IsIdempotentAndPreservesLockedOccurrences()
        {
            var versions = new[] { new ExpenseTermsVersion(1, new ExpenseTerms(new DateOnly(2026, 1, 10), null, "monthly", 120m, true)) };
            var expected = ExpenseSchedule.Expected(versions, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
            expected.Should().HaveCount(3);
            var states = expected.Select((x, index) => new ForecastState(Guid.NewGuid(), x.Date, true, index == 0, x.Version, x.Amount)).ToArray();
            Recurrence.Plan(states, expected, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)).IsEmpty.Should().BeTrue();
            var next = versions.Append(new ExpenseTermsVersion(2, new ExpenseTerms(new DateOnly(2026, 1, 10), null, "monthly", 150m, false))).ToArray();
            var plan = Recurrence.Plan(states, ExpenseSchedule.Expected(next, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
            plan.Updates.Should().HaveCount(2, "the paid January occurrence is preserved");
        }

        [Fact]
        public void Contributions_SuggestSettlementWithoutSecondExpense()
        {
            var lines = new[] { new ContributionLine(Ana, "Ana", 50m, 100m), new ContributionLine(Bruno, "Bruno", 50m, 0m) };
            Contributions.Settle(lines).Should().ContainSingle().Which.Should().Be(new Settlement(Bruno, "Bruno", Ana, "Ana", 50m));
        }
    }
}
