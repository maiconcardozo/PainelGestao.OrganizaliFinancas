using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;

namespace PainelGestao.OrganizaliFinancas.Domain.Interfaces
{
    /// <summary>Foundation transaction contract; repositories are added with their use cases.</summary>
    public interface IUnitOfWork : IDapperUnitOfWork
    {
        ITenantRepository TenantRepository { get; }
        IPilotRepository PilotRepository { get; }
        IAuthenticationRepository AuthenticationRepository { get; }
        IMemberRepository MemberRepository { get; }
        IHouseholdRepository HouseholdRepository { get; }
        ICatalogRepository CatalogRepository { get; }
        IExpenseTypeRepository ExpenseTypeRepository { get; }
        IAccountRepository AccountRepository { get; }
        IIncomeRepository IncomeRepository { get; }
        IIdempotencyRepository IdempotencyRepository { get; }
        IExpenseRepository ExpenseRepository { get; }
        ICardRepository CardRepository { get; }
        IPlanningRepository PlanningRepository { get; }
        IGoalRepository GoalRepository { get; }
        IAttachmentRepository AttachmentRepository { get; }
        IImportRepository ImportRepository { get; }
    }
}
