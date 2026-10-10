using Microsoft.AspNetCore.Mvc;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Api.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
            try { await next(context); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Abort();
            }
            catch (Exception exception) when (!context.Response.HasStarted)
            {
                var status = exception switch
                {
                    InputValidationException => 400,
                    BadHttpRequestException request => request.StatusCode,
                    _ => 500
                };
                if (status == 500)
                    logger.LogError("Falha {ExceptionType}. Correlação {TraceId}.",
                        exception.GetType().Name, context.TraceIdentifier);
                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = status == 500 ? "Falha inesperada" : "Dados inválidos",
                    Detail = status == 500 ? "Falha inesperada. Tente novamente." : "Os dados informados são inválidos.",
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;
                if (exception is InputValidationException validation)
                    problem.Extensions["errors"] = validation.Errors;
                context.Response.Clear();
                context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(problem, options: null,
                    contentType: "application/problem+json", cancellationToken: context.RequestAborted);
            }
        }
    }
}
