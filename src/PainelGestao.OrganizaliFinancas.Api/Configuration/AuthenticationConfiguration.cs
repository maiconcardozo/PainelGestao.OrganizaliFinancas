using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Shared.Constants;
using PainelGestao.OrganizaliFinancas.Shared.Settings;

namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Configure Jwt.");
            if (Encoding.UTF8.GetByteCount(jwt.Key) < 32 || string.IsNullOrWhiteSpace(jwt.Issuer) ||
                string.IsNullOrWhiteSpace(jwt.Audience) || jwt.AccessTokenMinutes is < 1 or > 60 ||
                jwt.RefreshTokenDays is < 1 or > 30)
                throw new InvalidOperationException("Configuração JWT inválida.");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer=true, ValidateAudience=true, ValidateLifetime=true, ValidateIssuerSigningKey=true,
                    ValidIssuer=jwt.Issuer, ValidAudience=jwt.Audience,
                    IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew=TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal!;
                        if (!Guid.TryParse(principal.FindFirst(TokenClaims.UserCode)?.Value, out var userCode) ||
                            !int.TryParse(principal.FindFirst(TokenClaims.IdTenant)?.Value, out var idTenant) ||
                            !int.TryParse(principal.FindFirst(TokenClaims.SessionVersion)?.Value, out var version))
                        { context.Fail("Sessão inválida."); return; }
                        var work = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
                        if (await work.AuthenticationRepository.FindAuthorizedAsync(
                            userCode, idTenant, version, context.HttpContext.RequestAborted) is null)
                            context.Fail("Sessão inválida.");
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await Results.Problem(statusCode:401, title:"Autenticação necessária", detail:"Entre novamente.",
                            instance:context.Request.Path).ExecuteAsync(context.HttpContext);
                    },
                    OnForbidden = context => Results.Problem(statusCode:403, title:"Acesso negado",
                        detail:"Você não tem permissão para esta operação.", instance:context.Request.Path).ExecuteAsync(context.HttpContext)
                };
            });
            services.AddAuthorization();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit=10, Window=TimeSpan.FromMinutes(1), QueueLimit=0 }));
                options.OnRejected = (context, _) => new ValueTask(Results.Problem(statusCode:429,
                    title:"Muitas tentativas",detail:"Aguarde um minuto antes de tentar novamente.").ExecuteAsync(context.HttpContext));
            });
            return services;
        }
    }
}
