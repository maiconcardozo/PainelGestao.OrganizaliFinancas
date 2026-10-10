"use client";
import { fieldTypes, type FieldDefinition } from "@/lib/expense-types";
export function FieldEditor({ fields, onChange }: { fields: FieldDefinition[]; onChange: (fields: FieldDefinition[]) => void }) {
  function update(index: number, patch: Partial<FieldDefinition>) { onChange(fields.map((field, position) => position === index ? { ...field, ...patch } : field)); }
  return <fieldset className="field-definitions"><legend>Campos adicionais</legend>
    <p className="form-hint">Exemplos: número da instalação, leitura do medidor ou referência da compra.</p>
    {fields.map((field, index) => <div className="field-definition" key={field.key}>
      <label>Nome do campo<input value={field.label} maxLength={100} required onChange={event => update(index, { label: event.target.value })} /></label>
      <label>Tipo<select value={field.type} onChange={event => update(index, { type: event.target.value, maxLength: null, min: null, max: null, options: event.target.value === "list" ? [""] : null })}>
        {Object.entries(fieldTypes).map(([key, label]) => <option key={key} value={key}>{label}</option>)}
      </select></label>
      <label className="checkbox-label"><input type="checkbox" checked={field.required} onChange={event => update(index, { required: event.target.checked })} />Obrigatório</label>
      {field.type === "text" && <label>Máximo de caracteres<input type="number" min={1} max={2000} value={field.maxLength ?? ""} placeholder="2000" onChange={event => update(index, { maxLength: event.target.value ? Number(event.target.value) : null })} /></label>}
      {field.type === "decimal" && <div className="field-limits"><label>Valor mínimo<input type="number" step="any" value={field.min ?? ""} onChange={event => update(index, { min: event.target.value ? Number(event.target.value) : null })} /></label>
        <label>Valor máximo<input type="number" step="any" value={field.max ?? ""} onChange={event => update(index, { max: event.target.value ? Number(event.target.value) : null })} /></label></div>}
      {field.type === "list" && <label>Opções (uma por linha)<textarea value={field.options?.join("\n") ?? ""} maxLength={5100} required onChange={event => update(index, { options: event.target.value.split("\n") })} /></label>}
      <button className="secondary-button" type="button" onClick={() => onChange(fields.filter((_, position) => position !== index))}>Remover campo</button>
    </div>)}
    <button className="secondary-button" type="button" disabled={fields.length >= 30} onClick={() => onChange([...fields, {
      key: `campo_${crypto.randomUUID().replaceAll("-", "").slice(0, 16)}`, label: "", type: "text", required: false, maxLength: null, min: null, max: null, options: null,
    }])}>Adicionar campo</button>
  </fieldset>;
}
