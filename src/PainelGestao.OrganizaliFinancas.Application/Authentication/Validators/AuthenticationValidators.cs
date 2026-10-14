using FluentValidation;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Commands;
namespace PainelGestao.OrganizaliFinancas.Application.Authentication.Validators
{
    public class AuthenticateUserValidator : AbstractValidator<AuthenticateUserCommand>
    {
        public AuthenticateUserValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180).WithMessage("Informe um e-mail válido.");
            RuleFor(x => x.Password).NotEmpty().MaximumLength(128).WithMessage("Informe a senha.");
        }
    }
    public class RenewSessionValidator : AbstractValidator<RenewSessionCommand>
    {
        public RenewSessionValidator() { RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256); }
    }
    public class RequestRecoveryValidator : AbstractValidator<RequestRecoveryCommand>
    {
        public RequestRecoveryValidator() { RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180); }
    }
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128).WithMessage("Informe a senha atual.");
            RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128).WithMessage("A nova senha precisa ter de 12 a 128 caracteres.");
            RuleFor(x => x.NewPassword).NotEqual(x => x.CurrentPassword).WithMessage("A nova senha precisa ser diferente da atual.");
        }
    }
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        }
    }
}
