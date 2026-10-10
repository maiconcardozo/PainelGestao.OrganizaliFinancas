import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview, Forecast, IncomeSource, IncomeSummary, Receipt } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { IncomeOverview, MonthlyComparison } from "@/components/finance/IncomeOverview";
import { Forecasts } from "@/components/finance/Forecasts";
import { ReceiptForm, ReceiptList } from "@/components/finance/Receipts";
import { IncomeSources } from "@/components/finance/IncomeSources";
export default async function IncomePage() {
  const session = await requirePermission("household.read");
  const [summary, forecasts, receipts, sources, people, accounts, incomeTypes] = await Promise.all([
    fetchFromApi<IncomeSummary>("income/summary"), fetchFromApi<Forecast[]>("income-forecasts"), fetchFromApi<Receipt[]>("receipts"),
    fetchFromApi<IncomeSource[]>("income-sources"), fetchFromApi<Person[]>("people"), fetchFromApi<AccountsOverview>("accounts"),
    fetchFromApi<CatalogItem[]>("catalogs/income-types")]);
  const canEdit = session.permissions.includes("finance.write");
  return <section className="page-content"><h1>Rendas</h1>
    <p className="intro">Acompanhe o que cada morador espera receber, o que de fato entrou e a média mensal do ano.</p>
    <IncomeOverview summary={summary} />
    <Forecasts forecasts={forecasts} canEdit={canEdit} />
    {canEdit && <ReceiptForm accounts={accounts.accounts} people={people} sources={sources} forecasts={forecasts} incomeTypes={incomeTypes} today={accounts.asOf} />}
    <h2>Recebimentos do ano</h2>
    <ReceiptList receipts={receipts} canReverse={session.permissions.includes("payments.reverse")} />
    <MonthlyComparison summary={summary} />
    <IncomeSources sources={sources} people={people} incomeTypes={incomeTypes} canEdit={canEdit} today={accounts.asOf} />
  </section>;
}
