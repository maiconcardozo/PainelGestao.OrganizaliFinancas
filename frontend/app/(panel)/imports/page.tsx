import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { ImportBatch } from "@/lib/complements";
import type { Person } from "@/components/households/HouseholdForms";
import { ImportStatement } from "@/components/imports/ImportStatement";

export default async function ImportsPage() {
  const session = await requirePermission("household.read");
  const [accounts, people, categories, incomeTypes, history] = await Promise.all([
    fetchFromApi<AccountsOverview>("accounts"), fetchFromApi<Person[]>("people"), fetchFromApi<CatalogItem[]>("catalogs/categories"),
    fetchFromApi<CatalogItem[]>("catalogs/income-types"), fetchFromApi<ImportBatch[]>("imports")]);
  return <ImportStatement accounts={accounts.accounts} people={people} categories={categories} incomeTypes={incomeTypes} history={history}
    canEdit={session.permissions.includes("finance.write")} canSettle={session.permissions.includes("payments.settle")} />;
}
