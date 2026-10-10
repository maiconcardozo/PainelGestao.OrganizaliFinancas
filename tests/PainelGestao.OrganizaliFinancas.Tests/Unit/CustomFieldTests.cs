using System.Text.Json;
using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class CustomFieldTests
    {
        private static readonly CustomFieldDefinition[] Fields = [
            new("meter", "Instalação", "text", true, 10, null, null, null),
            new("amount", "Leitura", "decimal", true, null, 0, 100, null),
            new("date", "Data", "date", true, null, null, null, null),
            new("paid", "Confirmado", "boolean", true, null, null, null, null),
            new("option", "Opção", "list", true, null, null, null, ["A", "B"])
        ];
        private static Dictionary<string, JsonElement> Values(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        [Fact]
        public void ValidTypedValues_AcceptFalseAndZero()
        {
            CustomFields.ValidateDefinitions(Fields);
            CustomFields.ValidateValues(Fields, Values("""{"meter":"123","amount":0,"date":"2026-10-10","paid":false,"option":"A"}"""));
        }
        [Theory]
        [InlineData("{}")]
        [InlineData("""{"meter":"123","amount":"10","date":"2026-10-10","paid":false,"option":"A"}""")]
        [InlineData("""{"meter":"123","amount":101,"date":"2026-10-10","paid":false,"option":"A"}""")]
        [InlineData("""{"meter":"123","amount":10,"date":"2026-02-30","paid":false,"option":"A"}""")]
        [InlineData("""{"meter":"123","amount":10,"date":"2026-10-10","paid":"false","option":"A"}""")]
        [InlineData("""{"meter":"123","amount":10,"date":"2026-10-10","paid":false,"option":"C"}""")]
        [InlineData("""{"meter":"123","amount":10,"date":"2026-10-10","paid":false,"option":"A","extra":1}""")]
        public void InvalidValues_AreRejected(string json)
        {
            var validate = () => CustomFields.ValidateValues(Fields, Values(json));
            validate.Should().Throw<InputValidationException>();
        }
        [Fact]
        public void DuplicateKeys_AreRejected()
        {
            var validate = () => CustomFields.ValidateDefinitions([Fields[0], Fields[0]]);
            validate.Should().Throw<InputValidationException>();
        }
    }
}
