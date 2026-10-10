"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, monthLabel, toApiMoney, type Account, type Forecast, type IncomeSource, type Receipt } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { useFinanceAction } from "./useFinanceAction";

type FormProps = { accounts: Account[]; people: Person[]; sources: IncomeSource[]; forecasts: Forecast[]; incomeTypes: CatalogItem[]; today: string };

/** D22: any real entry can be registered at any time; linking it to a forecast is optional. */
export function ReceiptForm({ accounts, people, sources, forecasts, incomeTypes, today }: FormProps) {
  const action = useFinanceAction();
  const activePeople = people.filter(person => person.isActive);
  const [personCode, setPersonCode] = useState(activePeople[0]?.code ?? "");
  const [sourceCode, setSourceCode] = useState("");
  const [forecastCode, setForecastCode] = useState("");
  const personSources = sources.filter(source => source.isActive && source.personCode === personCode);
  const openForecasts = forecasts.filter(item => item.sourceCode === sourceCode && item.status === "open");
  function chooseSource(code: string) {
    setSourceCode(code);
    const pending = forecasts.filter(item => item.sourceCode === code && item.status === "open");
    setForecastCode((pending.find(item => item.occurrenceDate <= today && Number(item.residual ?? 0) > 0) ?? pending[0])?.code ?? "");
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget; const data = new FormData(form);
    const amount = toApiMoney(data.get("amount"));
    const saved = await action.send("receipts", "POST", {
      accountCode: data.get("account"), personCode, incomeSourceCode: sourceCode || null, incomeTypeCode: data.get("incomeType") || null,
      receiptDate: data.get("date"), amount, description: data.get("description") || null,
      allocations: forecastCode ? [{ forecastCode, amount }] : null,
    }, "Recebimento registrado.", true);
    if (saved) { form.reset(); setSourceCode(""); setForecastCode(""); }
  }
  if (accounts.filter(account => account.isActive).length === 0 || activePeople.length === 0)
    return <div className="empty-state"><h2>Registrar recebimento</h2><p>Cadastre ao menos uma conta e um morador para registrar as entradas de dinheiro.</p></div>;
  return <form className="data-form" onSubmit={save}><h2>Registrar recebimento</h2>
    <p className="form-hint">Informe o valor que realmente entrou. Se for maior que a base prevista, ele conta inteiro e a diferença aparece no comparativo.</p>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    <div className="form-row">
      <label>Quem recebeu<select name="receiptPerson" value={personCode} onChange={event => { setPersonCode(event.target.value); chooseSource(""); }} required>
        {activePeople.map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
      <label>Fonte de renda<select name="receiptSource" value={sourceCode} onChange={event => chooseSource(event.target.value)}>
        <option value="">Entrada avulsa, sem fonte</option>{personSources.map(source => <option key={source.code} value={source.code}>{source.name}</option>)}</select></label>
    </div>
    {sourceCode && <label>Previsão atendida<select name="receiptForecast" value={forecastCode} onChange={event => setForecastCode(event.target.value)}>
      <option value="">Não vincular a uma previsão</option>
      {openForecasts.map(item => <option key={item.code} value={item.code}>{monthLabel(item.competence)} · {civilDate(item.occurrenceDate)} · {item.expectedAmount ? `previsto ${money(item.expectedAmount)}` : "valor variável"}</option>)}
    </select></label>}
    {!sourceCode && <label>Tipo de renda<select name="incomeType" defaultValue=""><option value="">Sem tipo</option>
      {incomeTypes.filter(item => item.isActive).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>}
    <div className="form-row">
      <label>Conta de destino<select name="account" required>{accounts.filter(account => account.isActive).map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
      <label>Data em que entrou<input type="date" name="date" required defaultValue={today} max={today} /></label>
    </div>
    <div className="form-row">
      <label>Valor recebido<input name="amount" required inputMode="decimal" placeholder="16.000,00" /></label>
      <label>Descrição<input name="description" maxLength={200} placeholder="Opcional, ex.: comissão plano X" /></label>
    </div>
    <button className="primary-button" disabled={action.busy}>Registrar recebimento</button>
  </form>;
}

export function ReceiptList({ receipts, canReverse }: { receipts: Receipt[]; canReverse: boolean }) {
  if (receipts.length === 0) return <p className="form-hint">Nenhum recebimento registrado neste ano.</p>;
  return <ul className="entry-list">{receipts.map(receipt => <ReceiptRow key={receipt.code} receipt={receipt} canReverse={canReverse} />)}</ul>;
}

function ReceiptRow({ receipt, canReverse }: { receipt: Receipt; canReverse: boolean }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  async function reverse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await action.send(`receipts/${receipt.code}/reversals`, "POST", { reason: new FormData(event.currentTarget).get("reason") }, "Recebimento estornado.", true);
  }
  return <li className={receipt.isReversed ? "muted-row" : ""}>
    <div><strong>{money(receipt.amount)}</strong> · {receipt.personName}{receipt.sourceName && ` · ${receipt.sourceName}`}
      <span className="entry-meta">{civilDate(receipt.receiptDate)} em {receipt.accountName}{receipt.description && ` · ${receipt.description}`}</span>
      {receipt.isReversed && <span className="entry-meta">Estornado: {receipt.reversalReason}</span>}</div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {canReverse && !receipt.isReversed && (open
      ? <form className="inline-form" onSubmit={reverse}><label>Motivo do estorno<input name="reason" required maxLength={300} /></label>
        <button className="secondary-button" disabled={action.busy}>Confirmar estorno</button></form>
      : <button type="button" className="secondary-button" onClick={() => setOpen(true)}>Estornar</button>)}
  </li>;
}
