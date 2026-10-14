using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Commands;
using PainelGestao.OrganizaliFinancas.Application.Authentication.DTOs;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Authentication.Handlers
{
    public class AuthenticateUserHandler(IUnitOfWork work, IPasswordHasher hasher, ITokenService tokens)
        : IRequestHandler<AuthenticateUserCommand, AuthenticationResultDto>
    {
        public async Task<AuthenticationResultDto> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await work.AuthenticationRepository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
            if (user is null || !hasher.Verify(user.PasswordHash, request.Password))
                throw new UnauthenticatedException("E-mail ou senha inválidos.");
            var result = Issue(user, work, tokens);
            await work.CommitAsync(cancellationToken);
            return result;
        }
        internal static AuthenticationResultDto Issue(User user, IUnitOfWork work, ITokenService tokens)
        {
            var access = tokens.CreateAccessToken(user);
            var (value, hash, expires) = tokens.CreateRefreshToken();
            work.AuthenticationRepository.AddRefresh(RefreshToken.Create(user, hash, expires));
            return new(new TokensDto(access.Value, access.ExpiresAt, value, expires));
        }
    }
    public class RenewSessionHandler(IUnitOfWork work, ITokenService tokens)
        : IRequestHandler<RenewSessionCommand, AuthenticationResultDto>
    {
        public Task<AuthenticationResultDto> Handle(RenewSessionCommand request, CancellationToken cancellationToken) =>
            work.ExecuteInTransactionAsync(async () =>
            {
                var hash = tokens.ComputeHash(request.RefreshToken);
                var user = await work.AuthenticationRepository.LockRefreshUserAsync(hash, cancellationToken)
                    ?? throw new UnauthenticatedException("Sessão expirada. Entre novamente.");
                work.AuthenticationRepository.RevokeRefresh(hash);
                return AuthenticateUserHandler.Issue(user, work, tokens);
            }, cancellationToken);
    }
    public class SignOutHandler(IUnitOfWork work) : IRequestHandler<SignOutCommand>
    {
        public async Task Handle(SignOutCommand request, CancellationToken cancellationToken)
        {
            var user = await work.AuthenticationRepository.FindAuthorizedAsync(request.UserCode, request.IdTenant, request.Version, cancellationToken);
            if (user is null) throw new UnauthenticatedException("Sessão expirada.");
            work.AuthenticationRepository.RevokeSessions(user.Id, user.IdTenant, user.Code.ToString());
            await work.CommitAsync(cancellationToken);
        }
    }
    public class GetSessionHandler(IUnitOfWork work) : IRequestHandler<GetSessionQuery, SessionDto>
    {
        public async Task<SessionDto> Handle(GetSessionQuery request, CancellationToken cancellationToken)
        {
            var user = await work.AuthenticationRepository.FindAuthorizedAsync(request.UserCode, request.IdTenant, request.Version, cancellationToken)
                ?? throw new UnauthenticatedException("Sessão expirada.");
            var roles = await work.MemberRepository.GetRoleKeysAsync(user.IdTenant, user.Id, cancellationToken);
            var permissions = Services.HouseholdAccess.Permissions(roles);
            var menu = new List<SessionMenuItemDto>
            {
                new("household", "Minha casa", "/household", "House"),
                new("my-account", "Minha conta", "/my-account", "UserRound")
            };
            if (permissions.Contains("members.manage")) menu.Add(new("members.manage", "Integrantes", "/members", "Users"));
            if (permissions.Contains("household.read"))
            {
                // The dashboard (D18) opens the financial area; registers come after the daily screens.
                menu.Insert(0, new("dashboard", "Painel", "/dashboard", "LayoutDashboard"));
                menu.Add(new("accounts", "Contas", "/accounts", "Landmark"));
                menu.Add(new("income", "Rendas", "/income", "TrendingUp"));
                menu.Add(new("expenses", "Despesas", "/expenses", "Receipt"));
                menu.Add(new("cards", "Cartões", "/cards", "CreditCard"));
                menu.Add(new("imports", "Importar extrato", "/imports", "Upload"));
                menu.Add(new("cash-flow", "Fluxo de caixa", "/cash-flow", "ChartLine"));
                menu.Add(new("budget", "Orçamento", "/budget", "PiggyBank"));
                menu.Add(new("goals", "Metas", "/goals", "Target"));
                menu.Add(new("analysis", "Análise", "/analysis", "ChartColumn"));
                menu.Add(new("alerts", "Alertas", "/alerts", "Bell"));
                menu.Add(new("people", "Moradores", "/people", "Users"));
                menu.Add(new("catalogs", "Cadastros financeiros", "/catalogs", "Tags"));
                menu.Add(new("expense-types", "Tipos de despesa", "/expense-types", "Tags"));
                menu.Add(new("household-settings", "Configurações da casa", "/household-settings", "Settings"));
            }
            menu.Add(new("help", "Ajuda", "/help", "CircleHelp"));
            return new(user.Code, user.Name, user.Email, roles, permissions, menu.ToArray());
        }
    }
    public class RequestRecoveryHandler(IUnitOfWork work, ITokenService tokens, IRecoveryDelivery delivery)
        : IRequestHandler<RequestRecoveryCommand>
    {
        public async Task Handle(RequestRecoveryCommand request, CancellationToken cancellationToken)
        {
            if (!delivery.IsConfigured) throw new ServiceUnavailableException("Recuperação ainda indisponível.");
            var user = await work.AuthenticationRepository.FindByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
            if (user is null) return;
            var (value, hash, _) = tokens.CreateRefreshToken();
            work.AuthenticationRepository.AddRecovery(user, hash, DateTime.UtcNow.AddMinutes(15));
            await work.CommitAsync(cancellationToken);
            await delivery.DeliverAsync(user.Email, value, cancellationToken);
        }
    }
    public class ChangePasswordHandler(IUnitOfWork work, IPasswordHasher hasher) : IRequestHandler<ChangePasswordCommand>
    {
        public Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken) =>
            work.ExecuteInTransactionAsync(async () =>
            {
                var user = await work.AuthenticationRepository.FindAuthorizedAsync(request.UserCode, request.IdTenant, request.Version, cancellationToken)
                    ?? throw new UnauthenticatedException("Sessão expirada. Entre novamente.");
                if (!hasher.Verify(user.PasswordHash, request.CurrentPassword))
                    throw new InputValidationException(new Dictionary<string, string[]> { ["CurrentPassword"] = ["A senha atual não confere."] });
                // The session version advances with the hash, so every open session (including this one) is closed.
                work.AuthenticationRepository.UpdatePassword(user, hasher.Hash(request.NewPassword));
                return true;
            }, cancellationToken);
    }
    public class ResetPasswordHandler(IUnitOfWork work, ITokenService tokens, IPasswordHasher hasher)
        : IRequestHandler<ResetPasswordCommand>
    {
        public Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
            work.ExecuteInTransactionAsync(async () =>
            {
                var hash = tokens.ComputeHash(request.Token);
                var user = await work.AuthenticationRepository.LockRecoveryUserAsync(hash, cancellationToken)
                    ?? throw new UnauthenticatedException("Link inválido ou expirado.");
                work.AuthenticationRepository.ResetPassword(user, hash, hasher.Hash(request.Password));
                return true;
            }, cancellationToken);
    }
}
