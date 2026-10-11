import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview } from "@/lib/finance";
import type { Card, Invoice, Purchase } from "@/lib/cards";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType } from "@/lib/expense-types";
import type { Person } from "@/components/households/HouseholdForms";
import { CardsView } from "@/components/cards/CardsView";

export default async function CardsPage() {
  const session = await requirePermission("household.read");
  const [cards, invoices, purchases, accounts, people, types, categories] = await Promise.all([
    fetchFromApi<Card[]>("cards"), fetchFromApi<Invoice[]>("card-invoices"), fetchFromApi<Purchase[]>("card-purchases"), fetchFromApi<AccountsOverview>("accounts"),
    fetchFromApi<Person[]>("people"), fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories")]);
  return <CardsView cards={cards} invoices={invoices} purchases={purchases} accounts={accounts.accounts} people={people}
    catalogs={{ types, categories, creditors: [], methods: [], people }} today={accounts.asOf}
    canEdit={session.permissions.includes("finance.write")} canSettle={session.permissions.includes("payments.settle")} />;
}
