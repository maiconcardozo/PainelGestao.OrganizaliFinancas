using FluentValidation;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Validators
{
    public class ChangeSubscriptionStatusCommandValidator : AbstractValidator<ChangeSubscriptionStatusCommand>
    {
        public ChangeSubscriptionStatusCommandValidator()
        {
            RuleFor(x => x.HouseholdCode).NotEmpty();
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Actor).NotEmpty().MaximumLength(256);
        }
    }
}
