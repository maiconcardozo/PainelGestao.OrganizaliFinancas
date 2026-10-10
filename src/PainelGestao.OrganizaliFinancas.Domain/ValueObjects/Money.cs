using System.Globalization;
using System.Text.RegularExpressions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>BRL amounts travel as invariant decimal strings ("1250.50") and are stored as decimal(19,2).</summary>
    public static partial class Money
    {
        public const decimal Max = 99_999_999_999_999_999.99m;
        [GeneratedRegex(@"^-?\d{1,17}(\.\d{1,2})?$", RegexOptions.CultureInvariant)]
        private static partial Regex Pattern();

        public static bool TryParse(string? value, out decimal amount)
        {
            amount = 0;
            return value is not null && Pattern().IsMatch(value) &&
                decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
        }
        public static bool IsPositive(string? value) => TryParse(value, out var amount) && amount > 0;
        public static decimal Parse(string value) =>
            TryParse(value, out var amount) ? amount : throw new FormatException("Valor monetário inválido.");
        public static string Format(decimal value) => Round(value).ToString("0.00", CultureInfo.InvariantCulture);
        public static decimal Round(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
