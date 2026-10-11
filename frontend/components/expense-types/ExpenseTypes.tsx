"use client";

import { useState, type FormEvent } from "react";
import { FlaskConical, Receipt } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card, EditButton, Empty, IconAction, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { behaviors, type ExpenseType, type FieldDefinition } from "@/lib/expense-types";
import type { CatalogItem } from "@/lib/catalogs";
import { FieldEditor } from "./FieldEditor";
import { FieldPreview } from "./FieldPreview";

export function ExpenseTypes({ types, categories, canEdit }: { types: ExpenseType[]; categories: CatalogItem[]; canEdit: boolean }) {
  const action = useFinanceAction();
  const [view, chooseView] = useViewMode("organizali.expense-types.view");
  const [editing, setEditing] = useState<ExpenseType | "new" | null>(null);
  const [fields, setFields] = useState<FieldDefinition[]>([]);
  const [preview, setPreview] = useState<ExpenseType | null>(null);
  const current = editing === "new" ? null : editing;
  const categoryName = (code: string | null) => code ? categories.find(item => item.code === code)?.name : null;

  function open(type: ExpenseType | "new") { setEditing(type); setFields(type === "new" ? [] : type.fields); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(`expense-types${current ? `/${current.code}` : ""}`, current ? "PUT" : "POST", {
      name: data.get("name"), behavior: data.get("behavior"), defaultCategoryCode: data.get("category") || null,
      isActive: current ? data.has("active") : true, version: current?.version ?? 0,
      fields: fields.map(field => ({ ...field, options: field.options?.map(value => value.trim()).filter(Boolean) ?? null })),
    }, "Tipo salvo. As definições anteriores foram preservadas.");
    if (saved) setEditing(null);
  }

  const meta = (type: ExpenseType) => `${behaviors[type.behavior]} · ${type.fields.length === 1 ? "1 campo adicional" : `${type.fields.length} campos adicionais`} · versão ${type.version}`;
  const actions = (type: ExpenseType) => <>
    <IconAction label={`Experimentar campos de ${type.name}`} onClick={() => setPreview(type)}><FlaskConical size={15} /></IconAction>
    {canEdit && <EditButton name={type.name} onClick={() => open(type)} />}
  </>;

  return <section className="page-content">
    <PageHeader title="Tipos de despesa" description="Água, Luz, uma compra da casa: cada tipo diz como a despesa funciona e quais informações precisam ser preenchidas."
      action={canEdit && <NewButton label="Novo tipo" onClick={() => open("new")} />} />
    <Notices error={editing || preview ? undefined : action.error} message={action.message} />

    <ListBar count={types.length} singular="tipo" plural="tipos" view={view} onChange={chooseView} label="Como mostrar os tipos" />
    {types.length === 0 ? <Empty title={canEdit ? "Nenhum tipo ainda. Crie o primeiro, como Água ou Luz." : "Nenhum tipo cadastrado."} /> :
      view === "list" ? <ListFrame>{types.map(type => <li key={type.code} className={`type-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${type.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Receipt size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{type.name}</span><span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">{meta(type)}</span></span>
        {!type.isActive && <Pill tone="muted">Inativo</Pill>}
        <span className="flex shrink-0 gap-1">{actions(type)}</span>
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{types.map(type => <div key={type.code} className="type-item"><Card muted={!type.isActive}>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0"><p className="flex items-center gap-2 font-semibold"><Receipt size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{type.name}</span></p>
            <p className="mt-0.5 text-xs text-[var(--text-secondary)]">{behaviors[type.behavior]}</p></div>
          <div className="flex shrink-0 gap-1">{actions(type)}</div>
        </div>
        <dl className="grid gap-y-1.5 text-sm">
          <div className="flex justify-between gap-3"><dt className="text-[var(--text-muted)]">Categoria sugerida</dt><dd>{categoryName(type.defaultCategoryCode) ?? "Nenhuma"}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-[var(--text-muted)]">Campos adicionais</dt><dd className="num">{type.fields.length}</dd></div>
        </dl>
        <div className="mt-auto"><Pill tone={type.isActive ? "success" : "muted"}>{type.isActive ? `Ativo · versão ${type.version}` : "Inativo"}</Pill></div>
      </Card></div>)}</div>}

    {editing && <Modal title={current ? `Editar ${current.name}` : "Novo tipo de despesa"} onClose={() => setEditing(null)} error={action.error}>
      <form className="data-form" onSubmit={save}>
        <label>Nome<input name="name" required maxLength={160} defaultValue={current?.name ?? ""} autoComplete="off" /></label>
        <div className="form-row">
          <label>Como funciona<select name="behavior" defaultValue={current?.behavior ?? "single"}>{Object.entries(behaviors).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
          <label>Categoria sugerida<select name="category" defaultValue={current?.defaultCategoryCode ?? ""}><option value="">Sem sugestão</option>
            {categories.filter(item => item.isActive || item.code === current?.defaultCategoryCode).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
        </div>
        {current && <label className="checkbox-label"><input type="checkbox" name="active" defaultChecked={current.isActive} />Tipo ativo</label>}
        <FieldEditor fields={fields} onChange={setFields} />
        <ModalActions busy={action.busy} label="Salvar tipo" onCancel={() => setEditing(null)} />
      </form>
    </Modal>}

    {preview && <Modal title={`Experimentar: ${preview.name}`} onClose={() => setPreview(null)} width="max-w-lg">
      <FieldPreview key={`${preview.code}-${preview.version}`} type={preview} />
    </Modal>}
  </section>;
}
