using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Domain.ValueObjects
{
    public sealed record CustomFieldDefinition(string Key, string Label, string Type, bool Required,
        int? MaxLength, decimal? Min, decimal? Max, string[]? Options);

    public static class CustomFields
    {
        public static void ValidateDefinitions(IReadOnlyList<CustomFieldDefinition> fields)
        {
            var errors = new Dictionary<string, string[]>();
            if (fields.Count > 30) errors["Fields"] = ["Use até 30 campos por tipo."];
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < fields.Count; index++)
            {
                var field = fields[index];
                if (field is null || string.IsNullOrWhiteSpace(field.Key) || !Regex.IsMatch(field.Key, "^[a-z][a-z0-9_]{0,39}$") ||
                    !keys.Add(field.Key) || string.IsNullOrWhiteSpace(field.Label) || field.Label.Length > 100 ||
                    field.Type is not ("text" or "decimal" or "date" or "boolean" or "list"))
                { errors[$"Fields[{index}]"] = ["Definição inválida: chave única, rótulo e tipo são obrigatórios."]; continue; }
                if (field.MaxLength is < 1 or > 2000 || (field.MaxLength is not null && field.Type != "text") ||
                    ((field.Min is not null || field.Max is not null) && field.Type != "decimal") || field.Min > field.Max)
                    errors[$"Fields[{index}]"] = ["Limites incompatíveis com o tipo do campo."];
                if (field.Type == "list" && (field.Options is null || field.Options.Length is < 1 or > 50 ||
                    field.Options.Any(option => string.IsNullOrWhiteSpace(option) || option.Length > 100) ||
                    field.Options.Distinct(StringComparer.Ordinal).Count() != field.Options.Length))
                    errors[$"Fields[{index}].Options"] = ["Informe de 1 a 50 opções distintas, com até 100 caracteres."];
                if (field.Type != "list" && field.Options is { Length: > 0 }) errors[$"Fields[{index}].Options"] = ["Opções são exclusivas de campos de lista."];
            }
            if (errors.Count > 0) throw new InputValidationException(errors);
        }

        public static void ValidateValues(IReadOnlyList<CustomFieldDefinition> fields, IReadOnlyDictionary<string, JsonElement> values)
        {
            var errors = new Dictionary<string, string[]>();
            var defined = fields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
            foreach (var key in values.Keys.Where(key => !defined.Contains(key))) errors[key] = ["Campo não definido nesta versão."];
            foreach (var field in fields)
            {
                var present = values.TryGetValue(field.Key, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);
                if (!present || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
                {
                    if (field.Required) errors[field.Key] = [$"Informe {field.Label}."];
                    continue;
                }
                var valid = field.Type switch
                {
                    "text" => value.ValueKind == JsonValueKind.String && value.GetString()!.Length <= (field.MaxLength ?? 2000),
                    "decimal" => value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) &&
                        (field.Min is null || number >= field.Min) && (field.Max is null || number <= field.Max),
                    "date" => value.ValueKind == JsonValueKind.String && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                    "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                    "list" => value.ValueKind == JsonValueKind.String && field.Options!.Contains(value.GetString(), StringComparer.Ordinal),
                    _ => false
                };
                if (!valid) errors[field.Key] = [$"Valor inválido para {field.Label}."];
            }
            if (errors.Count > 0) throw new InputValidationException(errors);
        }
    }
}
