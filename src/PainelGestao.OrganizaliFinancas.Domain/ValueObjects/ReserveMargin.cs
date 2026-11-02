namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>
    /// A22: how much the household can still spend in a month without eating the minimum reserve.
    /// margin = projected closing balance − reserve; red when the closing balance is below the reserve (the shortfall says by how much),
    /// yellow when the margin is positive but under the attention threshold, green otherwise. Unused margin rolls over through the balance.
    /// </summary>
    public static class ReserveMargin
    {
        public const decimal AttentionThreshold = 1000m;
        public static readonly string[] Statuses = ["green", "yellow", "red"];
        public static decimal Margin(decimal closing, decimal reserve) => Money.Round(closing - reserve);
        public static decimal Shortfall(decimal closing, decimal reserve) => Math.Max(0, Money.Round(reserve - closing));
        public static string Status(decimal closing, decimal reserve)
        {
            var margin = Margin(closing, reserve);
            return margin < 0 ? "red" : margin < AttentionThreshold ? "yellow" : "green";
        }
    }
}
