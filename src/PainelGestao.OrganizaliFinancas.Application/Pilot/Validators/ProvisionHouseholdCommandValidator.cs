using FluentValidation;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Validators
{
    public class ProvisionHouseholdCommandValidator : AbstractValidator<ProvisionHouseholdCommand>
    {
        public ProvisionHouseholdCommandValidator()
        {
            RuleFor(x => x.Key).NotEmpty();
            RuleFor(x => x.HouseholdName).NotEmpty().MaximumLength(160);
            RuleFor(x => x.OwnerName).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
            RuleFor(x => x.Actor).NotEmpty().MaximumLength(256);
        }
    }
}
