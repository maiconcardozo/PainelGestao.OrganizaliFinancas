using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Households
{
    public sealed record PersonDto(Guid Code, string Name, bool IsActive, int Version);
    public sealed record PersonPayload(string Name, bool IsActive, int Version);
    public sealed record SettingsDto(string TimeZone, string Language, string Currency, DateOnly? StartDate, int Version);
    public sealed record SettingsPayload(string TimeZone, DateOnly? StartDate, int Version);
    public sealed record ListPeopleQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<PersonDto>>;
    public sealed record SavePersonCommand(SessionIdentity Identity, Guid? Code, PersonPayload Payload) : IRequest<PersonDto>;
    public sealed record GetSettingsQuery(SessionIdentity Identity) : IRequest<SettingsDto>;
    public sealed record SaveSettingsCommand(SessionIdentity Identity, SettingsPayload Payload) : IRequest<SettingsDto>;

    public class SavePersonValidator : AbstractValidator<SavePersonCommand>
    {
        public SavePersonValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => !x.Code.HasValue).WithMessage("Uma nova pessoa deve estar ativa.");
        }
    }
    public class SaveSettingsValidator : AbstractValidator<SaveSettingsCommand>
    {
        public SaveSettingsValidator()
        {
            RuleFor(x => x.Payload.TimeZone).NotEmpty().Must(IsSupported).WithMessage("Informe um fuso IANA válido.");
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.StartDate).Must(date => date is null || date.Value.Year >= 1000).WithMessage("Data inválida.");
        }
        private static bool IsSupported(string zone) => !string.IsNullOrWhiteSpace(zone) && zone.Length <= 100 &&
            TimeZoneInfo.TryConvertIanaIdToWindowsId(zone, out _) && TimeZoneInfo.TryFindSystemTimeZoneById(zone, out _);
    }
    public class ListPeopleHandler(IUnitOfWork work) : IRequestHandler<ListPeopleQuery, IReadOnlyList<PersonDto>>
    {
        public async Task<IReadOnlyList<PersonDto>> Handle(ListPeopleQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.HouseholdRepository.ListPeopleAsync(request.Identity.IdTenant, ct))
                .Select(person => new PersonDto(person.Code, person.Name, person.IsActive, person.Version)).ToArray();
        }
    }
    public class SavePersonHandler(IUnitOfWork work) : IRequestHandler<SavePersonCommand, PersonDto>
    {
        public Task<PersonDto> Handle(SavePersonCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "household.manage", ct);
            Person person;
            if (request.Code is Guid code)
            {
                person = await work.HouseholdRepository.FindPersonAsync(user.IdTenant, code, ct) ?? throw new NotFoundException("Pessoa não encontrada.");
                if (person.Version != request.Payload.Version) throw new ConflictException("Cadastro alterado. Atualize a página antes de salvar.");
                person.Update(request.Payload.Name, request.Payload.IsActive, user.Code.ToString());
                work.HouseholdRepository.UpdatePerson(person);
            }
            else
            {
                person = Person.Create(user.IdTenant, request.Payload.Name, user.Code.ToString());
                work.HouseholdRepository.AddPerson(person);
            }
            return new PersonDto(person.Code, person.Name, person.IsActive, person.Version);
        }, ct);
    }
    public class GetSettingsHandler(IUnitOfWork work) : IRequestHandler<GetSettingsQuery, SettingsDto>
    {
        public async Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct)
        {
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var settings = await work.HouseholdRepository.GetSettingsAsync(user.IdTenant, ct) ?? HouseholdSettings.Create(user.IdTenant, user.Code.ToString());
            return Map(settings);
        }
        internal static SettingsDto Map(HouseholdSettings settings) => new(settings.TimeZone, settings.Language, settings.Currency,
            settings.StartDate is DateTime date ? DateOnly.FromDateTime(date) : null, settings.Version);
    }
    public class SaveSettingsHandler(IUnitOfWork work) : IRequestHandler<SaveSettingsCommand, SettingsDto>
    {
        public Task<SettingsDto> Handle(SaveSettingsCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "household.manage", ct);
            var stored = await work.HouseholdRepository.GetSettingsAsync(user.IdTenant, ct);
            var settings = stored ?? HouseholdSettings.Create(user.IdTenant, user.Code.ToString());
            if (settings.Version != request.Payload.Version) throw new ConflictException("Configuração alterada. Atualize a página antes de salvar.");
            settings.Update(request.Payload.TimeZone, request.Payload.StartDate, user.Code.ToString());
            work.HouseholdRepository.SaveSettings(settings, stored is not null);
            return GetSettingsHandler.Map(settings);
        }, ct);
    }
}
