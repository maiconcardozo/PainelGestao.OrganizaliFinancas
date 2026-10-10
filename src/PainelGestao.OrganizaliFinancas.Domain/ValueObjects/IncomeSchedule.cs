using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>D22: approved frequencies. Biweekly means every 14 days, not two fixed days in the month.</summary>
    public static class IncomeFrequency
    {
        private static readonly Dictionary<string, (int Days, int Months)> Steps = new(StringComparer.Ordinal)
        {
            ["weekly"] = (7, 0), ["biweekly"] = (14, 0), ["monthly"] = (0, 1),
            ["quarterly"] = (0, 3), ["semiannual"] = (0, 6), ["annual"] = (0, 12)
        };
        public static bool IsValid(string? frequency) => frequency is not null && Steps.ContainsKey(frequency);
        // Month steps always start from the anchor, so day 31 becomes the month's last day and returns to 31 afterwards.
        public static DateOnly Nth(string frequency, DateOnly anchor, int index)
        {
            var (days, months) = Steps[frequency];
            return days > 0 ? anchor.AddDays(days * index) : anchor.AddMonths(months * index);
        }
    }

    /// <summary>Immutable terms of one income source version. Fixed income has an expected net amount; variable income is estimated by its average (D24).</summary>
    public sealed record IncomeTerms(DateOnly ValidFrom, DateOnly? EndDate, string Frequency, string AmountMode, decimal? ExpectedNetAmount, decimal? GrossAmount)
    {
        public bool IsVariable => AmountMode == "variable";
        public void Validate()
        {
            var errors = new Dictionary<string, string[]>();
            if (!IncomeFrequency.IsValid(Frequency)) errors["Frequency"] = ["Periodicidade inválida."];
            if (AmountMode is not ("fixed" or "variable")) errors["AmountMode"] = ["Informe se o valor é fixo ou variável."];
            if (AmountMode == "fixed" && ExpectedNetAmount is not > 0) errors["ExpectedNetAmount"] = ["Informe o líquido esperado maior que zero."];
            if (AmountMode == "variable" && (ExpectedNetAmount is not null || GrossAmount is not null))
                errors["ExpectedNetAmount"] = ["Renda variável usa a média dos recebimentos; não informe valor esperado."];
            if (GrossAmount is not null && (GrossAmount <= 0 || GrossAmount < ExpectedNetAmount)) errors["GrossAmount"] = ["O bruto deve ser maior ou igual ao líquido."];
            if (EndDate < ValidFrom) errors["EndDate"] = ["O término não pode ser anterior ao início."];
            if (ValidFrom.Year < 2000) errors["ValidFrom"] = ["Data inicial inválida."];
            if (errors.Count > 0) throw new InputValidationException(errors);
        }
        public IEnumerable<DateOnly> Occurrences(DateOnly from, DateOnly to)
        {
            var last = EndDate is DateOnly end && end < to ? end : to;
            for (var index = 0; ; index++)
            {
                var date = IncomeFrequency.Nth(Frequency, ValidFrom, index);
                if (date > last) yield break;
                if (date >= from) yield return date;
            }
        }
    }
    public sealed record IncomeTermsVersion(int Version, IncomeTerms Terms);
    public sealed record PlannedOccurrence(DateOnly Date, int Version, decimal? Amount);
    /// <summary>Locked forecasts have live allocations or were closed and are never rewritten.</summary>
    public sealed record ForecastState(Guid Code, DateOnly Date, bool IsActive, bool Locked, int SourceVersion, decimal? ExpectedAmount);
    public sealed record ForecastPlan(IReadOnlyList<PlannedOccurrence> Inserts, IReadOnlyList<(Guid Code, PlannedOccurrence Target)> Updates, IReadOnlyList<Guid> Cancels)
    {
        public bool IsEmpty => Inserts.Count == 0 && Updates.Count == 0 && Cancels.Count == 0;
    }

    public static class IncomeSchedule
    {
        /// <summary>Generation window: previous 12 months (for receipts registered late) through the D23 horizon.</summary>
        public static (DateOnly From, DateOnly To) Window(DateOnly today, int horizonMonths)
        {
            var month = new DateOnly(today.Year, today.Month, 1);
            return (month.AddMonths(-12), month.AddMonths(horizonMonths + 1).AddDays(-1));
        }

        public static IReadOnlyList<PlannedOccurrence> Expected(IReadOnlyList<IncomeTermsVersion> versions, DateOnly from, DateOnly to)
        {
            var ordered = versions.OrderBy(x => x.Terms.ValidFrom).ThenBy(x => x.Version).ToArray();
            var result = new List<PlannedOccurrence>();
            for (var index = 0; index < ordered.Length; index++)
            {
                var current = ordered[index];
                var segmentEnd = index + 1 < ordered.Length ? ordered[index + 1].Terms.ValidFrom.AddDays(-1) : to;
                var end = segmentEnd < to ? segmentEnd : to;
                var start = current.Terms.ValidFrom > from ? current.Terms.ValidFrom : from;
                if (end < start) continue;
                result.AddRange(current.Terms.Occurrences(start, end).Select(date => new PlannedOccurrence(date, current.Version, current.Terms.ExpectedNetAmount)));
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

    /// <summary>D24: calendar year over closed months; in January the previous full year divided by 12.</summary>
    public sealed record AverageWindow(DateOnly From, DateOnly ToExclusive, int Months);
    public static class IncomeAverage
    {
        public static AverageWindow Window(DateOnly today) => today.Month == 1
            ? new(new DateOnly(today.Year - 1, 1, 1), new DateOnly(today.Year, 1, 1), 12)
            : new(new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, today.Month, 1), today.Month - 1);
        public static decimal Of(decimal total, AverageWindow window) => Money.Round(total / window.Months);
    }
}
