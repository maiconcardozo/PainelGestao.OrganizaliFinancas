"use client";
import Link from "next/link";
import { type FormEvent } from "react";
import { civilDate } from "@/lib/finance";
import type { Alert, AlertPreference } from "@/lib/planning";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { AlertBadge } from "./Widgets";

/** RF31: reading, snoozing or resolving never changes the cause; the alert returns only if the condition changes. */
export function AlertList({ alerts, canAct }: { alerts: Alert[]; canAct: boolean }) {
  if (alerts.length === 0) return <div className="empty-state"><h2>Nenhum alerta ativo</h2><p>Quando algo pedir atenção, como uma conta perto do vencimento ou um orçamento estourado, aparece aqui.</p></div>;
  return <ul className="expense-list">{alerts.map(alert => <AlertRow key={`${alert.key}-${alert.status}`} alert={alert} canAct={canAct} />)}</ul>;
}

function AlertRow({ alert, canAct }: { alert: Alert; canAct: boolean }) {
  const action = useFinanceAction();
  const act = (name: string, days?: number) => action.send("alerts/actions", "POST", { key: alert.key, action: name, days: days ?? null },
    name === "read" ? "Marcado como lido." : name === "snooze" ? "Alerta adiado." : "Alerta resolvido.");
  return <li className={`expense-row alert-${alert.severity}${alert.status === "read" ? " muted-row" : ""}`}>
    <div className="expense-main"><div><AlertBadge alert={alert} /> <strong>{alert.title}</strong><span className="entry-meta">{alert.detail}</span></div>
      <span className="entry-meta">{civilDate(alert.date)}{alert.status === "read" && " · lido"}</span></div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    <div className="row-actions"><Link className="secondary-button" href={alert.link}>Ver detalhes</Link>
      {canAct && <>{alert.status === "new" && <button type="button" className="secondary-button" disabled={action.busy} onClick={() => void act("read")}>Marcar como lido</button>}
        <button type="button" className="secondary-button" disabled={action.busy} onClick={() => void act("snooze", 7)}>Adiar 7 dias</button>
        <button type="button" className="text-button" disabled={action.busy} onClick={() => void act("resolve")}>Resolver</button></>}</div>
  </li>;
}

export function AlertPreferences({ preference, canEdit }: { preference: AlertPreference; canEdit: boolean }) {
  const action = useFinanceAction();
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    await action.send("alert-preferences", "PUT", { warningPercent: Number(data.get("warning")), limitPercent: Number(data.get("limit")),
      dueSoonDays: Number(data.get("dueSoonDays")), version: preference.version }, "Preferências salvas.");
  }
  return <form className="data-form" onSubmit={save} key={preference.version}><h2>Quando avisar</h2>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    <div className="form-row">
      <label>Perto do orçamento (%)<input type="number" name="warning" min={1} max={100} required defaultValue={preference.warningPercent} disabled={!canEdit} /></label>
      <label>Limite do orçamento (%)<input type="number" name="limit" min={1} max={200} required defaultValue={preference.limitPercent} disabled={!canEdit} /></label>
      <label>Avisar vencimentos com antecedência (dias)<input type="number" name="dueSoonDays" min={0} max={30} required defaultValue={preference.dueSoonDays} disabled={!canEdit} /></label>
    </div>
    {canEdit && <button className="primary-button" disabled={action.busy}>Salvar preferências</button>}
  </form>;
}
