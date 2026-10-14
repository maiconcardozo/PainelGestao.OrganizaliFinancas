"use client";
import { useState, type FormEvent } from "react";
import { Target } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card, EditButton, Empty, Figure, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { civilDate, money, toApiMoney, type Account } from "@/lib/finance";
import type { Goal } from "@/lib/complements";

/** Metas de reserva (RF38): target, deadline and the account that holds the money. Progress is that account's balance. */
export function Goals({ goals, accounts, today, canEdit }: { goals: Goal[]; accounts: Account[]; today: string; canEdit: boolean }) {
  const action = useFinanceAction();
  const [view, chooseView] = useViewMode("organizali.goals.view");
  const [editing, setEditing] = useState<Goal | "new" | null>(null);
  const current = editing === "new" ? null : editing;
  const activeAccounts = accounts.filter(account => account.isActive);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(current ? `goals/${current.code}` : "goals", current ? "PUT" : "POST", {
      name: data.get("name"), targetAmount: toApiMoney(data.get("targetAmount")), targetDate: data.get("targetDate") || null, accountCode: data.get("account"),
      note: data.get("note") || null, isActive: current ? data.has("isActive") : true, version: current?.version ?? 0,
    }, current ? "Meta atualizada." : "Meta criada.");
    if (saved) setEditing(null);
  }

  const status = (goal: Goal) => !goal.isActive ? <Pill tone="muted">Encerrada</Pill> : goal.reached ? <Pill tone="success">Alcançada</Pill> : <Pill tone="primary">Em andamento</Pill>;
  const deadline = (goal: Goal) => goal.targetDate ? `até ${civilDate(goal.targetDate)}${goal.monthsLeft !== null ? ` · ${goal.monthsLeft} ${goal.monthsLeft === 1 ? "mês" : "meses"}` : ""}` : "sem prazo";

  return <section className="page-content">
    <PageHeader title="Metas de reserva" description="Quanto a casa quer guardar, até quando e em qual conta. O progresso é o saldo da conta vinculada; reservar não cria renda nem despesa."
      action={canEdit && <NewButton label="Nova meta" onClick={() => setEditing("new")} />} />
    <Notices error={editing ? undefined : action.error} message={action.message} />

    <ListBar count={goals.length} singular="meta" plural="metas" view={view} onChange={chooseView} label="Como mostrar as metas" />
    {goals.length === 0 ? <Empty title={canEdit ? (activeAccounts.length === 0 ? "Cadastre primeiro a conta onde a reserva vai ficar (poupança, por exemplo) e depois crie a meta." : "Nenhuma meta ainda. Crie a reserva de emergência ou a viagem do ano com valor alvo e prazo.") : "Nenhuma meta cadastrada. Um titular ou editor pode criar as metas da casa."} /> :
      view === "list" ? <ListFrame>{goals.map(goal => <li key={goal.code} className={`flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${goal.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Target size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{goal.name}</span>
          <span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">{goal.accountName} · {deadline(goal)}</span>
          <span className="progress" aria-hidden><span style={{ width: `${goal.percent}%` }} /></span></span>
        <span className="num shrink-0 text-right text-sm font-semibold">{money(goal.saved)}<span className="block text-xs font-normal text-[var(--text-muted)]">de {money(goal.targetAmount)} ({goal.percent}%)</span></span>
        {status(goal)}
        {canEdit && <EditButton name={goal.name} onClick={() => setEditing(goal)} />}
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{goals.map(goal => <Card key={goal.code} muted={!goal.isActive}>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0"><p className="flex items-center gap-2 font-semibold"><Target size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{goal.name}</span></p>
            <p className="mt-0.5 text-xs text-[var(--text-secondary)]">{goal.accountName} · {deadline(goal)}</p></div>
          {canEdit && <EditButton name={goal.name} onClick={() => setEditing(goal)} />}
        </div>
        <Figure label="Guardado" value={money(goal.saved)} hint={<>de {money(goal.targetAmount)} · faltam {money(goal.remaining)}</>} />
        <div className="progress" role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={goal.percent} aria-label={`${goal.percent}% da meta`}><span style={{ width: `${goal.percent}%` }} /></div>
        <p className="text-xs text-[var(--text-secondary)]">{goal.reached ? "Meta alcançada." : goal.monthlyNeeded ? (goal.monthsLeft === 0 ? "Prazo chegou: falta o valor acima." : `Guardar ${money(goal.monthlyNeeded)} por mês até o prazo.`) : "Sem prazo definido."}</p>
        {goal.note && <p className="text-xs text-[var(--text-muted)]">{goal.note}</p>}
        <div>{status(goal)}</div>
      </Card>)}</div>}

    <p className="form-hint mt-4">Para a meta andar, transfira dinheiro para a conta vinculada (Contas → Transferir). Encerrar a meta preserva o histórico.</p>

    {editing && <Modal title={current ? `Editar ${current.name}` : "Nova meta"} onClose={() => setEditing(null)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={save}>
        <label>Nome<input name="name" required maxLength={160} defaultValue={current?.name ?? ""} placeholder="Reserva de emergência, Viagem, Carro" autoComplete="off" /></label>
        <div className="form-row">
          <label>Valor alvo<input name="targetAmount" required inputMode="decimal" defaultValue={current?.targetAmount.replace(".", ",") ?? ""} placeholder="10.000,00" /></label>
          <label>Prazo (opcional)<input type="date" name="targetDate" min={current ? undefined : today} defaultValue={current?.targetDate ?? ""} /></label>
        </div>
        <label>Conta onde a reserva fica<select name="account" required defaultValue={current?.accountCode ?? activeAccounts[0]?.code ?? ""}>
          {accounts.filter(account => account.isActive || account.code === current?.accountCode).map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
        <label>Observação (opcional)<input name="note" maxLength={300} defaultValue={current?.note ?? ""} /></label>
        {current && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={current.isActive} />Meta ativa</label>}
        <p className="form-hint">O progresso é o saldo atual da conta escolhida. Use uma conta separada para cada reserva que quiser acompanhar.</p>
        <ModalActions busy={action.busy} label={current ? "Salvar meta" : "Criar meta"} onCancel={() => setEditing(null)} />
      </form>
    </Modal>}
  </section>;
}
