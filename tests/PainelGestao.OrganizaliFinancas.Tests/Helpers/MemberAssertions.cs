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
using PainelGestao.OrganizaliFinancas.Application.Members;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;

namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class MemberAssertions
    {
        private const string Password = "Synthetic-member-pass-123!";
        public static async Task VerifyAsync(ServiceProvider provider, string connection)
        {
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var home = await mediator.Send(new ProvisionHouseholdCommand(Guid.NewGuid(), "Members", "Owner", "members@example.test", Password, "test"));
            var other = await mediator.Send(new ProvisionHouseholdCommand(Guid.NewGuid(), "Other members", "Other", "other-members@example.test", Password, "test"));
            await mediator.Send(new ChangeSubscriptionStatusCommand(home.HouseholdCode, SubscriptionStatus.Active, "Test", "test"));
            var work = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await work.PilotRepository.InitializeAccessAsync(home.HouseholdCode, "test", default);
            await work.PilotRepository.InitializeAccessAsync(home.HouseholdCode, "test", default);

            await using var factory = new Factory(connection);
            using var owner = factory.CreateClient();
            await Login(owner, "members@example.test");
            var session = (await owner.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me"))!.Data;
            session.Roles.Should().Equal("owner");
            session.Menu.Should().Contain(x => x.Key == "members.manage");
            (await owner.PutAsJsonAsync($"/api/members/{home.UserCode}", new UpdateMemberPayload("reader", false))).StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await owner.PutAsJsonAsync($"/api/members/{other.UserCode}", new UpdateMemberPayload("reader", true))).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await owner.PostAsJsonAsync("/api/members/invitations", new InviteMemberPayload("Invalid", "invalid@example.test", "platform-admin")))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var editorInvite = await Invite(owner, "Editor", "editor@example.test", "editor");
            using var editor = factory.CreateClient();
            (await editor.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(editorInvite.Token, Password)))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await editor.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(editorInvite.Token, Password)))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await Login(editor, "editor@example.test");
            var editorSession = (await editor.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me"))!.Data;
            editorSession.Permissions.Should().Contain("payments.settle").And.NotContain("payments.reverse");
            editorSession.Menu.Should().NotContain(x => x.Key == "members.manage");
            (await editor.GetAsync("/api/members")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await editor.PostAsJsonAsync("/api/members/invitations", new InviteMemberPayload("Denied", "denied@example.test", "owner")))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await owner.PutAsJsonAsync($"/api/members/{editorSession.UserCode}", new UpdateMemberPayload("reader", false)))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await editor.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await Login(editor, "editor@example.test");
            var reader = (await editor.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me"))!.Data;
            reader.Permissions.Should().Equal("household.read");
            (await owner.PutAsJsonAsync($"/api/members/{reader.UserCode}", new UpdateMemberPayload("reader", true)))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await editor.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var revoked = await Invite(owner, "Revoked", "revoked@example.test", "reader");
            (await owner.DeleteAsync($"/api/members/invitations/{revoked.Code}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await editor.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(revoked.Token, Password)))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var pending = (await owner.GetFromJsonAsync<SuccessDetails<MembersDto>>("/api/members"))!.Data;
            pending.Invitations.Should().BeEmpty();
            pending.Members.Should().HaveCount(2);
            await VerifyConcurrentAcceptanceAndLastOwner(connection, owner, home.UserCode);
        }

        private static async Task VerifyConcurrentAcceptanceAndLastOwner(string connection, HttpClient owner, Guid ownerCode)
        {
            var invite = await Invite(owner, "Second owner", "second-owner@example.test", "owner");
            await using var factory = new Factory(connection);
            using var first = factory.CreateClient();
            using var second = factory.CreateClient();
            var accepts = await Task.WhenAll(first.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(invite.Token, Password)),
                second.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationPayload(invite.Token, Password)));
            accepts.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.NoContent, HttpStatusCode.Unauthorized]);
            await Login(second, "second-owner@example.test");
            var secondSession = (await second.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me"))!.Data;
            var changes = await Task.WhenAll(owner.PutAsJsonAsync($"/api/members/{ownerCode}", new UpdateMemberPayload("reader", false)),
                second.PutAsJsonAsync($"/api/members/{secondSession.UserCode}", new UpdateMemberPayload("reader", false)));
            changes.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.NoContent, HttpStatusCode.Conflict]);
            await using var database = new MySqlConnection(connection);
            var links = await database.QuerySingleAsync<int>("SELECT COUNT(*) FROM UserRole ur JOIN User u ON u.Id=ur.IdUser WHERE u.Email='second-owner@example.test' AND ur.IsActive=1");
            links.Should().Be(1);
        }
        private static async Task<InvitationDto> Invite(HttpClient client, string name, string email, string role)
        {
            var response = await client.PostAsJsonAsync("/api/members/invitations", new InviteMemberPayload(name, email, role));
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content.ReadFromJsonAsync<SuccessDetails<InvitationDto>>())!.Data;
        }
        private static async Task Login(HttpClient client, string email)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var data = (await response.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.Tokens.AccessToken);
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
