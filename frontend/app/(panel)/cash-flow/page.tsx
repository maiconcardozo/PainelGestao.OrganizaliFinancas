import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { civilDate, money, monthLabel, signOf } from "@/lib/finance";
import { addMonths, capitalize } from "@/lib/expenses";
import type { CashFlow } from "@/lib/planning";

/** RF27: realized until today and projected afterwards, always marked; opening + inflows + outflows = closing. */
export default async function CashFlowPage({ searchParams }: { searchParams: Promise<{ month?: string | string[] }> }) {
  await requirePermission("household.read");
  const requested = (await searchParams).month;
  const query = typeof requested === "string" && /^\d{4}-(0[1-9]|1[0-2])$/.test(requested) ? { month: `${requested}-01` } : undefined;
  const flow = await fetchFromApi<CashFlow>("cash-flow", query);
  const peak = Math.max(...flow.months.map(m => Math.max(Number(m.inflow), -Number(m.outflow))), 1);
  return <section className="page-content"><h1>Fluxo de caixa</h1>
    <p className="intro">Entradas e saídas de dinheiro da casa. Até hoje são valores realizados; depois, previstos pelas rendas fixas e contas em aberto. Transferências entre contas da casa não contam.</p>

    <h2>Mês a mês</h2>
    <div className="month-bars" role="img" aria-label="Entradas e saídas por mês; meses projetados aparecem tracejados.">
      {flow.months.map(m => <div key={m.month} className={`month-bar ${m.isProjected ? "projected" : ""} ${m.month === flow.month ? "selected" : ""}`}>
        <Link href={`/cash-flow?month=${m.month}`} aria-label={`${capitalize(monthLabel(m.month))}: entradas ${money(m.inflow)}, saídas ${money(m.outflow.replace("-", ""))}`}>
          <span className="bar in" style={{ height: `${(Number(m.inflow) / peak) * 100}%` }} />
          <span className="bar out" style={{ height: `${(-Number(m.outflow) / peak) * 100}%` }} />
        </Link>
        <span className="bar-label">{monthLabel(m.month).slice(0, 3)}</span>
      </div>)}
    </div>
    <p className="legend"><span className="key in" />Entradas <span className="key out" />Saídas <span className="key projected" />Previsto</p>
    <div className="table-scroll"><table className="finance-table"><caption className="sr-only">Fluxo mensal</caption>
      <thead><tr><th scope="col">Mês</th><th scope="col" className="number">Saldo inicial</th><th scope="col" className="number">Entradas</th><th scope="col" className="number">Saídas</th>
        <th scope="col" className="number">Saldo final</th></tr></thead>
      <tbody>{flow.months.map(m => <tr key={m.month} className={m.month === flow.month ? "selected-row" : ""}>
        <th scope="row">{capitalize(monthLabel(m.month))}{m.isProjected && <span className="tag">Previsto</span>}</th>
        <td className="number">{money(m.opening)}</td><td className="number positive">{money(m.inflow)}</td><td className="number negative">{money(m.outflow)}</td>
        <td className={`number ${signOf(m.closing) < 0 ? "negative" : ""}`}>{money(m.closing)}{signOf(m.other) !== 0 && <span className="entry-meta">inclui {money(m.other)} de contas abertas no mês</span>}</td>
      </tr>)}</tbody></table></div>

    <nav className="month-nav" aria-label="Mês do fluxo diário">
      <Link href={`/cash-flow?month=${addMonths(flow.month, -1)}`} aria-label="Mês anterior"><ChevronLeft aria-hidden="true" size={20} /><span className="nav-text">Anterior</span></Link>
      <strong aria-current="date">{capitalize(monthLabel(flow.month))}</strong>
      <Link href={`/cash-flow?month=${addMonths(flow.month, 1)}`} aria-label="Próximo mês"><span className="nav-text">Próximo</span><ChevronRight aria-hidden="true" size={20} /></Link>
    </nav>
    <h2>Dia a dia</h2>
    <div className="table-scroll"><table className="finance-table"><caption>Saldo inicial {money(flow.opening)} · saldo final {money(flow.closing)}</caption>
      <thead><tr><th scope="col">Dia</th><th scope="col" className="number">Entradas</th><th scope="col" className="number">Saídas</th><th scope="col" className="number">Saldo</th></tr></thead>
      <tbody>{flow.days.map(day => <tr key={day.date} className={day.isProjected ? "projected-row" : ""}>
        <th scope="row">{civilDate(day.date)}{day.date === flow.today && <span className="tag">Hoje</span>}{day.isProjected && <span className="tag">Previsto</span>}</th>
        <td className="number positive">{money(day.inflow)}</td><td className="number negative">{money(day.outflow)}</td>
        <td className={`number ${signOf(day.balance) < 0 ? "negative" : ""}`}>{money(day.balance)}</td></tr>)}</tbody></table></div>
  </section>;
}
