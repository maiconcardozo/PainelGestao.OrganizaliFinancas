"use client";
import { useState, type FormEvent } from "react";
import Link from "next/link";
import { Ban, CreditCard, Eye, HandCoins, Pencil, RotateCcw, ShoppingBag, Trash2 } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { ConfirmModal } from "@/components/common/ConfirmModal";
import { Attachments } from "@/components/common/Attachments";
import { Empty, ModalActions } from "@/components/common/Ui";
import { civilDate, monthLabel, money, shortMonth, toApiMoney, type Account } from "@/lib/finance";
import type { Card } from "@/lib/cards";
import { adjustmentKinds, paidWith, paymentSource, statusLabels, type Expense, type ExpenseDetail } from "@/lib/expenses";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import type { Person } from "@/components/households/HouseholdForms";
import { ShareInputs, type Catalogs } from "./DetailInputs";
import { InvoiceHint, PaymentSourceSelect, defaultSource } from "./PaymentSource";

type Rights = { canEdit: boolean; canSettle: boolean };
type Props = Rights & { items: Expense[]; accounts: Account[]; cards: Card[]; catalogs: Catalogs; today: string };

export function ExpenseList({ items, accounts, cards, catalogs, today, canEdit, canSettle }: Props) {
  if (items.length === 0) return <Empty title={canEdit ? "Nenhuma despesa neste mês. Use “Nova despesa” para lançar contas avulsas, recorrentes ou compras parceladas." : "Nenhuma despesa neste mês."} />;
  // A19: a card invoice is one line with the total of its purchases; everything else is an obligation of the household.
  return <ul className="expense-list">{items.map(item => item.cardInvoiceCode
    ? <InvoiceRow key={`${item.code}-${item.version}-${item.paid}-${item.adjustments}`} expense={item} accounts={accounts} catalogs={catalogs} today={today} canEdit={canEdit} canSettle={canSettle} />
    : <ExpenseRow key={`${item.code}-${item.version}-${item.paid}-${item.adjustments}`} expense={item}
      accounts={accounts} cards={cards} catalogs={catalogs} today={today} canEdit={canEdit} canSettle={canSettle} />)}</ul>;
}

const smallButton = "inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-1.5 text-xs font-semibold transition hover:bg-[var(--surface-2)]";

