import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview } from "@/lib/finance";
import type { Card, Invoice, Purchase } from "@/lib/cards";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType } from "@/lib/expense-types";
import type { Person } from "@/components/households/HouseholdForms";
import { CardsView } from "@/components/cards/CardsView";

/** `?invoice=<code>` (from "Ver compras" in Despesas, A19) selects that invoice's card and opens its purchases. */
export default async function CardsPage({ searchParams }: { searchParams: Promise<{ invoice?: string | string[] }> }) {
  const session = await requirePermission("household.read");
  const requested = (await searchParams).invoice;
  const focusInvoice = typeof requested === "string" && /^[0-9a-f-]{36}$/i.test(requested) ? requested : undefined;
  const [cards, invoices, purchases, accounts, people, types, categories] = await Promise.all([
    fetchFromApi<Card[]>("cards"), fetchFromApi<Invoice[]>("card-invoices"), fetchFromApi<Purchase[]>("card-purchases"), fetchFromApi<AccountsOverview>("accounts"),
    fetchFromApi<Person[]>("people"), fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories")]);
  return <CardsView cards={cards} invoices={invoices} purchases={purchases} accounts={accounts.accounts} people={people}
    catalogs={{ types, categories, creditors: [], methods: [], people }} today={accounts.asOf} focusInvoice={focusInvoice}
    canEdit={session.permissions.includes("finance.write")} canSettle={session.permissions.includes("payments.settle")} />;
}
