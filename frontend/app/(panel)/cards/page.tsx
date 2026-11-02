import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview } from "@/lib/finance";
import type { Card, Invoice, InvoiceDetail, ListRange, Purchase } from "@/lib/cards";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType } from "@/lib/expense-types";
import type { Person, Settings } from "@/components/households/HouseholdForms";
import { CardsView } from "@/components/cards/CardsView";

type Params = { invoice?: string | string[]; from?: string | string[] };
const guid = /^[0-9a-f-]{36}$/i;
const yearMonth = /^\d{4}-(0[1-9]|1[0-2])$/;
function one(value: string | string[] | undefined) { return typeof value === "string" ? value : undefined; }
function shiftMonth(month: string, delta: number) {
  const [year, index] = month.split("-").map(Number);
  return new Date(Date.UTC(year, index - 1 + delta, 1)).toISOString().slice(0, 7);
}

/**
 * `?invoice=<code>` ("Ver compras" in Despesas, A19) selects that invoice's card and opens its purchases, reaching back to its month when it is older
 * than the list. `?from=<yyyy-MM>` ("Ver anteriores", A21) lists invoices and purchases from that month on; by default the list starts two months back.
 */
export default async function CardsPage({ searchParams }: { searchParams: Promise<Params> }) {
  const session = await requirePermission("household.read");
  const params = await searchParams;
  const requested = one(params.invoice);
  const focusInvoice = requested && guid.test(requested) ? requested : undefined;
  const [accounts, settings] = await Promise.all([fetchFromApi<AccountsOverview>("accounts"), fetchFromApi<Settings>("household-settings")]);
  const today = accounts.asOf;
  const month = today.slice(0, 7);
  const defaultFrom = shiftMonth(month, -2);
  // The API lists at most 36 months of invoices and two years of purchases: 23 months back plus 12 ahead stays inside both limits.
  const floor = shiftMonth(month, -23);
  const wanted = one(params.from);
  let from = wanted && yearMonth.test(wanted) && wanted < defaultFrom ? wanted : defaultFrom;
  if (focusInvoice) {
    const focused = await fetchFromApi<InvoiceDetail>(`card-invoices/${focusInvoice}`).catch(() => null);
    if (focused && focused.invoice.referenceMonth < from) from = focused.invoice.referenceMonth;
  }
  if (from < floor) from = floor;
  const [cards, invoices, purchases, people, types, categories] = await Promise.all([
    fetchFromApi<Card[]>("cards"), fetchFromApi<Invoice[]>("card-invoices", { from: `${from}-01`, to: `${shiftMonth(month, 12)}-01` }),
    fetchFromApi<Purchase[]>("card-purchases", { from: `${from}-01`, to: today }), fetchFromApi<Person[]>("people"),
    fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories")]);
  const previous = shiftMonth(from, -12);
  const range: ListRange = { from, earlier: from > floor ? (previous < floor ? floor : previous) : null, isDefault: from === defaultFrom };
  return <CardsView cards={cards} invoices={invoices} purchases={purchases} accounts={accounts.accounts} people={people}
    catalogs={{ types, categories, creditors: [], methods: [], people, cards: [] }} today={today} focusInvoice={focusInvoice} range={range} startDate={settings.startDate}
    canEdit={session.permissions.includes("finance.write")} canSettle={session.permissions.includes("payments.settle")} />;
}
