namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>Materialization shared by income forecasts and recurring expenses (D23 horizon, one row per rule and date).</summary>
    public static class Recurrence
    {
        /// <summary>Generation window: previous 12 months (for entries registered late) through the D23 horizon.</summary>
        public static (DateOnly From, DateOnly To) Window(DateOnly today, int horizonMonths)
        {
            var month = new DateOnly(today.Year, today.Month, 1);
            return (month.AddMonths(-12), month.AddMonths(horizonMonths + 1).AddDays(-1));
        }

        public static IEnumerable<DateOnly> Occurrences(string frequency, DateOnly anchor, DateOnly? endDate, DateOnly from, DateOnly to)
        {
            var last = endDate is DateOnly end && end < to ? end : to;
            for (var index = 0; ; index++)
            {
                var date = IncomeFrequency.Nth(frequency, anchor, index);
                if (date > last) yield break;
                if (date >= from) yield return date;
            }
        }

        /// <summary>Each version governs the dates from its start until the next version starts.</summary>
        public static IReadOnlyList<PlannedOccurrence> Expected<T>(IReadOnlyList<T> versions, Func<T, int> version, Func<T, DateOnly> validFrom,
            Func<T, DateOnly, DateOnly, IEnumerable<DateOnly>> occurrences, Func<T, decimal?> amount, DateOnly from, DateOnly to)
        {
            var ordered = versions.OrderBy(validFrom).ThenBy(version).ToArray();
            var result = new List<PlannedOccurrence>();
            for (var index = 0; index < ordered.Length; index++)
            {
                var current = ordered[index];
                var segmentEnd = index + 1 < ordered.Length ? validFrom(ordered[index + 1]).AddDays(-1) : to;
                var end = segmentEnd < to ? segmentEnd : to;
                var start = validFrom(current) > from ? validFrom(current) : from;
                if (end < start) continue;
                result.AddRange(occurrences(current, start, end).Select(date => new PlannedOccurrence(date, version(current), amount(current))));
            }
            return result;
        }

        /// <summary>Idempotent reconciliation: running it twice with the same input produces an empty plan the second time.</summary>
        public static ForecastPlan Plan(IReadOnlyList<ForecastState> existing, IReadOnlyList<PlannedOccurrence> expected, DateOnly from, DateOnly to)
        {
            var byDate = existing.ToDictionary(x => x.Date);
            var inserts = new List<PlannedOccurrence>();
            var updates = new List<(Guid, PlannedOccurrence)>();
            foreach (var target in expected)
            {
                if (!byDate.TryGetValue(target.Date, out var current)) { inserts.Add(target); continue; }
                if (current.Locked) continue;
                if (!current.IsActive || current.SourceVersion != target.Version || current.ExpectedAmount != target.Amount)
                    updates.Add((current.Code, target));
            }
            var dates = expected.Select(x => x.Date).ToHashSet();
            var cancels = existing.Where(x => x.IsActive && !x.Locked && x.Date >= from && x.Date <= to && !dates.Contains(x.Date))
                .Select(x => x.Code).ToArray();
            return new ForecastPlan(inserts, updates, cancels);
        }
    }
}
