"use client";
import { useState, type FormEvent } from "react";
import { civilDate, frequencies, money, toApiMoney } from "@/lib/finance";
import { changeLabels, type Recurrence, type RecurrencePlan } from "@/lib/expenses";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export function Recurrences({ recurrences, canEdit, today }: { recurrences: Recurrence[]; canEdit: boolean; today: string }) {
  const action = useFinanceAction();
  if (recurrences.length === 0) return <p className="form-hint">Nenhuma conta recorrente. Contas como aluguel, água e internet podem ser lançadas uma vez como recorrentes.</p>;
  return <>
    {canEdit && <div className="row-actions"><button type="button" className="secondary-button" disabled={action.busy}
      onClick={() => void action.send("expense-recurrences/generate", "POST", undefined, "Vencimentos atualizados até o mês atual + 12.")}>Atualizar vencimentos</button>
      {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}</div>}
    <ul className="entry-list">{recurrences.map(item => <RecurrenceRow key={`${item.code}-${item.version}`} recurrence={item} canEdit={canEdit} today={today} />)}</ul>
  </>;
}

/** "This and next": the change starts at the chosen date; paid, adjusted or individually edited occurrences are preserved. */
function RecurrenceRow({ recurrence, canEdit, today }: { recurrence: Recurrence; canEdit: boolean; today: string }) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState(false);
  const [plan, setPlan] = useState<RecurrencePlan | null>(null);
  const start = recurrence.validFrom > today ? recurrence.validFrom : today;
  function terms(data: FormData) {
    return { validFrom: data.get("validFrom"), endDate: data.get("endDate") || null, frequency: data.get("frequency"), amount: toApiMoney(data.get("amount")),
      isEstimated: data.has("isEstimated"), isActive: data.has("isActive") };
  }
  async function preview(event: React.MouseEvent<HTMLButtonElement>) {
    setPlan(await action.send<RecurrencePlan>(`expense-recurrences/preview?code=${recurrence.code}`, "POST", terms(new FormData(event.currentTarget.form!)), "Prévia pronta. Nada foi gravado."));
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(`expense-recurrences/${recurrence.code}`, "PUT", {
      details: { description: data.get("description"), expenseTypeCode: recurrence.expenseTypeCode, expenseTypeVersion: recurrence.expenseTypeVersion,
        categoryCode: recurrence.categoryCode, creditorCode: recurrence.creditorCode, paymentMethodCode: recurrence.paymentMethodCode, payerPersonCode: recurrence.payerPersonCode,
        fields: recurrence.expenseTypeCode ? recurrence.fields : null, splitMode: recurrence.shares.length ? "percent" : null, shares: recurrence.shares.length ? recurrence.shares : null },
      terms: terms(data), version: recurrence.version,
    }, "Recorrência alterada a partir da data escolhida.");
    if (saved) { setEditing(false); setPlan(null); }
  }
  return <li className={recurrence.isActive ? "" : "muted-row"}>
    <div><strong>{recurrence.description}</strong>{!recurrence.isActive && <span className="tag">Encerrada</span>}
      <span className="entry-meta">{frequencies[recurrence.frequency]} desde {civilDate(recurrence.validFrom)}{recurrence.endDate && ` até ${civilDate(recurrence.endDate)}`}
        {` · ${money(recurrence.amount)}${recurrence.isEstimated ? " estimado" : ""}`}{recurrence.categoryName && ` · ${recurrence.categoryName}`}</span></div>
    {canEdit && !editing && <button type="button" className="secondary-button" onClick={() => setEditing(true)}>Alterar esta e próximas</button>}
    {editing && <form className="data-form nested-form" onSubmit={save}><h3>Alterar {recurrence.description} a partir de uma data</h3>
      {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
      <div className="form-row">
        <label>Descrição<input name="description" required maxLength={200} defaultValue={recurrence.description} /></label>
        <label>Valor<input name="amount" required inputMode="decimal" defaultValue={recurrence.amount.replace(".", ",")} /></label>
      </div>
      <div className="form-row">
        <label>Vale a partir de<input type="date" name="validFrom" required min={recurrence.validFrom} defaultValue={start} /></label>
        <label>Último vencimento (opcional)<input type="date" name="endDate" defaultValue={recurrence.endDate ?? ""} /></label>
        <label>Periodicidade<select name="frequency" defaultValue={recurrence.frequency}>{Object.entries(frequencies).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      </div>
      <label className="checkbox-label"><input type="checkbox" name="isEstimated" defaultChecked={recurrence.isEstimated} />Valor estimado</label>
      <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={recurrence.isActive} />Recorrência ativa (desmarque para encerrar os próximos vencimentos)</label>
      <div className="row-actions"><button type="button" className="secondary-button" disabled={action.busy} onClick={preview}>Ver o que muda</button>
        <button className="primary-button" disabled={action.busy}>Salvar esta e próximas</button>
        <button type="button" className="text-button" onClick={() => { setEditing(false); setPlan(null); }}>Desistir</button></div>
      {plan && <><p className="form-hint">{plan.updated} vencimento(s) mudam, {plan.inserted} novo(s), {plan.cancelled} deixam de existir e {plan.preserved} já pago(s) ou editado(s) ficam como estão.</p>
        <ul className="entry-list compact">{plan.next.map(item => <li key={item.date}><span>{civilDate(item.date)} · {changeLabels[item.change]}</span><span className="number">{money(item.amount)}</span></li>)}</ul></>}
    </form>}
  </li>;
}
