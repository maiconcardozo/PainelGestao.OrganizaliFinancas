"use client";
import { useState } from "react";
import { Plus } from "lucide-react";
import { NewButton, Notices, PageHeader } from "@/components/common/Ui";
import { money, type Account } from "@/lib/finance";
import type { Card, Invoice, Purchase } from "@/lib/cards";
import type { Catalogs } from "@/components/expenses/DetailInputs";
import type { Person } from "@/components/households/HouseholdForms";
import { CardList, CardModal } from "./Cards";
import { PurchaseModal } from "./PurchaseForm";
import { Invoices, Purchases } from "./Invoices";

type Props = { cards: Card[]; invoices: Invoice[]; purchases: Purchase[]; accounts: Account[]; people: Person[]; catalogs: Catalogs; today: string; canEdit: boolean; canSettle: boolean };

/** Cartões: the card cadastro and the purchase entry open from the header, as in RevendaPRO's "Novo …" flow. */
export function CardsView({ cards, invoices, purchases, accounts, people, catalogs, today, canEdit, canSettle }: Props) {
  const [editing, setEditing] = useState<Card | "new" | null>(null);
  const [buying, setBuying] = useState(false);
  const [notice, setNotice] = useState("");
  const committed = cards.reduce((sum, card) => sum + Number(card.committed), 0).toFixed(2);
  const nextDue = invoices.filter(invoice => Number(invoice.openBalance) > 0).sort((a, b) => a.dueDate.localeCompare(b.dueDate))[0];
  const canBuy = cards.some(card => card.isActive);
  function done(message: string) { setEditing(null); setBuying(false); setNotice(message); }
  return <section className="page-content">
    <PageHeader title="Cartões" description="Compras no cartão entram nas faturas sem tirar dinheiro da conta. O dinheiro sai uma vez, quando a fatura é paga."
      action={canEdit && <div className="flex flex-wrap gap-2">
        {canBuy && <button type="button" className="secondary-button" onClick={() => setBuying(true)}><Plus size={16} />Lançar compra</button>}
        <NewButton label="Novo cartão" onClick={() => setEditing("new")} />
      </div>} />
    <Notices message={notice} />
    {cards.length > 0 && <dl className="money-summary money-grid">
      <div><dt>Comprometido nos cartões</dt><dd>{money(committed)}</dd></div>
      <div><dt>Próxima fatura a pagar</dt><dd>{nextDue ? money(nextDue.openBalance) : "—"}</dd>{nextDue && <dd className="figure-note">{nextDue.cardName}</dd>}</div>
    </dl>}
    <CardList cards={cards} canEdit={canEdit} onEdit={setEditing} />
    <h2>Faturas</h2>
    <Invoices cards={cards} invoices={invoices} accounts={accounts} people={people} today={today} canSettle={canSettle} />
    <h2>Compras recentes</h2>
    <Purchases purchases={purchases} canEdit={canEdit} />
    {editing && <CardModal card={editing === "new" ? null : editing} people={people} onClose={() => setEditing(null)} onSaved={done} />}
    {buying && <PurchaseModal cards={cards} catalogs={catalogs} today={today} onClose={() => setBuying(false)} onSaved={done} />}
  </section>;
}
