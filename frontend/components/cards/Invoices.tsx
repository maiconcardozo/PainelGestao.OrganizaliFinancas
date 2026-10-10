"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, monthLabel, type Account } from "@/lib/finance";
import { invoiceStatus, type Card, type Invoice, type InvoiceDetail, type Purchase } from "@/lib/cards";
import type { Person } from "@/components/households/HouseholdForms";
import { PayForm } from "@/components/expenses/ExpenseList";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

type Props = { cards: Card[]; invoices: Invoice[]; accounts: Account[]; people: Person[]; today: string; canSettle: boolean };

/** RF25: one row per invoice; paying uses the invoice's own obligation, so cash leaves the account only once. */
export function Invoices({ cards, invoices, accounts, people, today, canSettle }: Props) {
  const [chosen, setCardCode] = useState("");
  // A card created after the first render must still be selectable, so the default is derived on every render.
  const cardCode = cards.some(card => card.code === chosen) ? chosen : cards.find(card => card.isActive)?.code ?? cards[0]?.code ?? "";
  const shown = invoices.filter(invoice => invoice.cardCode === cardCode && (invoice.itemCount > 0 || invoice.paid !== "0.00"));
  if (cards.length === 0) return null;
  return <>
    {cards.length > 1 && <label className="inline-select">Cartão<select value={cardCode} onChange={event => setCardCode(event.target.value)}>
      {cards.map(card => <option key={card.code} value={card.code}>{card.name}</option>)}</select></label>}
    {shown.length === 0 ? <p className="form-hint">Nenhuma fatura com compras neste cartão.</p> :
      <ul className="expense-list">{shown.map(invoice => <InvoiceRow key={`${invoice.code}-${invoice.amount}-${invoice.paid}`} invoice={invoice} accounts={accounts}
        people={people} today={today} canSettle={canSettle} />)}</ul>}
  </>;
}

function InvoiceRow({ invoice, accounts, people, today, canSettle }: Omit<Props, "cards" | "invoices"> & { invoice: Invoice }) {
  const action = useFinanceAction();
  const [panel, setPanel] = useState<"" | "items" | "pay">("");
  const [detail, setDetail] = useState<InvoiceDetail | null>(null);
  async function toggleItems() {
    if (panel === "items") { setPanel(""); return; }
    const loaded = await action.read<InvoiceDetail>(`card-invoices/${invoice.code}`);
    if (loaded) { setDetail(loaded); setPanel("items"); }
  }
  const credit = Number(invoice.openBalance) < 0;
  const statusClass = invoice.isOverdue ? "overdue" : invoice.status === "paid" ? "paid" : invoice.status === "partial" ? "partial" : "";
  return <li className={`expense-row ${invoice.isOverdue ? "is-overdue" : invoice.status === "paid" ? "status-paid" : invoice.status === "partial" ? "status-partial" : ""}`}>
    <div className="expense-main">
      <div><strong>Fatura de {monthLabel(invoice.referenceMonth)}</strong>
        <span className="entry-meta">Fecha {civilDate(invoice.closingDate)} · vence {civilDate(invoice.dueDate)} · {invoice.itemCount} lançamento(s)</span></div>
      <div className="number"><strong>{money(invoice.amount)}</strong>
        <span className="entry-meta">{credit ? `crédito de ${money(String(-Number(invoice.openBalance)))}` : invoice.paid !== "0.00" ? `em aberto ${money(invoice.openBalance)}` : ""}</span></div>
    </div>
    <p className="status-line"><span className={`status-tag ${statusClass}`}>{invoice.isOverdue ? "Vencida" : invoiceStatus[invoice.status]}</span></p>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    <div className="row-actions">
      {canSettle && Number(invoice.openBalance) > 0 && accounts.some(account => account.isActive) &&
        <button type="button" className="secondary-button" aria-expanded={panel === "pay"} onClick={() => setPanel(panel === "pay" ? "" : "pay")}>Pagar fatura</button>}
      <button type="button" className="secondary-button" aria-expanded={panel === "items"} disabled={action.busy} onClick={toggleItems}>Ver compras</button>
    </div>
    {panel === "pay" && <PayForm expense={{ code: invoice.expenseCode, description: `fatura de ${monthLabel(invoice.referenceMonth)}`, openBalance: invoice.openBalance,
      payerPersonCode: null, paymentMethodCode: null }} accounts={accounts} people={people} today={today} />}
    {panel === "items" && detail && <ul className="entry-list compact">{detail.items.map(item => <li key={item.code} className={item.isActive ? "" : "muted-row"}>
      <span>{item.description}{item.kind === "installment" && item.installmentCount > 1 && ` · ${item.installmentNumber}/${item.installmentCount}`}
        <span className="entry-meta">{item.kind === "credit" ? `Estorno: ${item.note}` : `Comprado em ${civilDate(item.purchaseDate)}`}{item.categoryName && ` · ${item.categoryName}`}{!item.isActive && " · cancelada"}</span></span>
      <span className={`number ${item.kind === "credit" ? "positive" : ""}`}>{money(item.amount)}</span></li>)}</ul>}
  </li>;
}

/** Merchant refund: cancels installments still in open invoices and credits the remainder; there is no bank entry. */
export function Purchases({ purchases, canEdit }: { purchases: Purchase[]; canEdit: boolean }) {
  if (purchases.length === 0) return <p className="form-hint">Nenhuma compra nos últimos meses.</p>;
  return <ul className="entry-list">{purchases.map(purchase => <PurchaseRow key={`${purchase.code}-${purchase.version}`} purchase={purchase} canEdit={canEdit} />)}</ul>;
}

function PurchaseRow({ purchase, canEdit }: { purchase: Purchase; canEdit: boolean }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  const remaining = Number(purchase.total) - Number(purchase.refunded);
  async function refund(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const amount = String(data.get("amount") ?? "").trim();
    await action.send(`card-purchases/${purchase.code}/refunds`, "POST", { amount: amount ? amount.replace(/\./g, "").replace(",", ".") : null, reason: data.get("reason"), date: null },
      "Estorno registrado na fatura.", true);
  }
  return <li className={remaining === 0 ? "muted-row" : ""}>
    <div><strong>{money(purchase.total)}</strong> · {purchase.description}{purchase.count > 1 && ` em ${purchase.count}x`}
      <span className="entry-meta">{civilDate(purchase.purchaseDate)} no {purchase.cardName}{purchase.buyerName && ` · ${purchase.buyerName}`}{purchase.categoryName && ` · ${purchase.categoryName}`}</span>
      {Number(purchase.refunded) > 0 && <span className="entry-meta">Estornado {money(purchase.refunded)}</span>}</div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {canEdit && remaining > 0 && (open
      ? <form className="inline-form" onSubmit={refund}><label>Valor (vazio = restante)<input name="amount" inputMode="decimal" placeholder={remaining.toFixed(2).replace(".", ",")} /></label>
        <label>Motivo<input name="reason" required maxLength={300} /></label><button className="secondary-button" disabled={action.busy}>Confirmar estorno</button></form>
      : <button type="button" className="secondary-button" onClick={() => setOpen(true)}>Estorno da loja</button>)}
  </li>;
}
