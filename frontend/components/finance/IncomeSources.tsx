"use client";
import { useState, type FormEvent } from "react";
import { TrendingUp } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { EditButton, Empty, ModalActions, Pill } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { civilDate, frequencies, money, toApiMoney, type IncomeSource } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { useFinanceAction } from "./useFinanceAction";

type ListProps = { sources: IncomeSource[]; canEdit: boolean; onEdit: (source: IncomeSource) => void };

/** The income sources cadastro: one row per source, editing opens a new validity period in the modal. */
export function IncomeSourceList({ sources, canEdit, onEdit }: ListProps) {
  if (sources.length === 0) return <Empty title="Nenhuma fonte cadastrada. Uma fonte gera as previsões; entradas avulsas, como comissões, também podem ser registradas sem fonte." />;
  return <ListFrame>{sources.map(source => <li key={source.code} className={`source-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${source.isActive ? "" : "opacity-60"}`}>
    <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><TrendingUp size={17} /></span>
    <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{source.name} <span className="font-normal text-[var(--text-secondary)]">· {source.personName}</span></span>
      <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{frequencies[source.frequency]} desde {civilDate(source.validFrom)}{source.endDate && ` até ${civilDate(source.endDate)}`}
        {` · média ${money(source.monthlyAverage)}/mês`}</span></span>
    <span className="hidden shrink-0 text-right sm:block">
      <span className="num block text-sm font-semibold">{source.amountMode === "fixed" ? money(source.expectedNetAmount) : "Variável"}</span>
      <span className="block text-xs text-[var(--text-muted)]">{source.amountMode === "fixed" ? (source.grossAmount ? `bruto ${money(source.grossAmount)}` : "líquido") : "pela média do ano"}</span>
    </span>
    {!source.isActive && <Pill tone="muted">Inativa</Pill>}
    {canEdit && <EditButton name={source.name} onClick={() => onEdit(source)} />}
  </li>)}</ListFrame>;
}

type FormProps = { source: IncomeSource | null; people: Person[]; incomeTypes: CatalogItem[]; today: string; onClose: () => void; onSaved: (message: string) => void };

export function IncomeSourceModal({ source, people, incomeTypes, today, onClose, onSaved }: FormProps) {
  const action = useFinanceAction();
  const [mode, setMode] = useState(source?.amountMode ?? "fixed");
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const success = source ? "Nova vigência salva. Previsões já recebidas foram preservadas." : "Fonte criada e previsões geradas.";
    const saved = await action.send(source ? `income-sources/${source.code}` : "income-sources", source ? "PUT" : "POST", {
      personCode: source?.personCode ?? data.get("person"), incomeTypeCode: data.get("incomeType") || null, name: data.get("name"),
      isActive: source ? data.has("isActive") : true, version: source?.version ?? 0, validFrom: data.get("validFrom"), endDate: data.get("endDate") || null,
      frequency: data.get("frequency"), amountMode: mode,
      expectedNetAmount: mode === "fixed" ? toApiMoney(data.get("expected")) : null,
      grossAmount: mode === "fixed" && data.get("gross") ? toApiMoney(data.get("gross")) : null,
    }, success);
    if (saved) onSaved(success);
  }
  return <Modal title={source ? `Nova vigência de ${source.name}` : "Nova fonte de renda"} onClose={onClose} error={action.error}>
    <form className="data-form" onSubmit={save}>
      <div className="form-row">
        {source ? <p className="form-hint">Morador: {source.personName}. Para outro morador, cadastre outra fonte.</p> :
          <label>Morador<select name="person" required>{people.filter(person => person.isActive).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>}
        <label>Nome<input name="name" required maxLength={160} defaultValue={source?.name ?? ""} placeholder="Salário PJ, Comissões, Aluguel" autoComplete="off" /></label>
      </div>
      <fieldset className="choice-group"><legend>Como é o valor</legend>
        <label className="checkbox-label"><input type="radio" name="mode" checked={mode === "fixed"} onChange={() => setMode("fixed")} />Tem uma base fixa (ex.: PJ, salário)</label>
        <label className="checkbox-label"><input type="radio" name="mode" checked={mode === "variable"} onChange={() => setMode("variable")} />Varia todo mês (ex.: comissão); a previsão usa a média do ano</label>
      </fieldset>
      {mode === "fixed" && <div className="form-row">
        <label>Líquido esperado<input name="expected" required inputMode="decimal" defaultValue={source?.expectedNetAmount ?? ""} placeholder="14.000,00" /></label>
        <label>Bruto (opcional)<input name="gross" inputMode="decimal" defaultValue={source?.grossAmount ?? ""} /></label>
      </div>}
      <div className="form-row">
        <label>Periodicidade<select name="frequency" defaultValue={source?.frequency ?? "monthly"}>{Object.entries(frequencies).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
        <label>Tipo de renda<select name="incomeType" defaultValue={source?.incomeTypeCode ?? ""}><option value="">Sem tipo</option>
          {incomeTypes.filter(item => item.isActive || item.code === source?.incomeTypeCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>{source ? "Vale a partir de" : "Primeiro recebimento"}<input type="date" name="validFrom" required min={source?.validFrom} defaultValue={today} /></label>
        <label>Término (opcional)<input type="date" name="endDate" defaultValue={source?.endDate ?? ""} /></label>
      </div>
      {source && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={source.isActive} />Fonte ativa</label>}
      <p className="form-hint">As previsões são geradas até o mês atual mais 12 meses. Alterar a fonte não muda previsões anteriores nem as que já têm recebimento.</p>
      <ModalActions busy={action.busy} label={source ? "Salvar nova vigência" : "Cadastrar fonte"} onCancel={onClose} />
    </form>
  </Modal>;
}
