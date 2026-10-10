using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Commands;
using PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Enums;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;

namespace PainelGestao.OrganizaliFinancas.Tests.Helpers
{
    internal static class AuthenticationAssertions
    {
        public static async Task VerifyAsync(ServiceProvider provider, string connection)
        {
            await using var scope = provider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var pilot = await mediator.Send(new ProvisionHouseholdCommand(Guid.NewGuid(), "Auth house", "Owner",
                "auth@example.test", "Synthetic-auth-pass-123!", "test"));
            var delivery = new CapturingDelivery();
            await using var factory = new Factory(connection, delivery);
            using var client = factory.CreateClient();
            var credentials = new { email = "auth@example.test", password = "Synthetic-auth-pass-123!" };
            (await client.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await mediator.Send(new ChangeSubscriptionStatusCommand(pilot.HouseholdCode, SubscriptionStatus.Active, "Teste", "test"));
            var login = await client.PostAsJsonAsync("/api/auth/login", credentials);
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = (await login.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Tokens.AccessToken);
            var me = await client.GetFromJsonAsync<SuccessDetails<SessionDto>>("/api/auth/me");
            me!.Data.UserCode.Should().Be(pilot.UserCode);
            await AssertInvalidTokensAsync(client, result.Tokens.AccessToken);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Tokens.AccessToken);
            var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = result.Tokens.RefreshToken });
            refresh.StatusCode.Should().Be(HttpStatusCode.OK);
            (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = result.Tokens.RefreshToken }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var refreshed = (await refresh.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data;
            (await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refreshed.Tokens.RefreshToken }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            (await client.PostAsJsonAsync("/api/auth/recovery", new { email = credentials.email }))
                .StatusCode.Should().Be(HttpStatusCode.Accepted);
            delivery.Token.Should().NotBeNullOrWhiteSpace();
            var reset = new { token = delivery.Token, password = "Changed-synthetic-pass-456!" };
            (await client.PostAsJsonAsync("/api/auth/reset-password", reset)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await client.PostAsJsonAsync("/api/auth/reset-password", reset)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await client.PostAsJsonAsync("/api/auth/recovery", new { email = "absent@example.test" }))
                .StatusCode.Should().Be(HttpStatusCode.Accepted);
            delivery.Count.Should().Be(1);

            await using var secondFactory = new Factory(connection, delivery);
            using var second = secondFactory.CreateClient();
            (await second.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var changed = await second.PostAsJsonAsync("/api/auth/login", new { credentials.email, password = reset.password });
            changed.StatusCode.Should().Be(HttpStatusCode.OK);
            var changedTokens = (await changed.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data.Tokens;
            second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", changedTokens.AccessToken);
            await mediator.Send(new ChangeSubscriptionStatusCommand(pilot.HouseholdCode, SubscriptionStatus.Suspended, "Teste", "test"));
            (await second.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await mediator.Send(new ChangeSubscriptionStatusCommand(pilot.HouseholdCode, SubscriptionStatus.Active, "Teste", "test"));
            (await second.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var active = await second.PostAsJsonAsync("/api/auth/login", new { credentials.email, password = reset.password });
            var activeTokens = (await active.Content.ReadFromJsonAsync<SuccessDetails<AuthenticationResultDto>>())!.Data.Tokens;
            await using var database = new MySqlConnection(connection);
            await database.ExecuteAsync("UPDATE User SET IsBlocked=1 WHERE Code=@Code", new { Code = pilot.UserCode });
            second.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", activeTokens.AccessToken);
            (await second.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await second.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = activeTokens.RefreshToken }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task AssertInvalidTokensAsync(HttpClient client, string original)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(original);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Synthetic-integration-signing-key-at-least-32-chars"));
            string Issue(IEnumerable<Claim> claims, DateTime expires) => handler.WriteToken(new JwtSecurityToken(
                token.Issuer, token.Audiences.Single(), claims, expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
            var otherHouse = token.Claims.Where(c => c.Type != "tenant_id").Append(new Claim("tenant_id", "2147483647"));
            foreach (var invalid in new[] { Issue(token.Claims, DateTime.UtcNow.AddMinutes(-2)), Issue(otherHouse, DateTime.UtcNow.AddMinutes(5)), "invalid.jwt.token" })
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalid);
                (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
        }

        private sealed class CapturingDelivery : IRecoveryDelivery
        {
            public bool IsConfigured => true;
            public string? Token { get; private set; }
            public int Count { get; private set; }
            public Task DeliverAsync(string email, string token, CancellationToken cancellationToken)
            { Token = token; Count++; return Task.CompletedTask; }
        }
        private sealed class Factory(string connection, IRecoveryDelivery delivery) : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("OrganizaliFinancas:ConnectionString", connection);
                builder.UseSetting("Jwt:Key", "Synthetic-integration-signing-key-at-least-32-chars");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRecoveryDelivery>();
                    services.AddSingleton(delivery);
                });
            }
        }
    }
}
