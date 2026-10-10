using Dapper;
using Foundation.Dapper.Repositories;
using Foundation.Domain.Interfaces.UnitOfWork;
using PainelGestao.OrganizaliFinancas.Domain.Entities;
using PainelGestao.OrganizaliFinancas.Domain.Interfaces.Repositories;
using PainelGestao.OrganizaliFinancas.Infrastructure.Queries.Cards;
namespace PainelGestao.OrganizaliFinancas.Infrastructure.Repositories.Cards
{
    public class CardRepository(IDapperUnitOfWork work) : ICardRepository
    {
        private Task<T?> Single<T>(string sql, object parameters, CancellationToken ct) =>
            work.ExecuteReadAsync((connection, transaction) => connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct);
        private async Task<IReadOnlyList<T>> List<T>(string sql, object parameters, CancellationToken ct) =>
            (await work.ExecuteReadAsync((connection, transaction) => connection.QueryAsync<T>(
                new CommandDefinition(sql, parameters, transaction, cancellationToken: ct)), ct)).ToArray();

        public Task<IReadOnlyList<CardRecord>> ListCardsAsync(int idTenant, CancellationToken ct) =>
            List<CardRecord>(CardSql.Cards, new { IdTenant = idTenant }, ct);
        public Task<CreditCard?> FindCardAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<CreditCard>(CardSql.FindCard, new { IdTenant = idTenant, Code = code }, ct);
        public Task<CreditCard?> FindCardByIdAsync(int idTenant, int id, CancellationToken ct) =>
            Single<CreditCard>(CardSql.FindCardById, new { IdTenant = idTenant, Id = id }, ct);
        public void AddCard(CreditCard card) => new DapperRepository<CreditCard>(work).Add(card);
        public void UpdateCard(CreditCard card) => work.AddPendingWrite(CardSql.UpdateCard, card);

        public Task<IReadOnlyList<InvoiceRecord>> ListInvoicesAsync(int idTenant, Guid? cardCode, DateTime fromMonth, DateTime toMonth, CancellationToken ct) =>
            List<InvoiceRecord>(CardSql.Invoices, new { IdTenant = idTenant, CardCode = cardCode, From = fromMonth, To = toMonth }, ct);
        public Task<InvoiceRecord?> GetInvoiceAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<InvoiceRecord>(CardSql.GetInvoice, new { IdTenant = idTenant, Code = code }, ct);
        public async Task<IReadOnlyList<CardInvoice>> FindInvoicesAsync(int idTenant, int idCard, IReadOnlyCollection<DateTime> referenceMonths, CancellationToken ct) =>
            referenceMonths.Count == 0 ? [] : await List<CardInvoice>(CardSql.FindInvoices, new { IdTenant = idTenant, IdCard = idCard, Months = referenceMonths.ToArray() }, ct);
        public void AddInvoice(CardInvoice invoice, Obligation obligation)
        {
            new DapperRepository<CardInvoice>(work).Add(invoice);
            new DapperRepository<Obligation>(work).Add(obligation);
            work.AddPendingWrite(CardSql.LinkInvoice, new { invoice.IdTenant, InvoiceCode = invoice.Code, ObligationCode = obligation.Code });
        }
        public Task<IReadOnlyList<CardItemRecord>> ListItemsAsync(int idTenant, Guid invoiceCode, CancellationToken ct) =>
            List<CardItemRecord>(CardSql.Items, new { IdTenant = idTenant, InvoiceCode = invoiceCode }, ct);

        public Task<IReadOnlyList<PurchaseRecord>> ListPurchasesAsync(int idTenant, DateTime from, DateTime to, CancellationToken ct) =>
            List<PurchaseRecord>(CardSql.Purchases, new { IdTenant = idTenant, From = from, To = to }, ct);
        public Task<PurchaseRecord?> GetPurchaseAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<PurchaseRecord>(CardSql.GetPurchase, new { IdTenant = idTenant, Code = code }, ct);
        public Task<CardPurchase?> FindPurchaseAsync(int idTenant, Guid code, CancellationToken ct) =>
            Single<CardPurchase>(CardSql.FindPurchase, new { IdTenant = idTenant, Code = code }, ct);
        public Task<IReadOnlyList<PurchaseItemState>> ListPurchaseItemsAsync(int idTenant, int idPurchase, CancellationToken ct) =>
            List<PurchaseItemState>(CardSql.PurchaseItems, new { IdTenant = idTenant, IdPurchase = idPurchase }, ct);
        public void AddPurchase(CardPurchase purchase) => new DapperRepository<CardPurchase>(work).Add(purchase);
        public void UpdatePurchase(CardPurchase purchase) => work.AddPendingWrite(CardSql.UpdatePurchase, purchase);
        public void AddItem(int idTenant, Guid purchaseCode, Guid invoiceCode, string kind, int? number, decimal amount, DateTime competence, string? note, string actor) =>
            work.AddPendingWrite(CardSql.AddItem, new
            {
                Code = Guid.CreateVersion7(), IdTenant = idTenant, PurchaseCode = purchaseCode, InvoiceCode = invoiceCode, Kind = kind, Number = number,
                Amount = amount, Competence = competence, Note = note, Actor = actor
            });
        public void DeactivateItem(int idTenant, Guid itemCode, string actor) =>
            work.AddPendingWrite(CardSql.DeactivateItem, new { IdTenant = idTenant, Code = itemCode, Actor = actor });
        public void RecalculateInvoice(int idTenant, Guid invoiceCode, string actor) =>
            work.AddPendingWrite(CardSql.RecalculateInvoice, new { IdTenant = idTenant, InvoiceCode = invoiceCode, Actor = actor });
    }
}
