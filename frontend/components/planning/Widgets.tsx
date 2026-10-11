import Link from "next/link";
import { civilDate, money } from "@/lib/finance";
import { bandLabels, eventKinds, type Agenda, type AgendaItem, type Alert, type BudgetLine } from "@/lib/planning";

/** Progress against the limit; past 100% the bar is full and the excess is written, never hidden. */
export function BudgetBar({ line }: { line: BudgetLine }) {
  const used = line.usedPercent ? Math.min(Number(line.usedPercent), 100) : line.band === "exceeded" ? 100 : 0;
  return <div className={`budget-bar band-${line.band}`} role="img" aria-label={`${line.name}: ${bandLabels[line.band]}`}>
    <span style={{ width: `${used}%` }} />
  </div>;
}

export function BudgetFigures({ line }: { line: BudgetLine }) {
  if (line.limit === null) return <span className="entry-meta">{money(line.committed)} gastos · sem orçamento</span>;
  return <span className="entry-meta">{money(line.committed)} de {money(line.limit)}{line.usedPercent ? ` (${line.usedPercent.replace(".", ",")}%)` : ""}
    {line.band === "exceeded" && <strong className="negative"> · excesso de {money(line.excess)}</strong>}</span>;
}

const groups: [keyof Agenda, string][] = [["overdue", "Vencidos"], ["today", "Hoje"], ["next7", "Próximos 7 dias"], ["next30", "Até 30 dias"]];
export function AgendaList({ agenda, limit = 5 }: { agenda: Agenda; limit?: number }) {
  const any = groups.some(([key]) => agenda[key].length > 0);
  if (!any) return <p className="form-hint">Nada vence nos próximos 30 dias.</p>;
  return <div className="agenda">{groups.filter(([key]) => agenda[key].length > 0).map(([key, label]) =>
    <section key={key} aria-label={label}><h3 className={key === "overdue" ? "negative" : undefined}>{label}</h3>
      <ul className="entry-list compact">{agenda[key].slice(0, limit).map(item => <AgendaRow key={`${item.kind}-${item.code}`} item={item} />)}</ul>
      {agenda[key].length > limit && <p className="form-hint">e mais {agenda[key].length - limit}.</p>}
    </section>)}</div>;
}
function AgendaRow({ item }: { item: AgendaItem }) {
  return <li><Link href={item.link}><span>{item.description}<span className="entry-meta">{eventKinds[item.kind]} · {civilDate(item.date)}{item.isEstimate && " · estimado"}</span></span></Link>
    <span className={`number ${item.kind === "income" ? "positive" : ""}`}>{item.kind === "income" ? "+" : ""}{money(item.amount)}</span></li>;
}

export function AlertBadge({ alert }: { alert: Alert }) {
  const label = alert.severity === "critical" ? "Urgente" : alert.severity === "warning" ? "Atenção" : "Aviso";
  return <span className={`status-tag severity-${alert.severity}`}>{label}</span>;
}
