using FluentAssertions;
using FluentValidation;
using PainelGestao.OrganizaliFinancas.Application.Behaviors;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Tests.Unit
{
    public class ValidationBehaviorTests
    {
        [Fact]
        public async Task InvalidRequest_DoesNotInvokeHandler()
        {
            var validator = new InlineValidator<TestRequest>();
            validator.RuleFor(request => request.Name).NotEmpty().WithMessage("Informe o nome.");
            var behavior = new ValidationBehavior<TestRequest, string>([validator]);
            var handlerCalled = false;

            var action = () => behavior.Handle(new TestRequest(""), _ =>
            {
                handlerCalled = true;
                return Task.FromResult("unexpected");
            }, CancellationToken.None);

            var exception = await action.Should().ThrowAsync<InputValidationException>();
            exception.Which.Errors["Name"].Should().Equal("Informe o nome.");
            handlerCalled.Should().BeFalse();
        }

        [Fact]
        public async Task ValidRequest_PreservesResponseAndCancellationToken()
        {
            var validator = new InlineValidator<TestRequest>();
            validator.RuleFor(request => request.Name).NotEmpty();
            var behavior = new ValidationBehavior<TestRequest, string>([validator]);
            using var cancellation = new CancellationTokenSource();
            var handlerToken = CancellationToken.None;

            var response = await behavior.Handle(new TestRequest("Casa"), token =>
            {
                handlerToken = token;
                return Task.FromResult("accepted");
            }, cancellation.Token);

            response.Should().Be("accepted");
            handlerToken.Should().Be(cancellation.Token);
        }

        [Fact]
        public async Task RequestWithoutValidators_InvokesHandler()
        {
            var behavior = new ValidationBehavior<TestRequest, string>([]);
            var response = await behavior.Handle(new TestRequest("Casa"),
                _ => Task.FromResult("accepted"), CancellationToken.None);

            response.Should().Be("accepted");
        }

        public sealed record TestRequest(string Name);
    }
}
