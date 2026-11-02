"use client";
import { useState, type FormEvent } from "react";
import Link from "next/link";
import { CreditCard, PlusCircle } from "lucide-react";
import { civilDate, money, monthLabel, shortMonth, toApiMoney } from "@/lib/finance";
import { capitalize } from "@/lib/expenses";
import type { Card } from "@/lib/cards";
import type { CardLanding, Margin, MarginMonth } from "@/lib/planning";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export const marginStatus: Record<MarginMonth["status"], string> = { green: "Dentro da reserva", yellow: "Margem apertada", red: "Abaixo da reserva" };
const statusIcon: Record<MarginMonth["status"], string> = { green: "🟢", yellow: "🟡", red: "🔴" };

type Props = { margin: Margin; cards: Card[]; month?: string; compact?: boolean };

/**
 * A22, "Margem do mês": saldo no início + entradas − o que sai da conta = saldo previsto; menos a reserva mínima = margem para gastar.
 * What goes to a card leaves on the invoice, so it shows apart; the simulator reprojects the same months with a spend of today.
 */
export function MarginCard({ margin, cards, month, compact = false }: Props) {
  const selected = margin.months.find(item => item.month === month) ?? margin.months[0];
  const isCurrent = selected.month === margin.months[0].month;
  const reserve = Number(margin.reserve);
  const belowReserve = selected.status === "red";
  const invoiceMonths = [...new Set(selected.toCardGroups.map(group => group.invoiceMonth))];
  return <section className={`margin-card margin-${selected.status}${compact ? " compact" : ""}`} aria-labelledby={`margin-title-${selected.month}`}>
    <div className="statement-header">
      <h2 id={`margin-title-${selected.month}`}>Margem de {monthLabel(selected.month)}</h2>
      <span className={`status-tag margin-tag-${selected.status}`}>{statusIcon[selected.status]} {marginStatus[selected.status]}</span>
    </div>
    <dl className="margin-lines">
      <div><dt>Saldo no início do mês</dt><dd>{money(selected.opening)}</dd></div>
      <div><dt>+ Entradas previstas</dt><dd>{money(selected.inflow)}</dd></div>
      <div><dt>− Sai da conta este mês <span className="entry-meta">contas + faturas que vencem no mês{isCurrent ? ", já pagas ou a pagar" : ""}</span></dt>
        <dd>{money(selected.outflow.replace("-", ""))}<span className="figure-note">a pagar: contas {money(selected.bills)} · faturas {money(selected.invoices)}</span></dd></div>
      {Number(selected.other) !== 0 && <div><dt>± Outros (saldo inicial de contas abertas no mês)</dt><dd>{money(selected.other)}</dd></div>}
      <div className="margin-total"><dt>= Saldo previsto no fim do mês</dt><dd className={belowReserve ? "alert-figure" : ""}>{money(selected.closing)}</dd></div>
      <div><dt>− Reserva mínima <Link href="/household-settings" className="text-button">ajustar</Link></dt><dd>{money(margin.reserve)}</dd></div>
      <div className="margin-result"><dt>= Margem para gastar</dt>
        <dd>{money(selected.margin)}{belowReserve && <span className="figure-note alert-figure">{money(selected.shortfall)} abaixo da reserva</span>}</dd></div>
    </dl>
    <p className="margin-card-line"><CreditCard size={15} aria-hidden="true" />
      <span><strong>Vai para o cartão{invoiceMonths.length === 1 && ` (sai em ${shortMonth(invoiceMonths[0])})`}</strong> {money(selected.toCard)}
        {selected.toCardGroups.length > 0 && <> — {selected.toCardGroups.map(group =>
          `${group.name}${group.count > 1 ? ` (${group.count} itens)` : ""} ${money(group.amount)}${invoiceMonths.length > 1 ? ` em ${shortMonth(group.invoiceMonth)}` : ""}`).join(" · ")}</>}
        {selected.toCardGroups.length === 0 && <span className="entry-meta"> nenhuma conta deste mês marcada para pagar com cartão</span>}</span></p>
    <p className="margin-card-line"><PlusCircle size={15} aria-hidden="true" /><span>Sobra de meses anteriores entra na margem: o saldo previsto de um mês é o início do seguinte.</span></p>
    <ol className="margin-strip" aria-label="Margem dos próximos meses">{margin.months.map(item =>
      <li key={item.month} className={`margin-chip margin-${item.status}${item.month === selected.month ? " is-selected" : ""}`}>
        <span>{shortMonth(item.month)}</span><strong>{money(item.margin)}</strong><span aria-label={marginStatus[item.status]}>{statusIcon[item.status]}</span></li>)}</ol>
    {!compact && <Simulator base={margin} cards={cards} reserve={reserve} />}
    {compact && <p className="form-hint">Reserva de {money(margin.reserve)} · simule um gasto e veja em qual fatura cai uma compra de hoje no <Link href="/dashboard">Painel</Link>.</p>}
  </section>;
}

