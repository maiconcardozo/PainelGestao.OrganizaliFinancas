"use client";
import { useState, type FormEvent } from "react";
import { Wallet } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { AttachmentsToggle } from "@/components/common/Attachments";
import { Empty, ModalActions } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { civilDate, money, monthLabel, toApiMoney, type Account, type Forecast, type IncomeSource, type Receipt } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { ReverseButton } from "./ReverseDialog";
import { useFinanceAction } from "./useFinanceAction";

type FormProps = { accounts: Account[]; people: Person[]; sources: IncomeSource[]; forecasts: Forecast[]; incomeTypes: CatalogItem[]; today: string; onClose: () => void; onSaved: (message: string) => void };

/** D22: any real entry can be registered at any time; linking it to a forecast is optional. */
export function ReceiptModal({ accounts, people, sources, forecasts, incomeTypes, today, onClose, onSaved }: FormProps) {
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
    const data = new FormData(event.currentTarget);
    const amount = toApiMoney(data.get("amount"));
    const saved = await action.send("receipts", "POST", {
      accountCode: data.get("account"), personCode, incomeSourceCode: sourceCode || null, incomeTypeCode: data.get("incomeType") || null,
      receiptDate: data.get("date"), amount, description: data.get("description") || null,
      allocations: forecastCode ? [{ forecastCode, amount }] : null,
    }, "Recebimento registrado.", true);
    if (saved) onSaved("Recebimento registrado.");
  }
  if (accounts.filter(account => account.isActive).length === 0 || activePeople.length === 0)
    return <Modal title="Registrar recebimento" onClose={onClose} width="max-w-md">
      <p className="text-sm text-[var(--text-secondary)]">Cadastre ao menos uma conta e um morador para registrar as entradas de dinheiro.</p>
    </Modal>;
  return <Modal title="Registrar recebimento" onClose={onClose} error={action.error}>
    <form className="data-form" onSubmit={save}>
      <p className="form-hint">Informe o valor que realmente entrou. Se for maior que a base prevista, ele conta inteiro e a diferença aparece no comparativo.</p>
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
      <ModalActions busy={action.busy} label="Registrar recebimento" onCancel={onClose} />
    </form>
  </Modal>;
}

export function ReceiptList({ receipts, canReverse, canEdit = false, onDone }: { receipts: Receipt[]; canReverse: boolean; canEdit?: boolean; onDone: (message: string) => void }) {
  if (receipts.length === 0) return <Empty title="Nenhum recebimento registrado neste ano." />;
  return <ListFrame>{receipts.map(receipt => <li key={receipt.code} className={`receipt-item flex flex-wrap items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4 ${receipt.isReversed ? "opacity-60" : ""}`}>
    <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--success)]"><Wallet size={17} /></span>
    <span className="min-w-0 flex-1"><span className="block font-semibold">{receipt.personName}{receipt.sourceName && <span className="font-normal text-[var(--text-secondary)]"> · {receipt.sourceName}</span>}</span>
      <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(receipt.receiptDate)} em {receipt.accountName}{receipt.description && ` · ${receipt.description}`}{receipt.isReversed && ` · Estornado: ${receipt.reversalReason}`}</span></span>
    <span className="num shrink-0 text-sm font-semibold text-[var(--success)]">{money(receipt.amount)}</span>
    <AttachmentsToggle kind="receipt" target={receipt.code} canEdit={canEdit && !receipt.isReversed} />
    {canReverse && !receipt.isReversed && <ReverseButton path={`receipts/${receipt.code}/reversals`} title="Estornar recebimento" success="Recebimento estornado." onDone={onDone}
      summary={<>{money(receipt.amount)} recebido por {receipt.personName} em {civilDate(receipt.receiptDate)} sai de {receipt.accountName}, e a previsão volta a ficar em aberto.</>} />}
  </li>)}</ListFrame>;
}
