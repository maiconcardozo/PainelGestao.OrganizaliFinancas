import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { ExpenseType } from "@/lib/expense-types";
import type { CatalogItem } from "@/lib/catalogs";
import { ExpenseTypes } from "@/components/expense-types/ExpenseTypes";
export default async function ExpenseTypesPage() {
  const session = await requirePermission("household.read");
  const [types, categories] = await Promise.all([fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories")]);
  return <ExpenseTypes types={types} categories={categories} canEdit={session.permissions.includes("finance.write")} />;
}
