using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    /// <summary>One line of a bank statement: positive amounts entered the account, negative amounts left it.</summary>
    public sealed record ParsedTransaction(DateOnly Date, decimal Amount, string Description, string? ExternalId);

    /// <summary>
    /// Reads OFX (SGML or XML) and CSV statements exported by Brazilian banks. Parsing never writes anything:
    /// the household reviews the preview and decides line by line (RF44).
    /// </summary>
    public static partial class StatementParser
    {
        public const int MaxLines = 2000;
        private static readonly string[] DateFormats = ["dd/MM/yyyy", "dd/MM/yy", "yyyy-MM-dd", "dd-MM-yyyy", "yyyyMMdd", "dd.MM.yyyy", "d/M/yyyy"];

        public static string DetectFormat(string fileName, string content)
        {
            if (fileName.EndsWith(".ofx", StringComparison.OrdinalIgnoreCase) || content.Contains("<OFX", StringComparison.OrdinalIgnoreCase)
                || content.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase) || content.Contains("<STMTTRN>", StringComparison.OrdinalIgnoreCase))
                return "ofx";
            return "csv";
        }

        public static IReadOnlyList<ParsedTransaction> Parse(string format, string content)
        {
            var lines = format == "ofx" ? ParseOfx(content) : ParseCsv(content);
            if (lines.Count == 0) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = ["Nenhum lançamento reconhecido no arquivo. Confira se é um extrato OFX ou CSV com data, descrição e valor."] });
            if (lines.Count > MaxLines) throw new InputValidationException(new Dictionary<string, string[]> { ["File"] = [$"Importe até {MaxLines} lançamentos por arquivo."] });
            return lines;
        }

        /// <summary>Same account, date, amount and bank id (or normalized description) → same line. Used to refuse a second import.</summary>
        public static string Fingerprint(Guid accountCode, DateOnly date, decimal amount, string description, string? externalId)
        {
            var key = externalId is { Length: > 0 } ? $"id:{externalId.Trim()}" : $"desc:{Normalize(description)}";
            var text = $"{accountCode:N}|{date:yyyy-MM-dd}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{key}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..40];
        }
        private static string Normalize(string value) => Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();

        [GeneratedRegex(@"<STMTTRN>(.*?)(</STMTTRN>|(?=<STMTTRN>)|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex TransactionBlocks();
        [GeneratedRegex(@"<(?<tag>[A-Z0-9.]+)>(?<value>[^<\r\n]*)", RegexOptions.IgnoreCase)]
        private static partial Regex TagValues();

        private static List<ParsedTransaction> ParseOfx(string content)
        {
            var result = new List<ParsedTransaction>();
            foreach (Match block in TransactionBlocks().Matches(content))
            {
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match tag in TagValues().Matches(block.Groups[1].Value))
                    values.TryAdd(tag.Groups["tag"].Value, tag.Groups["value"].Value.Trim());
                if (!values.TryGetValue("DTPOSTED", out var posted) || posted.Length < 8 || !values.TryGetValue("TRNAMT", out var amountText)) continue;
                if (!DateOnly.TryParseExact(posted[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
                if (!TryMoney(amountText, out var amount) || amount == 0) continue;
                var description = values.GetValueOrDefault("MEMO");
                if (string.IsNullOrWhiteSpace(description)) description = values.GetValueOrDefault("NAME");
                if (string.IsNullOrWhiteSpace(description)) description = values.GetValueOrDefault("TRNTYPE") ?? "Lançamento importado";
                values.TryGetValue("FITID", out var id);
                result.Add(new ParsedTransaction(date, amount, Clip(description!), string.IsNullOrWhiteSpace(id) ? null : id));
            }
            return result;
        }

        private static List<ParsedTransaction> ParseCsv(string content)
        {
            var rows = content.Replace("\r\n", "\n").Split('\n').Select(line => line.Trim('\r')).Where(line => line.Trim().Length > 0).ToArray();
            if (rows.Length == 0) return [];
            var delimiter = DetectDelimiter(rows[0]);
            var table = rows.Select(row => SplitCsv(row, delimiter)).Where(cells => cells.Length >= 2).ToList();
            if (table.Count == 0) return [];
            var columns = Columns.Detect(table);
            var result = new List<ParsedTransaction>();
            foreach (var cells in table.Skip(columns.HasHeader ? 1 : 0))
            {
                if (columns.Date >= cells.Length || !TryDate(cells[columns.Date], out var date)) continue;
                decimal amount;
                if (columns.Amount is int amountColumn)
                {
                    if (amountColumn >= cells.Length || !TryMoney(cells[amountColumn], out amount)) continue;
                    if (columns.Sign is int signColumn && signColumn < cells.Length && cells[signColumn].Trim().ToUpperInvariant() is "D" or "DEBITO" or "DÉBITO" or "SAIDA" or "SAÍDA") amount = -Math.Abs(amount);
                }
                else
                {
                    var credit = columns.Credit is int c && c < cells.Length && TryMoney(cells[c], out var creditValue) ? Math.Abs(creditValue) : 0m;
                    var debit = columns.Debit is int d && d < cells.Length && TryMoney(cells[d], out var debitValue) ? Math.Abs(debitValue) : 0m;
                    amount = credit - debit;
                }
                if (amount == 0) continue;
                var description = columns.Description is int descriptionColumn && descriptionColumn < cells.Length ? cells[descriptionColumn] : "Lançamento importado";
                var id = columns.Id is int idColumn && idColumn < cells.Length && cells[idColumn].Trim().Length > 0 ? cells[idColumn].Trim() : null;
                result.Add(new ParsedTransaction(date, amount, Clip(string.IsNullOrWhiteSpace(description) ? "Lançamento importado" : description), id));
            }
            return result;
        }

        private sealed record Columns(bool HasHeader, int Date, int? Amount, int? Credit, int? Debit, int? Sign, int? Description, int? Id)
        {
            private static readonly string[] DateNames = ["data", "date", "dt "];
            private static readonly string[] AmountNames = ["valor", "amount", "montante", "quantia", "value"];
            private static readonly string[] CreditNames = ["crédito", "credito", "credit", "entrada"];
            private static readonly string[] DebitNames = ["débito", "debito", "debit", "saída", "saida"];
            private static readonly string[] DescriptionNames = ["descri", "hist", "memo", "lançamento", "lancamento", "detalhe", "estabelecimento", "title", "título", "titulo"];
            private static readonly string[] IdNames = ["identificador", "fitid", "documento", "doc", "id"];
            private static readonly string[] SignNames = ["tipo", "natureza", "d/c", "dc"];

            public static Columns Detect(List<string[]> table)
            {
                var header = table[0].Select(cell => cell.Trim().ToLowerInvariant()).ToArray();
                var hasHeader = header.Any(cell => DateNames.Concat(AmountNames).Concat(DescriptionNames).Any(name => cell.Contains(name)));
                if (hasHeader)
                {
                    int? Find(string[] names) { for (var i = 0; i < header.Length; i++) if (names.Any(name => header[i].Contains(name))) return i; return null; }
                    var credit = Find(CreditNames); var debit = Find(DebitNames);
                    var amount = Find(AmountNames);
                    if (amount is int a && (a == credit || a == debit)) amount = null;
                    if (amount is null && (credit is null || debit is null)) { amount = Find(AmountNames); credit = debit = null; }
                    var date = Find(DateNames) ?? 0;
                    var sign = amount is null ? null : Find(SignNames);
                    return new Columns(true, date, amount, credit, debit, sign, Find(DescriptionNames) ?? LongestText(table.Skip(1).ToList(), [date, amount, credit, debit]), Find(IdNames));
                }
                // No header: the first date-like column, the first money-like column after it, and the longest text column.
                var sample = table[0];
                int dateColumn = -1, amountColumn = -1;
                for (var i = 0; i < sample.Length; i++) if (TryDate(sample[i], out _)) { dateColumn = i; break; }
                if (dateColumn < 0) return new Columns(false, 0, null, null, null, null, null, null);
                for (var i = 0; i < sample.Length; i++) if (i != dateColumn && !TryDate(sample[i], out _) && TryMoney(sample[i], out _) && !sample[i].Any(char.IsLetter)) { amountColumn = i; break; }
                return new Columns(false, dateColumn, amountColumn < 0 ? null : amountColumn, null, null, null, LongestText(table, [dateColumn, amountColumn]), null);
            }
            private static int? LongestText(List<string[]> rows, int?[] taken)
            {
                var best = (Column: -1, Length: 0);
                var width = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
                for (var i = 0; i < width; i++)
                {
                    if (taken.Contains(i)) continue;
                    var length = rows.Sum(r => i < r.Length && r[i].Any(char.IsLetter) ? r[i].Length : 0);
                    if (length > best.Length) best = (i, length);
                }
                return best.Column < 0 ? null : best.Column;
            }
        }

        private static char DetectDelimiter(string line)
        {
            var candidates = new[] { ';', ',', '\t', '|' };
            return candidates.OrderByDescending(c => line.Count(ch => ch == c)).First();
        }
        private static string[] SplitCsv(string line, char delimiter)
        {
            var cells = new List<string>(); var current = new StringBuilder(); var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else quoted = !quoted; }
                else if (ch == delimiter && !quoted) { cells.Add(current.ToString().Trim()); current.Clear(); }
                else current.Append(ch);
            }
            cells.Add(current.ToString().Trim());
            return cells.ToArray();
        }
        private static bool TryDate(string text, out DateOnly date)
        {
            var value = text.Trim();
            if (value.Length > 10) value = value[..10];
            return DateOnly.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
        private static bool TryMoney(string text, out decimal amount)
        {
            amount = 0;
            var value = text.Trim().Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "").Replace(" ", "");
            if (value.Length == 0) return false;
            var negative = false;
            if (value.StartsWith('(') && value.EndsWith(')')) { negative = true; value = value[1..^1]; }
            if (value.EndsWith('-')) { negative = true; value = value[..^1]; }
            if (value.EndsWith("D", StringComparison.OrdinalIgnoreCase) && value.Length > 1 && char.IsDigit(value[^2])) { negative = true; value = value[..^1]; }
            else if (value.EndsWith("C", StringComparison.OrdinalIgnoreCase) && value.Length > 1 && char.IsDigit(value[^2])) value = value[..^1];
            if (value.StartsWith('-')) { negative = !negative; value = value[1..]; }
            if (value.StartsWith('+')) value = value[1..];
            if (value.Contains(',') && value.Contains('.')) value = value.LastIndexOf(',') > value.LastIndexOf('.') ? value.Replace(".", "").Replace(',', '.') : value.Replace(",", "");
            else if (value.Contains(',')) value = value.Replace(',', '.');
            if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)) return false;
            amount = decimal.Round(negative ? -parsed : parsed, 2);
            return true;
        }
        private static string Clip(string text) { var value = Regex.Replace(text.Trim(), @"\s+", " "); return value.Length > 200 ? value[..200] : value; }
    }
}
