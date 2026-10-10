using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Expenses
{
    public class ListExpensesHandler(IUnitOfWork work) : IRequestHandler<ListExpensesQuery, ExpenseListDto>
    {
        public async Task<ExpenseListDto> Handle(ListExpensesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var from = request.From ?? new DateOnly(today.Year, today.Month, 1);
            var to = request.To ?? from.AddMonths(1).AddDays(-1);
            if (from > to || to.DayNumber - from.DayNumber > 366)
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até 366 dias."] });
            var records = await work.ExpenseRepository.ListAsync(idTenant, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), request.IncludeOverdue, ct);
            var items = await ExpenseMapper.ReadAsync(work, idTenant, records, today, ct);
            var inPeriod = items.Where(x => x.DueDate >= from && x.Status != "cancelled").ToArray();
            decimal Sum(IEnumerable<ExpenseDto> list, Func<ExpenseDto, string> value) => list.Sum(x => Money.Parse(value(x)));
            return new ExpenseListDto(from, to, today, Money.Format(inPeriod.Sum(x => Money.Parse(x.Amount) + Money.Parse(x.Adjustments))),
                Money.Format(Sum(inPeriod, x => x.Paid)), Money.Format(Sum(inPeriod, x => x.OpenBalance)), Money.Format(Sum(items.Where(x => x.IsOverdue), x => x.OpenBalance)), items);
        }
    }
    public class GetExpenseHandler(IUnitOfWork work) : IRequestHandler<GetExpenseQuery, ExpenseDetailDto>
    {
        public async Task<ExpenseDetailDto> Handle(GetExpenseQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return await ReadAsync(work, request.Identity.IdTenant, request.Code, ct);
        }
        internal static async Task<ExpenseDetailDto> ReadAsync(IUnitOfWork work, int idTenant, Guid code, CancellationToken ct)
        {
            var obligation = await work.ExpenseRepository.FindAsync(idTenant, code, ct) ?? throw new NotFoundException("Despesa não encontrada.");
            var expense = await ExpenseMapper.GetAsync(work, idTenant, code, ct);
            var adjustments = (await work.ExpenseRepository.ListAdjustmentsAsync(idTenant, obligation.Id, ct))
                .Select(x => new AdjustmentDto(x.Code, x.Kind, Money.Format(x.Amount), DateOnly.FromDateTime(x.AdjustmentDate), x.Reason)).ToArray();
            var payments = (await work.ExpenseRepository.ListAllocationsByObligationAsync(idTenant, obligation.Id, ct)).Select(ExpenseMapper.Allocation).ToArray();
            return new ExpenseDetailDto(expense, adjustments, payments);
        }
    }

    /// <summary>Single bill (RF16), optionally settled in the same transaction when the household already paid it.</summary>
    public class CreateExpenseHandler(IUnitOfWork work) : IRequestHandler<CreateExpenseCommand, ExpenseDto>
    {
        private const string Operation = "expenses.create";
        public async Task<ExpenseDto> Handle(CreateExpenseCommand request, CancellationToken ct)
        {
            var code = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var hash = Idempotency.Hash(payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing) return existing;
                if (payload.Payment is not null) await HouseholdAccess.RequireAsync(work, request.Identity, "payments.settle", ct);
                var actor = user.Code.ToString();
                var amount = Money.Parse(payload.Amount);
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "single", null, ct);
                var shares = Shares.Resolve(amount, resolved.Details.SplitMode, resolved.Shares);
                var obligation = Obligation.Create(user.IdTenant, resolved.Details, payload.Competence ?? payload.DueDate, payload.DueDate, amount, actor);
                work.ExpenseRepository.Add(obligation);
                work.ExpenseRepository.ReplaceShares(user.IdTenant, obligation.Code, shares, actor);
                if (payload.Payment is ImmediatePaymentPayload immediate)
                    await PaymentWriter.PayAsync(work, user.IdTenant, immediate.AccountCode, immediate.PaymentDate, immediate.PayerPersonCode, obligation.IdPaymentMethod,
                        obligation.Description, [(obligation.Code, amount)], actor, ct);
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, obligation.Code, actor);
                return obligation.Code;
            }, ct);
            return await ExpenseMapper.GetAsync(work, request.Identity.IdTenant, code, ct);
        }
    }
    /// <summary>Edits this obligation only. A recurring occurrence is detached so later regenerations keep this edit.</summary>
    public class UpdateExpenseHandler(IUnitOfWork work) : IRequestHandler<UpdateExpenseCommand, ExpenseDto>
    {
        public async Task<ExpenseDto> Handle(UpdateExpenseCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var obligation = await work.ExpenseRepository.FindAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Despesa não encontrada.");
                if (obligation.Version != payload.Version) throw new ConflictException("Despesa alterada. Atualize a página antes de salvar.");
                var behavior = obligation.IdRecurringExpense is not null ? "recurring" : obligation.IdInstallmentPlan is not null ? "installments" : "single";
                var previous = new ObligationDetails(obligation.Description, obligation.IdExpenseType, obligation.ExpenseTypeVersion, obligation.IdCategory,
                    obligation.IdCreditor, obligation.IdPaymentMethod, obligation.IdPayerPerson, obligation.FieldValuesJson, obligation.SplitMode);
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, behavior, previous, ct);
                var amount = Money.Parse(payload.Amount);
                var totals = (await work.ExpenseRepository.TotalsAsync(user.IdTenant, [obligation.Id], ct)).Single();
                if (amount + totals.Adjustments < totals.Paid) throw new ConflictException("O novo valor ficaria abaixo do que já foi pago.");
                var actor = user.Code.ToString();
                obligation.Edit(resolved.Details, payload.Competence ?? payload.DueDate, payload.DueDate, amount, totals.Paid, actor);
                work.ExpenseRepository.Update(obligation);
                work.ExpenseRepository.ReplaceShares(user.IdTenant, obligation.Code, Shares.Resolve(amount, resolved.Details.SplitMode, resolved.Shares), actor);
                return true;
            }, ct);
            return await ExpenseMapper.GetAsync(work, request.Identity.IdTenant, request.Code, ct);
        }
    }
    public class CancelExpenseHandler(IUnitOfWork work) : IRequestHandler<CancelExpenseCommand, ExpenseDto>
    {
        public async Task<ExpenseDto> Handle(CancelExpenseCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var obligation = await work.ExpenseRepository.FindAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Despesa não encontrada.");
                if (obligation.Version != request.Payload.Version) throw new ConflictException("Despesa alterada. Atualize a página antes de cancelar.");
                var totals = (await work.ExpenseRepository.TotalsAsync(user.IdTenant, [obligation.Id], ct)).Single();
                obligation.Cancel(request.Payload.Reason, totals.Paid, user.Code.ToString());
                work.ExpenseRepository.Update(obligation);
                return true;
            }, ct);
            return await ExpenseMapper.GetAsync(work, request.Identity.IdTenant, request.Code, ct);
        }
    }
    /// <summary>Interest, fine, discount or adjustment with justification (RF20); it changes the open balance, never the account.</summary>
    public class AddAdjustmentHandler(IUnitOfWork work) : IRequestHandler<AddAdjustmentCommand, ExpenseDetailDto>
    {
        public async Task<ExpenseDetailDto> Handle(AddAdjustmentCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "payments.settle", ct);
                var obligation = await work.ExpenseRepository.FindAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Despesa não encontrada.");
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                var date = request.Payload.Date ?? today;
                HouseholdCalendar.EnsureNotFuture(date, today, "Date");
                var totals = (await work.ExpenseRepository.TotalsAsync(user.IdTenant, [obligation.Id], ct)).Single();
                work.ExpenseRepository.AddAdjustment(ObligationAdjustment.Create(obligation, request.Payload.Kind, Money.Parse(request.Payload.Amount), date,
                    request.Payload.Reason, obligation.Amount + totals.Adjustments - totals.Paid, user.Code.ToString()));
                return true;
            }, ct);
            return await GetExpenseHandler.ReadAsync(work, request.Identity.IdTenant, request.Code, ct);
        }
    }

    /// <summary>Writes one payment: cash out, allocations and the ledger movement in the caller's transaction.</summary>
    internal static class PaymentWriter
    {
        public static async Task<Payment> PayAsync(IUnitOfWork work, int idTenant, Guid accountCode, DateOnly date, Guid? payerCode, int? idPaymentMethod,
            string? description, IReadOnlyList<(Guid ObligationCode, decimal Amount)> allocations, string actor, CancellationToken ct)
        {
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            HouseholdCalendar.EnsureNotFuture(date, today, "PaymentDate");
            var account = await work.AccountRepository.FindAsync(idTenant, accountCode, ct) ?? throw new NotFoundException("Conta não encontrada.");
            Person? payer = null;
            if (payerCode is Guid code)
                payer = await work.HouseholdRepository.FindPersonAsync(idTenant, code, ct) ?? throw new NotFoundException("Pagador não encontrado nesta casa.");
            var amount = allocations.Sum(x => x.Amount);
            var payment = Payment.Create(account, payer, idPaymentMethod, date, amount, description, actor);
            work.ExpenseRepository.AddPayment(payment);
            var label = string.IsNullOrEmpty(payment.Description) ? "Pagamento" : $"Pagamento: {payment.Description}";
            work.AccountRepository.AddMovement(AccountMovement.Create(account, date, -amount, "payment", label, actor), paymentCode: payment.Code);
            foreach (var (obligationCode, value) in allocations) work.ExpenseRepository.AddAllocation(idTenant, payment.Code, obligationCode, value, actor);
            return payment;
        }
    }
    public class ListPaymentsHandler(IUnitOfWork work) : IRequestHandler<ListPaymentsQuery, IReadOnlyList<PaymentDto>>
    {
        public async Task<IReadOnlyList<PaymentDto>> Handle(ListPaymentsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var from = request.From ?? new DateOnly(today.Year, today.Month, 1).AddMonths(-2);
            var to = request.To ?? today;
            if (from > to || to.DayNumber - from.DayNumber > 731)
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até dois anos."] });
            var payments = await work.ExpenseRepository.ListPaymentsAsync(idTenant, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct);
            var allocations = await work.ExpenseRepository.ListAllocationsByPaymentAsync(idTenant, payments.Select(x => x.Code).ToArray(), ct);
            return payments.Select(x => ExpenseMapper.Payment(x, allocations)).ToArray();
        }
        internal static async Task<PaymentDto> ReadAsync(IUnitOfWork work, int idTenant, Guid code, CancellationToken ct)
        {
            var payment = await work.ExpenseRepository.GetPaymentAsync(idTenant, code, ct) ?? throw new NotFoundException("Pagamento não encontrado.");
            return ExpenseMapper.Payment(payment, await work.ExpenseRepository.ListAllocationsByPaymentAsync(idTenant, [code], ct));
        }
    }
    /// <summary>
    /// RF19/RF20: partial or total payment of one or more obligations, with interest, fine and discount recorded as separate adjustments.
    /// The household lock serializes concurrent payments, so two requests can never settle more than the open balance.
    /// </summary>
    public class CreatePaymentHandler(IUnitOfWork work) : IRequestHandler<CreatePaymentCommand, PaymentDto>
    {
        private const string Operation = "payments.create";
        public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken ct)
        {
            var code = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "payments.settle", ct);
                var payload = request.Payload;
                var hash = Idempotency.Hash(payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing) return existing;
                var actor = user.Code.ToString();
                var obligations = await work.ExpenseRepository.FindManyAsync(user.IdTenant, payload.Allocations.Select(x => x.ExpenseCode).ToArray(), ct);
                if (obligations.Count != payload.Allocations.Length) throw new NotFoundException("Despesa não encontrada nesta casa.");
                var totals = (await work.ExpenseRepository.TotalsAsync(user.IdTenant, obligations.Select(x => x.Id).ToArray(), ct)).ToDictionary(x => x.Id);
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                var allocations = new List<(Guid, decimal)>();
                foreach (var item in payload.Allocations)
                {
                    var obligation = obligations.Single(x => x.Code == item.ExpenseCode);
                    obligation.EnsureOpen();
                    var open = obligation.Amount + totals[obligation.Id].Adjustments - totals[obligation.Id].Paid;
                    foreach (var (kind, value) in new[] { ("interest", item.Interest), ("fine", item.Fine), ("discount", item.Discount) })
                    {
                        if (value is null) continue;
                        var adjustment = ObligationAdjustment.Create(obligation, kind, Money.Parse(value), payload.PaymentDate > today ? today : payload.PaymentDate,
                            payload.AdjustmentReason!, open, actor);
                        work.ExpenseRepository.AddAdjustment(adjustment);
                        open += adjustment.Amount;
                    }
                    var amount = Money.Parse(item.Amount);
                    if (amount > open)
                        throw new ConflictException($"O pagamento de {Money.Format(amount)} supera o saldo aberto de {Money.Format(open)} em {obligation.Description}.");
                    allocations.Add((obligation.Code, amount));
                }
                int? idMethod = null;
                if (payload.PaymentMethodCode is Guid method)
                    idMethod = (await work.CatalogRepository.FindAsync(Domain.Interfaces.Repositories.CatalogKind.PaymentMethods, user.IdTenant, method, ct)
                        ?? throw new NotFoundException("Forma de pagamento não encontrada.")).Id;
                var description = payload.Description ?? (obligations.Count == 1 ? obligations[0].Description : null);
                var payment = await PaymentWriter.PayAsync(work, user.IdTenant, payload.AccountCode, payload.PaymentDate, payload.PayerPersonCode, idMethod,
                    description, allocations, actor, ct);
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, payment.Code, actor);
                return payment.Code;
            }, ct);
            return await ListPaymentsHandler.ReadAsync(work, request.Identity.IdTenant, code, ct);
        }
    }
    /// <summary>RF21: owner only. The original payment and movement stay; a compensating movement restores the account and the open balance.</summary>
    public class ReversePaymentHandler(IUnitOfWork work) : IRequestHandler<ReversePaymentCommand, PaymentDto>
    {
        private const string Operation = "payments.reverse";
        public async Task<PaymentDto> Handle(ReversePaymentCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "payments.reverse", ct);
                var hash = Idempotency.Hash(request.Code, request.Payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is not null) return true;
                var payment = await work.ExpenseRepository.FindPaymentAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Pagamento não encontrado.");
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                var date = request.Payload.ReversalDate ?? today;
                HouseholdCalendar.EnsureNotFuture(date, today, "ReversalDate");
                var actor = user.Code.ToString();
                payment.Reverse(request.Payload.Reason, actor);
                work.ExpenseRepository.MarkPaymentReversed(payment);
                foreach (var original in await work.AccountRepository.FindOriginalMovementsAsync(user.IdTenant, null, null, payment.Id, ct))
                    work.AccountRepository.AddMovement(original.Compensate(date, $"Estorno: {payment.ReversalReason}", actor));
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, payment.Code, actor);
                return true;
            }, ct);
            return await ListPaymentsHandler.ReadAsync(work, request.Identity.IdTenant, request.Code, ct);
        }
    }

    /// <summary>RF41: who is responsible for each split obligation of the month, who actually paid it and the suggested internal settlement.</summary>
    public class GetContributionsHandler(IUnitOfWork work) : IRequestHandler<GetContributionsQuery, ContributionsDto>
    {
        public async Task<ContributionsDto> Handle(GetContributionsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var idTenant = request.Identity.IdTenant;
            var today = await HouseholdCalendar.TodayAsync(work, idTenant, ct);
            var month = Obligation.Month(request.Month ?? today);
            var (from, to) = (HouseholdCalendar.Day(month), HouseholdCalendar.Day(month.AddMonths(1)));
            var responsible = await work.ExpenseRepository.ResponsibleTotalsAsync(idTenant, from, to, ct);
            var paid = await work.ExpenseRepository.PaidBySplitPayerAsync(idTenant, from, to, ct);
            var lines = responsible.Select(x => (x.PersonCode, x.PersonName)).Concat(paid.Select(x => (x.PersonCode, x.PersonName))).Distinct()
                .Select(person => new ContributionLine(person.PersonCode, person.PersonName,
                    responsible.Where(x => x.PersonCode == person.PersonCode).Sum(x => x.Total), paid.Where(x => x.PersonCode == person.PersonCode).Sum(x => x.Total)))
                .OrderBy(x => x.PersonName).ToArray();
            return new ContributionsDto(month.ToString("yyyy-MM"),
                lines.Select(x => new ContributionDto(x.PersonCode, x.PersonName, Money.Format(x.Responsible), Money.Format(x.Paid), Money.Format(x.Balance))).ToArray(),
                Contributions.Settle(lines).Select(x => new SettlementDto(x.FromPersonCode, x.FromPersonName, x.ToPersonCode, x.ToPersonName, Money.Format(x.Amount))).ToArray());
        }
    }
}
