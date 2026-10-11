"use client";
import { useState, type FormEvent } from "react";
import { civilDate, frequencies, money, toApiMoney, type Account } from "@/lib/finance";
import { changeLabels, type InstallmentPreview, type RecurrencePlan } from "@/lib/expenses";
import { Modal } from "@/components/common/Modal";
import { ModalActions } from "@/components/common/Ui";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { DetailInputs, ShareInputs, detailsFrom, type Catalogs } from "./DetailInputs";

type Kind = "single" | "recurring" | "installments";
const kinds: Record<Kind, string> = { single: "Conta avulsa", recurring: "Conta recorrente", installments: "Compra parcelada" };

type Props = { catalogs: Catalogs; accounts: Account[]; today: string; canSettle: boolean; onClose: () => void; onSaved: (message: string) => void };

/** "Nova despesa": the three ways of entering a bill, in one modal with a segmented switch. */
export function ExpenseModal({ catalogs, accounts, today, canSettle, onClose, onSaved }: Props) {
  const action = useFinanceAction();
  const [kind, setKind] = useState<Kind>("single");
  const [typeCode, setTypeCode] = useState("");
  const [paid, setPaid] = useState(false);
  const [estimated, setEstimated] = useState(false);
  const [installments, setInstallments] = useState<InstallmentPreview | null>(null);
  const [recurrence, setRecurrence] = useState<RecurrencePlan | null>(null);
  const activeAccounts = accounts.filter(account => account.isActive);
  function choose(next: Kind) { setKind(next); setTypeCode(""); setInstallments(null); setRecurrence(null); }

  async function preview(event: React.MouseEvent<HTMLButtonElement>) {
    const data = new FormData(event.currentTarget.form!);
    if (kind === "installments")
      setInstallments(await action.send<InstallmentPreview>("expense-installments/preview", "POST", {
        total: toApiMoney(data.get("total")), count: Number(data.get("count")), firstDueDate: data.get("firstDueDate"), frequency: data.get("frequency") }, "Prévia pronta. Nada foi gravado."));
    else
      setRecurrence(await action.send<RecurrencePlan>("expense-recurrences/preview", "POST", {
        validFrom: data.get("validFrom"), endDate: data.get("endDate") || null, frequency: data.get("frequency"), amount: toApiMoney(data.get("amount")),
        isEstimated: estimated, isActive: true }, "Prévia pronta. Nada foi gravado."));
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const details = detailsFrom(data, catalogs, typeCode);
    let saved: unknown;
    const success = kind === "single" ? (paid ? "Despesa lançada e paga." : "Despesa lançada.")
      : kind === "recurring" ? "Conta recorrente criada; os próximos vencimentos já aparecem na lista." : "Compra parcelada lançada; cada parcela aparece no mês do seu vencimento.";
    if (kind === "single")
      saved = await action.send("expenses", "POST", {
        details, competence: data.get("competence") ? `${data.get("competence")}-01` : null, dueDate: data.get("dueDate"), amount: toApiMoney(data.get("amount")), version: 0,
        payment: paid ? { accountCode: data.get("account"), paymentDate: data.get("paymentDate"), payerPersonCode: data.get("payer") || null } : null,
      }, paid ? "Despesa lançada e paga." : "Despesa lançada.", true);
    else if (kind === "recurring")
      saved = await action.send("expense-recurrences", "POST", {
        details, version: 0, terms: { validFrom: data.get("validFrom"), endDate: data.get("endDate") || null, frequency: data.get("frequency"),
          amount: toApiMoney(data.get("amount")), isEstimated: estimated, isActive: true } }, "Conta recorrente criada; os próximos vencimentos já aparecem na lista.", true);
    else
      saved = await action.send("expense-installments", "POST", {
        details, plan: { total: toApiMoney(data.get("total")), count: Number(data.get("count")), firstDueDate: data.get("firstDueDate"), frequency: data.get("frequency") },
      }, "Compra parcelada lançada; cada parcela aparece no mês do seu vencimento.", true);
    if (saved) onSaved(success);
  }

  return <Modal title="Nova despesa" onClose={onClose} error={action.error} width="max-w-3xl"><form className="data-form" onSubmit={save} key={kind}>
    <div><div className="segmented" role="tablist" aria-label="Tipo de lançamento">{(Object.keys(kinds) as Kind[]).map(item =>
      <button key={item} type="button" role="tab" aria-selected={kind === item} onClick={() => choose(item)}>{kinds[item]}</button>)}</div></div>
    <DetailInputs catalogs={catalogs} behavior={kind} typeCode={typeCode} onType={setTypeCode} />

    {kind === "single" && <>
      <div className="form-row">
        <label>Valor<input name="amount" required inputMode="decimal" placeholder="250,00" /></label>
        <label>Vencimento<input type="date" name="dueDate" required defaultValue={today} /></label>
        <label>Competência (opcional)<input type="month" name="competence" placeholder="mês do vencimento" /></label>
      </div>
      {canSettle && activeAccounts.length > 0 && <fieldset className="choice-group"><legend>Pagamento</legend>
        <label className="checkbox-label"><input type="checkbox" checked={paid} onChange={event => setPaid(event.target.checked)} />Já foi paga</label>
        {paid && <div className="form-row">
          <label>Conta de saída<select name="account" required>{activeAccounts.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
          <label>Data do pagamento<input type="date" name="paymentDate" required defaultValue={today} max={today} /></label>
        </div>}
      </fieldset>}
    </>}

    {kind === "recurring" && <>
      <div className="form-row">
        <label>Valor<input name="amount" required inputMode="decimal" placeholder="120,00" /></label>
        <label>Periodicidade<select name="frequency" defaultValue="monthly">{Object.entries(frequencies).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      </div>
      <label className="checkbox-label"><input type="checkbox" checked={estimated} onChange={event => setEstimated(event.target.checked)} />Valor estimado (confirmo quando a conta chegar, ex.: água e luz)</label>
      <div className="form-row">
        <label>Primeiro vencimento<input type="date" name="validFrom" required defaultValue={today} /></label>
        <label>Último vencimento (opcional)<input type="date" name="endDate" /></label>
      </div>
      <button type="button" className="secondary-button" disabled={action.busy} onClick={preview}>Ver próximos vencimentos</button>
      {recurrence && <ul className="entry-list compact" aria-label="Prévia dos vencimentos">{recurrence.next.map(item =>
        <li key={item.date}><span>{civilDate(item.date)} · {changeLabels[item.change]}</span><span className="number">{money(item.amount)}</span></li>)}</ul>}
    </>}

    {kind === "installments" && <>
      <div className="form-row">
        <label>Valor total<input name="total" required inputMode="decimal" placeholder="1.200,00" /></label>
        <label>Parcelas<input type="number" name="count" required min={2} max={120} defaultValue={3} /></label>
      </div>
      <div className="form-row">
        <label>Primeiro vencimento<input type="date" name="firstDueDate" required defaultValue={today} /></label>
        <label>Intervalo<select name="frequency" defaultValue="monthly">{Object.entries(frequencies).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      </div>
      <button type="button" className="secondary-button" disabled={action.busy} onClick={preview}>Ver parcelas</button>
      {installments && <div className="table-scroll"><table className="finance-table"><caption>{installments.count} parcelas somando {money(installments.total)}</caption>
        <thead><tr><th scope="col">Parcela</th><th scope="col">Vencimento</th><th scope="col" className="number">Valor</th></tr></thead>
        <tbody>{installments.items.map(item => <tr key={item.number}><td>{item.number}/{installments.count}</td><td>{civilDate(item.dueDate)}</td>
          <td className="number">{money(item.amount)}</td></tr>)}</tbody></table></div>}
    </>}

    <ShareInputs key={`${kind}-shares`} people={catalogs.people} percentOnly={kind !== "single"} />
    <ModalActions busy={action.busy} label={kind === "single" ? "Lançar despesa" : kind === "recurring" ? "Criar conta recorrente" : "Lançar compra parcelada"} onCancel={onClose} />
  </form></Modal>;
}
