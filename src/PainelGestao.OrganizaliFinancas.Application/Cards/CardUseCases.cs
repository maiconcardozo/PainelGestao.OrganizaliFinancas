using System.Text.Json;
using FluentValidation;
using MediatR;
using PainelGestao.OrganizaliFinancas.Application.Authentication.Services;
using PainelGestao.OrganizaliFinancas.Application.Expenses;
using PainelGestao.OrganizaliFinancas.Application.Finance;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Cards
{
    public sealed record CardPayload(Guid HolderPersonCode, string Name, int ClosingDay, int DueDay, string? CreditLimit, string ClosingDayRule, bool IsActive, int Version);
    /// <summary>Available is an estimate (limit − open balance of all invoices, future installments included) and may differ from the bank.</summary>
    public sealed record CardDto(Guid Code, string Name, bool IsActive, int Version, Guid HolderPersonCode, string HolderName, int ClosingDay, int DueDay,
        string? CreditLimit, string ClosingDayRule, string Committed, string? Available);
    public sealed record InvoiceDto(Guid Code, Guid CardCode, string CardName, string ReferenceMonth, DateOnly ClosingDate, DateOnly DueDate, Guid ExpenseCode,
        string Amount, string Adjustments, string Paid, string OpenBalance, string Status, bool IsOverdue, int ItemCount);
    public sealed record CardItemDto(Guid Code, Guid PurchaseCode, string Description, DateOnly PurchaseDate, string Kind, int? InstallmentNumber, int InstallmentCount,
        string Amount, string? CategoryName, bool IsActive, string? Note);
    public sealed record InvoiceDetailDto(InvoiceDto Invoice, IReadOnlyList<CardItemDto> Items);
    public sealed record PurchasePreviewPayload(Guid CardCode, DateOnly PurchaseDate, string Total, int Count, int FirstInvoiceShift);
    public sealed record PurchasePreviewItemDto(int Number, string ReferenceMonth, DateOnly ClosingDate, DateOnly DueDate, string Amount);
    public sealed record PurchasePreviewDto(string Total, IReadOnlyList<PurchasePreviewItemDto> Items);
    public sealed record PurchasePayload(Guid CardCode, Guid? BuyerPersonCode, ExpenseDetailsPayload Details, DateOnly PurchaseDate, string Total, int Count, int FirstInvoiceShift);
    public sealed record PurchaseDto(Guid Code, Guid CardCode, string CardName, string Description, DateOnly PurchaseDate, string Total, int Count, string Refunded,
        Guid? CategoryCode, string? CategoryName, Guid? BuyerPersonCode, string? BuyerName, Guid? ExpenseTypeCode, int? ExpenseTypeVersion, JsonElement Fields, int Version);
    /// <summary>Only the description of the purchase changes; amount, date and installments are fixed once the items are in the invoices.</summary>
    public sealed record UpdatePurchasePayload(Guid? BuyerPersonCode, ExpenseDetailsPayload Details, int Version);
    public sealed record RefundPayload(string? Amount, string Reason, DateOnly? Date);

    public sealed record ListCardsQuery(SessionIdentity Identity) : IRequest<IReadOnlyList<CardDto>>;
    public sealed record SaveCardCommand(SessionIdentity Identity, Guid? Code, CardPayload Payload) : IRequest<CardDto>;
    public sealed record ListInvoicesQuery(SessionIdentity Identity, Guid? CardCode, DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<InvoiceDto>>;
    public sealed record GetInvoiceQuery(SessionIdentity Identity, Guid Code) : IRequest<InvoiceDetailDto>;
    public sealed record PreviewPurchaseQuery(SessionIdentity Identity, PurchasePreviewPayload Payload) : IRequest<PurchasePreviewDto>;
    public sealed record CreatePurchaseCommand(SessionIdentity Identity, Guid Key, PurchasePayload Payload) : IRequest<PurchaseDto>;
    public sealed record ListPurchasesQuery(SessionIdentity Identity, DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<PurchaseDto>>;
    public sealed record RefundPurchaseCommand(SessionIdentity Identity, Guid Key, Guid Code, RefundPayload Payload) : IRequest<PurchaseDto>;
    public sealed record UpdatePurchaseCommand(SessionIdentity Identity, Guid Code, UpdatePurchasePayload Payload) : IRequest<PurchaseDto>;
    public sealed record DeletePurchaseCommand(SessionIdentity Identity, Guid Code) : IRequest;
    public sealed record DeleteCardCommand(SessionIdentity Identity, Guid Code) : IRequest;

    public class SaveCardValidator : AbstractValidator<SaveCardCommand>
    {
        public SaveCardValidator()
        {
            RuleFor(x => x.Payload.Name).NotEmpty().MaximumLength(60).WithMessage("Informe um apelido com até 60 caracteres.");
            RuleFor(x => x.Payload.HolderPersonCode).NotEmpty().WithMessage("Escolha o titular do cartão.");
            RuleFor(x => x.Payload.ClosingDay).InclusiveBetween(1, 31);
            RuleFor(x => x.Payload.DueDay).InclusiveBetween(1, 31);
            RuleFor(x => x.Payload.CreditLimit).Must(value => value is null || Money.TryParse(value, out var amount) && amount >= 0)
                .WithMessage("Informe o limite como valor positivo, por exemplo 5000.00.");
            RuleFor(x => x.Payload.ClosingDayRule).Must(rule => CardSchedule.ClosingDayRules.Contains(rule)).WithMessage("Escolha em qual fatura entra a compra do dia do fechamento.");
            RuleFor(x => x.Payload.IsActive).Equal(true).When(x => x.Code is null).WithMessage("Um novo cartão deve estar ativo.");
        }
    }
    public class PurchasePreviewValidator : AbstractValidator<PurchasePreviewPayload>
    {
        public PurchasePreviewValidator()
        {
            RuleFor(x => x.CardCode).NotEmpty();
            RuleFor(x => x.Total).PositiveMoney();
            RuleFor(x => x.Count).InclusiveBetween(1, Installments.MaxCount).WithMessage($"Informe de 1 a {Installments.MaxCount} parcelas.");
            RuleFor(x => x.FirstInvoiceShift).InclusiveBetween(-1, 1).WithMessage("A fatura de destino só pode mudar um ciclo.");
        }
    }
    public class PreviewPurchaseValidator : AbstractValidator<PreviewPurchaseQuery>
    {
        public PreviewPurchaseValidator() => RuleFor(x => x.Payload).NotNull().SetValidator(new PurchasePreviewValidator());
    }
    public class CreatePurchaseValidator : AbstractValidator<CreatePurchaseCommand>
    {
        public CreatePurchaseValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new SingleExpenseDetailsValidator());
            RuleFor(x => x.Payload.Details.Shares).Must(shares => shares is null || shares.Length == 0).When(x => x.Payload.Details is not null)
                .WithMessage("Compras no cartão ainda não são divididas entre moradores.");
            RuleFor(x => new PurchasePreviewPayload(x.Payload.CardCode, x.Payload.PurchaseDate, x.Payload.Total, x.Payload.Count, x.Payload.FirstInvoiceShift))
                .SetValidator(new PurchasePreviewValidator()).OverridePropertyName("Payload");
        }
    }
    public class UpdatePurchaseValidator : AbstractValidator<UpdatePurchaseCommand>
    {
        public UpdatePurchaseValidator()
        {
            RuleFor(x => x.Payload.Details).NotNull().SetValidator(new SingleExpenseDetailsValidator());
            RuleFor(x => x.Payload.Details.Shares).Must(shares => shares is null || shares.Length == 0).When(x => x.Payload.Details is not null)
                .WithMessage("Compras no cartão ainda não são divididas entre moradores.");
        }
    }
    public class RefundPurchaseValidator : AbstractValidator<RefundPurchaseCommand>
    {
        public RefundPurchaseValidator()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage("Envie o cabeçalho Idempotency-Key.");
            RuleFor(x => x.Payload.Amount).PositiveMoney().When(x => x.Payload.Amount is not null);
            RuleFor(x => x.Payload.Reason).NotEmpty().MaximumLength(300).WithMessage("Informe o motivo do estorno.");
        }
    }

    internal static class CardMapper
    {
        public static CardDto Card(CardRecord x) => new(x.Code, x.Name, x.IsActive, x.Version, x.HolderPersonCode, x.HolderName, x.ClosingDay, x.DueDay,
            MoneyRules.Format(x.CreditLimit), x.ClosingDayRule, Money.Format(x.Committed), MoneyRules.Format(x.CreditLimit - x.Committed));
        /// <summary>Open until the closing date unless already settled; afterwards closed, partially paid or paid. Credit means refunds above the amount.</summary>
        public static InvoiceDto Invoice(InvoiceRecord x, DateOnly today)
        {
            var open = x.Amount + x.Adjustments - x.Paid;
            var closing = DateOnly.FromDateTime(x.ClosingDate);
            var due = DateOnly.FromDateTime(x.DueDate);
            // Paid in advance still reads as paid; new purchases in the cycle reopen the balance automatically.
            var status = open < 0 ? "credit" : open == 0 && x.Paid > 0 ? "paid" : today <= closing ? "open" : x.Paid > 0 ? "partial" : open > 0 ? "closed" : "paid";
            return new InvoiceDto(x.Code, x.CardCode, x.CardName, x.ReferenceMonth.ToString("yyyy-MM"), closing, due, x.ExpenseCode, Money.Format(x.Amount),
                Money.Format(x.Adjustments), Money.Format(x.Paid), Money.Format(open), status, open > 0 && due < today, x.ItemCount);
        }
        public static PurchaseDto Purchase(PurchaseRecord x) => new(x.Code, x.CardCode, x.CardName, x.Description, DateOnly.FromDateTime(x.PurchaseDate),
            Money.Format(x.TotalAmount), x.InstallmentCount, Money.Format(x.RefundedAmount), x.CategoryCode, x.CategoryName, x.BuyerPersonCode, x.BuyerName,
            x.ExpenseTypeCode, x.ExpenseTypeVersion, ExpenseMapper.Fields(x.FieldValuesJson), x.Version);
    }

    /// <summary>Finds or creates the invoices of the planned cycles, each with its backing obligation, inside the caller's transaction.</summary>
    internal static class InvoiceWriter
    {
        public static async Task<Dictionary<DateOnly, CardInvoice>> EnsureAsync(IUnitOfWork work, CreditCard card, IEnumerable<CardCycle> cycles, string actor, CancellationToken ct)
        {
            var wanted = cycles.GroupBy(x => x.ReferenceMonth).Select(x => x.First()).ToArray();
            var existing = (await work.CardRepository.FindInvoicesAsync(card.IdTenant, card.Id, wanted.Select(x => x.ReferenceMonth.ToDateTime(TimeOnly.MinValue)).ToArray(), ct))
                .ToDictionary(x => DateOnly.FromDateTime(x.ReferenceMonth));
            foreach (var cycle in wanted.Where(x => !existing.ContainsKey(x.ReferenceMonth)))
            {
                var invoice = CardInvoice.Create(card, cycle, actor);
                work.CardRepository.AddInvoice(invoice, Obligation.CreateInvoice(card, invoice, actor));
                existing[cycle.ReferenceMonth] = invoice;
            }
            return existing;
        }
    }

    public class ListCardsHandler(IUnitOfWork work) : IRequestHandler<ListCardsQuery, IReadOnlyList<CardDto>>
    {
        public async Task<IReadOnlyList<CardDto>> Handle(ListCardsQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            return (await work.CardRepository.ListCardsAsync(request.Identity.IdTenant, ct)).Select(CardMapper.Card).ToArray();
        }
    }
    public class SaveCardHandler(IUnitOfWork work) : IRequestHandler<SaveCardCommand, CardDto>
    {
        public async Task<CardDto> Handle(SaveCardCommand request, CancellationToken ct)
        {
            var code = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var holder = await work.HouseholdRepository.FindPersonAsync(user.IdTenant, payload.HolderPersonCode, ct) ?? throw new NotFoundException("Titular não encontrado nesta casa.");
                var limit = payload.CreditLimit is null ? (decimal?)null : Money.Parse(payload.CreditLimit);
                var actor = user.Code.ToString();
                if (request.Code is Guid existing)
                {
                    var card = await work.CardRepository.FindCardAsync(user.IdTenant, existing, ct) ?? throw new NotFoundException("Cartão não encontrado.");
                    if (card.Version != payload.Version) throw new ConflictException("Cartão alterado. Atualize a página antes de salvar.");
                    if (!holder.IsActive && holder.Id != card.IdHolderPerson) throw new ConflictException("Escolha um titular ativo.");
                    card.Update(holder, payload.Name, payload.ClosingDay, payload.DueDay, limit, payload.ClosingDayRule, payload.IsActive, actor);
                    work.CardRepository.UpdateCard(card);
                    return card.Code;
                }
                if (!holder.IsActive) throw new ConflictException("Escolha um titular ativo.");
                var created = CreditCard.Create(holder, payload.Name, payload.ClosingDay, payload.DueDay, limit, payload.ClosingDayRule, actor);
                work.CardRepository.AddCard(created);
                return created.Code;
            }, ct);
            return CardMapper.Card((await work.CardRepository.ListCardsAsync(request.Identity.IdTenant, ct)).Single(x => x.Code == code));
        }
    }
    /// <summary>A card leaves only when nothing depends on it: no purchases and no invoice with payment, adjustment, share or receipt. Invoices left empty go with it.</summary>
    public class DeleteCardHandler(IUnitOfWork work) : IRequestHandler<DeleteCardCommand>
    {
        public async Task Handle(DeleteCardCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var card = await work.CardRepository.FindCardAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Cartão não encontrado.");
                var usage = await work.CardRepository.GetCardUsageAsync(user.IdTenant, card.Id, ct);
                if (usage.Purchases > 0)
                    throw new ConflictException($"O cartão {card.Name} tem {usage.Purchases} compra(s) lançada(s). Exclua as compras antes ou apenas desative o cartão.");
                if (usage.LockedInvoices > 0)
                    throw new ConflictException($"O cartão {card.Name} tem fatura com pagamento, ajuste ou comprovante registrado. Desative o cartão em vez de excluir.");
                work.CardRepository.RemoveEmptyInvoices(user.IdTenant, card.Id);
                work.CardRepository.RemoveCard(user.IdTenant, card.Id);
                return true;
            }, ct);
        }
    }
    public class ListInvoicesHandler(IUnitOfWork work) : IRequestHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>>
    {
        public async Task<IReadOnlyList<InvoiceDto>> Handle(ListInvoicesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var month = Obligation.Month(today);
            var from = request.From is DateOnly start ? Obligation.Month(start) : month.AddMonths(-2);
            var to = request.To is DateOnly end ? Obligation.Month(end) : month.AddMonths(12);
            if (from > to || to > from.AddMonths(36))
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até 36 meses."] });
            return (await work.CardRepository.ListInvoicesAsync(request.Identity.IdTenant, request.CardCode, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct))
                .Select(x => CardMapper.Invoice(x, today)).ToArray();
        }
    }
    public class GetInvoiceHandler(IUnitOfWork work) : IRequestHandler<GetInvoiceQuery, InvoiceDetailDto>
    {
        public async Task<InvoiceDetailDto> Handle(GetInvoiceQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var invoice = await work.CardRepository.GetInvoiceAsync(request.Identity.IdTenant, request.Code, ct) ?? throw new NotFoundException("Fatura não encontrada.");
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var items = await work.CardRepository.ListItemsAsync(request.Identity.IdTenant, request.Code, ct);
            return new InvoiceDetailDto(CardMapper.Invoice(invoice, today), items.Select(x => new CardItemDto(x.Code, x.PurchaseCode, x.Description,
                DateOnly.FromDateTime(x.PurchaseDate), x.Kind, x.InstallmentNumber, x.InstallmentCount, Money.Format(x.Amount), x.CategoryName, x.IsActive, x.Note)).ToArray());
        }
    }
    public class PreviewPurchaseHandler(IUnitOfWork work) : IRequestHandler<PreviewPurchaseQuery, PurchasePreviewDto>
    {
        public async Task<PurchasePreviewDto> Handle(PreviewPurchaseQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var payload = request.Payload;
            var card = await work.CardRepository.FindCardAsync(request.Identity.IdTenant, payload.CardCode, ct) ?? throw new NotFoundException("Cartão não encontrado.");
            var items = CardSchedule.Plan(Money.Parse(payload.Total), payload.Count, payload.PurchaseDate, card.ClosingDay, card.DueDay, card.ClosingDayRule, payload.FirstInvoiceShift);
            return new PurchasePreviewDto(Money.Format(items.Sum(x => x.Amount)), items.Select(x => new PurchasePreviewItemDto(x.Number,
                x.Cycle.ReferenceMonth.ToString("yyyy-MM"), x.Cycle.ClosingDate, x.Cycle.DueDate, Money.Format(x.Amount))).ToArray());
        }
    }
    /// <summary>RF24: the purchase never touches a bank account; each installment becomes an item of its cycle's invoice.</summary>
    public class CreatePurchaseHandler(IUnitOfWork work) : IRequestHandler<CreatePurchaseCommand, PurchaseDto>
    {
        private const string Operation = "card-purchases.create";
        public async Task<PurchaseDto> Handle(CreatePurchaseCommand request, CancellationToken ct)
        {
            var code = await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var hash = Idempotency.Hash(payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is Guid existing) return existing;
                var actor = user.Code.ToString();
                var card = await work.CardRepository.FindCardAsync(user.IdTenant, payload.CardCode, ct) ?? throw new NotFoundException("Cartão não encontrado.");
                int? idBuyer = null;
                if (payload.BuyerPersonCode is Guid buyerCode)
                    idBuyer = (await work.HouseholdRepository.FindPersonAsync(user.IdTenant, buyerCode, ct) ?? throw new NotFoundException("Morador não encontrado nesta casa.")).Id;
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                HouseholdCalendar.EnsureNotFuture(payload.PurchaseDate, today, "PurchaseDate");
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "card", null, ct);
                var total = Money.Parse(payload.Total);
                var items = CardSchedule.Plan(total, payload.Count, payload.PurchaseDate, card.ClosingDay, card.DueDay, card.ClosingDayRule, payload.FirstInvoiceShift);
                var purchase = CardPurchase.Create(card, idBuyer, resolved.Details, payload.PurchaseDate, total, payload.Count, actor);
                work.CardRepository.AddPurchase(purchase);
                var invoices = await InvoiceWriter.EnsureAsync(work, card, items.Select(x => x.Cycle), actor, ct);
                foreach (var item in items)
                {
                    var invoice = invoices[item.Cycle.ReferenceMonth];
                    work.CardRepository.AddItem(user.IdTenant, purchase.Code, invoice.Code, "installment", item.Number, item.Amount, invoice.ReferenceMonth, null, actor);
                }
                foreach (var invoice in invoices.Values) work.CardRepository.RecalculateInvoice(user.IdTenant, invoice.Code, actor);
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, purchase.Code, actor);
                return purchase.Code;
            }, ct);
            return CardMapper.Purchase((await work.CardRepository.GetPurchaseAsync(request.Identity.IdTenant, code, ct))!);
        }
    }
    public class UpdatePurchaseHandler(IUnitOfWork work) : IRequestHandler<UpdatePurchaseCommand, PurchaseDto>
    {
        public async Task<PurchaseDto> Handle(UpdatePurchaseCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var payload = request.Payload;
                var purchase = await work.CardRepository.FindPurchaseAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Compra não encontrada.");
                if (purchase.Version != payload.Version) throw new ConflictException("Compra alterada. Atualize a página antes de salvar.");
                int? idBuyer = null;
                if (payload.BuyerPersonCode is Guid buyerCode)
                {
                    var buyer = await work.HouseholdRepository.FindPersonAsync(user.IdTenant, buyerCode, ct) ?? throw new NotFoundException("Morador não encontrado nesta casa.");
                    if (!buyer.IsActive && buyer.Id != purchase.IdBuyerPerson) throw new ConflictException("Escolha um morador ativo.");
                    idBuyer = buyer.Id;
                }
                var previous = new ObligationDetails(purchase.Description, purchase.IdExpenseType, purchase.ExpenseTypeVersion, purchase.IdCategory, null, null, null, purchase.FieldValuesJson, null);
                var resolved = await ExpenseDetailsResolver.ResolveAsync(work, user.IdTenant, payload.Details, "card", previous, ct);
                purchase.UpdateDetails(resolved.Details, idBuyer, user.Code.ToString());
                work.CardRepository.UpdatePurchaseDetails(purchase);
                return true;
            }, ct);
            return CardMapper.Purchase((await work.CardRepository.GetPurchaseAsync(request.Identity.IdTenant, request.Code, ct))!);
        }
    }
    /// <summary>Removes the purchase and its installments from the invoices; refused once any of those invoices received a payment, so the cash history stays whole.</summary>
    public class DeletePurchaseHandler(IUnitOfWork work) : IRequestHandler<DeletePurchaseCommand>
    {
        public async Task Handle(DeletePurchaseCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var purchase = await work.CardRepository.FindPurchaseAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Compra não encontrada.");
                if (await work.CardRepository.PurchaseHasPaidInvoiceAsync(user.IdTenant, purchase.Id, ct))
                    throw new ConflictException("Uma fatura com esta compra já recebeu pagamento. Estorne o pagamento antes ou registre um estorno da loja.");
                var actor = user.Code.ToString();
                work.CardRepository.RemovePurchase(user.IdTenant, purchase.Id);
                work.CardRepository.RecalculateCardInvoices(user.IdTenant, purchase.IdCreditCard, actor);
                work.CardRepository.RemoveEmptyInvoices(user.IdTenant, purchase.IdCreditCard);
                return true;
            }, ct);
        }
    }
    public class ListPurchasesHandler(IUnitOfWork work) : IRequestHandler<ListPurchasesQuery, IReadOnlyList<PurchaseDto>>
    {
        public async Task<IReadOnlyList<PurchaseDto>> Handle(ListPurchasesQuery request, CancellationToken ct)
        {
            await HouseholdAccess.RequireAsync(work, request.Identity, "household.read", ct);
            var today = await HouseholdCalendar.TodayAsync(work, request.Identity.IdTenant, ct);
            var from = request.From ?? Obligation.Month(today).AddMonths(-2);
            var to = request.To ?? today;
            if (from > to || to.DayNumber - from.DayNumber > 731)
                throw new InputValidationException(new Dictionary<string, string[]> { ["From"] = ["Escolha um período de até dois anos."] });
            return (await work.CardRepository.ListPurchasesAsync(request.Identity.IdTenant, HouseholdCalendar.Day(from), HouseholdCalendar.Day(to), ct)).Select(CardMapper.Purchase).ToArray();
        }
    }
    /// <summary>
    /// Merchant refund: installments still in open invoices are cancelled from the last one backwards while they fit in the refund;
    /// the remainder becomes a credit in the invoice of the refund date. No bank movement is created.
    /// </summary>
    public class RefundPurchaseHandler(IUnitOfWork work) : IRequestHandler<RefundPurchaseCommand, PurchaseDto>
    {
        private const string Operation = "card-purchases.refund";
        public async Task<PurchaseDto> Handle(RefundPurchaseCommand request, CancellationToken ct)
        {
            await work.ExecuteInTransactionAsync(async () =>
            {
                await work.MemberRepository.LockHouseholdAsync(request.Identity.IdTenant, ct);
                var user = await HouseholdAccess.RequireAsync(work, request.Identity, "finance.write", ct);
                var hash = Idempotency.Hash(request.Code, request.Payload);
                if (await Idempotency.ReplayAsync(work, user.IdTenant, Operation, request.Key, hash, ct) is not null) return true;
                var actor = user.Code.ToString();
                var purchase = await work.CardRepository.FindPurchaseAsync(user.IdTenant, request.Code, ct) ?? throw new NotFoundException("Compra não encontrada.");
                var card = (await work.CardRepository.FindCardByIdAsync(user.IdTenant, purchase.IdCreditCard, ct))!;
                var today = await HouseholdCalendar.TodayAsync(work, user.IdTenant, ct);
                var date = request.Payload.Date ?? today;
                HouseholdCalendar.EnsureNotFuture(date, today, "Date");
                var amount = request.Payload.Amount is null ? purchase.TotalAmount - purchase.RefundedAmount : Money.Parse(request.Payload.Amount);
                purchase.Refund(amount, actor);
                work.CardRepository.UpdatePurchase(purchase);
                var touched = new HashSet<Guid>();
                var remaining = amount;
                var openItems = (await work.CardRepository.ListPurchaseItemsAsync(user.IdTenant, purchase.Id, ct))
                    .Where(x => x.Kind == "installment" && DateOnly.FromDateTime(x.ClosingDate) >= date).Reverse();
                foreach (var item in openItems)
                {
                    if (item.Amount > remaining) break;
                    work.CardRepository.DeactivateItem(user.IdTenant, item.Code, actor);
                    touched.Add(item.InvoiceCode);
                    remaining -= item.Amount;
                }
                if (remaining > 0)
                {
                    var cycle = CardSchedule.ClosingIn(CardSchedule.ClosingMonthFor(date, card.ClosingDay, card.ClosingDayRule), card.ClosingDay, card.DueDay);
                    var invoice = (await InvoiceWriter.EnsureAsync(work, card, [cycle], actor, ct))[cycle.ReferenceMonth];
                    work.CardRepository.AddItem(user.IdTenant, purchase.Code, invoice.Code, "credit", null, -remaining, invoice.ReferenceMonth, request.Payload.Reason.Trim(), actor);
                    touched.Add(invoice.Code);
                }
                foreach (var invoiceCode in touched) work.CardRepository.RecalculateInvoice(user.IdTenant, invoiceCode, actor);
                Idempotency.Record(work, user.IdTenant, Operation, request.Key, hash, purchase.Code, actor);
                return true;
            }, ct);
            return CardMapper.Purchase((await work.CardRepository.GetPurchaseAsync(request.Identity.IdTenant, request.Code, ct))!);
        }
    }
}
