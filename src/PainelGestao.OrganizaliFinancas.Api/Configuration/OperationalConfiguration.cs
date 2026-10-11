using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using PainelGestao.OrganizaliFinancas.Application.Planning;
using PainelGestao.OrganizaliFinancas.Shared.Settings;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    /// <summary>Production concerns of the host: trusted proxies, per-session limits, structured logs, headers and the daily routine.</summary>
    public static class OperationalConfiguration
    {
        public static WebApplicationBuilder AddOperations(this WebApplicationBuilder builder)
        {
            var jobs = builder.Configuration.GetSection(JobSettings.SectionName).Get<JobSettings>() ?? new JobSettings();
            if (!builder.Environment.IsDevelopment())
            {
                builder.Logging.ClearProviders();
                builder.Logging.AddJsonConsole(options => { options.IncludeScopes = false; options.UseUtcTimestamp = true; });
            }
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                // Only the configured reverse proxies (the web container network) may state the client address.
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
                foreach (var network in jobs.TrustedProxyNetworks) options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
                options.ForwardLimit = 2;
            });
            builder.Services.AddRateLimiter(options => options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                context.Request.Path.StartsWithSegments("/api")
                    ? RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context),
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = jobs.ApiRequestsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })
                    : RateLimitPartition.GetNoLimiter(string.Empty)));
            if (jobs.HorizonEnabled && string.IsNullOrEmpty(builder.Configuration["Operation"])) builder.Services.AddHostedService<HorizonWorker>();
            return builder;
        }

        /// <summary>One bucket per session (hash of the bearer token, never the token itself) or per client address.</summary>
        private static string PartitionKey(HttpContext context)
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            return authorization.StartsWith("Bearer ", StringComparison.Ordinal)
                ? "s:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(authorization)))[..32]
                : "a:" + (context.Connection.RemoteIpAddress ?? IPAddress.None);
        }

        /// <summary>Financial responses must not be cached or framed; logs carry method, route, status and time, never bodies or query strings.</summary>
        public static WebApplication UseOperations(this WebApplication app)
        {
            app.UseForwardedHeaders();
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Organizali.Http");
            app.Use(async (context, next) =>
            {
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers.XContentTypeOptions = "nosniff";
                    headers.XFrameOptions = "DENY";
                    headers["Referrer-Policy"] = "no-referrer";
                    if (context.Request.Path.StartsWithSegments("/api")) headers.CacheControl = "no-store";
                    return Task.CompletedTask;
                });
                await next(context);
                if (!context.Request.Path.StartsWithSegments("/health"))
                    logger.LogInformation("HTTP {Method} {Path} {Status} {ElapsedMs}ms {TraceId}", context.Request.Method, context.Request.Path.Value,
                        context.Response.StatusCode, (int)System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.TraceIdentifier);
            });
            return app;
        }
    }

    /// <summary>Runs the horizon roll at startup and then every configured interval; failures are logged and retried next cycle.</summary>
    public sealed class HorizonWorker(IServiceScopeFactory scopes, JobSettings settings, ILogger<HorizonWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Clamp(settings.HorizonIntervalHours, 1, 168)));
            do
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new RollHorizonCommand("system:horizon"), stoppingToken);
                    logger.LogInformation("Horizonte {Status}: {Households} casas, {Inserted} criadas, {Updated} alteradas, {Cancelled} canceladas.",
                        result.Skipped ? "em execução em outra instância" : "atualizado", result.Households, result.Inserted, result.Updated, result.Cancelled);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError("Falha na rotina de horizonte: {ExceptionType}.", exception.GetType().Name);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
