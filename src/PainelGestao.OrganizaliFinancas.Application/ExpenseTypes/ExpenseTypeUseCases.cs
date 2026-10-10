using System.Text.Json;
using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.ExpenseTypes
{
    public sealed record ExpenseTypePayload(string Name, bool IsActive, int Version, string Behavior, Guid? DefaultCategoryCode, CustomFieldDefinition[] Fields);
    public sealed record ExpenseTypeDto(Guid Code, string Name, bool IsActive, int Version, string Behavior, Guid? DefaultCategoryCode, CustomFieldDefinition[] Fields);
    public sealed record ListExpenseTypesQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<ExpenseTypeDto>>;
    public sealed record GetExpenseTypeVersionQuery(SessionIdentity Identity, Guid Code, int Version) : IRequest<ExpenseTypeDto>;
    public sealed record SaveExpenseTypeCommand(SessionIdentity Identity, Guid? Code, ExpenseTypePayload Payload) : IRequest<ExpenseTypeDto>;
    public sealed record ValidateFieldsPayload(int Version, Dictionary<string, JsonElement> Values);
    public sealed record ValidateFieldsCommand(SessionIdentity Identity, Guid Code, ValidateFieldsPayload Payload) : IRequest;
    public class SaveExpenseTypeValidator : AbstractValidator<SaveExpenseTypeCommand>
    {
        public SaveExpenseTypeValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null);
            RuleFor(x => x.Payload.Behavior).Must(value => value is "single" or "recurring" or "installments" or "card").WithMessage("Comportamento inválido.");
            RuleFor(x => x.Payload.Fields).NotNull();
        }
    }
    public class ValidateFieldsValidator : AbstractValidator<ValidateFieldsCommand>
    {
        public ValidateFieldsValidator()
        {
            RuleFor(x => x.Payload.Version).GreaterThan(0);
            RuleFor(x => x.Payload.Values).NotNull().Must(values => values is null || values.Count <= 30);
        }
    }
    public class ListExpenseTypesHandler(IUnitOfWork work) : IRequestHandler<ListExpenseTypesQuery, IReadOnlyList<ExpenseTypeDto>>
    {
        public async Task<IReadOnlyList<ExpenseTypeDto>> Handle(ListExpenseTypesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.ExpenseTypeRepository.ListAsync(request.Identity.IdTenant, ct)).Select(Map).ToArray();
        }
        internal static ExpenseTypeDto Map(ExpenseTypeRecord record) => new(record.Code, record.Name, record.IsActive, record.Version,
            record.Behavior, record.DefaultCategoryCode, JsonSerializer.Deserialize<CustomFieldDefinition[]>(record.FieldsJson)!);
    }
    public class GetExpenseTypeVersionHandler(IUnitOfWork work) : IRequestHandler<GetExpenseTypeVersionQuery, ExpenseTypeDto>
    {
        public async Task<ExpenseTypeDto> Handle(GetExpenseTypeVersionQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var record = await work.ExpenseTypeRepository.FindVersionAsync(request.Identity.IdTenant, request.Code, request.Version, ct)
                ?? throw new NotFoundException("Versão não encontrada nesta casa.");
            return ListExpenseTypesHandler.Map(record);
        }
    }
    public class SaveExpenseTypeHandler(IUnitOfWork work) : IRequestHandler<SaveExpenseTypeCommand, ExpenseTypeDto>
    {
        public Task<ExpenseTypeDto> Handle(SaveExpenseTypeCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            CustomFields.ValidateDefinitions(request.Payload.Fields);
            var type = request.Code is Guid code
                ? await work.ExpenseTypeRepository.FindAsync(user.IdTenant, code, ct) ?? throw new NotFoundException("Tipo não encontrado.")
                : ExpenseType.Create(user.IdTenant, request.Payload.Name, user.Code.ToString());
            if (request.Code is not null && type.Version != request.Payload.Version) throw new ConflictException("Tipo alterado. Atualize a página antes de salvar.");
            int? idCategory = null;
            if (request.Payload.DefaultCategoryCode is Guid categoryCode)
            {
                var category = await work.CatalogRepository.FindAsync(CatalogKind.Categories, user.IdTenant, categoryCode, ct)
                    ?? throw new NotFoundException("Categoria não encontrada nesta casa.");
                if (!category.IsActive && request.Payload.IsActive) throw new ConflictException("Escolha uma categoria ativa.");
                idCategory = category.Id;
            }
            if (request.Code is not null) type.Update(request.Payload.Name, request.Payload.IsActive, user.Code.ToString());
            work.ExpenseTypeRepository.Save(type, request.Code is not null, request.Payload.Behavior, idCategory,
                JsonSerializer.Serialize(request.Payload.Fields), user.Code.ToString());
            return new ExpenseTypeDto(type.Code, type.Name, type.IsActive, type.Version, request.Payload.Behavior, request.Payload.DefaultCategoryCode, request.Payload.Fields);
        }, ct);
    }
    public class ValidateFieldsHandler(IUnitOfWork work) : IRequestHandler<ValidateFieldsCommand>
    {
        public async Task Handle(ValidateFieldsCommand request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var type = await work.ExpenseTypeRepository.FindVersionAsync(request.Identity.IdTenant, request.Code, request.Payload.Version, ct)
                ?? throw new NotFoundException("Versão não encontrada nesta casa.");
            CustomFields.ValidateValues(JsonSerializer.Deserialize<CustomFieldDefinition[]>(type.FieldsJson)!, request.Payload.Values);
        }
    }
}