function ExpenseRow({ expense, accounts, cards, catalogs, today, canEdit, canSettle }: Rights & { expense: Expense; accounts: Account[]; cards: Card[]; catalogs: Catalogs; today: string }) {
  const action = useFinanceAction();
  const [panel, setPanel] = useState<"" | "pay" | "detail" | "cancel" | "delete" | "reactivate" | "reason">("");
  const [detail, setDetail] = useState<ExpenseDetail | null>(null);
  const open = expense.status === "open" || expense.status === "partial";
  // A18: only an entry nobody paid can leave for good; a paid one keeps the cash history until its payment is reversed.
  const removable = canEdit && (expense.status === "open" || expense.status === "cancelled");
  const paidNotice = canEdit && (expense.status === "paid" || expense.status === "partial");
  async function remove() {
    if (await action.send(`expenses/${expense.code}`, "DELETE", undefined, "Despesa excluída.")) setPanel("");
  }
  // A20: a cancellation made by mistake is undone, and the reason written at cancellation can be corrected.
  async function reactivate() {
    if (await action.send(`expenses/${expense.code}/reactivate`, "POST", { version: expense.version }, "Cancelamento desfeito. A despesa voltou a contar no mês.")) setPanel("");
  }
  async function editReason(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await action.send(`expenses/${expense.code}/cancel-reason`, "PUT", { reason: new FormData(event.currentTarget).get("reason"), version: expense.version }, "Motivo atualizado.")) setPanel("");
  }
  async function showDetail() {
    const loaded = await action.read<ExpenseDetail>(`expenses/${expense.code}`);
    if (loaded) { setDetail(loaded); setPanel("detail"); }
  }
  async function cancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await action.send(`expenses/${expense.code}/cancel`, "POST", { reason: new FormData(event.currentTarget).get("reason"), version: expense.version }, "Despesa cancelada.")) setPanel("");
  }
  const origin = expense.installmentNumber ? `Parcela ${expense.installmentNumber}/${expense.installmentCount}` : expense.recurrenceCode ? (expense.isDetached ? "Recorrente, editada" : "Recorrente") : null;
  return <li className={`expense-row status-${expense.status}${expense.isOverdue ? " is-overdue" : ""}`}>
    <div className="expense-main">
      <div><strong>{expense.description}</strong>
        <span className="entry-meta">Vence {civilDate(expense.dueDate)} · competência {monthLabel(expense.competence)}
          {expense.categoryName && ` · ${expense.categoryName}`}{expense.creditorName && ` · ${expense.creditorName}`}{origin && ` · ${origin}`}</span>
        {expense.shares.length > 0 && <span className="entry-meta">Responsáveis: {expense.shares.map(share => `${share.personName} ${money(share.amount)}`).join(", ")}</span>}
      </div>
      <div className="number"><strong>{money(expense.status === "paid" || expense.status === "cancelled" ? expense.amount : expense.openBalance)}</strong>
        {expense.status === "partial" && <span className="entry-meta">em aberto de {money((Number(expense.amount) + Number(expense.adjustments)).toFixed(2))}</span>}</div>
    </div>
    <div className="flex flex-wrap items-center justify-between gap-2">
      <p className="status-line">
        <span className={`status-tag ${expense.isOverdue ? "overdue" : expense.status}`}>{expense.isOverdue ? "Vencida" : statusLabels[expense.status]}</span>
        {expense.isEstimated && open && <span className="status-tag estimate">Estimada</span>}
        {/* A21: paid on a credit card, the expense stays here as history; the amount counts once, inside the invoice line of that month. */}
        {expense.cardPayment ? <span className="entry-meta" title="Já está dentro do total da fatura; não soma de novo no mês">
          <CreditCard size={12} className="mr-1 inline" aria-hidden="true" />Paga · cartão {expense.cardPayment.cardName} · fatura {shortMonth(expense.cardPayment.invoiceMonth)}</span>
          : expense.status === "paid" && expense.paidOn && <span className="entry-meta">Paga em {civilDate(expense.paidOn)}</span>}
        {expense.status === "cancelled" && expense.cancelReason && <span className="entry-meta">Motivo: {expense.cancelReason}
          {canEdit && <button type="button" onClick={() => setPanel("reason")} aria-label="Corrigir o motivo do cancelamento" title="Corrigir motivo"
            className="ml-1 inline-flex rounded p-0.5 align-middle text-[var(--text-secondary)] hover:text-[var(--primary)]"><Pencil size={12} /></button>}</span>}
      </p>
      <div className="row-actions">
        {canSettle && open && (accounts.some(account => account.isActive) || cards.length > 0) && <button type="button" onClick={() => setPanel("pay")}
          className="inline-flex items-center gap-1.5 rounded-md bg-[var(--primary)] px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-[var(--primary-strong)]"><HandCoins size={14} />Pagar</button>}
        <button type="button" disabled={action.busy} onClick={() => void showDetail()} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--primary)]`}><Eye size={14} />Detalhes</button>
        {canEdit && expense.status === "open" && <button type="button" onClick={() => setPanel("cancel")} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--critical)]`}><Ban size={14} />Cancelar despesa</button>}
        {canEdit && expense.status === "cancelled" && <button type="button" onClick={() => setPanel("reactivate")} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--primary)]`}><RotateCcw size={14} />Desfazer cancelamento</button>}
        {(removable || paidNotice) && <button type="button" onClick={() => setPanel("delete")} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--critical)]`}><Trash2 size={14} />Excluir</button>}
      </div>
    </div>
    {panel === "reactivate" && <ConfirmModal title={`Desfazer o cancelamento de ${expense.description}`} label="Reativar despesa" busy={action.busy} error={action.error} onConfirm={() => void reactivate()} onClose={() => setPanel("")}>
      <p className="text-sm">{expense.description} de {money(expense.amount)}, vencimento {civilDate(expense.dueDate)}, volta a ficar em aberto e a contar no mês e no orçamento, com os mesmos valores e responsáveis.</p>
    </ConfirmModal>}
    {panel === "reason" && <Modal title="Corrigir o motivo do cancelamento" onClose={() => setPanel("")} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={editReason}>
        <label>Motivo<input name="reason" required maxLength={300} autoComplete="off" defaultValue={expense.cancelReason ?? ""} /></label>
        <ModalActions busy={action.busy} label="Salvar motivo" onCancel={() => setPanel("")} />
      </form>
    </Modal>}
    {panel === "delete" && paidNotice && <Modal title={`Excluir ${expense.description}`} onClose={() => setPanel("")} width="max-w-md">
      <p className="text-sm">Esta despesa já tem pagamento registrado ({money(expense.paid)}). Para excluir, primeiro estorne o pagamento em <strong>Pagamentos recentes</strong>, no fim desta página:
        o dinheiro volta para a conta e a despesa reabre. Depois o botão Excluir apaga a despesa de vez.</p>
    </Modal>}
    {panel === "delete" && removable && <ConfirmModal title={`Excluir ${expense.description}`} label="Excluir despesa" busy={action.busy} error={action.error} onConfirm={() => void remove()} onClose={() => setPanel("")}>
      <p className="text-sm">{expense.description} de {money(expense.amount)}, vencimento {civilDate(expense.dueDate)}, some de vez: não fica no histórico nem conta em mês nenhum.
        {expense.recurrenceCode ? " A recorrência continua; só esta ocorrência deixa de existir e não é recriada." : expense.installmentNumber ? " As outras parcelas continuam." : ""}
        {" "}Se preferir manter o registro com um motivo, use “Cancelar despesa”.</p>
    </ConfirmModal>}
    {panel === "" && action.error && <p className="form-error" role="alert">{action.error}</p>}
    {panel === "pay" && <PayModal expense={expense} accounts={accounts} cards={cards} people={catalogs.people} today={today} onClose={() => setPanel("")} />}
    {panel === "cancel" && <Modal title={`Cancelar ${expense.description}`} onClose={() => setPanel("")} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={cancel}>
        <p className="form-hint">A despesa deixa de contar no mês e no orçamento. O registro fica no histórico com o motivo.</p>
        <label>Por que cancelar?<input name="reason" required maxLength={300} autoComplete="off" /></label>
        <ModalActions busy={action.busy} label="Confirmar cancelamento" onCancel={() => setPanel("")} />
      </form>
    </Modal>}
    {panel === "detail" && detail && <Modal title={expense.description} onClose={() => setPanel("")} width="max-w-3xl">
      <DetailPanel detail={detail} catalogs={catalogs} canEdit={canEdit} canSettle={canSettle} />
    </Modal>}
  </li>;
}

