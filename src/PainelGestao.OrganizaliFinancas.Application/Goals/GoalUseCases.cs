using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Goals
{
    public sealed record GoalPayload(string Name, string TargetAmount, DateOnly? TargetDate, Guid AccountCode, string? Note, bool IsActive, int Version);
    /// <summary>Saved is the balance of the linked account (never below zero); the goal never creates income or expense (RF38).</summary>
    public sealed record GoalDto(Guid Code, string Name, string TargetAmount, DateOnly? TargetDate, string? Note, bool IsActive, int Version, Guid AccountCode,
        string AccountName, bool AccountActive, string Saved, string Remaining, int Percent, int? MonthsLeft, string? MonthlyNeeded, bool Reached);
    public sealed record ListGoalsQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<GoalDto>>;
    public sealed record SaveGoalCommand(SessionIdentity Identity, Guid? Code, GoalPayload Payload) : IRequest<GoalDto>;

    public class SaveGoalValidator : AbstractValidator<SaveGoalCommand>
    {
        public SaveGoalValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160).WithMessage("Informe o nome da meta com até 160 caracteres.");
            RuleFor(x => x.Payload.TargetAmount).PositiveMoney();
            RuleFor(x => x.Payload.Note).MaximumLength(300);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null).WithMessage("Uma nova meta começa ativa.");
        }
    }

    public class ListGoalsHandler(IUnitOfWork work) : IRequestHandler<ListGoalsQuery, IReadOnlyList<GoalDto>>
    {
        public async Task<IReadOnlyList<GoalDto>> Handle(ListGoalsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var goals = await work.GoalRepository.ListAsync(request.Identity.IdTenant, HouseholdCalendar.Day(today), ct);
            return goals.Select(x => Map(x, today)).ToArray();
        }
        internal static GoalDto Map(GoalRecord x, DateOnly today)
        {
            var saved = Math.Max(0, x.Balance);
            var remaining = Math.Max(0, x.TargetAmount - saved);
            var percent = (int)Math.Min(100, Math.Floor(saved * 100 / x.TargetAmount));
            int? monthsLeft = null; decimal? monthly = null;
            if (x.TargetDate is DateTime target)
            {
                var deadline = DateOnly.FromDateTime(target);
                var months = (deadline.Year - today.Year) * 12 + deadline.Month - today.Month + (deadline.Day >= today.Day ? 0 : -1);
                monthsLeft = Math.Max(0, months);
                monthly = remaining == 0 ? 0 : monthsLeft > 0 ? decimal.Round(remaining / monthsLeft.Value, 2, MidpointRounding.AwayFromZero) : remaining;
            }
            return new GoalDto(x.Code, x.Name, Money.Format(x.TargetAmount), x.TargetDate is DateTime d ? DateOnly.FromDateTime(d) : null, x.Note, x.IsActive, x.Version,
                x.AccountCode, x.AccountName, x.AccountActive, Money.Format(saved), Money.Format(remaining), percent, monthsLeft, MoneyRules.Format(monthly), remaining == 0);
        }
    }

    public class SaveGoalHandler(IUnitOfWork work) : IRequestHandler<SaveGoalCommand, GoalDto>
    {
        public async Task<GoalDto> Handle(SaveGoalCommand request, CancellationToken ct)
        {
            var code = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var actor = user.Code.ToString();
                var account = await work.AccountRepository.FindAsync(user.IdTenant, payload.AccountCode, ct) ?? throw new NotFoundException("Conta não encontrada nesta casa.");
                var goal = request.Code is Guid existing
                    ? await work.GoalRepository.FindAsync(user.IdTenant, existing, ct) ?? throw new NotFoundException("Meta não encontrada.")
                    : null;
                if (goal is not null && goal.Version != payload.Version) throw new ConflictException("Meta alterada. Atualize a página antes de salvar.");
                if (!account.IsActive && payload.IsActive && (goal is null || goal.IdFinancialAccount != account.Id))
                    throw new ConflictException("Escolha uma conta ativa para guardar a reserva.");
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                if (goal is null && payload.TargetDate is DateOnly date && date < today)
                    throw new InputValidationException(new Dictionary<string, string[]> { ["TargetDate"] = ["O prazo da meta precisa ser hoje ou depois."] });
                var target = Money.Parse(payload.TargetAmount);
                if (goal is null)
                {
                    goal = SavingsGoal.Create(user.IdTenant, payload.Name, target, payload.TargetDate, account, payload.Note, actor);
                    work.GoalRepository.Add(goal);
                }
                else
                {
                    goal.Update(payload.Name, target, payload.TargetDate, account, payload.Note, payload.IsActive, actor);
                    work.GoalRepository.Update(goal);
                }
                return goal.Code;
            }, ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var record = (await work.GoalRepository.ListAsync(request.Identity.IdTenant, HouseholdCalendar.Day(today), ct)).Single(x => x.Code == code);
            return ListGoalsHandler.Map(record, today);
        }
    }
}
