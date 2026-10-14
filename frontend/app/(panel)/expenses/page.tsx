import Link from "next/link";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { money, monthLabel, type AccountsOverview } from "@/lib/finance";
import { addMonths, capitalize, type Contributions as ContributionData, type ExpenseList as ListData, type Payment, type Recurrence } from "@/lib/expenses";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType } from "@/lib/expense-types";
import type { Person } from "@/components/households/HouseholdForms";
import { ExpenseList } from "@/components/expenses/ExpenseList";
import { ExpensesView } from "@/components/expenses/ExpensesView";
import { Payments } from "@/components/expenses/Payments";
import { Recurrences } from "@/components/expenses/Recurrences";
import { Contributions } from "@/components/expenses/Contributions";

export default async function ExpensesPage({ searchParams }: { searchParams: Promise<{ month?: string | string[] }> }) {
  const session = await requirePermission("household.read");
  const requested = (await searchParams).month;
  const accounts = await fetchFromApi<AccountsOverview>("accounts");
  const month = typeof requested === "string" && /^\d{4}-(0[1-9]|1[0-2])$/.test(requested) ? requested : accounts.asOf.slice(0, 7);
  const first = `${month}-01`;
  const last = `${addMonths(month, 1)}-01`;
  const [list, payments, recurrences, contributions, people, types, categories, creditors, methods] = await Promise.all([
    fetchFromApi<ListData>("expenses", { from: first, to: new Date(Date.parse(`${last}T00:00:00Z`) - 86400000).toISOString().slice(0, 10) }),
    fetchFromApi<Payment[]>("payments"), fetchFromApi<Recurrence[]>("expense-recurrences"), fetchFromApi<ContributionData>("expenses/contributions", { month: first }),
    fetchFromApi<Person[]>("people"), fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories"),
    fetchFromApi<CatalogItem[]>("catalogs/creditors"), fetchFromApi<CatalogItem[]>("catalogs/payment-methods")]);
  const catalogs = { types, categories, creditors, methods, people };
  const canEdit = session.permissions.includes("finance.write");
  const canSettle = session.permissions.includes("payments.settle");
  return <ExpensesView catalogs={catalogs} accounts={accounts.accounts} today={list.today} canEdit={canEdit} canSettle={canSettle}>
    <nav className="month-nav" aria-label="Mês das despesas">
      <Link href={`/expenses?month=${addMonths(month, -1)}`} aria-label="Mês anterior"><ChevronLeft aria-hidden="true" size={20} /><span className="nav-text">Anterior</span></Link>
      <strong aria-current="date">{capitalize(monthLabel(month))}</strong>
      <Link href={`/expenses?month=${addMonths(month, 1)}`} aria-label="Próximo mês"><span className="nav-text">Próximo</span><ChevronRight aria-hidden="true" size={20} /></Link>
    </nav>
    <dl className="money-summary money-grid">
      <div><dt>Despesas do mês</dt><dd>{money(list.total)}</dd></div>
      <div><dt>Pago</dt><dd>{money(list.paid)}</dd></div>
      <div><dt>Falta pagar</dt><dd>{money(list.open)}</dd></div>
      <div><dt>Vencidas em aberto</dt><dd className={Number(list.overdue) > 0 ? "alert-figure" : ""}>{money(list.overdue)}</dd></div>
    </dl>
    <ExpenseList items={list.items} accounts={accounts.accounts} catalogs={catalogs} today={list.today} canEdit={canEdit} canSettle={canSettle} />
    <h2>Rateio de {monthLabel(month)}</h2>
    <Contributions data={contributions} />
    <h2>Contas recorrentes</h2>
    <Recurrences recurrences={recurrences} canEdit={canEdit} today={list.today} />
    <h2>Pagamentos recentes</h2>
    <Payments payments={payments} canReverse={session.permissions.includes("payments.reverse")} canEdit={canEdit} />
  </ExpensesView>;
}