/** "E se eu gastar…": the same months recomputed by the API with a spend of today, in cash or on a card. */
function Simulator({ base, cards, reserve }: { base: Margin; cards: Card[]; reserve: number }) {
  const action = useFinanceAction();
  const [result, setResult] = useState<Margin | null>(null);
  async function simulate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const card = String(data.get("form") ?? "");
    const query = new URLSearchParams({ amount: toApiMoney(data.get("amount")) });
    if (card) query.set("cardCode", card);
    const loaded = await action.read<Margin>(`margin?${query}`);
    setResult(loaded ?? null);
  }
  const simulation = result?.simulation ?? null;
  const changed = simulation ? simulation.months.map((item, index) => ({ after: item, before: result!.months[index] })).filter(pair => pair.after.margin !== pair.before.margin) : [];
  const first = changed[0];
  const turnedRed = changed.filter(pair => pair.after.status === "red");
  return <form className="margin-sim" onSubmit={simulate}>
    <h3>E se eu gastar…</h3>
    <div className="form-row">
      <label>Valor<input name="amount" required inputMode="decimal" placeholder="1.000,00" /></label>
      <label>Forma<select name="form" defaultValue=""><option value="">À vista (sai da conta hoje)</option>
        {cards.map(card => <option key={card.code} value={card.code}>Cartão {card.name}</option>)}</select></label>
      <div className="self-end"><button className="secondary-button" disabled={action.busy}>Simular</button></div>
    </div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {simulation && <div className="margin-sim-result" role="status">
      {simulation.cardName && simulation.invoiceMonth && simulation.invoiceDueDate && <p>Cai na fatura de <strong>{shortMonth(simulation.invoiceMonth)}</strong> do {simulation.cardName} (vence {civilDate(simulation.invoiceDueDate)}); nada sai da conta antes disso.</p>}
      {first ? <p>A margem de <strong>{shortMonth(first.after.month)}</strong> cai de {money(first.before.margin)} para <strong>{money(first.after.margin)}</strong> {statusIcon[first.after.status]}
        {changed.length > 1 && <> e os meses seguintes caem {money(simulation.amount)} cada ({changed.slice(1).map(pair => `${shortMonth(pair.after.month)} ${money(pair.after.margin)} ${statusIcon[pair.after.status]}`).join(" · ")})</>}.</p>
        : <p>Esse gasto só sai depois dos meses mostrados; as margens até {shortMonth(base.months[base.months.length - 1].month)} não mudam.</p>}
      {turnedRed.length > 0 && <ul>{turnedRed.map(pair => <li key={pair.after.month}>{capitalize(shortMonth(pair.after.month))} fecha em {money(pair.after.closing)} ({money(pair.after.shortfall)} abaixo da reserva de {money(String(reserve))}) 🔴</li>)}</ul>}
    </div>}
  </form>;
}

/** "Hoje, em qual fatura cai?": one line per active card with the closing day and where a purchase made today lands. */
export function CardLandingTable({ cards, today }: { cards: CardLanding[]; today: string }) {
  if (cards.length === 0) return null;
  return <div className="table-scroll"><table className="finance-table margin-landing"><caption>Hoje, {civilDate(today)}, em qual fatura cai uma compra em cada cartão</caption>
    <thead><tr><th scope="col">Cartão</th><th scope="col">Fecha</th><th scope="col">Compra de hoje cai em</th></tr></thead>
    <tbody>{cards.map(card => <tr key={card.cardCode}><td>{card.cardName}</td><td>dia {card.closingDay}</td>
      <td>fatura <strong>{shortMonth(card.referenceMonth)}</strong> (vence {civilDate(card.dueDate)}) até {civilDate(card.lastDay)} · depois {shortMonth(card.nextReferenceMonth)}</td></tr>)}</tbody>
  </table></div>;
}
