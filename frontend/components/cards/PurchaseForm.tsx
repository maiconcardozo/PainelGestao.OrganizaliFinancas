"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, monthLabel, toApiMoney } from "@/lib/finance";
import { fieldValues } from "@/lib/expenses";
import type { Card, Purchase, PurchasePreview } from "@/lib/cards";
import type { Catalogs } from "@/components/expenses/DetailInputs";
import { CustomFieldInputs } from "@/components/expenses/DetailInputs";
import { Modal } from "@/components/common/Modal";
import { ModalActions } from "@/components/common/Ui";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

/** RF24: purchase on the card with a preview of the invoices; the first invoice can be moved one cycle before saving (D13 premise A06). */
export function PurchaseModal({ cards, catalogs, today, onClose, onSaved }: { cards: Card[]; catalogs: Catalogs; today: string; onClose: () => void; onSaved: (message: string) => void }) {
  const action = useFinanceAction();
  const [typeCode, setTypeCode] = useState("");
  const [preview, setPreview] = useState<PurchasePreview | null>(null);
  const active = cards.filter(card => card.isActive);
  const type = catalogs.types.find(item => item.code === typeCode);
  function previewPayload(data: FormData) {
    return { cardCode: data.get("card"), purchaseDate: data.get("purchaseDate"), total: toApiMoney(data.get("total")), count: Number(data.get("count")),
      firstInvoiceShift: Number(data.get("shift")) };
  }
  async function showPreview(event: React.MouseEvent<HTMLButtonElement>) {
    setPreview(await action.send<PurchasePreview>("card-purchases/preview", "POST", previewPayload(new FormData(event.currentTarget.form!)), "Prévia pronta. Nada foi gravado."));
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send("card-purchases", "POST", {
      ...previewPayload(data), buyerPersonCode: data.get("buyer") || null,
      details: { description: data.get("description"), expenseTypeCode: type?.code ?? null, expenseTypeVersion: type?.version ?? null, categoryCode: data.get("category") || null,
        creditorCode: null, paymentMethodCode: null, payerPersonCode: null, fields: type ? fieldValues(data, type.fields) : null, splitMode: null, shares: null },
    }, "Compra lançada. As parcelas já aparecem nas faturas; nenhuma conta foi movimentada.", true);
    if (saved) onSaved("Compra lançada. As parcelas já aparecem nas faturas; nenhuma conta foi movimentada.");
  }
  if (active.length === 0) return null;
  return <Modal title="Lançar compra no cartão" onClose={onClose} error={action.error} width="max-w-3xl"><form className="data-form" onSubmit={save}>
    {preview && action.message && <p role="status">{action.message}</p>}
    <div className="form-row">
      <label>Cartão<select name="card" required onChange={() => setPreview(null)}>{active.map(card => <option key={card.code} value={card.code}>{card.name}</option>)}</select></label>
      <label>Descrição<input name="description" required maxLength={200} placeholder="Mercado, Farmácia, Passagem" /></label>
    </div>
    <div className="form-row">
      <label>Tipo de despesa<select name="expenseType" value={typeCode} onChange={event => setTypeCode(event.target.value)}><option value="">Sem tipo</option>
        {catalogs.types.filter(item => item.behavior === "card" && item.isActive).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <label>Categoria<select name="category" defaultValue={type?.defaultCategoryCode ?? ""} key={type?.code ?? "none"}><option value="">Sem categoria</option>
        {catalogs.categories.filter(item => item.isActive).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <label>Quem comprou<select name="buyer" defaultValue=""><option value="">Não informado</option>
        {catalogs.people.filter(person => person.isActive).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
    </div>
    {type && <CustomFieldInputs key={`${type.code}-${type.version}`} fields={type.fields} />}
    <div className="form-row">
      <label>Valor total<input name="total" required inputMode="decimal" placeholder="300,00" onChange={() => setPreview(null)} /></label>
      <label>Parcelas<input type="number" name="count" required min={1} max={120} defaultValue={1} onChange={() => setPreview(null)} /></label>
      <label>Data da compra<input type="date" name="purchaseDate" required defaultValue={today} max={today} onChange={() => setPreview(null)} /></label>
    </div>
    <label>Primeira parcela<select name="shift" defaultValue="0" onChange={() => setPreview(null)}>
      <option value="0">Na fatura indicada pelo fechamento</option><option value="-1">Uma fatura antes</option><option value="1">Uma fatura depois</option></select></label>
    <button type="button" className="secondary-button" disabled={action.busy} onClick={showPreview}>Ver em quais faturas cai</button>
    {preview && <div className="table-scroll"><table className="finance-table"><caption>{preview.items.length} parcela(s) somando {money(preview.total)}</caption>
      <thead><tr><th scope="col">Parcela</th><th scope="col">Fatura</th><th scope="col">Fecha</th><th scope="col">Vence</th><th scope="col" className="number">Valor</th></tr></thead>
      <tbody>{preview.items.map(item => <tr key={item.number}><td>{item.number}/{preview.items.length}</td><td>{monthLabel(item.referenceMonth)}</td>
        <td>{civilDate(item.closingDate)}</td><td>{civilDate(item.dueDate)}</td><td className="number">{money(item.amount)}</td></tr>)}</tbody></table></div>}
    <p className="form-hint">A compra não sai da conta bancária agora: ela entra na fatura, e o dinheiro sai uma única vez quando a fatura for paga.</p>
    <ModalActions busy={action.busy} label="Lançar compra" onCancel={onClose} />
  </form></Modal>;
}

/** Edits what describes the purchase. Amount, date and installments stay fixed because the items are already in the invoices. */
export function PurchaseEditModal({ purchase, catalogs, onClose, onSaved }: { purchase: Purchase; catalogs: Catalogs; onClose: () => void; onSaved: (message: string) => void }) {
  const action = useFinanceAction();
  const [typeCode, setTypeCode] = useState(purchase.expenseTypeCode ?? "");
  const type = catalogs.types.find(item => item.code === typeCode);
  const message = "Compra atualizada. Valor, data e parcelas não mudaram.";
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(`card-purchases/${purchase.code}`, "PUT", {
      buyerPersonCode: data.get("buyer") || null, version: purchase.version,
      details: { description: data.get("description"), expenseTypeCode: type?.code ?? null, expenseTypeVersion: type?.version ?? null, categoryCode: data.get("category") || null,
        creditorCode: null, paymentMethodCode: null, payerPersonCode: null, fields: type ? fieldValues(data, type.fields) : null, splitMode: null, shares: null },
    }, message);
    if (saved) onSaved(message);
  }
  return <Modal title={`Editar ${purchase.description}`} onClose={onClose} error={action.error}><form className="data-form" onSubmit={save}>
    <div className="form-row">
      <label>Descrição<input name="description" required maxLength={200} defaultValue={purchase.description} /></label>
      <label>Tipo de despesa<select name="expenseType" value={typeCode} onChange={event => setTypeCode(event.target.value)}><option value="">Sem tipo</option>
        {catalogs.types.filter(item => item.behavior === "card" && (item.isActive || item.code === purchase.expenseTypeCode)).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
    </div>
    {type && <CustomFieldInputs key={`${type.code}-${type.version}`} fields={type.fields} values={type.code === purchase.expenseTypeCode ? purchase.fields : undefined} />}
    <div className="form-row">
      <label>Categoria<select name="category" defaultValue={purchase.categoryCode ?? ""} key={type?.code ?? "none"}><option value="">Sem categoria</option>
        {catalogs.categories.filter(item => item.isActive || item.code === purchase.categoryCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <label>Quem comprou<select name="buyer" defaultValue={purchase.buyerPersonCode ?? ""}><option value="">Não informado</option>
        {catalogs.people.filter(person => person.isActive || person.code === purchase.buyerPersonCode).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
    </div>
    <p className="form-hint">{money(purchase.total)} em {purchase.count}x, comprado em {civilDate(purchase.purchaseDate)} no {purchase.cardName}. Para mudar valor, data ou parcelas, exclua a compra e lance de novo.</p>
    <ModalActions busy={action.busy} label="Salvar compra" onCancel={onClose} />
  </form></Modal>;
}
