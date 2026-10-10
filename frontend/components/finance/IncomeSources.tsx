"use client";
import { useState, type FormEvent } from "react";
import { civilDate, frequencies, money, toApiMoney, type IncomeSource } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { useFinanceAction } from "./useFinanceAction";

type Props = { sources: IncomeSource[]; people: Person[]; incomeTypes: CatalogItem[]; canEdit: boolean; today: string };

export function IncomeSources({ sources, people, incomeTypes, canEdit, today }: Props) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState<IncomeSource | null>(null);
  const [mode, setMode] = useState("fixed");
  function edit(source: IncomeSource | null) { setEditing(source); setMode(source?.amountMode ?? "fixed"); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget; const data = new FormData(form);
    const saved = await action.send(editing ? `income-sources/${editing.code}` : "income-sources", editing ? "PUT" : "POST", {
      personCode: editing?.personCode ?? data.get("person"), incomeTypeCode: data.get("incomeType") || null, name: data.get("name"),
      isActive: editing ? data.has("isActive") : true, version: editing?.version ?? 0, validFrom: data.get("validFrom"), endDate: data.get("endDate") || null,
      frequency: data.get("frequency"), amountMode: mode,
      expectedNetAmount: mode === "fixed" ? toApiMoney(data.get("expected")) : null,
      grossAmount: mode === "fixed" && data.get("gross") ? toApiMoney(data.get("gross")) : null,
    }, editing ? "Nova vigência salva. Previsões já recebidas foram preservadas." : "Fonte criada e previsões geradas.");
    if (saved) { form.reset(); edit(null); }
  }
  return <section aria-labelledby="sources-title"><h2 id="sources-title">Fontes de renda</h2>
    {sources.length === 0 ? <p className="form-hint">Nenhuma fonte cadastrada. Uma fonte gera as previsões; entradas avulsas, como comissões, também podem ser registradas sem fonte.</p> :
      <ul className="entry-list">{sources.map(source => <li key={source.code} className={source.isActive ? "" : "muted-row"}>
        <div><strong>{source.name}</strong> · {source.personName}
          <span className="entry-meta">{frequencies[source.frequency]} desde {civilDate(source.validFrom)}{source.endDate && ` até ${civilDate(source.endDate)}`}
            {source.amountMode === "fixed" ? ` · líquido ${money(source.expectedNetAmount)}${source.grossAmount ? ` (bruto ${money(source.grossAmount)})` : ""}` : " · valor variável"}
            {` · média ${money(source.monthlyAverage)}/mês`}{!source.isActive && " · inativa"}</span></div>
        {canEdit && <button type="button" className="secondary-button" onClick={() => edit(source)}>Editar</button>}
      </li>)}</ul>}
    {canEdit && <form className="data-form" key={`${editing?.code ?? "new"}-${editing?.version ?? 0}`} onSubmit={save}>
      <h3>{editing ? `Nova vigência de ${editing.name}` : "Nova fonte de renda"}</h3>
      {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
      <div className="form-row">
        {editing ? <p className="form-hint">Morador: {editing.personName}. Para outro morador, cadastre outra fonte.</p> :
          <label>Morador<select name="person" required>{people.filter(person => person.isActive).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>}
        <label>Nome<input name="name" required maxLength={160} defaultValue={editing?.name ?? ""} placeholder="Salário PJ, Comissões, Aluguel" /></label>
      </div>
      <fieldset className="choice-group"><legend>Como é o valor</legend>
        <label className="checkbox-label"><input type="radio" name="mode" checked={mode === "fixed"} onChange={() => setMode("fixed")} />Tem uma base fixa (ex.: PJ, salário)</label>
        <label className="checkbox-label"><input type="radio" name="mode" checked={mode === "variable"} onChange={() => setMode("variable")} />Varia todo mês (ex.: comissão); a previsão usa a média do ano</label>
      </fieldset>
      {mode === "fixed" && <div className="form-row">
        <label>Líquido esperado<input name="expected" required inputMode="decimal" defaultValue={editing?.expectedNetAmount ?? ""} placeholder="14.000,00" /></label>
        <label>Bruto (opcional)<input name="gross" inputMode="decimal" defaultValue={editing?.grossAmount ?? ""} /></label>
      </div>}
      <div className="form-row">
        <label>Periodicidade<select name="frequency" defaultValue={editing?.frequency ?? "monthly"}>{Object.entries(frequencies).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
        <label>Tipo de renda<select name="incomeType" defaultValue={editing?.incomeTypeCode ?? ""}><option value="">Sem tipo</option>
          {incomeTypes.filter(item => item.isActive || item.code === editing?.incomeTypeCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>{editing ? "Vale a partir de" : "Primeiro recebimento"}<input type="date" name="validFrom" required min={editing?.validFrom} defaultValue={today} /></label>
        <label>Término (opcional)<input type="date" name="endDate" defaultValue={editing?.endDate ?? ""} /></label>
      </div>
      {editing && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={editing.isActive} />Fonte ativa</label>}
      <p className="form-hint">As previsões são geradas até o mês atual mais 12 meses. Alterar a fonte não muda previsões anteriores nem as que já têm recebimento.</p>
      <div className="row-actions"><button className="primary-button" disabled={action.busy}>{editing ? "Salvar nova vigência" : "Cadastrar fonte"}</button>
        {editing && <button type="button" className="secondary-button" onClick={() => edit(null)}>Cancelar edição</button>}</div>
    </form>}
  </section>;
}
