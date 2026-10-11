import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { civilDate, money, monthLabel } from "@/lib/finance";
import { addMonths, capitalize } from "@/lib/expenses";
import { eventKinds, type Dashboard } from "@/lib/planning";
import { ProjectionChart } from "@/components/planning/ProjectionChart";
import { AgendaList, AlertBadge, BudgetBar, BudgetFigures } from "@/components/planning/Widgets";

/** D18/RF26: one consistent picture of the month; every figure links to the screen that explains it. */
export default async function DashboardPage({ searchParams }: { searchParams: Promise<{ month?: string | string[] }> }) {
  await requirePermission("household.read");
  const requested = (await searchParams).month;
  const query = typeof requested === "string" && /^\d{4}-(0[1-9]|1[0-2])$/.test(requested) ? { month: `${requested}-01` } : undefined;
  const data = await fetchFromApi<Dashboard>("dashboard", query);
  const month = data.month;
  const projection = data.projection;
  const watched = data.budget.lines.filter(line => line.band !== "none" && line.categoryCode).sort((a, b) => Number(b.usedPercent ?? 0) - Number(a.usedPercent ?? 0)).slice(0, 5);
  return <section className="page-content dashboard"><h1>Painel da casa</h1>
    <nav className="month-nav" aria-label="Mês do painel">
      <Link href={`/dashboard?month=${addMonths(month, -1)}`} aria-label="Mês anterior"><ChevronLeft aria-hidden="true" size={20} /><span className="nav-text">Anterior</span></Link>
      <strong aria-current="date">{capitalize(monthLabel(month))}</strong>
      <Link href={`/dashboard?month=${addMonths(month, 1)}`} aria-label="Próximo mês"><span className="nav-text">Próximo</span><ChevronRight aria-hidden="true" size={20} /></Link>
    </nav>
    <dl className="money-summary money-grid">
      <div><dt><Link href="/accounts">Saldo hoje</Link></dt><dd>{money(data.balance)}</dd></div>
      <div><dt><Link href="/income">Recebido no mês</Link></dt><dd>{money(data.income.received)}</dd><dd className="figure-note">de {money(data.income.expected)} previstos</dd></div>
      <div><dt><Link href={`/expenses?month=${month}`}>Despesas pagas</Link></dt><dd>{money(data.expenses.paid)}</dd><dd className="figure-note">de {money(data.expenses.total)} no mês</dd></div>
      <div><dt><Link href="/cash-flow">Saldo em {civilDate(projection.to)}</Link></dt><dd className={Number(projection.end) < 0 ? "alert-figure" : ""}>{money(projection.end)}</dd>
        <dd className="figure-note">projetado</dd></div>
    </dl>

    <section className="dashboard-block" aria-labelledby="projection-title-h">
      <div className="statement-header"><h2 id="projection-title-h">Saldo projetado nos próximos 60 dias</h2><Link className="secondary-button" href="/cash-flow">Ver fluxo de caixa</Link></div>
      {projection.firstDeficit && <div className="deficit-callout" role="alert">
        <strong>O saldo da casa fica negativo em {civilDate(projection.firstDeficit)}</strong>
        <span>O menor saldo previsto é {money(projection.lowest)} em {civilDate(projection.lowestDate)}. Contas que levam ao déficit:</span>
        <ul>{projection.deficitCauses.map(cause => <li key={`${cause.kind}-${cause.code}`}>{cause.description} · {eventKinds[cause.kind]} · {money(cause.amount.replace("-", ""))}</li>)}</ul>
      </div>}
      <ProjectionChart projection={projection} />
      <p className="form-hint">Parte do saldo real de hoje, soma as rendas fixas previstas e desconta contas e faturas em aberto (as vencidas contam hoje). Renda variável e rendas atrasadas não entram. A projeção não altera saldo nenhum.</p>
    </section>

    <div className="dashboard-columns">
      <section aria-labelledby="agenda-title"><div className="statement-header"><h2 id="agenda-title">Agenda</h2><Link href={`/expenses?month=${data.today.slice(0, 7)}`} className="text-button">Despesas</Link></div>
        <AgendaList agenda={data.agenda} /></section>
      <section aria-labelledby="alerts-title"><div className="statement-header"><h2 id="alerts-title">Alertas{data.unreadAlerts > 0 && <span className="count-badge">{data.unreadAlerts}</span>}</h2>
        <Link href="/alerts" className="text-button">Central de alertas</Link></div>
        {data.alerts.length === 0 ? <p className="form-hint">Nenhum alerta ativo.</p> :
          <ul className="entry-list compact">{data.alerts.map(alert => <li key={alert.key} className={alert.status === "read" ? "muted-row" : ""}>
            <Link href={alert.link}><span><AlertBadge alert={alert} /> {alert.title}<span className="entry-meta">{alert.detail}</span></span></Link></li>)}</ul>}
      </section>
    </div>

    <section className="dashboard-block" aria-labelledby="budget-title">
      <div className="statement-header"><h2 id="budget-title">Orçamento de {monthLabel(month)}</h2><Link href={`/budget?month=${month}`} className="secondary-button">Ajustar orçamento</Link></div>
      {watched.length === 0 ? <p className="form-hint">Nenhuma categoria com orçamento neste mês. Defina limites para acompanhar os gastos por categoria.</p> :
        <ul className="budget-list">{watched.map(line => <li key={line.categoryCode}><div className="budget-head"><strong>{line.name}</strong><BudgetFigures line={line} /></div><BudgetBar line={line} /></li>)}</ul>}
      <p className="form-hint">Comprometido no mês: {money(data.budget.totalCommitted)} · limites: {money(data.budget.totalLimit)} · sem orçamento: {money(data.budget.unbudgeted)}.</p>
    </section>
  </section>;
}
