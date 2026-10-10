using MediatR;
using PainelGestao.OrganizaliFinancas.Api.Contracts;
using PainelGestao.OrganizaliFinancas.Application.Health.Queries;
namespace PainelGestao.OrganizaliFinancas.Api.Configuration
{
    public static class HealthEndpoints
    {
        public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/health").WithTags("Disponibilidade");
            group.MapGet("/live", (HttpContext context) => Results.Ok(
                new SuccessDetails<string>(200, "Disponível", "A API está em execução.", context.Request.Path, "Healthy")))
                .WithName("Liveness").WithSummary("Verifica se a API está em execução.")
                .WithDescription("Sem parâmetros. Não acessa banco nem comprova disponibilidade das dependências.")
                .Produces<SuccessDetails<string>>();
            group.MapGet("/ready", async (IMediator mediator, HttpContext context, CancellationToken cancellationToken) =>
            {
                if (await mediator.Send(new GetReadinessQuery(), cancellationToken))
                    return Results.Ok(new SuccessDetails<string>(200, "Disponível",
                        "Conexão e tabela base disponíveis.", context.Request.Path, "Healthy"));
                return Results.Problem(statusCode: 503, title: "Indisponível",
                    detail: "A dependência de persistência ainda não está disponível.",
                    instance: context.Request.Path,
                    extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier });
            })
                .WithName("Readiness").WithSummary("Verifica a conexão e a tabela base.")
                .WithDescription("""
                    Consulta somente de leitura. Não aplica migrations. Sem parâmetros de entrada.

                    | Propriedade | Tipo | Descrição e condições do contrato |
                    |---|---|---|
                    | status | integer | 200 em sucesso; 503 se a persistência estiver indisponível. |
                    | detail | string | Mensagem em português, sem dados de conexão. |
                    | data | string | Healthy somente no envelope de sucesso. |
                    | traceId | string | Correlação no erro de indisponibilidade. |
                    """)
                .Produces<SuccessDetails<string>>().ProducesProblem(503);
            return endpoints;
        }
    }
}
