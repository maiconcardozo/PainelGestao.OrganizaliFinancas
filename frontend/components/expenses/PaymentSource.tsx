"use client";
import { useEffect, useState } from "react";
import { civilDate, monthLabel, type Account } from "@/lib/finance";
import type { Card, PurchasePreview } from "@/lib/cards";
import { cardOption } from "@/lib/expenses";

type SelectProps = { accounts: Account[]; cards: Card[]; value: string; onChange: (value: string) => void };

/** "Pagar com": a bank account, or a credit card when the household has one. The value is the account code or `card:<code>`. */
export function PaymentSourceSelect({ accounts, cards, value, onChange }: SelectProps) {
  return <label>Pagar com<select name="source" required value={value} onChange={event => onChange(event.target.value)}>
    <optgroup label="Conta">{accounts.filter(account => account.isActive).map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</optgroup>
    {cards.length > 0 && <optgroup label="Cartão de crédito">{cards.map(card => <option key={card.code} value={`${cardOption}${card.code}`}>{card.name}</option>)}</optgroup>}
  </select></label>;
}

export function isCardSource(value: string) { return value.startsWith(cardOption); }
export function defaultSource(accounts: Account[], cards: Card[]) {
  return accounts.find(account => account.isActive)?.code ?? (cards[0] ? `${cardOption}${cards[0].code}` : "");
}

type InvoicePreview = PurchasePreview["items"][number];

/** Which invoice the card's closing day sends this payment to; the server computes it, so the rule lives in one place. */
export function InvoiceHint({ source, date }: { source: string; date: string }) {
  // The answer is keyed by what was asked, so a reply for another card or date is never shown; no state is reset inside the effect.
  const key = `${source}|${date}`;
  const [preview, setPreview] = useState<{ key: string; item: InvoicePreview | null } | null>(null);
  useEffect(() => {
    if (!isCardSource(source) || !date) return;
    let current = true;
    fetch("/api/backend/card-purchases/preview", { method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ cardCode: source.slice(cardOption.length), purchaseDate: date, total: "1.00", count: 1, firstInvoiceShift: 0 }) })
      .then(response => response.ok ? response.json() : null)
      .then(body => { if (current) setPreview({ key, item: (body?.data as PurchasePreview | undefined)?.items[0] ?? null }); })
      .catch(() => { if (current) setPreview({ key, item: null }); });
    return () => { current = false; };
  }, [source, date, key]);
  if (!isCardSource(source)) return null;
  const invoice = preview?.key === key ? preview.item : null;
  return <p className="form-hint">No cartão nada sai da conta agora: o valor entra na fatura
    {invoice ? <> de <strong>{monthLabel(invoice.referenceMonth)}</strong> (fecha {civilDate(invoice.closingDate)}, vence {civilDate(invoice.dueDate)})</> : " conforme o fechamento do cartão"}
    {" "}e a despesa fica paga. O dinheiro sai quando a fatura for paga.</p>;
}
