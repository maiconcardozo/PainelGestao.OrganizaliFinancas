using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Accounts
{
    public sealed record AccountPayload(string Name, bool IsActive, int Version, DateOnly OpeningDate, string OpeningBalance);
    public sealed record AccountDto(Guid Code, string Name, bool IsActive, int Version, DateOnly OpeningDate, string OpeningBalance, string Balance, bool HasMovements);
    public sealed record AccountsOverviewDto(DateOnly AsOf, string Total, IReadOnlyList<AccountDto> Accounts);
    public sealed record StatementLineDto(Guid Code, DateOnly Date, string Amount, string Kind, string Description, string Balance,
        Guid? TransferCode, Guid? ReceiptCode, Guid? ReversedMovementCode);
    public sealed record StatementDto(Guid AccountCode, string AccountName, DateOnly From, DateOnly To, string OpeningBalance, string Credits,
        string Debits, string ClosingBalance, IReadOnlyList<StatementLineDto> Lines);
    public sealed record TransferPayload(Guid SourceAccountCode, Guid TargetAccountCode, DateOnly TransferDate, string Amount, string? Description);
    public sealed record TransferDto(Guid Code, Guid SourceAccountCode, string SourceAccountName, Guid TargetAccountCode, string TargetAccountName,
        DateOnly TransferDate, string Amount, string Description, bool IsReversed, string? ReversalReason);

    public sealed record ListAccountsQuery(SessionIdentity Identity) : IRequest<AccountsOverviewDto>;
    public sealed record SaveAccountCommand(SessionIdentity Identity, Guid? Code, AccountPayload Payload) : IRequest<AccountDto>;
    public sealed record GetStatementQuery(SessionIdentity Identity, Guid Code, DateOnly? From, DateOnly? To) : IRequest<StatementDto>;
    public sealed record ListTransfersQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<TransferDto>>;
    public sealed record CreateTransferCommand(SessionIdentity Identity, Guid Key, TransferPayload Payload) : IRequest<TransferDto>;
    public sealed record ReverseTransferCommand(SessionIdentity Identity, Guid Key, Guid Code, ReversalPayload Payload) : IRequest<TransferDto>;

    public class SaveAccountValidator : AbstractValidator<SaveAccountCommand>
    {
        public SaveAccountValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Payload.Version).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null).WithMessage("Uma nova conta deve estar ativa.");
            RuleFor(x => x.Payload.OpeningDate).Must(date => date.Year >= 2000).WithMessage("Data de abertura inválida.");
            RuleFor(x => x.Payload.OpeningBalance).SignedMoney();
        }
    }
    public class CreateTransferValidator : AbstractValidator<CreateTransferCommand>
    {
        public CreateTransferValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Amount).PositiveMoney();
            RuleFor(x => x.Payload.TargetAccountCode).NotEqual(x => x.Payload.SourceAccountCode).WithMessage("Escolha contas diferentes.");
            RuleFor(x => x.Payload.Description).MaximumLength(200);
        }
    }
    public class ReverseTransferValidator : AbstractValidator<ReverseTransferCommand>
    {
        public ReverseTransferValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe a justificativa do estorno.");
        }
    }

    public class ListAccountsHandler(IUnitOfWork work) : IRequestHandler<ListAccountsQuery, AccountsOverviewDto>
    {
        public async Task<AccountsOverviewDto> Handle(ListAccountsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var accounts = await work.AccountRepository.ListAsync(request.Identity.IdTenant, HouseholdCalendar.Day(today), ct);
            return new(today, Money.Format(accounts.Sum(x => x.Balance)), accounts.Select(Map).ToArray());
        }
        internal static AccountDto Map(AccountRecord x) => new(x.Code, x.Name, x.IsActive, x.Version, DateOnly.FromDateTime(x.OpeningDate),
            Money.Format(x.OpeningBalance), Money.Format(x.Balance), x.HasMovements);
    }
    public class SaveAccountHandler(IUnitOfWork work) : IRequestHandler<SaveAccountCommand, AccountDto>
    {
        public Task<AccountDto> Handle(SaveAccountCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var actor = user.Code.ToString();
            var payload = request.Payload;
            var opening = Money.Parse(payload.OpeningBalance);
            FinancialAccount account;
            var hasMovements = false;
            if (request.Code is Guid code)
            {
                account = await work.AccountRepository.FindAsync(user.IdTenant, code, ct) ?? throw new NotFoundException("Conta não encontrada.");
                if (account.Version != payload.Version) throw new ConflictException("Conta alterada. Atualize a página antes de salvar.");
                hasMovements = await work.AccountRepository.HasMovementsAsync(user.IdTenant, account.Id, ct);
                account.Update(payload.Name, payload.IsActive, payload.OpeningDate, opening, hasMovements, actor);
                work.AccountRepository.Update(account);
            }
            else
            {
                account = FinancialAccount.Create(user.IdTenant, payload.Name, payload.OpeningDate, opening, actor);
                work.AccountRepository.Add(account);
            }
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            var moved = hasMovements ? await work.AccountRepository.SumBeforeAsync(user.IdTenant, account.Id, HouseholdCalendar.Day(today.AddDays(1)), ct) : 0;
            return new AccountDto(account.Code, account.Name, account.IsActive, account.Version, account.OpeningDay(),
                Money.Format(account.OpeningBalance), Money.Format(account.OpeningBalance + moved), hasMovements);
        }, ct);
    }
    public class GetStatementHandler(IUnitOfWork work) : IRequestHandler<GetStatementQuery, StatementDto>
    {
        public async Task<StatementDto> Handle(GetStatementQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var account = await work.AccountRepository.FindAsync(request.Identity.IdTenant, request.Code, ct)
                ?? throw new NotFoundException("Conta não encontrada.");
            var today = await HouseholdCalendar.TodayAsync(work, account.IdTenant, ct);
            var to = request.To ?? today;
            var from = request.From ?? new DateOnly(to.Year, to.Month, 1);
            if (from > to || to.DayNumber - from.DayNumber > 366)
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até 366 dias."] });
            var before = account.OpeningBalance + await work.AccountRepository.SumBeforeAsync(account.IdTenant, account.Id, HouseholdCalendar.Day(from), ct);
            var movements = await work.AccountRepository.ListMovementsAsync(account.IdTenant, account.Id, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct);
            var running = before;
            var lines = movements.Select(x =>
            {
                running += x.Amount;
                return new StatementLineDto(x.Code, DateOnly.FromDateTime(x.MovementDate), Money.Format(x.Amount), x.Kind, x.Description,
                    Money.Format(running), x.TransferCode, x.ReceiptCode, x.ReversedMovementCode);
            }).ToArray();
            return new StatementDto(account.Code, account.Name, from, to, Money.Format(before),
                Money.Format(movements.Where(x => x.Amount > 0).Sum(x => x.Amount)), Money.Format(movements.Where(x => x.Amount < 0).Sum(x => x.Amount)),
                Money.Format(running), lines);
        }
    }
    public class ListTransfersHandler(IUnitOfWork work) : IRequestHandler<ListTransfersQuery, IReadOnlyList<TransferDto>>
    {
        public async Task<IReadOnlyList<TransferDto>> Handle(ListTransfersQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.AccountRepository.ListTransfersAsync(request.Identity.IdTenant, ct)).Select(Map).ToArray();
        }
        internal static TransferDto Map(TransferRecord x) => new(x.Code, x.SourceAccountCode, x.SourceAccountName, x.TargetAccountCode,
            x.TargetAccountName, DateOnly.FromDateTime(x.TransferDate), Money.Format(x.Amount), x.Description, x.DtReversed is not null, x.ReversalReason);
    }
    public class CreateTransferHandler(IUnitOfWork work) : IRequestHandler<CreateTransferCommand, TransferDto>
    {
        private const string Operation = "transfers.create";
        public Task<TransferDto> Handle(CreateTransferCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
            var payload = request.Payload;
            var hash = Idempotency.Hash(payload);
            if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing)
                return ListTransfersHandler.Map((await work.AccountRepository.GetTransferAsync(user.IdTenant, existing, ct))!);
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            HouseholdCalendar.EnsureNotFuture(payload.TransferDate, today, "TransferDate");
            var source = await work.AccountRepository.FindAsync(user.IdTenant, payload.SourceAccountCode, ct) ?? throw new NotFoundException("Conta de origem não encontrada.");
            var target = await work.AccountRepository.FindAsync(user.IdTenant, payload.TargetAccountCode, ct) ?? throw new NotFoundException("Conta de destino não encontrada.");
            var actor = user.Code.ToString();
            var amount = Money.Parse(payload.Amount);
            var transfer = Transfer.Create(source, target, payload.TransferDate, amount, payload.Description, actor);
            var label = string.IsNullOrEmpty(transfer.Description) ? "Transferência" : transfer.Description;
            work.AccountRepository.AddTransfer(transfer);
            work.AccountRepository.AddMovement(AccountMovement.Create(source, payload.TransferDate, -amount, "transfer_out", $"{label} para {target.Name}", actor), transfer.Code);
            work.AccountRepository.AddMovement(AccountMovement.Create(target, payload.TransferDate, amount, "transfer_in", $"{label} de {source.Name}", actor), transfer.Code);
            Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, transfer.Code, actor);
            return new TransferDto(transfer.Code, source.Code, source.Name, target.Code, target.Name, payload.TransferDate, Money.Format(amount),
                transfer.Description, false, null);
        }, ct);
    }
    public class ReverseTransferHandler(IUnitOfWork work) : IRequestHandler<ReverseTransferCommand, TransferDto>
    {
        private const string Operation = "transfers.reverse";
        public Task<TransferDto> Handle(ReverseTransferCommand request, CancellationToken ct) => work.ExecuteInTransactionAsync(async () =>
        {
            await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
            var user = await HouseholdAccess.RequireAsync(work, request.Identity, "payments.reverse", ct);
            var hash = Idempotency.Hash(request.Code, request.Payload);
            if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is not null)
                return ListTransfersHandler.Map((await work.AccountRepository.GetTransferAsync(user.IdTenant, request.Code, ct))!);
            var transfer = await work.AccountRepository.FindTransferAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Transferência não encontrada.");
            var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
            var date = request.Payload.ReversalDate ?? today;
            HouseholdCalendar.EnsureNotFuture(date, today, "ReversalDate");
            var actor = user.Code.ToString();
            transfer.Reverse(request.Payload.Reason, actor);
            work.AccountRepository.MarkTransferReversed(transfer);
            foreach (var original in await work.AccountRepository.FindOriginalMovementsAsync(user.IdTenant, transfer.Id, null, ct))
                work.AccountRepository.AddMovement(original.Compensate(date, $"Estorno: {transfer.ReversalReason}", actor));
            Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, transfer.Code, actor);
            var record = (await work.AccountRepository.GetTransferAsync(user.IdTenant, transfer.Code, ct))!;
            return ListTransfersHandler.Map(record with { DtReversed = transfer.DtReversed, ReversalReason = transfer.ReversalReason });
        }, ct);
    }
}
