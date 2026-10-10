"use client";
import { useState } from "react";
import type { CatalogItem } from "@/lib/catalogs";
import type { ExpenseType, FieldDefinition } from "@/lib/expense-types";
import type { Person } from "@/components/households/HouseholdForms";
import { fieldValues, sharesFrom } from "@/lib/expenses";

export type Catalogs = { types: ExpenseType[]; categories: CatalogItem[]; creditors: CatalogItem[]; methods: CatalogItem[]; people: Person[] };

/** Inputs for the custom fields of one expense type version, named field_<key>. */
export function CustomFieldInputs({ fields, values }: { fields: FieldDefinition[]; values?: Record<string, unknown> }) {
  if (fields.length === 0) return null;
  return <fieldset className="choice-group"><legend>Campos do tipo</legend><div className="form-row">
    {fields.map(field => {
      const current = values?.[field.key];
      const name = `field_${field.key}`;
      return <label key={field.key}>{field.label}{field.required ? "" : " (opcional)"}
        {field.type === "list" || field.type === "boolean"
          ? <select name={name} required={field.required} defaultValue={current === undefined ? "" : String(current)}>
            <option value="">Selecione</option>
            {field.type === "boolean" ? <><option value="true">Sim</option><option value="false">Não</option></> : field.options?.map(option => <option key={option} value={option}>{option}</option>)}
          </select>
          : <input name={name} required={field.required} defaultValue={current === undefined ? "" : String(current)}
            type={field.type === "decimal" ? "number" : field.type === "date" ? "date" : "text"} step={field.type === "decimal" ? "any" : undefined}
            min={field.min ?? undefined} max={field.max ?? undefined} maxLength={field.maxLength ?? (field.type === "text" ? 2000 : undefined)} />}
      </label>;
    })}
  </div></fieldset>;
}

/** D10: optional split by value or percent between residents; the payer stays a separate choice. */
export function ShareInputs({ people, percentOnly, initialMode, initial }: { people: Person[]; percentOnly: boolean; initialMode?: string | null;
  initial?: Record<string, string> }) {
  const [enabled, setEnabled] = useState(Boolean(initialMode));
  const [mode, setMode] = useState(percentOnly ? "percent" : initialMode ?? "percent");
  const active = people.filter(person => person.isActive || initial?.[person.code]);
  if (active.length < 2) return null;
  return <fieldset className="choice-group"><legend>Responsáveis</legend>
    <label className="checkbox-label"><input type="checkbox" name="splitEnabled" checked={enabled} onChange={event => setEnabled(event.target.checked)} />Dividir entre moradores</label>
    {enabled && <>
      {!percentOnly && <div className="row-actions" role="radiogroup" aria-label="Como dividir">
        <label className="checkbox-label"><input type="radio" name="splitMode" value="percent" checked={mode === "percent"} onChange={() => setMode("percent")} />Por percentual</label>
        <label className="checkbox-label"><input type="radio" name="splitMode" value="value" checked={mode === "value"} onChange={() => setMode("value")} />Por valor</label>
      </div>}
      {percentOnly && <input type="hidden" name="splitMode" value="percent" />}
      <div className="form-row">{active.map(person => <label key={person.code}>{person.name} {mode === "percent" ? "(%)" : "(R$)"}
        <input name={`share_${person.code}`} inputMode="decimal" defaultValue={initial?.[person.code] ?? ""} placeholder={mode === "percent" ? "50" : "50,00"} /></label>)}</div>
      <p className="form-hint">Deixe em branco quem não participa. {mode === "percent" ? "Os percentuais somam 100%." : "Os valores somam o total da despesa."} Quem paga continua sendo informado no pagamento.</p>
    </>}
  </fieldset>;
}

/** Description, type, category, creditor, method and expected payer; the type filters by behavior and drives the custom fields. */
export function DetailInputs({ catalogs, behavior, typeCode, onType, values }: { catalogs: Catalogs; behavior: string; typeCode: string;
  onType: (code: string) => void; values?: { description?: string; categoryCode?: string | null; creditorCode?: string | null; paymentMethodCode?: string | null;
    payerPersonCode?: string | null; fields?: Record<string, unknown> } }) {
  const types = catalogs.types.filter(type => type.behavior === behavior && (type.isActive || type.code === typeCode));
  const type = catalogs.types.find(item => item.code === typeCode);
  const activeOr = (items: CatalogItem[], code?: string | null) => items.filter(item => item.isActive || item.code === code);
  return <>
    <div className="form-row">
      <label>Descrição<input name="description" required maxLength={200} defaultValue={values?.description ?? ""} placeholder="Conta de luz, Aluguel, Mercado" /></label>
      <label>Tipo de despesa<select name="expenseType" value={typeCode} onChange={event => onType(event.target.value)}>
        <option value="">Sem tipo</option>{types.map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
    </div>
    {type && <CustomFieldInputs key={`${type.code}-${type.version}`} fields={type.fields} values={values?.fields} />}
    <div className="form-row">
      <label>Categoria<select name="category" defaultValue={values?.categoryCode ?? type?.defaultCategoryCode ?? ""} key={type?.code ?? "none"}>
        <option value="">{type?.defaultCategoryCode ? "Padrão do tipo" : "Sem categoria"}</option>
        {activeOr(catalogs.categories, values?.categoryCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <label>Credor<select name="creditor" defaultValue={values?.creditorCode ?? ""}><option value="">Sem credor</option>
        {activeOr(catalogs.creditors, values?.creditorCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
    </div>
    <div className="form-row">
      <label>Forma de pagamento<select name="paymentMethod" defaultValue={values?.paymentMethodCode ?? ""}><option value="">Não definida</option>
        {activeOr(catalogs.methods, values?.paymentMethodCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <label>Quem costuma pagar<select name="payer" defaultValue={values?.payerPersonCode ?? ""}><option value="">A casa</option>
        {catalogs.people.filter(person => person.isActive || person.code === values?.payerPersonCode).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
    </div>
  </>;
}

/** Builds the API "details" object from a form that rendered DetailInputs and ShareInputs. */
export function detailsFrom(data: FormData, catalogs: Catalogs, typeCode: string) {
  const type = catalogs.types.find(item => item.code === typeCode);
  const split = data.has("splitEnabled");
  const mode = String(data.get("splitMode") ?? "percent");
  return {
    description: data.get("description"), expenseTypeCode: type?.code ?? null, expenseTypeVersion: type?.version ?? null,
    categoryCode: data.get("category") || null, creditorCode: data.get("creditor") || null, paymentMethodCode: data.get("paymentMethod") || null,
    payerPersonCode: data.get("payer") || null, fields: type ? fieldValues(data, type.fields) : null,
    splitMode: split ? mode : null, shares: split ? sharesFrom(data, catalogs.people, mode) : null,
  };
}
