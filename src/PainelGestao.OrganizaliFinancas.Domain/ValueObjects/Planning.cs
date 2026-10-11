using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    public sealed record CategoryNode(Guid Code, string Name, Guid? ParentCode, bool IsActive);
    /// <summary>Spending of one category in a competence. Card items count in the month of their invoice (rule 7).</summary>
    public sealed record CategorySpend(Guid? CategoryCode, decimal Bills, decimal Card, decimal Paid)
    {
        public decimal Committed => Bills + Card;
    }
    public sealed record BudgetLine(Guid? CategoryCode, string Name, Guid? ParentCode, int Depth, decimal? Limit, decimal Committed, decimal Paid, decimal Card,
        decimal? UsedPercent, string Band, decimal Excess, bool CountsInTotal);
    public sealed record BudgetSummary(IReadOnlyList<BudgetLine> Lines, decimal TotalLimit, decimal TotalCommitted, decimal TotalPaid, decimal Unbudgeted);

    /// <summary>
    /// Rule 8: a parent aggregates its children; a child budget and a parent ceiling can coexist, but the household total only uses the
    /// highest budgeted level. No budget means "Sem orçamento"; a zero limit is valid and any positive spending exceeds it.
    /// </summary>
    public static class BudgetEvaluation
    {
        public static void ValidateThresholds(int warning, int limit)
        {
            if (warning is < 1 or > 100 || limit is < 1 or > 200 || warning > limit)
                throw new InputValidationException(new Dictionary<string, string[]> { ["WarningPercent"] = ["Use aproximação entre 1% e 100%, menor ou igual ao limite (até 200%)."] });
        }
        public static string Band(decimal committed, decimal? limit, int warningPercent, int limitPercent)
        {
            if (limit is not decimal value) return "none";
            if (value == 0) return committed > 0 ? "exceeded" : "ok";
            var used = committed * 100 / value;
            return used >= limitPercent ? (committed > value ? "exceeded" : "limit") : used >= warningPercent ? "warning" : "ok";
        }
        public static BudgetSummary Evaluate(IReadOnlyList<CategoryNode> categories, IReadOnlyDictionary<Guid, decimal> limits, IReadOnlyList<CategorySpend> spend,
            int warningPercent, int limitPercent)
        {
            var byCode = categories.ToDictionary(x => x.Code);
            var children = categories.Where(x => x.ParentCode is not null).ToLookup(x => x.ParentCode!.Value);
            var own = spend.Where(x => x.CategoryCode is not null).GroupBy(x => x.CategoryCode!.Value)
                .ToDictionary(x => x.Key, x => (Bills: x.Sum(s => s.Bills), Card: x.Sum(s => s.Card), Paid: x.Sum(s => s.Paid)));
            (decimal Bills, decimal Card, decimal Paid) Subtree(Guid code)
            {
                var total = own.GetValueOrDefault(code);
                foreach (var child in children[code]) { var sub = Subtree(child.Code); total = (total.Bills + sub.Bills, total.Card + sub.Card, total.Paid + sub.Paid); }
                return total;
            }
            IEnumerable<Guid> Ancestors(CategoryNode node)
            {
                for (var parent = node.ParentCode; parent is Guid code && byCode.TryGetValue(code, out var next); parent = next.ParentCode) yield return code;
            }
            var lines = new List<BudgetLine>();
            void Add(CategoryNode node, int depth)
            {
                var total = Subtree(node.Code);
                decimal? limit = limits.TryGetValue(node.Code, out var value) ? value : null;
                var committed = total.Bills + total.Card;
                if (node.IsActive || limit is not null || committed != 0)
                    lines.Add(new BudgetLine(node.Code, node.Name, node.ParentCode, depth, limit, committed, total.Paid, total.Card,
                        limit is > 0 ? Money.Round(committed * 100 / limit.Value) : null, Band(committed, limit, warningPercent, limitPercent),
                        limit is decimal l ? Math.Max(0, committed - l) : 0, limit is not null && !Ancestors(node).Any(limits.ContainsKey)));
                foreach (var child in children[node.Code].OrderBy(x => x.Name)) Add(child, depth + 1);
            }
            foreach (var root in categories.Where(x => x.ParentCode is null || !byCode.ContainsKey(x.ParentCode.Value)).OrderBy(x => x.Name)) Add(root, 0);
            var loose = spend.Where(x => x.CategoryCode is null || !byCode.ContainsKey(x.CategoryCode.Value)).ToArray();
            if (loose.Sum(x => x.Committed) != 0)
                lines.Add(new BudgetLine(null, "Sem categoria", null, 0, null, loose.Sum(x => x.Committed), loose.Sum(x => x.Paid), loose.Sum(x => x.Card), null, "none", 0, false));
            var counted = lines.Where(x => x.CountsInTotal).ToArray();
            var totalCommitted = spend.Sum(x => x.Committed);
            var coveredByBudget = counted.Sum(x => x.Committed);
            return new BudgetSummary(lines, counted.Sum(x => x.Limit ?? 0), totalCommitted, spend.Sum(x => x.Paid), totalCommitted - coveredByBudget);
        }
    }

    /// <summary>Signed cash event: inflows positive, outflows negative. Projections never change real balances.</summary>
    public sealed record CashEvent(DateOnly Date, decimal Amount, string Kind, string Description, Guid? Code, bool IsEstimate);
    public sealed record ProjectionDay(DateOnly Date, decimal Inflow, decimal Outflow, decimal Balance, IReadOnlyList<CashEvent> Events);
    public sealed record CashProjection(DateOnly From, DateOnly To, decimal Start, IReadOnlyList<ProjectionDay> Days, DateOnly? FirstDeficit, decimal Lowest, DateOnly LowestDate)
    {
        public decimal End => Days.Count == 0 ? Start : Days[^1].Balance;
    }
    /// <summary>
    /// Rule 1/8: projected balance = current balance + open expected receipts − open future disbursements, without re-applying realized movements.
    /// Open items already overdue are placed on today, because they still have to be settled.
    /// </summary>
    public static class CashProjector
    {
        public static CashProjection Project(decimal start, DateOnly today, DateOnly until, IEnumerable<CashEvent> events)
        {
            var placed = events.Where(x => x.Amount != 0 && x.Date <= until).Select(x => x.Date < today ? x with { Date = today } : x)
                .GroupBy(x => x.Date).OrderBy(x => x.Key).ToArray();
            var balance = start;
            DateOnly? deficit = start < 0 ? today : null;
            var (lowest, lowestDate) = (start, today);
            var days = new List<ProjectionDay>();
            foreach (var day in placed)
            {
                var inflow = day.Where(x => x.Amount > 0).Sum(x => x.Amount);
                var outflow = day.Where(x => x.Amount < 0).Sum(x => x.Amount);
                balance += inflow + outflow;
                if (balance < 0 && deficit is null) deficit = day.Key;
                if (balance < lowest) (lowest, lowestDate) = (balance, day.Key);
                days.Add(new ProjectionDay(day.Key, inflow, outflow, balance, day.OrderBy(x => x.Amount).ToArray()));
            }
            return new CashProjection(today, until, start, days, deficit, lowest, lowestDate);
        }
    }

    /// <summary>
    /// RF31: an alert is identified by its condition (Key) and its relevant state (Fingerprint). Reading never resolves the cause;
    /// a resolved or snoozed alert only returns when the fingerprint changes (new band, new version) or the snooze expires.
    /// </summary>
    public sealed record AlertCondition(string Key, string Fingerprint, string Kind, string Severity, string Title, string Detail, string Link, DateOnly Date);
    public sealed record AlertMemory(string Key, string Fingerprint, string Status, DateOnly? SnoozedUntil);
    public sealed record ActiveAlert(AlertCondition Condition, string Status);
    public static class AlertCenter
    {
        public static readonly string[] Statuses = ["read", "snoozed", "resolved"];
        public static IReadOnlyList<ActiveAlert> Merge(IReadOnlyList<AlertCondition> conditions, IReadOnlyList<AlertMemory> memory, DateOnly today)
        {
            var known = memory.ToDictionary(x => x.Key);
            var result = new List<ActiveAlert>();
            foreach (var condition in conditions.GroupBy(x => x.Key).Select(x => x.First()))
            {
                if (!known.TryGetValue(condition.Key, out var state) || state.Fingerprint != condition.Fingerprint) { result.Add(new(condition, "new")); continue; }
                if (state.Status == "resolved") continue;
                if (state.Status == "snoozed" && state.SnoozedUntil > today) continue;
                result.Add(new(condition, state.Status == "read" ? "read" : "new"));
            }
            var order = new Dictionary<string, int> { ["critical"] = 0, ["warning"] = 1, ["info"] = 2 };
            return result.OrderBy(x => order.GetValueOrDefault(x.Condition.Severity, 3)).ThenBy(x => x.Condition.Date).ToArray();
        }
    }
}
