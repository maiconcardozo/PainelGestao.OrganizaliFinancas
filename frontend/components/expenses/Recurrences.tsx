"use client";
import { useState, type FormEvent } from "react";
import { Repeat, RefreshCw } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { EditButton, Empty, Notices, Pill } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { civilDate, frequencies, money, toApiMoney } from "@/lib/finance";
import { changeLabels, type Recurrence, type RecurrencePlan } from "@/lib/expenses";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export function Recurrences({ recurrences, canEdit, today }: { recurrences: Recurrence[]; canEdit: boolean; today: string }) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState<Recurrence | null>(null);
  if (recurrences.length === 0) return <Empty title="Nenhuma conta recorrente. Aluguel, água e internet podem ser lançados uma vez como recorrentes em “Nova despesa”." />;
  return <>
    {canEdit && <div className="mb-3 flex justify-end">
      <button type="button" className="secondary-button" disabled={action.busy}
        onClick={() => void action.send("expense-recurrences/generate", "POST", undefined, "Vencimentos atualizados até o mês atual + 12.")}><RefreshCw size={15} />Atualizar vencimentos</button>
    </div>}
    <Notices error={editing ? undefined : action.error} message={action.message} />
    <ListFrame>{recurrences.map(item => <li key={`${item.code}-${item.version}`} className={`recurrence-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${item.isActive ? "" : "opacity-60"}`}>
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Repeat size={17} /></span>
      <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{item.description}</span>
        <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{frequencies[item.frequency]} desde {civilDate(item.validFrom)}{item.endDate && ` até ${civilDate(item.endDate)}`}{item.categoryName && ` · ${item.categoryName}`}</span></span>
      <span className="shrink-0 text-right"><span className="num block text-sm font-semibold">{money(item.amount)}</span>{item.isEstimated && <span className="block text-xs text-[var(--warning)]">estimado</span>}</span>
      {!item.isActive && <Pill tone="muted">Encerrada</Pill>}
      {canEdit && <EditButton name={`${item.description} a partir de uma data`} onClick={() => setEditing(item)} />}
    </li>)}</ListFrame>
    {editing && <RecurrenceModal key={`${editing.code}-${editing.version}`} recurrence={editing} today={today} onClose={() => setEditing(null)} />}
  </>;
}

/** "This and next": the change starts at the chosen date; paid, adjusted or individually edited occurrences are preserved. */
function RecurrenceModal({ recurrence, today, onClose }: { recurrence: Recurrence; today: string; onClose: () => void }) {
  const action = useFinanceAction();
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
    if (saved) onClose();
  }
  return <Modal title={`Alterar ${recurrence.description}`} onClose={onClose} error={action.error} width="max-w-3xl">
    <form className="data-form" onSubmit={save}>
      <p className="form-hint">A mudança vale para esta e as próximas ocorrências a partir da data escolhida. Vencimentos pagos, ajustados ou editados um a um ficam como estão.</p>
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
      <div><button type="button" className="secondary-button" disabled={action.busy} onClick={preview}>Ver o que muda</button></div>
      {plan && <><p className="form-hint">{plan.updated} vencimento(s) mudam, {plan.inserted} novo(s), {plan.cancelled} deixam de existir e {plan.preserved} já pago(s) ou editado(s) ficam como estão.</p>
        <ul className="entry-list compact">{plan.next.map(item => <li key={item.date}><span>{civilDate(item.date)} · {changeLabels[item.change]}</span><span className="number">{money(item.amount)}</span></li>)}</ul></>}
      <div className="modal-actions">
        <button type="button" onClick={onClose} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm font-medium text-[var(--text-secondary)] hover:bg-[var(--surface-2)]">Cancelar</button>
        <button type="submit" disabled={action.busy} className="rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--primary-strong)] disabled:opacity-60">{action.busy ? "Salvando..." : "Salvar esta e próximas"}</button>
      </div>
    </form>
  </Modal>;
}