/**
 * A19: one line per card invoice due in the month, with the total of the card's purchases. It is paid as one (bank account only) and
 * "Ver compras" opens the invoice in Cartões, where the purchases are entered, edited, refunded or removed.
 */
function InvoiceRow({ expense, accounts, catalogs, today, canEdit, canSettle }: Rights & { expense: Expense; accounts: Account[]; catalogs: Catalogs; today: string }) {
  const action = useFinanceAction();
  const [panel, setPanel] = useState<"" | "pay" | "detail">("");
  const [detail, setDetail] = useState<ExpenseDetail | null>(null);
  const open = expense.status === "open" || expense.status === "partial";
  const count = expense.cardItemCount ?? 0;
  async function showDetail() {
    const loaded = await action.read<ExpenseDetail>(`expenses/${expense.code}`);
    if (loaded) { setDetail(loaded); setPanel("detail"); }
  }
  return <li className={`expense-row status-${expense.status}${expense.isOverdue ? " is-overdue" : ""}`}>
    <div className="expense-main">
      <div><strong>{expense.description}</strong>
        <span className="entry-meta"><CreditCard size={12} className="mr-1 inline" aria-hidden="true" />Vence {civilDate(expense.dueDate)} · {count} {count === 1 ? "compra" : "compras"} no cartão{" "}
          {expense.cardName} · competência {monthLabel(expense.competence)}</span>
      </div>
      <div className="number"><strong>{money(expense.status === "paid" ? expense.amount : expense.openBalance)}</strong>
        {expense.status === "partial" && <span className="entry-meta">em aberto de {money((Number(expense.amount) + Number(expense.adjustments)).toFixed(2))}</span>}</div>
    </div>
    <div className="flex flex-wrap items-center justify-between gap-2">
      <p className="status-line">
        <span className={`status-tag ${expense.isOverdue ? "overdue" : expense.status}`}>{expense.isOverdue ? "Vencida" : statusLabels[expense.status]}</span>
        {expense.status === "paid" && expense.paidOn && <span className="entry-meta">Paga em {civilDate(expense.paidOn)}</span>}
      </p>
      <div className="row-actions">
        {canSettle && open && accounts.some(account => account.isActive) && <button type="button" onClick={() => setPanel("pay")}
          className="inline-flex items-center gap-1.5 rounded-md bg-[var(--primary)] px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-[var(--primary-strong)]"><HandCoins size={14} />Pagar fatura</button>}
        <Link href={`/cards?invoice=${expense.cardInvoiceCode}`} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--primary)]`}><ShoppingBag size={14} />Ver compras</Link>
        <button type="button" disabled={action.busy} onClick={() => void showDetail()} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--primary)]`}><Eye size={14} />Detalhes</button>
      </div>
    </div>
    {panel === "" && action.error && <p className="form-error" role="alert">{action.error}</p>}
    {panel === "pay" && <PayModal expense={expense} accounts={accounts} people={catalogs.people} today={today} onClose={() => setPanel("")} />}
    {panel === "detail" && detail && <Modal title={expense.description} onClose={() => setPanel("")} width="max-w-3xl">
      <DetailPanel detail={detail} catalogs={catalogs} canEdit={canEdit} canSettle={canSettle} />
    </Modal>}
  </li>;
}

