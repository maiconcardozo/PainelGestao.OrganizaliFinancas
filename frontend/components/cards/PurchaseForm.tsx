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

const purchaseSaved = "Compra lançada. As parcelas já aparecem nas faturas; nenhuma conta foi movimentada.";

/**
 * RF24: purchase on the card with a preview of the invoices; the first invoice can be moved one cycle before saving (D13 premise A06).
 * A21: when the first installment falls in an invoice before the household's first month of use (or, without a start date, in an invoice already
 * due), the purchase is confirmed before it is saved, so a wrong date does not slip into an old invoice unnoticed.
 */
export function PurchaseModal({ cards, catalogs, today, startDate, onClose, onSaved }: { cards: Card[]; catalogs: Catalogs; today: string; startDate: string | null;
  onClose: () => void; onSaved: (message: string) => void }) {
  const action = useFinanceAction();
  const [typeCode, setTypeCode] = useState("");
  const [preview, setPreview] = useState<PurchasePreview | null>(null);
  const [running, setRunning] = useState(false);
  const [pending, setPending] = useState<{ payload: unknown; month: string } | null>(null);
  const active = cards.filter(card => card.isActive);
  const type = catalogs.types.find(item => item.code === typeCode);
  const firstMonth = startDate?.slice(0, 7) ?? null;
  function previewPayload(data: FormData) {
    return { cardCode: data.get("card"), purchaseDate: data.get("purchaseDate"), total: toApiMoney(data.get("total")), count: Number(data.get("count")),
      firstInvoiceShift: running ? 0 : Number(data.get("shift")), currentInstallment: running ? Number(data.get("currentInstallment")) : 1,
      currentInvoiceMonth: running ? `${data.get("currentInvoice")}-01` : null };
  }
  async function showPreview(event: React.MouseEvent<HTMLButtonElement>) {
    setPreview(await action.send<PurchasePreview>("card-purchases/preview", "POST", previewPayload(new FormData(event.currentTarget.form!)), "Prévia pronta. Nada foi gravado."));
  }
  async function submit(payload: unknown) {
    setPending(null);
    if (await action.send("card-purchases", "POST", payload, purchaseSaved, true)) onSaved(purchaseSaved);
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const payload = {
      ...previewPayload(data), buyerPersonCode: data.get("buyer") || null,
      details: { description: data.get("description"), expenseTypeCode: type?.code ?? null, expenseTypeVersion: type?.version ?? null, categoryCode: data.get("category") || null,
        creditorCode: null, paymentMethodCode: null, payerPersonCode: null, fields: type ? fieldValues(data, type.fields) : null, splitMode: null, shares: null },
    };
    const plan = preview ?? await action.send<PurchasePreview>("card-purchases/preview", "POST", previewPayload(data), "Prévia pronta. Nada foi gravado.");
    if (!plan) return;
    if (!preview) setPreview(plan);
    const first = plan.items[0];
    const early = first !== undefined && (firstMonth ? first.referenceMonth < firstMonth : first.dueDate < today);
    if (early) setPending({ payload, month: first.referenceMonth }); else await submit(payload);
  }
  if (active.length === 0) return null;
  return <Modal title="Lançar compra no cartão" onClose={onClose} error={action.error} width="max-w-3xl">
    {pending && <Modal title="Compra em fatura antiga" onClose={() => setPending(null)} width="max-w-md">
      <div className="data-form">
        <p className="text-sm">Essa compra cai na fatura de <strong>{monthLabel(pending.month)}</strong>, que {firstMonth
          ? <>é anterior a <strong>{monthLabel(firstMonth)}</strong>, o primeiro mês do painel (Data de início de uso, em Casa)</> : "já venceu"}.
          Confira a <strong>Data da compra</strong> e as <strong>Parcelas</strong> antes de confirmar: o que já foi pago antes do painel não deve entrar.</p>
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={() => setPending(null)}>Voltar e corrigir</button>
          <button type="button" className="primary-button" disabled={action.busy} onClick={() => void submit(pending.payload)}>Lançar mesmo assim</button>
        </div>
      </div>
    </Modal>}
    <form className="data-form" onSubmit={save}>
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
    <label className="checkbox-label"><input type="checkbox" checked={running} onChange={event => { setRunning(event.target.checked); setPreview(null); }} />
      Compra já em andamento: as parcelas anteriores já vieram em faturas passadas</label>
    {running ? <div className="form-row">
      <label>Parcela atual<input type="number" name="currentInstallment" required min={1} max={120} defaultValue={2} onChange={() => setPreview(null)} /></label>
      <label>Está na fatura de<input type="month" name="currentInvoice" required defaultValue={today.slice(0, 7)} onChange={() => setPreview(null)} /></label>
    </div> : <label>Primeira parcela<select name="shift" defaultValue="0" onChange={() => setPreview(null)}>
      <option value="0">Na fatura indicada pelo fechamento</option><option value="-1">Uma fatura antes</option><option value="1">Uma fatura depois</option></select></label>}
    {running && <p className="form-hint">Valor total = parcela × número de parcelas. Só a parcela atual e as seguintes entram nas faturas; as anteriores não são lançadas de novo.</p>}
    <button type="button" className="secondary-button" disabled={action.busy} onClick={showPreview}>Ver em quais faturas cai</button>
    {preview && <div className="table-scroll"><table className="finance-table"><caption>{preview.items.length} parcela(s) a lançar somando {money(preview.total)}</caption>
      <thead><tr><th scope="col">Parcela</th><th scope="col">Fatura</th><th scope="col">Fecha</th><th scope="col">Vence</th><th scope="col" className="number">Valor</th></tr></thead>
      <tbody>{preview.items.map(item => <tr key={item.number}><td>{item.number}/{preview.count}</td><td>{monthLabel(item.referenceMonth)}</td>
        <td>{civilDate(item.closingDate)}</td><td>{civilDate(item.dueDate)}</td><td className="number">{money(item.amount)}</td></tr>)}</tbody></table></div>}
    <p className="form-hint">A compra não sai da conta bancária agora: ela entra na fatura, e o dinheiro sai uma única vez quando a fatura for paga.</p>
    <ModalActions busy={action.busy} label="Lançar compra" onCancel={onClose} />
  </form></Modal>;
}

