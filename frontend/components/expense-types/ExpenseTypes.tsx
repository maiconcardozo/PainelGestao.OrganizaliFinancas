"use client";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { behaviors, type ExpenseType, type FieldDefinition } from "@/lib/expense-types";
import type { CatalogItem } from "@/lib/catalogs";
import { FieldEditor } from "./FieldEditor";
import { FieldPreview } from "./FieldPreview";
export function ExpenseTypes({ types, categories, canEdit }: { types: ExpenseType[]; categories: CatalogItem[]; canEdit: boolean }) {
  const router = useRouter();
  const [selected, setSelected] = useState<ExpenseType | null>(null);
  const [fields, setFields] = useState<FieldDefinition[]>([]);
  const [preview, setPreview] = useState<ExpenseType | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  function edit(type: ExpenseType | null) { setSelected(type); setFields(type?.fields ?? []); setError(""); setMessage(""); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); setBusy(true); setError(""); setMessage("");
    try {
      const response = await fetch(`/api/backend/expense-types${selected ? `/${selected.code}` : ""}`, {
        method: selected ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({
          name: data.get("name"), behavior: data.get("behavior"), defaultCategoryCode: data.get("category") || null,
          isActive: selected ? data.has("active") : true, version: selected?.version ?? 0,
          fields: fields.map(field => ({ ...field, options: field.options?.map(value => value.trim()).filter(Boolean) ?? null })),
        }),
      });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(Object.values(body?.errors ?? {}).flat().join(" ") || body?.detail || "Não foi possível salvar o tipo."); return; }
      edit(body.data); setMessage("Tipo salvo. As definições anteriores foram preservadas."); router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <section className="page-content"><h1>Tipos de despesa</h1>
    <p className="intro">Crie tipos como Água, Luz ou uma compra da casa e escolha quais informações precisam ser preenchidas.</p>
    <p className="availability">Aqui você prepara os tipos. Lançamentos, parcelas e faturas serão disponibilizados nas próximas etapas do piloto.</p>
    <div className="type-list">{types.length === 0 && <p>Nenhum tipo cadastrado.</p>}{types.map(type => <article key={type.code}>
      <div><h2>{type.name}</h2><p>{behaviors[type.behavior]} · Versão {type.version} · {type.isActive ? "Ativo" : "Inativo"}</p></div>
      <div className="type-actions">{canEdit && <button className="secondary-button" type="button" onClick={() => edit(type)}>Editar tipo</button>}
        <button className="secondary-button" type="button" onClick={() => setPreview(type)}>Experimentar campos</button></div>
    </article>)}</div>
    {preview && <FieldPreview key={`${preview.code}-${preview.version}`} type={preview} />}
    {canEdit && <><button className="secondary-button" type="button" onClick={() => edit(null)}>Novo tipo</button>
      <form className="data-form" key={`${selected?.code ?? "new"}-${selected?.version ?? 0}`} onSubmit={save}>
        <h2>{selected ? `Editar ${selected.name}` : "Novo tipo de despesa"}</h2>
        {error && <p className="form-error" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
        <label>Nome<input name="name" required maxLength={160} defaultValue={selected?.name ?? ""} /></label>
        <label>Como funciona<select name="behavior" defaultValue={selected?.behavior ?? "single"}>{Object.entries(behaviors).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
        <label>Categoria sugerida<select name="category" defaultValue={selected?.defaultCategoryCode ?? ""}><option value="">Sem sugestão</option>
          {categories.filter(item => item.isActive || item.code === selected?.defaultCategoryCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
        {selected && <label className="checkbox-label"><input type="checkbox" name="active" defaultChecked={selected.isActive} />Tipo ativo</label>}
        <FieldEditor fields={fields} onChange={setFields} /><button className="primary-button" disabled={busy}>Salvar tipo</button>
      </form></>}
  </section>;
}
