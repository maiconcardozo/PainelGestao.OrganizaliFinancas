using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Services;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Tests.Integration
{
    public class ApiFoundationTests
    {
        [Theory]
        [InlineData(true, HttpStatusCode.OK)]
        [InlineData(false, HttpStatusCode.ServiceUnavailable)]
        public async Task Readiness_UsesMediatorAndReflectsDependencyState(bool ready, HttpStatusCode expected)
        {
            await using var factory = new Factory(new ReadinessStub(_ => Task.FromResult(ready)));
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/health/ready");
            response.StatusCode.Should().Be(expected);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("status").GetInt32().Should().Be((int)expected);
            response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        }

        [Fact]
        public async Task UnhandledFailure_DoesNotLeakInternalDetails()
        {
            await using var factory = new Factory(new ReadinessStub(_ => throw new InvalidOperationException("secret-connection")));
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/health/ready");
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            var body = await response.Content.ReadAsStringAsync();
            body.Should().NotContain("secret-connection").And.Contain("traceId");
        }

        [Fact]
        public async Task ValidationFailure_ReturnsFieldErrors()
        {
            await using var factory = new Factory(new ReadinessStub(_ =>
                throw new InputValidationException(new Dictionary<string, string[]> { ["name"] = ["Informe o nome."] })));
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/health/ready");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            json.RootElement.GetProperty("errors").GetProperty("name")[0].GetString().Should().Be("Informe o nome.");
        }

        [Fact]
        public async Task Liveness_DoesNotCallPersistence()
        {
            await using var factory = new Factory(new ReadinessStub(_ => throw new InvalidOperationException()));
            using var client = factory.CreateClient();
            (await client.GetAsync("/health/live")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Swagger_ServesRealOperationsAndLocalAssets()
        {
            await using var factory = new Factory(new ReadinessStub(_ => Task.FromResult(false)));
            using var client = factory.CreateClient();
            var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
            var paths = json.RootElement.GetProperty("paths");
            paths.EnumerateObject().Select(path => path.Name).Should().Contain(["/health/live", "/health/ready", "/api/auth/login", "/api/auth/me"]);
            paths.GetProperty("/health/ready").GetProperty("get").GetProperty("responses")
                .TryGetProperty("503", out _).Should().BeTrue();
            foreach (var path in new[] { "/swagger/index.html", "/swagger-ui/organizali-docs.css",
                "/swagger-ui/organizali-docs.js", "/swagger-ui/organizali-logo.svg" })
                (await client.GetAsync(path)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private sealed class ReadinessStub(Func<CancellationToken, Task<bool>> read) : IDatabaseReadiness
        {
            public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => read(cancellationToken);
        }

        private sealed class Factory(IDatabaseReadiness readiness) : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Jwt:Key", "Synthetic-api-foundation-signing-key-for-tests-only");
                builder.UseSetting("OrganizaliFinancas:ConnectionString", "server=127.0.0.1;port=1;database=unused;user=unused");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDatabaseReadiness>();
                    services.AddSingleton(readiness);
                });
            }
        }
    }
}
