import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { money, monthLabel } from "@/lib/finance";
import { addMonths, capitalize } from "@/lib/expenses";
import type { Budget } from "@/lib/planning";
import { BudgetEditor } from "@/components/planning/BudgetEditor";

export default async function BudgetPage({ searchParams }: { searchParams: Promise<{ month?: string | string[] }> }) {
  const session = await requirePermission("household.read");
  const requested = (await searchParams).month;
  const query = typeof requested === "string" && /^\d{4}-(0[1-9]|1[0-2])$/.test(requested) ? { month: `${requested}-01` } : undefined;
  const budget = await fetchFromApi<Budget>("budgets", query);
  return <section className="page-content"><h1>Orçamento</h1>
    <p className="intro">Limite por categoria no mês. O comprometido soma as despesas da competência, inclusive as parcelas do cartão da fatura do mês; o pago aparece à parte.</p>
    <nav className="month-nav" aria-label="Mês do orçamento">
      <Link href={`/budget?month=${addMonths(budget.month, -1)}`} aria-label="Mês anterior"><ChevronLeft aria-hidden="true" size={20} /><span className="nav-text">Anterior</span></Link>
      <strong aria-current="date">{capitalize(monthLabel(budget.month))}</strong>
      <Link href={`/budget?month=${addMonths(budget.month, 1)}`} aria-label="Próximo mês"><span className="nav-text">Próximo</span><ChevronRight aria-hidden="true" size={20} /></Link>
    </nav>
    <dl className="money-summary money-grid">
      <div><dt>Limites do mês</dt><dd>{money(budget.totalLimit)}</dd></div>
      <div><dt>Comprometido</dt><dd>{money(budget.totalCommitted)}</dd></div>
      <div><dt>Pago</dt><dd>{money(budget.totalPaid)}</dd></div>
      <div><dt>Fora de orçamento</dt><dd>{money(budget.unbudgeted)}</dd></div>
    </dl>
    <p className="form-hint">Alertas em {budget.warningPercent}% (perto do limite) e {budget.limitPercent}% (limite). A categoria pai soma as filhas; o total da casa usa só o limite mais alto de cada ramo. Ajuste os percentuais na <Link href="/alerts">central de alertas</Link>.</p>
    <BudgetEditor key={budget.month} budget={budget} canEdit={session.permissions.includes("budgets.write")} />
  </section>;
}
