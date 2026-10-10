using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dapper;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs;
using PainelGestao.OrganizaliFinancas.Application.Households;
using PainelGestao.OrganizaliFinancas.Application.Members;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class HouseholdAssertions
    {
        private const string Password = "Synthetic-household-pass-123!";
        public static async Task VerifyAsync(ServiceProvider provider, string connection)
        {
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            foreach (var email in new[] { "household@example.test", "other-household@example.test" })
            {
                var pilot = await mediator.Send(new ProvisionHouseholdCommand(Guid.NewGuid(), email, "Owner", email, Password, "test"));
                await mediator.Send(new ChangeSubscriptionStatusCommand(pilot.HouseholdCode, SubscriptionStatus.Active, "Test", "test"));
            }
            await using var factory = new Factory(connection);
            using var owner = factory.CreateClient();
            using var other = factory.CreateClient();
            await Login(owner, "household@example.test");
            await Login(other, "other-household@example.test");
            var created = await owner.PostAsJsonAsync("/api/people", new PersonPayload("Morador sem e-mail", true, 0));
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var person = (await created.Content.ReadFromJsonAsync<SuccessDetails<PersonDto>>())!.Data;
            (await other.PutAsJsonAsync($"/api/people/{person.Code}", new PersonPayload("Não alterar", true, 0))).StatusCode.Should().Be(HttpStatusCode.NotFound);
            var edits = await Task.WhenAll(
                owner.PutAsJsonAsync($"/api/people/{person.Code}", new PersonPayload("Nome atualizado", false, 0)),
                owner.PutAsJsonAsync($"/api/people/{person.Code}", new PersonPayload("Nome atualizado", false, 0)));
            edits.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
            var people = (await owner.GetFromJsonAsync<SuccessDetails<PersonDto[]>>("/api/people"))!.Data;
            people.Should().ContainSingle().Which.IsActive.Should().BeFalse();
            (await other.GetFromJsonAsync<SuccessDetails<PersonDto[]>>("/api/people"))!.Data.Should().BeEmpty();
            var settings = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            settings.TimeZone.Should().Be("America/Sao_Paulo");
            settings.Currency.Should().Be("BRL"); settings.Language.Should().Be("pt-BR");
            var startDate = new DateOnly(2026, 10, 10);
            (await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload("America/Manaus", startDate, 0)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload("America/Manaus", startDate, 0)))
                .StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await owner.PutAsJsonAsync("/api/household-settings", new SettingsPayload("Invalid/Zone", startDate, 1)))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var saved = (await owner.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data;
            saved.StartDate.Should().Be(startDate); saved.TimeZone.Should().Be("America/Manaus");
            (await other.GetFromJsonAsync<SuccessDetails<SettingsDto>>("/api/household-settings"))!.Data.TimeZone.Should().Be("America/Sao_Paulo");

            var invitation = await owner.PostAsJsonAsync("/api/members/invitations", new InviteMemberPayload("Reader", "household-reader@example.test", "reader"));
            var token = (await invitation.Content.ReadFromJsonAsync<SuccessDetails<InvitationDto>>())!.Data.Token;
            using var reader = factory.CreateClient();
            (await reader.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(token, Password))).StatusCode.Should().Be(HttpStatusCode.NoContent);
            await Login(reader, "household-reader@example.test");
            (await reader.GetAsync("/api/people")).StatusCode.Should().Be(HttpStatusCode.OK);
            (await reader.PostAsJsonAsync("/api/people", new PersonPayload("Denied", true, 0))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await reader.PutAsJsonAsync("/api/household-settings", new SettingsPayload("America/Sao_Paulo", null, 1))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            await using var database = new MySqlConnection(connection);
            var actor = await database.QuerySingleAsync<string>("SELECT CreatedBy FROM Person WHERE Code=@Code", new { person.Code });
            var session = (await owner.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me"))!.Data;
            actor.Should().Be(session.UserCode.ToString());
            (await database.QuerySingleAsync<int>("SELECT COUNT(*) FROM User WHERE Name='Morador sem e-mail'")).Should().Be(0);
            await CatalogAssertions.VerifyAsync(owner, other, reader, connection);
            await ExpenseTypeAssertions.VerifyAsync(owner, other, reader);
        }
        private static async Task Login(HttpClient client, string email)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tokens = (await response.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data.Tokens;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        }
        private sealed class Factory(string connection) : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("OrganizaliFinancas:ConnectionString", connection);
                builder.UseSetting("Jwt:Key", "Synthetic-integration-signing-key-at-least-32-chars");
            }
        }
    }
}
