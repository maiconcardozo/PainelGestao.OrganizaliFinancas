using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PainelGestao.OrganizaliFinancas.Api.Middleware;

namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class ExceptionHandlingMiddlewareTests
    {
        [Fact]
        public async Task ClientCancellation_DoesNotProduceAnInternalServerError()
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
            using var stream = new MemoryStream();
            context.Response.Body = stream;
            var middleware = new ExceptionHandlingMiddleware(
                _ => throw new OperationCanceledException(cancellation.Token),
                NullLogger<ExceptionHandlingMiddleware>.Instance);
            await middleware.InvokeAsync(context);
            context.Response.StatusCode.Should().NotBe(500);
            stream.Length.Should().Be(0);
        }
    }
}
