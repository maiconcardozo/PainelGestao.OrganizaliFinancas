using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>One card billing cycle. The reference month is the month of the due date.</summary>
    public sealed record CardCycle(DateOnly ClosingDate, DateOnly DueDate)
    {
        public DateOnly ReferenceMonth => new(DueDate.Year, DueDate.Month, 1);
    }
    public sealed record PlannedCardItem(int Number, CardCycle Cycle, decimal Amount);

    /// <summary>
    /// Card cycles. D13 premise (A06): a purchase on the closing day goes to the next invoice unless the card is set to "current";
    /// the purchase preview can still move the first installment by one cycle before saving.
    /// </summary>
    public static class CardSchedule
    {
        public static readonly string[] ClosingDayRules = ["next", "current"];
        public static DateOnly Day(int year, int month, int day) => new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

        public static void ValidateDays(int closingDay, int dueDay, string rule)
        {
            var errors = new Dictionary<string, string[]>();
            if (closingDay is < 1 or > 31) errors["ClosingDay"] = ["Informe o dia de fechamento entre 1 e 31."];
            if (dueDay is < 1 or > 31) errors["DueDay"] = ["Informe o dia de vencimento entre 1 e 31."];
            if (!ClosingDayRules.Contains(rule)) errors["ClosingDayRule"] = ["Escolha em qual fatura entra a compra feita no dia do fechamento."];
            if (errors.Count > 0) throw new InputValidationException(errors);
        }

        /// <summary>Cycle that closes in the given month; the due date falls in the same month when it comes after the closing day.</summary>
        public static CardCycle ClosingIn(DateOnly month, int closingDay, int dueDay)
        {
            var closing = Day(month.Year, month.Month, closingDay);
            var dueMonth = dueDay > closingDay ? month : month.AddMonths(1);
            return new CardCycle(closing, Day(dueMonth.Year, dueMonth.Month, dueDay));
        }

        public static DateOnly ClosingMonthFor(DateOnly purchase, int closingDay, string rule)
        {
            var month = new DateOnly(purchase.Year, purchase.Month, 1);
            var closing = Day(purchase.Year, purchase.Month, closingDay);
            return purchase < closing || (purchase == closing && rule == "current") ? month : month.AddMonths(1);
        }

        /// <param name="shift">−1, 0 or +1: the preview lets the household correct the first invoice by one cycle.</param>
        public static IReadOnlyList<PlannedCardItem> Plan(decimal total, int count, DateOnly purchase, int closingDay, int dueDay, string rule, int shift = 0)
        {
            if (shift is < -1 or > 1) throw new InputValidationException(new Dictionary<string, string[]> { ["FirstInvoiceShift"] = ["A fatura de destino só pode mudar um ciclo."] });
            if (count is < 1 or > Installments.MaxCount) throw new InputValidationException(new Dictionary<string, string[]> { ["Count"] = [$"Informe de 1 a {Installments.MaxCount} parcelas."] });
            var amounts = count == 1 ? (total > 0 && Money.Round(total) == total ? [total]
                : throw new InputValidationException(new Dictionary<string, string[]> { ["Total"] = ["Informe um total positivo em centavos."] }))
                : Installments.Split(total, count);
            var first = ClosingMonthFor(purchase, closingDay, rule).AddMonths(shift);
            return amounts.Select((amount, index) => new PlannedCardItem(index + 1, ClosingIn(first.AddMonths(index), closingDay, dueDay), amount)).ToArray();
        }
    }
}
