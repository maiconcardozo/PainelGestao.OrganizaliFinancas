using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>Immutable terms of one recurring expense version. Estimated amounts (e.g. water bill) are confirmed per occurrence.</summary>
    public sealed record ExpenseTerms(DateOnly ValidFrom, DateOnly? EndDate, string Frequency, decimal Amount, bool IsEstimated)
    {
        public void Validate()
        {
            var errors = new Dictionary<string, string[]>();
            if (!IncomeFrequency.IsValid(Frequency)) errors["Frequency"] = ["Periodicidade inválida."];
            if (Amount <= 0 || Money.Round(Amount) != Amount) errors["Amount"] = ["Informe um valor positivo em centavos."];
            if (EndDate < ValidFrom) errors["EndDate"] = ["O término não pode ser anterior ao início."];
            if (ValidFrom.Year < 2000) errors["ValidFrom"] = ["Data inicial inválida."];
            if (errors.Count > 0) throw new InputValidationException(errors);
        }
        public IEnumerable<DateOnly> Occurrences(DateOnly from, DateOnly to) => Recurrence.Occurrences(Frequency, ValidFrom, EndDate, from, to);
    }
    public sealed record ExpenseTermsVersion(int Version, ExpenseTerms Terms);
    public static class ExpenseSchedule
    {
        public static IReadOnlyList<PlannedOccurrence> Expected(IReadOnlyList<ExpenseTermsVersion> versions, DateOnly from, DateOnly to) =>
            Recurrence.Expected(versions, x => x.Version, x => x.Terms.ValidFrom, (x, start, end) => x.Terms.Occurrences(start, end), x => x.Terms.Amount, from, to);
    }

    public sealed record PlannedInstallment(int Number, DateOnly DueDate, decimal Amount);
    /// <summary>Exact split: every installment gets the truncated cent value and the last one absorbs the residue (100 in 3 = 33.33 + 33.33 + 33.34).</summary>
    public static class Installments
    {
        public const int MaxCount = 120;
        public static IReadOnlyList<decimal> Split(decimal total, int count)
        {
            if (count is < 2 or > MaxCount) throw new InputValidationException(new Dictionary<string, string[]> { ["Count"] = [$"Informe de 2 a {MaxCount} parcelas."] });
            if (total <= 0 || Money.Round(total) != total) throw new InputValidationException(new Dictionary<string, string[]> { ["Total"] = ["Informe um total positivo em centavos."] });
            var part = Math.Floor(total * 100 / count) / 100;
            if (part <= 0) throw new InputValidationException(new Dictionary<string, string[]> { ["Total"] = ["Cada parcela precisa de ao menos R$ 0,01."] });
            return Enumerable.Range(0, count).Select(index => index == count - 1 ? total - part * (count - 1) : part).ToArray();
        }
        public static IReadOnlyList<PlannedInstallment> Plan(decimal total, int count, DateOnly firstDueDate, string frequency)
        {
            if (!IncomeFrequency.IsValid(frequency)) throw new InputValidationException(new Dictionary<string, string[]> { ["Frequency"] = ["Periodicidade inválida."] });
            return Split(total, count).Select((amount, index) => new PlannedInstallment(index + 1, IncomeFrequency.Nth(frequency, firstDueDate, index), amount)).ToArray();
        }
    }

    public sealed record ShareInput(Guid PersonCode, decimal Value);
    public sealed record ResolvedShare(Guid PersonCode, decimal Amount, decimal? Percent);
    /// <summary>D10 split by value or percentage. Cents follow the largest remainder method, ties broken by the person's Code.</summary>
    public static class Shares
    {
        public static bool IsMode(string? mode) => mode is "value" or "percent";
        public static IReadOnlyList<ResolvedShare> Resolve(decimal amount, string? mode, IReadOnlyList<ShareInput>? items)
        {
            if (items is null || items.Count == 0) return [];
            var errors = new Dictionary<string, string[]>();
            if (!IsMode(mode)) errors["SplitMode"] = ["Escolha dividir por valor ou por percentual."];
            if (items.Select(x => x.PersonCode).Distinct().Count() != items.Count) errors["Shares"] = ["Cada morador aparece uma única vez no rateio."];
            if (items.Any(x => x.Value <= 0)) errors["Shares"] = ["Cada parte do rateio deve ser maior que zero."];
            if (errors.Count == 0 && mode == "value" && items.Sum(x => x.Value) != amount)
                errors["Shares"] = [$"A soma do rateio ({Money.Format(items.Sum(x => x.Value))}) deve ser igual ao valor da despesa ({Money.Format(amount)})."];
            if (errors.Count == 0 && mode == "percent" && items.Sum(x => x.Value) != 100m) errors["Shares"] = ["Os percentuais do rateio devem somar 100%."];
            if (errors.Count > 0) throw new InputValidationException(errors);
            if (mode == "value") return items.Select(x => new ResolvedShare(x.PersonCode, x.Value, null)).ToArray();
            var raw = items.Select(x => (x.PersonCode, Percent: x.Value, Exact: amount * x.Value / 100m)).ToArray();
            var floors = raw.Select(x => (x.PersonCode, x.Percent, Cents: Math.Floor(x.Exact * 100), Remainder: x.Exact * 100 - Math.Floor(x.Exact * 100))).ToArray();
            var missing = (int)(amount * 100 - floors.Sum(x => x.Cents));
            var extra = floors.OrderByDescending(x => x.Remainder).ThenBy(x => x.PersonCode).Take(missing).Select(x => x.PersonCode).ToHashSet();
            return floors.Select(x => new ResolvedShare(x.PersonCode, (x.Cents + (extra.Contains(x.PersonCode) ? 1 : 0)) / 100m, x.Percent)).ToArray();
        }
    }

    /// <summary>Open balance = principal + confirmed additions − confirmed discounts − live payments. "Overdue" is derived, never stored.</summary>
    public sealed record ObligationState(string Status, decimal OpenBalance, bool IsOverdue);
    public static class ObligationBalance
    {
        public static ObligationState Of(decimal amount, decimal adjustments, decimal paid, bool cancelled, DateOnly dueDate, DateOnly today)
        {
            var open = cancelled ? 0 : amount + adjustments - paid;
            var status = cancelled ? "cancelled" : open <= 0 ? "paid" : paid > 0 ? "partial" : "open";
            return new ObligationState(status, open, !cancelled && open > 0 && dueDate < today);
        }
    }

    public sealed record ContributionLine(Guid PersonCode, string PersonName, decimal Responsible, decimal Paid)
    {
        public decimal Balance => Paid - Responsible;
    }
    public sealed record Settlement(Guid FromPersonCode, string FromPersonName, Guid ToPersonCode, string ToPersonName, decimal Amount);
    /// <summary>Who owes whom inside the household. It is a suggestion for an internal transfer, never a second expense or income.</summary>
    public static class Contributions
    {
        public static IReadOnlyList<Settlement> Settle(IReadOnlyList<ContributionLine> lines)
        {
            var debtors = lines.Where(x => x.Balance < 0).Select(x => (Line: x, Left: -x.Balance)).OrderByDescending(x => x.Left).ThenBy(x => x.Line.PersonCode).ToList();
            var creditors = lines.Where(x => x.Balance > 0).Select(x => (Line: x, Left: x.Balance)).OrderByDescending(x => x.Left).ThenBy(x => x.Line.PersonCode).ToList();
            var result = new List<Settlement>();
            int d = 0, c = 0;
            while (d < debtors.Count && c < creditors.Count)
            {
                var value = Math.Min(debtors[d].Left, creditors[c].Left);
                result.Add(new Settlement(debtors[d].Line.PersonCode, debtors[d].Line.PersonName, creditors[c].Line.PersonCode, creditors[c].Line.PersonName, value));
                debtors[d] = (debtors[d].Line, debtors[d].Left - value);
                creditors[c] = (creditors[c].Line, creditors[c].Left - value);
                if (debtors[d].Left == 0) d++;
                if (creditors[c].Left == 0) c++;
            }
            return result;
        }
    }
}
