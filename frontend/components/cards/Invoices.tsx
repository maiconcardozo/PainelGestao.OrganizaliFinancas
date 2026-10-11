"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, monthLabel, type Account } from "@/lib/finance";
import { invoiceStatus, type Card, type Invoice, type InvoiceDetail, type Purchase } from "@/lib/cards";
import type { Person } from "@/components/households/HouseholdForms";
import { ShoppingBag } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Empty, ModalActions } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { PayModal } from "@/components/expenses/ExpenseList";
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
    {shown.length === 0 ? <Empty title="Nenhuma fatura com compras neste cartão." /> :
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
    {panel === "pay" && <PayModal expense={{ code: invoice.expenseCode, description: `fatura de ${monthLabel(invoice.referenceMonth)}`, openBalance: invoice.openBalance,
      payerPersonCode: null, paymentMethodCode: null }} accounts={accounts} people={people} today={today} onClose={() => setPanel("")} />}
    {panel === "items" && detail && <ul className="entry-list compact">{detail.items.map(item => <li key={item.code} className={item.isActive ? "" : "muted-row"}>
      <span>{item.description}{item.kind === "installment" && item.installmentCount > 1 && ` · ${item.installmentNumber}/${item.installmentCount}`}
        <span className="entry-meta">{item.kind === "credit" ? `Estorno: ${item.note}` : `Comprado em ${civilDate(item.purchaseDate)}`}{item.categoryName && ` · ${item.categoryName}`}{!item.isActive && " · cancelada"}</span></span>
      <span className={`number ${item.kind === "credit" ? "positive" : ""}`}>{money(item.amount)}</span></li>)}</ul>}
  </li>;
}
/** Merchant refund: cancels installments still in open invoices and credits the remainder; there is no bank entry. */
export function Purchases({ purchases, canEdit }: { purchases: Purchase[]; canEdit: boolean }) {
  if (purchases.length === 0) return <Empty title="Nenhuma compra nos últimos meses." />;
  return <ListFrame>{purchases.map(purchase => <PurchaseRow key={`${purchase.code}-${purchase.version}`} purchase={purchase} canEdit={canEdit} />)}</ListFrame>;
}

function PurchaseRow({ purchase, canEdit }: { purchase: Purchase; canEdit: boolean }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  const remaining = Number(purchase.total) - Number(purchase.refunded);
  async function refund(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const amount = String(data.get("amount") ?? "").trim();
    if (await action.send(`card-purchases/${purchase.code}/refunds`, "POST", { amount: amount ? amount.replace(/\./g, "").replace(",", ".") : null, reason: data.get("reason"), date: null },
      "Estorno registrado na fatura.", true)) setOpen(false);
  }
  return <li className={`purchase-item flex flex-wrap items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${remaining === 0 ? "opacity-60" : ""}`}>
    <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><ShoppingBag size={17} /></span>
    <span className="min-w-0 flex-1"><span className="block font-semibold">{purchase.description}{purchase.count > 1 && <span className="font-normal text-[var(--text-secondary)]"> em {purchase.count}x</span>}</span>
      <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(purchase.purchaseDate)} no {purchase.cardName}{purchase.buyerName && ` · ${purchase.buyerName}`}{purchase.categoryName && ` · ${purchase.categoryName}`}
        {Number(purchase.refunded) > 0 && ` · estornado ${money(purchase.refunded)}`}</span></span>
    <span className="num shrink-0 text-sm font-semibold">{money(purchase.total)}</span>
    {canEdit && remaining > 0 && <button type="button" className="secondary-button" onClick={() => setOpen(true)}>Estorno da loja</button>}
    {open && <Modal title="Estorno da loja" onClose={() => setOpen(false)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={refund}>
        <p className="form-hint">{purchase.description} de {money(purchase.total)}: as parcelas ainda abertas são canceladas e o restante vira crédito na fatura. Nenhuma conta é movimentada.</p>
        <label>Valor (vazio = restante)<input name="amount" inputMode="decimal" placeholder={remaining.toFixed(2).replace(".", ",")} /></label>
        <label>Motivo<input name="reason" required maxLength={300} autoComplete="off" /></label>
        <ModalActions busy={action.busy} label="Confirmar estorno" onCancel={() => setOpen(false)} />
      </form>
    </Modal>}
  </li>;
}
