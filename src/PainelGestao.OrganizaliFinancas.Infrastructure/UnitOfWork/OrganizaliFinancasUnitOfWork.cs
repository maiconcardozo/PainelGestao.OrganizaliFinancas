using Foundation.Dapper;
using Foundation.Domain.Interfaces.Data;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;

namespace PainelGestao.OrganizaliFinancas.Infrastructure.UnitOfWork
{
    /// <summary>Uses the transaction implementation supplied by Foundation.Base.</summary>
    public class OrganizaliFinancasUnitOfWork(
        ISqlConnectionFactory connectionFactory,
        Func<IDapperUnitOfWork, ITenantRepository> tenantRepositoryFactory,
        Func<IDapperUnitOfWork, IPilotRepository> pilotRepositoryFactory,
        Func<IDapperUnitOfWork, IAuthenticationRepository> authenticationRepositoryFactory,
        Func<IDapperUnitOfWork, IMemberRepository> memberRepositoryFactory,
        Func<IDapperUnitOfWork, IHouseholdRepository> householdRepositoryFactory,
        Func<IDapperUnitOfWork, ICatalogRepository> catalogRepositoryFactory,
        Func<IDapperUnitOfWork, IExpenseTypeRepository> expenseTypeRepositoryFactory,
        Func<IDapperUnitOfWork, IAccountRepository> accountRepositoryFactory,
        Func<IDapperUnitOfWork, IIncomeRepository> incomeRepositoryFactory,
        Func<IDapperUnitOfWork, IIdempotencyRepository> idempotencyRepositoryFactory,
        Func<IDapperUnitOfWork, IExpenseRepository> expenseRepositoryFactory,
        Func<IDapperUnitOfWork, ICardRepository> cardRepositoryFactory)
        : DapperUnitOfWork(connectionFactory), IUnitOfWork
    {
        private ITenantRepository? _tenantRepository;
        public ITenantRepository TenantRepository => _tenantRepository ??= tenantRepositoryFactory(this);
        private IPilotRepository? _pilotRepository;
        public IPilotRepository PilotRepository => _pilotRepository ??= pilotRepositoryFactory(this);
        private IAuthenticationRepository? _authenticationRepository;
        public IAuthenticationRepository AuthenticationRepository => _authenticationRepository ??= authenticationRepositoryFactory(this);
        private IMemberRepository? _memberRepository;
        public IMemberRepository MemberRepository => _memberRepository ??= memberRepositoryFactory(this);
        private IHouseholdRepository? _householdRepository;
        public IHouseholdRepository HouseholdRepository => _householdRepository ??= householdRepositoryFactory(this);
        private ICatalogRepository? _catalogRepository;
        public ICatalogRepository CatalogRepository => _catalogRepository ??= catalogRepositoryFactory(this);
        private IExpenseTypeRepository? _expenseTypeRepository;
        public IExpenseTypeRepository ExpenseTypeRepository => _expenseTypeRepository ??= expenseTypeRepositoryFactory(this);
        private IAccountRepository? _accountRepository;
        public IAccountRepository AccountRepository => _accountRepository ??= accountRepositoryFactory(this);
        private IIncomeRepository? _incomeRepository;
        public IIncomeRepository IncomeRepository => _incomeRepository ??= incomeRepositoryFactory(this);
        private IIdempotencyRepository? _idempotencyRepository;
        public IIdempotencyRepository IdempotencyRepository => _idempotencyRepository ??= idempotencyRepositoryFactory(this);
        private IExpenseRepository? _expenseRepository;
        public IExpenseRepository ExpenseRepository => _expenseRepository ??= expenseRepositoryFactory(this);
        private ICardRepository? _cardRepository;
        public ICardRepository CardRepository => _cardRepository ??= cardRepositoryFactory(this);
    }
}
