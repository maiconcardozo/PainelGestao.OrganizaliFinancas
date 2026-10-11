import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview, Forecast, IncomeSource, IncomeSummary, Receipt } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { IncomeView } from "@/components/finance/IncomeView";
export default async function IncomePage() {
  const session = await requirePermission("household.read");
  const [summary, forecasts, receipts, sources, people, accounts, incomeTypes] = await Promise.all([
    fetchFromApi<IncomeSummary>("income/summary"), fetchFromApi<Forecast[]>("income-forecasts"), fetchFromApi<Receipt[]>("receipts"),
    fetchFromApi<IncomeSource[]>("income-sources"), fetchFromApi<Person[]>("people"), fetchFromApi<AccountsOverview>("accounts"),
    fetchFromApi<CatalogItem[]>("catalogs/income-types")]);
  return <IncomeView summary={summary} forecasts={forecasts} receipts={receipts} sources={sources} people={people} accounts={accounts} incomeTypes={incomeTypes}
    canEdit={session.permissions.includes("finance.write")} canReverse={session.permissions.includes("payments.reverse")} />;
}
