import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { money, type AccountsOverview } from "@/lib/finance";
import type { Card, Invoice, Purchase } from "@/lib/cards";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType } from "@/lib/expense-types";
import type { Person } from "@/components/households/HouseholdForms";
import { Cards } from "@/components/cards/Cards";
import { PurchaseForm } from "@/components/cards/PurchaseForm";
import { Invoices, Purchases } from "@/components/cards/Invoices";

export default async function CardsPage() {
  const session = await requirePermission("household.read");
  const [cards, invoices, purchases, accounts, people, types, categories] = await Promise.all([
    fetchFromApi<Card[]>("cards"), fetchFromApi<Invoice[]>("card-invoices"), fetchFromApi<Purchase[]>("card-purchases"), fetchFromApi<AccountsOverview>("accounts"),
    fetchFromApi<Person[]>("people"), fetchFromApi<ExpenseType[]>("expense-types"), fetchFromApi<CatalogItem[]>("catalogs/categories")]);
  const canEdit = session.permissions.includes("finance.write");
  const committed = cards.reduce((sum, card) => sum + Number(card.committed), 0).toFixed(2);
  const nextDue = invoices.filter(invoice => Number(invoice.openBalance) > 0).sort((a, b) => a.dueDate.localeCompare(b.dueDate))[0];
  return <section className="page-content"><h1>Cartões</h1>
    <p className="intro">Compras no cartão entram nas faturas sem tirar dinheiro da conta. O dinheiro sai uma vez, quando a fatura é paga.</p>
    {cards.length > 0 && <dl className="money-summary money-grid">
      <div><dt>Comprometido nos cartões</dt><dd>{money(committed)}</dd></div>
      <div><dt>Próxima fatura a pagar</dt><dd>{nextDue ? money(nextDue.openBalance) : "—"}</dd></div>
    </dl>}
    <Cards cards={cards} people={people} canEdit={canEdit} />
    {canEdit && <PurchaseForm cards={cards} catalogs={{ types, categories, creditors: [], methods: [], people }} today={accounts.asOf} />}
    <h2>Faturas</h2>
    <Invoices cards={cards} invoices={invoices} accounts={accounts.accounts} people={people} today={accounts.asOf} canSettle={session.permissions.includes("payments.settle")} />
    <h2>Compras recentes</h2>
    <Purchases purchases={purchases} canEdit={canEdit} />
  </section>;
}
