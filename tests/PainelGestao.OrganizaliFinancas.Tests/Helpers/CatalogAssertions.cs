using System.Net;
using System.Net.Http.Json;
using Dapper;
using FluentAssertions;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Catalogs;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class CatalogAssertions
    {
        public static async Task VerifyAsync(HttpClient owner, HttpClient other, HttpClient reader, string connection)
        {
            var parent = await Create(owner, "categories", "Moradia");
            var child = await Create(owner, "categories", "Energia", parent.Code);
            (await owner.PutAsJsonAsync($"/api/catalogs/categories/{parent.Code}", new CatalogPayload(parent.Name, true, 0, child.Code)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await owner.PutAsJsonAsync($"/api/catalogs/categories/{parent.Code}", new CatalogPayload(parent.Name, false, 0, null)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await other.PostAsJsonAsync("/api/catalogs/categories", new CatalogPayload("Cross-house", true, 0, parent.Code)))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            foreach (var kind in new[] { "categories", "income-types", "creditors", "payment-methods" })
            {
                var item = await Create(owner, kind, "Original");
                var path = $"/api/catalogs/{kind}/{item.Code}";
                (await other.PutAsJsonAsync(path, new CatalogPayload("Cross-house", true, 0, null))).StatusCode.Should().Be(HttpStatusCode.NotFound);
                (await reader.PostAsJsonAsync($"/api/catalogs/{kind}", new CatalogPayload("Denied", true, 0, null))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
                (await reader.GetAsync($"/api/catalogs/{kind}")).StatusCode.Should().Be(HttpStatusCode.OK);
                (await owner.PutAsJsonAsync(path, new CatalogPayload("Novo nome", false, 0, null))).StatusCode.Should().Be(HttpStatusCode.OK);
                (await owner.PutAsJsonAsync(path, new CatalogPayload("Versão antiga", true, 0, null))).StatusCode.Should().Be(HttpStatusCode.Conflict);
                var saved = (await owner.GetFromJsonAsync<SuccessDetails<CatalogItemDto[]>>($"/api/catalogs/{kind}"))!.Data.Single(x => x.Code == item.Code);
                saved.Name.Should().Be("Novo nome"); saved.IsActive.Should().BeFalse(); saved.Version.Should().Be(1);
                await using var database = new MySqlConnection(connection);
                var names = await database.QueryAsync<string>("SELECT Name FROM CatalogRevision WHERE ItemCode=@Code ORDER BY Version", new { item.Code });
                names.Should().Equal("Original", "Novo nome");
            }
            (await owner.PutAsJsonAsync($"/api/catalogs/categories/{child.Code}", new CatalogPayload(child.Name, false, 0, parent.Code)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await owner.PutAsJsonAsync($"/api/catalogs/categories/{parent.Code}", new CatalogPayload(parent.Name, false, 0, null)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }
        private static async Task<CatalogItemDto> Create(HttpClient client, string kind, string name, Guid? parent = null)
        {
            var response = await client.PostAsJsonAsync($"/api/catalogs/{kind}", new CatalogPayload(name, true, 0, parent));
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<CatalogItemDto>>())!.Data;
        }
    }
}
