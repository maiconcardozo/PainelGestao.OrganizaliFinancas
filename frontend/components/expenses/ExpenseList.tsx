"use client";
import { useState, type FormEvent } from "react";
import { Ban, Eye, HandCoins } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Empty, ModalActions } from "@/components/common/Ui";
import { civilDate, monthLabel, money, toApiMoney, type Account } from "@/lib/finance";
import { adjustmentKinds, statusLabels, type Expense, type ExpenseDetail } from "@/lib/expenses";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import type { Person } from "@/components/households/HouseholdForms";
import { ShareInputs, type Catalogs } from "./DetailInputs";

type Rights = { canEdit: boolean; canSettle: boolean };
type Props = Rights & { items: Expense[]; accounts: Account[]; catalogs: Catalogs; today: string };

export function ExpenseList({ items, accounts, catalogs, today, canEdit, canSettle }: Props) {
  if (items.length === 0) return <Empty title={canEdit ? "Nenhuma despesa neste mês. Use “Nova despesa” para lançar contas avulsas, recorrentes ou compras parceladas." : "Nenhuma despesa neste mês."} />;
  return <ul className="expense-list">{items.map(item => <ExpenseRow key={`${item.code}-${item.version}-${item.paid}-${item.adjustments}`} expense={item}
    accounts={accounts} catalogs={catalogs} today={today} canEdit={canEdit} canSettle={canSettle} />)}</ul>;
}

const smallButton = "inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-1.5 text-xs font-semibold transition hover:bg-[var(--surface-2)]";

function ExpenseRow({ expense, accounts, catalogs, today, canEdit, canSettle }: Rights & { expense: Expense; accounts: Account[]; catalogs: Catalogs; today: string }) {
  const action = useFinanceAction();
  const [panel, setPanel] = useState<"" | "pay" | "detail" | "cancel">("");
  const [detail, setDetail] = useState<ExpenseDetail | null>(null);
  const open = expense.status === "open" || expense.status === "partial";
  async function showDetail() {
    const loaded = await action.read<ExpenseDetail>(`expenses/${expense.code}`);
    if (loaded) { setDetail(loaded); setPanel("detail"); }
  }
  async function cancel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await action.send(`expenses/${expense.code}/cancel`, "POST", { reason: new FormData(event.currentTarget).get("reason"), version: expense.version }, "Despesa cancelada.")) setPanel("");
  }
  const origin = expense.cardInvoiceCode ? "Fatura do cartão" : expense.installmentNumber ? `Parcela ${expense.installmentNumber}/${expense.installmentCount}` : expense.recurrenceCode ? (expense.isDetached ? "Recorrente, editada" : "Recorrente") : null;
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
        {expense.status === "cancelled" && expense.cancelReason && <span className="entry-meta">Motivo: {expense.cancelReason}</span>}
      </p>
      <div className="row-actions">
        {canSettle && open && accounts.some(account => account.isActive) && <button type="button" onClick={() => setPanel("pay")}
          className="inline-flex items-center gap-1.5 rounded-md bg-[var(--primary)] px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-[var(--primary-strong)]"><HandCoins size={14} />Pagar</button>}
        <button type="button" disabled={action.busy} onClick={() => void showDetail()} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--primary)]`}><Eye size={14} />Detalhes</button>
        {canEdit && expense.status === "open" && !expense.cardInvoiceCode && <button type="button" onClick={() => setPanel("cancel")} className={`${smallButton} text-[var(--text-secondary)] hover:text-[var(--critical)]`}><Ban size={14} />Cancelar despesa</button>}
      </div>
    </div>
    {panel === "" && action.error && <p className="form-error" role="alert">{action.error}</p>}
    {panel === "pay" && <PayModal expense={expense} accounts={accounts} people={catalogs.people} today={today} onClose={() => setPanel("")} />}
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

export type Payable = Pick<Expense, "code" | "description" | "openBalance" | "payerPersonCode" | "paymentMethodCode">;
/** Partial or total payment in a modal; interest, fine and discount are shown separately before confirming. Also pays card invoices. */
export function PayModal({ expense, accounts, people, today, onClose }: { expense: Payable; accounts: Account[]; people: Person[]; today: string; onClose: () => void }) {
  const action = useFinanceAction();
  const [extra, setExtra] = useState({ interest: "", fine: "", discount: "" });
  const charges = Number(toApiMoney(extra.interest) || 0) + Number(toApiMoney(extra.fine) || 0) - Number(toApiMoney(extra.discount) || 0);
  const due = (Number(expense.openBalance) + charges).toFixed(2);
  async function pay(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const optional = (value: string) => value.trim() ? toApiMoney(value) : null;
    const saved = await action.send("payments", "POST", {
      accountCode: data.get("account"), paymentDate: data.get("paymentDate"), payerPersonCode: data.get("payer") || null, paymentMethodCode: expense.paymentMethodCode,
      description: null, adjustmentReason: data.get("reason") || null,
      allocations: [{ expenseCode: expense.code, amount: toApiMoney(data.get("amount")), interest: optional(extra.interest), fine: optional(extra.fine), discount: optional(extra.discount) }],
    }, "Pagamento registrado.", true);
    if (saved) onClose();
  }
  return <Modal title={`Pagar ${expense.description}`} onClose={onClose} error={action.error}>
    <form className="data-form pay-form" onSubmit={pay}>
      <div className="form-row">
        <label>Conta de saída<select name="account" required>{accounts.filter(account => account.isActive).map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
        <label>Data do pagamento<input type="date" name="paymentDate" required defaultValue={today} max={today} /></label>
        <label>Quem pagou<select name="payer" defaultValue={expense.payerPersonCode ?? ""}><option value="">A casa</option>
          {people.filter(person => person.isActive).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
      </div>
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
      <li key={item.paymentCode} className={item.isReversed ? "muted-row" : ""}><span>{civilDate(item.paymentDate)} · {item.accountName}{item.isReversed && " · estornado"}</span>
        <span className="number">{money(item.amount)}</span></li>)}</ul></>}
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
