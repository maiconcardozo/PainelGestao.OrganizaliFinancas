using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class PlanningTests
    {
        private static readonly Guid Home = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid Rent = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid Energy = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid Food = Guid.Parse("00000000-0000-0000-0000-000000000004");
        private static readonly CategoryNode[] Tree =
            [new(Home, "Moradia", null, true), new(Rent, "Aluguel", Home, true), new(Energy, "Luz", Home, true), new(Food, "Mercado", null, true)];

        [Theory]
        [InlineData("799.99", "ok")]
        [InlineData("900.00", "warning")]
        [InlineData("1000.00", "limit")]
        [InlineData("1100.00", "exceeded")]
        public void Band_FollowsRuleExamples(string committed, string band) =>
            BudgetEvaluation.Band(decimal.Parse(committed, System.Globalization.CultureInfo.InvariantCulture), 1000m, 80, 100).Should().Be(band);

        [Fact]
        public void Band_NoBudgetDiffersFromZeroLimit()
        {
            BudgetEvaluation.Band(10m, null, 80, 100).Should().Be("none");
            BudgetEvaluation.Band(0.01m, 0m, 80, 100).Should().Be("exceeded");
            BudgetEvaluation.Band(0m, 0m, 80, 100).Should().Be("ok");
        }

        [Fact]
        public void Evaluate_ParentAggregatesChildrenWithoutDoubleCountingTotal()
        {
            var limits = new Dictionary<Guid, decimal> { [Home] = 2000m, [Rent] = 1500m, [Food] = 1000m };
            var spend = new CategorySpend[] { new(Rent, 1500m, 0, 1500m), new(Energy, 250m, 0, 0), new(Food, 800m, 300m, 0), new(null, 40m, 0, 0) };
            var summary = BudgetEvaluation.Evaluate(Tree, limits, spend, 80, 100);
            var home = summary.Lines.Single(x => x.CategoryCode == Home);
            (home.Committed, home.Band, home.CountsInTotal).Should().Be((1750m, "warning", true));
            summary.Lines.Single(x => x.CategoryCode == Rent).CountsInTotal.Should().BeFalse("the parent ceiling already covers it");
            var food = summary.Lines.Single(x => x.CategoryCode == Food);
            (food.Committed, food.Card, food.Band, food.Excess).Should().Be((1100m, 300m, "exceeded", 100m), "card items count in their invoice competence");
            summary.TotalLimit.Should().Be(3000m);
            summary.TotalCommitted.Should().Be(2890m);
            summary.Unbudgeted.Should().Be(40m);
            summary.Lines.Should().Contain(x => x.Name == "Sem categoria" && x.Committed == 40m);
        }

        [Fact]
        public void Projection_FindsFirstDeficitAndKeepsCauses()
        {
            var today = new DateOnly(2026, 10, 10);
            var events = new CashEvent[]
            {
                new(new DateOnly(2026, 10, 5), -100m, "bill", "Vencida", null, false),
                new(new DateOnly(2026, 10, 15), -900m, "bill", "Aluguel", null, false),
                new(new DateOnly(2026, 10, 20), 1500m, "income", "Salário", null, false),
                new(new DateOnly(2026, 12, 1), -10m, "bill", "Fora do horizonte", null, false)
            };
            var projection = CashProjector.Project(500m, today, new DateOnly(2026, 11, 10), events);
            projection.Days.First().Date.Should().Be(today, "overdue items are placed on today");
            projection.FirstDeficit.Should().Be(new DateOnly(2026, 10, 15));
            (projection.Lowest, projection.LowestDate).Should().Be((-500m, new DateOnly(2026, 10, 15)));
            projection.End.Should().Be(1000m);
            projection.Days.Single(x => x.Date == new DateOnly(2026, 10, 15)).Events.Single().Description.Should().Be("Aluguel");
        }

        [Fact]
        public void Alerts_ReturnOnlyWhenTheConditionChangesOrSnoozeEnds()
        {
            var today = new DateOnly(2026, 10, 10);
            AlertCondition Budget(string band) => new("budget:x:2026-10", band, "budget", "warning", "Mercado", "", "/budget", today);
            var conditions = new[] { Budget("warning"), new AlertCondition("due:a", "v1", "due", "warning", "Luz", "", "/expenses", today) };
            AlertCenter.Merge(conditions, [], today).Should().OnlyContain(x => x.Status == "new");
            var memory = new[] { new AlertMemory("budget:x:2026-10", "warning", "resolved", null), new AlertMemory("due:a", "v1", "snoozed", today.AddDays(2)) };
            AlertCenter.Merge(conditions, memory, today).Should().BeEmpty("a resolved or snoozed condition is not repeated");
            AlertCenter.Merge([Budget("exceeded"), conditions[1]], memory, today).Should().ContainSingle(x => x.Condition.Key == "budget:x:2026-10" && x.Status == "new");
            AlertCenter.Merge(conditions, memory, today.AddDays(2)).Should().ContainSingle(x => x.Condition.Key == "due:a");
            AlertCenter.Merge(conditions, [new AlertMemory("due:a", "v1", "read", null)], today).Single(x => x.Condition.Key == "due:a").Status.Should().Be("read",
                "reading does not resolve the cause");
        }
    }
}
