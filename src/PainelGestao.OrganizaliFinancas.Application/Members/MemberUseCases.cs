using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;

namespace PainelGestao.OrganizaliFinancas.Application.Members
{
    public sealed record MembersDto(IReadOnlyList<MemberRecord> Members, IReadOnlyList<InvitationRecord> Invitations);
    public sealed record InviteMemberPayload(string Name, string Email, string RoleKey);
    public sealed record UpdateMemberPayload(string RoleKey, bool IsBlocked);
    public sealed record AcceptInvitationPayload(string Token, string Password);
    public sealed record InvitationDto(Guid Code, string Token, DateTime ExpiresAt);
    public sealed record ListMembersQuery(SessionIdentity Identity) : IRequest<MembersDto>;
    public sealed record InviteMemberCommand(SessionIdentity Identity, InviteMemberPayload Payload) : IRequest<InvitationDto>;
    public sealed record UpdateMemberCommand(SessionIdentity Identity, Guid Code, UpdateMemberPayload Payload) : IRequest;
    public sealed record RevokeInvitationCommand(SessionIdentity Identity, Guid Code) : IRequest;
    public sealed record AcceptInvitationCommand(AcceptInvitationPayload Payload) : IRequest;
    /// <summary>Owner-issued password reset link for a member, for households without an e-mail provider.</summary>
    public sealed record CreateRecoveryLinkCommand(SessionIdentity Identity, Guid Code) : IRequest<InvitationDto>;

    public class InviteMemberValidator : AbstractValidator<InviteMemberCommand>
    {
        public InviteMemberValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Email).NotEmpty().EmailAddress().MaximumLength(180);
            RuleFor(x => x.Payload.RoleKey).Must(key => key is "owner" or "editor" or "reader").WithMessage("Perfil inválido.");
        }
    }
    public class UpdateMemberValidator : AbstractValidator<UpdateMemberCommand>
    {
        public UpdateMemberValidator() => RuleFor(x => x.Payload.RoleKey)
            .Must(key => key is "owner" or "editor" or "reader").WithMessage("Perfil inválido.");
    }
    public class AcceptInvitationValidator : AbstractValidator<AcceptInvitationCommand>
    {
        public AcceptInvitationValidator()
        {
            RuleFor(x => x.Payload.Token).NotEmpty().MaximumLength(256);
            RuleFor(x => x.Payload.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
        }
    }
    public class ListMembersHandler(IUnitOfWork work) : IRequestHandler<ListMembersQuery, MembersDto>
    {
        public async Task<MembersDto> Handle(ListMembersQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "members.manage", ct);
            return new(await work.MemberRepository.ListAsync(request.Identity.IdTenant, ct),
                await work.MemberRepository.ListInvitationsAsync(request.Identity.IdTenant, ct));
        }
    }
    public class InviteMemberHandler(IUnitOfWork work, ITokenService tokens) : IRequestHandler<InviteMemberCommand, InvitationDto>
    {
        public Task<InvitationDto> Handle(InviteMemberCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var actor = await HouseholdAccess.RequireAsync(work, request.Identity, "members.manage", ct);
            var role = await work.MemberRepository.FindRoleAsync(actor.IdTenant, request.Payload.RoleKey, ct)
                ?? throw new NotFoundException("Perfil não encontrado.");
            var email = request.Payload.Email.Trim().ToLowerInvariant();
            if (await work.MemberRepository.EmailExistsAsync(email, ct)) throw new ConflictException("Não é possível convidar este e-mail.");
            var (value, hash, _) = tokens.CreateRefreshToken();
            var code = Guid.CreateVersion7();
            var expires = DateTime.UtcNow.AddHours(48);
            work.MemberRepository.AddInvitation(new(code, actor.IdTenant, role.Id, request.Payload.Name.Trim(), email, hash, expires, actor.Code.ToString()));
            return new InvitationDto(code, value, expires);
        }, ct);
    }
    public class UpdateMemberHandler(IUnitOfWork work) : IRequestHandler<UpdateMemberCommand>
    {
        public Task Handle(UpdateMemberCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var actor = await HouseholdAccess.RequireAsync(work, request.Identity, "members.manage", ct);
            var member = await work.MemberRepository.FindUserAsync(actor.IdTenant, request.Code, ct)
                ?? throw new NotFoundException("Integrante não encontrado.");
            var role = await work.MemberRepository.FindRoleAsync(actor.IdTenant, request.Payload.RoleKey, ct)
                ?? throw new NotFoundException("Perfil não encontrado.");
            var previous = await work.MemberRepository.GetRoleKeysAsync(actor.IdTenant, member.Id, ct);
            if (previous.Contains("owner") && (role.Key != "owner" || request.Payload.IsBlocked)
                && await work.MemberRepository.CountOwnersAsync(actor.IdTenant, ct) <= 1)
                throw new ConflictException("A casa precisa manter pelo menos um titular ativo.");
            work.MemberRepository.UpdateMember(member, role, request.Payload.IsBlocked, actor.Code.ToString());
            return true;
        }, ct);
    }
    public class RevokeInvitationHandler(IUnitOfWork work) : IRequestHandler<RevokeInvitationCommand>
    {
        public Task Handle(RevokeInvitationCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var actor = await HouseholdAccess.RequireAsync(work, request.Identity, "members.manage", ct);
            work.MemberRepository.RevokeInvitation(actor.IdTenant, request.Code, actor.Code.ToString());
            return true;
        }, ct);
    }
    public class CreateRecoveryLinkHandler(IUnitOfWork work, ITokenService tokens) : IRequestHandler<CreateRecoveryLinkCommand, InvitationDto>
    {
        public Task<InvitationDto> Handle(CreateRecoveryLinkCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var actor = await HouseholdAccess.RequireAsync(work, request.Identity, "members.manage", ct);
            var member = await work.MemberRepository.FindUserAsync(actor.IdTenant, request.Code, ct)
                ?? throw new NotFoundException("Integrante não encontrado.");
            if (member.IsBlocked) throw new ConflictException("Desbloqueie o acesso antes de gerar o link.");
            var (value, hash, _) = tokens.CreateRefreshToken();
            // Handed over in person by the owner, so it lasts longer than the e-mailed link; still one use only.
            var expires = DateTime.UtcNow.AddHours(24);
            work.AuthenticationRepository.AddRecovery(member, hash, expires);
            return new InvitationDto(member.Code, value, expires);
        }, ct);
    }
    public class AcceptInvitationHandler(IUnitOfWork work, ITokenService tokens, IPasswordHasher hasher) : IRequestHandler<AcceptInvitationCommand>
    {
        public async Task Handle(AcceptInvitationCommand request, CancellationToken ct)
        {
            var hash = tokens.ComputeHash(request.Payload.Token);
            var invitation = await work.MemberRepository.FindInvitationAsync(hash, ct)
                ?? throw new UnauthenticatedException("Convite inválido ou expirado.");
            await work.ExecuteInTransactionAsync(async () =>
            {
            await work.MemberRepository.LockHouseholdAsync(invitation.IdTenant, ct);
            // Re-read after the household lock: concurrent acceptance/revocation cannot reuse the token.
            invitation = await work.MemberRepository.FindInvitationAsync(hash, ct)
                ?? throw new UnauthenticatedException("Convite inválido ou expirado.");
            if (await work.MemberRepository.EmailExistsAsync(invitation.Email, ct)) throw new ConflictException("Este convite não pode mais ser utilizado.");
            work.MemberRepository.AcceptInvitation(invitation, Guid.CreateVersion7(), hasher.Hash(request.Payload.Password));
            return true;
            }, ct);
        }
    }
}
