"use client";
import { useState, type FormEvent } from "react";
import type { ExpenseType } from "@/lib/expense-types";
export function FieldPreview({ type }: { type: ExpenseType }) {
  const [busy, setBusy] = useState(false);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [message, setMessage] = useState("");
  async function validate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const form = new FormData(event.currentTarget); const values: Record<string, unknown> = {};
    for (const field of type.fields) {
      const value = String(form.get(field.key) ?? "");
      if (value === "") continue;
      values[field.key] = field.type === "decimal" ? Number(value) : field.type === "boolean" ? value === "true" : value;
    }
    setBusy(true); setErrors({}); setMessage("");
    try {
      const response = await fetch(`/api/backend/expense-types/${type.code}/validate`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ version: type.version, values }) });
      if (response.ok) { setMessage("Campos válidos. Nenhuma despesa foi criada."); return; }
      const body = await response.json().catch(() => null); setErrors(body?.errors ?? {}); setMessage(body?.detail ?? "Não foi possível validar os campos.");
    } catch { setMessage("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <form className="data-form" onSubmit={validate}><h2>Experimentar: {type.name}</h2>
    <p className="form-hint">Preencha para conferir as regras da versão {type.version}. Este teste não cria uma despesa.</p>
    {type.fields.length === 0 && <p>Este tipo não tem campos adicionais.</p>}
    {type.fields.map(field => <label key={field.key}>{field.label}{field.required ? " (obrigatório)" : ""}
      {field.type === "list" || field.type === "boolean" ? <select name={field.key} required={field.required} defaultValue="" aria-describedby={errors[field.key] ? `${field.key}-error` : undefined}>
        <option value="">Selecione</option>{field.type === "boolean" ? <><option value="true">Sim</option><option value="false">Não</option></> : field.options?.map(option => <option key={option} value={option}>{option}</option>)}
      </select> : <input name={field.key} required={field.required} type={field.type === "decimal" ? "number" : field.type === "date" ? "date" : "text"}
        min={field.min ?? undefined} max={field.max ?? undefined} step={field.type === "decimal" ? "any" : undefined} maxLength={field.maxLength ?? (field.type === "text" ? 2000 : undefined)} aria-describedby={errors[field.key] ? `${field.key}-error` : undefined} />}
      {errors[field.key] && <span className="field-error" id={`${field.key}-error`}>{errors[field.key].join(" ")}</span>}
    </label>)}
    {message && <p role="status">{message}</p>}
    <div className="modal-actions"><button className="primary-button" disabled={busy}>Conferir campos</button></div>
  </form>;
}