/**
 * Edits what describes the purchase and, while no invoice holding it was paid and there was no refund (A20), also its amount, date and
 * installments: the items are then planned again in the invoices, as if the purchase were entered anew.
 */
export function PurchaseEditModal({ purchase, catalogs, today, onClose, onSaved }: { purchase: Purchase; catalogs: Catalogs; today: string; onClose: () => void; onSaved: (message: string) => void }) {
  const action = useFinanceAction();
  const [typeCode, setTypeCode] = useState(purchase.expenseTypeCode ?? "");
  const type = catalogs.types.find(item => item.code === typeCode);
  const reschedulable = !purchase.paymentCode && Number(purchase.refunded) === 0;
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const total = reschedulable ? toApiMoney(data.get("total")) : null;
    const count = reschedulable ? Number(data.get("count")) : null;
    const purchaseDate = reschedulable ? String(data.get("purchaseDate")) : null;
    const changed = reschedulable && (total !== purchase.total || count !== purchase.count || purchaseDate !== purchase.purchaseDate);
    const message = changed ? "Compra atualizada. As parcelas foram refeitas nas faturas." : "Compra atualizada.";
    const saved = await action.send(`card-purchases/${purchase.code}`, "PUT", {
      buyerPersonCode: data.get("buyer") || null, version: purchase.version,
      purchaseDate: changed ? purchaseDate : null, total: changed ? total : null, count: changed ? count : null, firstInvoiceShift: changed ? Number(data.get("shift") ?? 0) : 0,
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
    {reschedulable ? <>
      <div className="form-row">
        <label>Valor total<input name="total" required inputMode="decimal" defaultValue={purchase.total.replace(".", ",")} /></label>
        <label>Parcelas<input type="number" name="count" required min={1} max={120} defaultValue={purchase.count} /></label>
        <label>Data da compra<input type="date" name="purchaseDate" required defaultValue={purchase.purchaseDate} max={today} /></label>
      </div>
      <label>Primeira parcela (se mudar valor, data ou parcelas)<select name="shift" defaultValue="0">
        <option value="0">Na fatura indicada pelo fechamento</option><option value="-1">Uma fatura antes</option><option value="1">Uma fatura depois</option></select></label>
      <p className="form-hint">Compra no {purchase.cardName}. Mudar valor, data ou parcelas refaz as parcelas nas faturas do cartão; a mudança é recusada se uma fatura com esta compra já foi paga.</p>
    </> : <p className="form-hint">{money(purchase.total)} em {purchase.count}x, comprado em {civilDate(purchase.purchaseDate)} no {purchase.cardName}.
      {purchase.paymentCode ? " É o pagamento de uma despesa no cartão: para mudar o valor, estorne o pagamento em Despesas e pague de novo." : " Já teve estorno da loja, então valor, data e parcelas não mudam mais."}</p>}
    <ModalActions busy={action.busy} label="Salvar compra" onCancel={onClose} />
  </form></Modal>;
}
