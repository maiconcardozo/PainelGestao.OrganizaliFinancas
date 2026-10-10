using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>D22: approved frequencies, shared by income sources and recurring expenses. Biweekly means every 14 days, not two fixed days in the month.</summary>
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
        public IEnumerable<DateOnly> Occurrences(DateOnly from, DateOnly to) => Recurrence.Occurrences(Frequency, ValidFrom, EndDate, from, to);
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
        public static (DateOnly From, DateOnly To) Window(DateOnly today, int horizonMonths) => Recurrence.Window(today, horizonMonths);

        public static IReadOnlyList<PlannedOccurrence> Expected(IReadOnlyList<IncomeTermsVersion> versions, DateOnly from, DateOnly to) =>
            Recurrence.Expected(versions, x => x.Version, x => x.Terms.ValidFrom, (x, start, end) => x.Terms.Occurrences(start, end), x => x.Terms.ExpectedNetAmount, from, to);

        public static ForecastPlan Plan(IReadOnlyList<ForecastState> existing, IReadOnlyList<PlannedOccurrence> expected, DateOnly from, DateOnly to) =>
            Recurrence.Plan(existing, expected, from, to);
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
