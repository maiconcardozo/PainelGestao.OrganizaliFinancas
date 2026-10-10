using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.ExpenseTypes;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class ExpenseTypeAssertions
    {
        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader)
        {
            var fields = new[] { new CustomFieldDefinition("installation", "Instalação", "text", true, 20, null, null, null) };
            var payload = new ExpenseTypePayload("Água", true, 0, "recurring", null, fields);
            var create = await owner.PostAsJsonAsync("/api/expense-types", payload);
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var type = (await create.Content.ReadFromJsonAsync<SuccessDetails<ExpenseTypeDto>>())!.Data;
            type.Version.Should().Be(1);
            (await owner.PostAsJsonAsync($"/api/expense-types/{type.Code}/validate", new { version = 1, values = new { } }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await owner.PostAsJsonAsync($"/api/expense-types/{type.Code}/validate", new { version = 1, values = new { installation = "12345" } }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await other.GetAsync($"/api/expense-types/{type.Code}/versions/1")).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await reader.PostAsJsonAsync("/api/expense-types", payload)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var changed = payload with { Version = 1, Fields = [new("reading", "Leitura", "decimal", true, null, 0, 10000, null)] };
            (await owner.PutAsJsonAsync($"/api/expense-types/{type.Code}", changed)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await owner.PutAsJsonAsync($"/api/expense-types/{type.Code}", changed)).StatusCode.Should().Be(HttpStatusCode.Conflict);
            var original = (await owner.GetFromJsonAsync<SuccessDetails<ExpenseTypeDto>>($"/api/expense-types/{type.Code}/versions/1"))!.Data;
            original.Fields.Single().Key.Should().Be("installation");
            (await owner.PostAsJsonAsync($"/api/expense-types/{type.Code}/validate", new { version = 1, values = new { installation = "12345" } }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await owner.PostAsJsonAsync($"/api/expense-types/{type.Code}/validate", new { version = 2, values = new { installation = "12345" } }))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await owner.PostAsJsonAsync($"/api/expense-types/{type.Code}/validate", new { version = 2, values = new { reading = 10.5m } }))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await owner.PostAsJsonAsync("/api/expense-types", payload with { Name = "Luz" })).StatusCode.Should().Be(HttpStatusCode.Created);
            (await owner.PostAsJsonAsync("/api/expense-types", payload with { Name = "Serviço particular", Behavior = "single", Fields = [] })).StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }
}
