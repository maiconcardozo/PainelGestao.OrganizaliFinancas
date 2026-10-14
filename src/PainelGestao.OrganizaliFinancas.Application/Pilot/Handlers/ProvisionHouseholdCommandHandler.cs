using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Pilot.Commands;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Security;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Pilot.Handlers
{
    public class ProvisionHouseholdCommandHandler(IUnitOfWork work, IPasswordHasher hasher)
        : IRequestHandler<ProvisionHouseholdCommand, PilotRecord>
    {
        public async Task<PilotRecord> Handle(ProvisionHouseholdCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var existing = await work.PilotRepository.FindAsync(request.Key, cancellationToken);
            if (existing is not null) return VerifyExisting(existing, request, email);
            var pilot = new NewPilot(request.Key, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
                request.HouseholdName.Trim(), request.OwnerName.Trim(), email, hasher.Hash(request.Password), request.Actor);
            var saved = await work.PilotRepository.ProvisionAsync(pilot, cancellationToken);
            // A new household starts with the standard catalog; repeating the provisioning never re-seeds (the key is found above).
            if (await work.PilotRepository.FindTenantIdAsync(saved.HouseholdCode, cancellationToken) is int idTenant)
                await Catalogs.DefaultCatalogSeeder.SeedAsync(work, idTenant, saved.UserCode.ToString(), cancellationToken);
            return VerifyExisting(saved, request, email);
        }

        private static PilotRecord VerifyExisting(PilotRecord existing, ProvisionHouseholdCommand request, string email)
        {
            if (existing.Email != email || existing.HouseholdName != request.HouseholdName.Trim() ||
                existing.OwnerName != request.OwnerName.Trim())
                throw new ConflictException("A chave de provisionamento já foi usada com outros dados.");
            // Repeating provisioning never changes the existing password.
            return existing;
        }
    }
}