export type Payable = Pick<Expense, "code" | "description" | "openBalance" | "payerPersonCode" | "paymentMethodCode">;
/**
 * Partial or total payment in a modal; interest, fine and discount are shown separately before confirming. Also pays card invoices.
 * With `cards`, the payment can be made on a credit card: nothing leaves an account and the amount enters the card's invoice (A17).
 */
export function PayModal({ expense, accounts, cards = [], people, today, onClose }: { expense: Payable; accounts: Account[]; cards?: Card[]; people: Person[]; today: string; onClose: () => void }) {
  const action = useFinanceAction();
  const [extra, setExtra] = useState({ interest: "", fine: "", discount: "" });
  const [source, setSource] = useState(() => defaultSource(accounts, cards));
  const [date, setDate] = useState(today);
  const charges = Number(toApiMoney(extra.interest) || 0) + Number(toApiMoney(extra.fine) || 0) - Number(toApiMoney(extra.discount) || 0);
  const due = (Number(expense.openBalance) + charges).toFixed(2);
  async function pay(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const optional = (value: string) => value.trim() ? toApiMoney(value) : null;
    const saved = await action.send("payments", "POST", {
      ...paymentSource(data.get("source")), paymentDate: data.get("paymentDate"), payerPersonCode: data.get("payer") || null, paymentMethodCode: expense.paymentMethodCode,
      description: null, adjustmentReason: data.get("reason") || null,
      allocations: [{ expenseCode: expense.code, amount: toApiMoney(data.get("amount")), interest: optional(extra.interest), fine: optional(extra.fine), discount: optional(extra.discount) }],
    }, "Pagamento registrado.", true);
    if (saved) onClose();
  }
  return <Modal title={`Pagar ${expense.description}`} onClose={onClose} error={action.error}>
    <form className="data-form pay-form" onSubmit={pay}>
      <div className="form-row">
        <PaymentSourceSelect accounts={accounts} cards={cards} value={source} onChange={setSource} />
        <label>Data do pagamento<input type="date" name="paymentDate" required value={date} max={today} onChange={event => setDate(event.target.value)} /></label>
        <label>Quem pagou<select name="payer" defaultValue={expense.payerPersonCode ?? ""}><option value="">A casa</option>
          {people.filter(person => person.isActive).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
      </div>
      <InvoiceHint source={source} date={date} />
      <div className="form-row">
        {(["interest", "fine", "discount"] as const).map(kind => <label key={kind}>{adjustmentKinds[kind]} (opcional)
          <input inputMode="decimal" value={extra[kind]} onChange={event => setExtra({ ...extra, [kind]: event.target.value })} /></label>)}
      </div>
      {charges !== 0 && <label>Justificativa dos encargos ou desconto<input name="reason" required maxLength={300} placeholder="Ex.: pago com 3 dias de atraso" /></label>}
      <label>Valor pago agora<input name="amount" required inputMode="decimal" key={due} defaultValue={due.replace(".", ",")} /></label>
      <p className="form-hint">Saldo em aberto {money(expense.openBalance)}{charges !== 0 && ` ${charges > 0 ? "+" : "−"} ${money(Math.abs(charges).toFixed(2))} = ${money(due)}`}. Pagar menos deixa o restante em aberto.</p>
      <ModalActions busy={action.busy} label="Confirmar pagamento" onCancel={onClose} />
    </form>
  </Modal>;
}

function DetailPanel({ detail, catalogs, canEdit, canSettle }: { detail: ExpenseDetail; catalogs: Catalogs; canEdit: boolean; canSettle: boolean }) {
  const action = useFinanceAction();
  const expense = detail.expense;
  const editable = canEdit && !expense.cardInvoiceCode && (expense.status === "open" || expense.status === "partial");
  async function edit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const split = data.has("splitEnabled");
    const mode = String(data.get("splitMode") ?? "percent");
    const shares = catalogs.people.map(person => ({ personCode: person.code, raw: String(data.get(`share_${person.code}`) ?? "").trim() })).filter(item => item.raw)
      .map(item => ({ personCode: item.personCode, value: mode === "percent" ? item.raw.replace(",", ".") : toApiMoney(item.raw) }));
    await action.send(`expenses/${expense.code}`, "PUT", {
      details: { description: data.get("description"), expenseTypeCode: expense.expenseTypeCode, expenseTypeVersion: expense.expenseTypeVersion,
        categoryCode: data.get("category") || null, creditorCode: expense.creditorCode, paymentMethodCode: expense.paymentMethodCode, payerPersonCode: expense.payerPersonCode,
        fields: expense.expenseTypeCode ? expense.fields : null, splitMode: split && shares.length ? mode : null, shares: split && shares.length ? shares : null },
      competence: `${data.get("competence")}-01`, dueDate: data.get("dueDate"), amount: toApiMoney(data.get("amount")), version: expense.version, payment: null,
    }, expense.recurrenceCode ? "Esta ocorrência foi alterada; as próximas continuam seguindo a recorrência." : "Despesa atualizada.");
  }
  async function adjust(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    await action.send(`expenses/${expense.code}/adjustments`, "POST", { kind: data.get("kind"), amount: toApiMoney(data.get("amount")), reason: data.get("reason"), date: null }, "Ajuste registrado.");
  }
  const initialShares = Object.fromEntries(expense.shares.map(share => [share.personCode, (expense.splitMode === "percent" ? share.percent : share.amount)?.replace(".", ",") ?? ""]));
  return <div className="expense-detail">
    <dl className="statement-totals">
      <div><dt>Valor original</dt><dd>{money(expense.amount)}</dd></div>
      <div><dt>Juros, multa e ajustes</dt><dd>{money(expense.adjustments)}</dd></div>
      <div><dt>Pago</dt><dd>{money(expense.paid)}</dd></div>
      <div><dt>Em aberto</dt><dd>{money(expense.openBalance)}</dd></div>
    </dl>
    {detail.adjustments.length > 0 && <><h3>Encargos e descontos</h3><ul className="entry-list compact">{detail.adjustments.map(item =>
      <li key={item.code}><span>{adjustmentKinds[item.kind]} · {civilDate(item.date)} · {item.reason}</span><span className="number">{money(item.amount)}</span></li>)}</ul></>}
    {detail.payments.length > 0 && <><h3>Pagamentos</h3><ul className="entry-list compact">{detail.payments.map(item =>
      <li key={item.paymentCode} className={item.isReversed ? "muted-row" : ""}><span>{civilDate(item.paymentDate)} · {paidWith(item)}{item.isReversed && " · estornado"}</span>
        <span className="number">{money(item.amount)}</span></li>)}</ul></>}
    <Attachments kind="expense" target={expense.code} canEdit={canEdit} />
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    {editable && <form className="data-form nested-form" onSubmit={edit}><h3>{expense.recurrenceCode ? "Alterar somente esta ocorrência" : "Alterar despesa"}</h3>
      <div className="form-row">
        <label>Descrição<input name="description" required maxLength={200} defaultValue={expense.description} /></label>
        <label>Categoria<select name="category" defaultValue={expense.categoryCode ?? ""}><option value="">Sem categoria</option>
          {catalogs.categories.filter(item => item.isActive || item.code === expense.categoryCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>Valor{expense.isEstimated ? " (confirme o valor real)" : ""}<input name="amount" required inputMode="decimal" defaultValue={expense.amount.replace(".", ",")} readOnly={Number(expense.paid) > 0} /></label>
        <label>Vencimento<input type="date" name="dueDate" required defaultValue={expense.dueDate} /></label>
        <label>Competência<input type="month" name="competence" required defaultValue={expense.competence} /></label>
      </div>
      {Number(expense.paid) > 0 && <p className="form-hint">Já existe pagamento: o valor original fica travado. Use juros, multa, desconto ou ajuste abaixo.</p>}
      <ShareInputs people={catalogs.people} percentOnly={false} initialMode={expense.splitMode} initial={initialShares} />
      <div><button className="primary-button" disabled={action.busy}>Salvar alteração</button></div>
    </form>}
    {canSettle && (expense.status === "open" || expense.status === "partial") && <form className="data-form nested-form" onSubmit={adjust}><h3>Registrar encargo, desconto ou ajuste</h3>
      <div className="form-row">
        <label>Tipo<select name="kind" defaultValue="interest">{Object.entries(adjustmentKinds).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
        <label>Valor<input name="amount" required inputMode="decimal" placeholder="10,00" /></label>
        <label>Justificativa<input name="reason" required maxLength={300} /></label>
      </div>
      <p className="form-hint">Desconto reduz o saldo; ajuste aceita valor negativo (ex.: -5,00). Ajustes mudam o saldo da despesa, não o da conta.</p>
      <div><button className="secondary-button" disabled={action.busy}>Registrar ajuste</button></div>
    </form>}
  </div>;
}
