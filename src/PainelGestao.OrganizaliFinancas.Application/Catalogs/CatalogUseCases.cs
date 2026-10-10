using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Catalogs
{
    public sealed record CatalogItemDto(Guid Code, string Name, bool IsActive, int Version, Guid? ParentCode);
    public sealed record CatalogPayload(string Name, bool IsActive, int Version, Guid? ParentCode);
    public sealed record ListCatalogQuery(SessionIdentity Identity, CatalogKind Kind) : IRequest<IReadOnlyList<CatalogItemDto>>;
    public sealed record SaveCatalogCommand(SessionIdentity Identity, CatalogKind Kind, Guid? Code, CatalogPayload Payload) : IRequest<CatalogItemDto>;
    public class SaveCatalogValidator : AbstractValidator<SaveCatalogCommand>
    {
        public SaveCatalogValidator()
        {
            RuleFor(x => x.Kind).IsInEnum();
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null);
            RuleFor(x => x.Payload.ParentCode).Null().When(x => x.Kind != CatalogKind.Categories);
        }
    }
    public class ListCatalogHandler(IUnitOfWork work) : IRequestHandler<ListCatalogQuery, IReadOnlyList<CatalogItemDto>>
    {
        public async Task<IReadOnlyList<CatalogItemDto>> Handle(ListCatalogQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var items = await work.CatalogRepository.ListAsync(request.Kind, request.Identity.IdTenant, ct);
            return items.Select(item => new CatalogItemDto(item.Code, item.Name, item.IsActive, item.Version,
                item is Category category ? items.FirstOrDefault(x => x.Id == category.IdParentCategory)?.Code : null)).ToArray();
        }
    }
    public class SaveCatalogHandler(IUnitOfWork work) : IRequestHandler<SaveCatalogCommand, CatalogItemDto>
    {
        public Task<CatalogItemDto> Handle(SaveCatalogCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var actor = user.Code.ToString();
            var item = request.Code is Guid code
                ? await work.CatalogRepository.FindAsync(request.Kind, user.IdTenant, code, ct) ?? throw new NotFoundException("Cadastro não encontrado.")
                : Create(request.Kind, user.IdTenant, request.Payload.Name, actor);
            if (item.Version != request.Payload.Version) throw new ConflictException("Cadastro alterado. Atualize a página antes de salvar.");
            if (item is Category category) await SetParent(category, request.Payload, ct);
            if (request.Code is not null) item.Update(request.Payload.Name, request.Payload.IsActive, actor);
            work.CatalogRepository.Save(item, request.Code is not null);
            work.CatalogRepository.AddRevision(CatalogRevision.Create(user.IdTenant, request.Kind.ToString(), item.Code, item.Name,
                request.Payload.ParentCode, item.IsActive, item.Version, actor));
            return new CatalogItemDto(item.Code, item.Name, item.IsActive, item.Version, request.Payload.ParentCode);
        }, ct);

        private async Task SetParent(Category category, CatalogPayload payload, CancellationToken ct)
        {
            var items = (await work.CatalogRepository.ListAsync(CatalogKind.Categories, category.IdTenant, ct)).Cast<Category>().ToArray();
            var parent = payload.ParentCode is Guid code ? items.SingleOrDefault(x => x.Code == code)
                ?? throw new NotFoundException("Categoria superior não encontrada nesta casa.") : null;
            if (parent is { IsActive: false } && payload.IsActive) throw new ConflictException("A categoria superior está inativa.");
            var visited = new HashSet<int>();
            for (var ancestor = parent; ancestor is not null; ancestor = items.SingleOrDefault(x => x.Id == ancestor.IdParentCategory))
                if (ancestor.Id == category.Id || !visited.Add(ancestor.Id)) throw new ConflictException("A hierarquia de categorias não pode formar um ciclo.");
            if (!payload.IsActive && items.Any(x => x.IdParentCategory == category.Id && x.IsActive))
                throw new ConflictException("Inative ou mova as subcategorias ativas antes de inativar esta categoria.");
            category.SetParent(parent?.Id);
        }
        private static ICatalogItem Create(CatalogKind kind, int idTenant, string name, string actor) => kind switch
        {
            CatalogKind.Categories => Category.Create(idTenant, name, actor),
            CatalogKind.IncomeTypes => IncomeType.Create(idTenant, name, actor),
            CatalogKind.Creditors => Creditor.Create(idTenant, name, actor),
            CatalogKind.PaymentMethods => PaymentMethod.Create(idTenant, name, actor),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }
}
