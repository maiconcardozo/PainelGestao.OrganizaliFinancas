using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces;
using PainelGestao.OrganizaliFinancas.Domain.ValueObjects;
using PainelGestao.OrganizaliFinancas.Shared.Exceptions;
namespace PainelGestao.OrganizaliFinancas.Application.Cards
{
    /// <summary>
    /// A17: an expense paid with a credit card becomes a one-installment purchase of the card. The card's closing day decides the invoice:
    /// up to the closing date it enters the invoice that closes in that month; after it, the next one (the card's own day-of-closing rule applies).
    /// No bank account moves; the cash leaves when the invoice is paid, once.
    /// </summary>
    internal static class CardCharges
    {
        public static async Task<CardPurchase> ChargeAsync(IUnitOfWork work, CreditCard card, int? idBuyer, string description, int? idCategory, DateOnly date,
            decimal amount, string actor, CancellationToken ct)
        {
            var details = new ObligationDetails(description.Length > 200 ? description[..200] : description, null, null, idCategory, null, null, null, "{}", null);
            var purchase = CardPurchase.Create(card, idBuyer, details, date, amount, 1, actor);
            work.CardRepository.AddPurchase(purchase);
            var item = CardSchedule.Plan(amount, 1, date, card.ClosingDay, card.DueDay, card.ClosingDayRule).Single();
            var invoice = (await InvoiceWriter.EnsureAsync(work, card, [item.Cycle], actor, ct))[item.Cycle.ReferenceMonth];
            work.CardRepository.AddItem(card.IdTenant, purchase.Code, invoice.Code, "installment", 1, amount, invoice.ReferenceMonth, null, actor);
            work.CardRepository.RecalculateInvoice(card.IdTenant, invoice.Code, actor);
            return purchase;
        }

        /// <summary>
        /// Reversing a card payment takes its purchase out of the invoice the same way a merchant refund does: the item is cancelled and the purchase
        /// reads as fully refunded, so the history stays. Refused once an invoice holding the item received a payment, as that cash is already recorded.
        /// </summary>
        public static async Task UndoAsync(IUnitOfWork work, int idTenant, int idPurchase, string actor, CancellationToken ct)
        {
            if (await work.CardRepository.PurchaseHasPaidInvoiceAsync(idTenant, idPurchase, ct))
                throw new ConflictException("A fatura que recebeu esta despesa já foi paga. Estorne o pagamento da fatura antes de estornar este pagamento.");
            var purchase = await work.CardRepository.FindPurchaseByIdAsync(idTenant, idPurchase, ct) ?? throw new NotFoundException("Compra do cartão não encontrada.");
            var invoices = new HashSet<Guid>();
            foreach (var item in await work.CardRepository.ListPurchaseItemsAsync(idTenant, idPurchase, ct))
            {
                work.CardRepository.DeactivateItem(idTenant, item.Code, actor);
                invoices.Add(item.InvoiceCode);
            }
            foreach (var invoice in invoices) work.CardRepository.RecalculateInvoice(idTenant, invoice, actor);
            var remaining = purchase.TotalAmount - purchase.RefundedAmount;
            if (remaining > 0)
            {
                purchase.Refund(remaining, actor);
                work.CardRepository.UpdatePurchase(purchase);
            }
        }
    }
}
