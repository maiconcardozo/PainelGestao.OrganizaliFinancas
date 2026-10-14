"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { CreditCard, FolderTree, Store, Tag, TrendingUp, type LucideIcon } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card, EditButton, Empty, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { catalogs, type CatalogKey, type CatalogItem } from "@/lib/catalogs";

const singular: Record<CatalogKey, string> = { categories: "categoria", "income-types": "tipo de renda", creditors: "credor", "payment-methods": "forma de pagamento" };
const plural: Record<CatalogKey, string> = { categories: "categorias", "income-types": "tipos de renda", creditors: "credores", "payment-methods": "formas de pagamento" };
const newLabel: Record<CatalogKey, string> = { categories: "Nova categoria", "income-types": "Novo tipo de renda", creditors: "Novo credor", "payment-methods": "Nova forma de pagamento" };
const icons: Record<CatalogKey, LucideIcon> = { categories: FolderTree, "income-types": TrendingUp, creditors: Store, "payment-methods": CreditCard };

export function CatalogForm({ kind, items, canEdit }: { kind: CatalogKey; items: CatalogItem[]; canEdit: boolean }) {
  const action = useFinanceAction();
  const [view, chooseView] = useViewMode(`organizali.catalog.${kind}.view`);
  const [editing, setEditing] = useState<CatalogItem | "new" | null>(null);
  const current = editing === "new" ? null : editing;
  const Icon = icons[kind] ?? Tag;
  const parentName = (item: CatalogItem) => item.parentCode ? items.find(parent => parent.code === item.parentCode)?.name : null;

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(`catalogs/${kind}${current ? `/${current.code}` : ""}`, current ? "PUT" : "POST",
      { name: data.get("name"), isActive: current ? data.has("isActive") : true, version: current?.version ?? 0, parentCode: data.get("parentCode") || null },
      "Cadastro salvo.");
    if (saved) setEditing(null);
  }

  const status = (item: CatalogItem) => <Pill tone={item.isActive ? "success" : "muted"}>{item.isActive ? "Ativo" : "Inativo"}</Pill>;

  return <section className="page-content">
    <PageHeader title={catalogs[kind]} description="Os nomes usados nos lançamentos da casa. Alterar um nome preserva as versões anteriores para o histórico financeiro."
      action={canEdit && <NewButton label={newLabel[kind]} onClick={() => setEditing("new")} />} />
    <nav className="catalog-tabs" aria-label="Cadastros financeiros">{Object.entries(catalogs).map(([key, name]) =>
      <Link key={key} href={`/catalogs/${key}`} aria-current={kind === key ? "page" : undefined}>{name}</Link>)}</nav>
    <Notices error={editing ? undefined : action.error} message={action.message} />

    <ListBar count={items.length} singular={singular[kind]} plural={plural[kind]} view={view} onChange={chooseView} label={`Como mostrar ${plural[kind]}`} />
    {items.length === 0 ? <Empty title={canEdit ? "Nada cadastrado por aqui. Adicione os nomes que fazem sentido para a sua casa ou carregue o conjunto padrão." : "Nada cadastrado. Um titular ou editor pode adicionar os cadastros da casa."}
      action={canEdit && <button type="button" className="secondary-button" disabled={action.busy}
        onClick={() => void action.send("catalogs/defaults", "POST", undefined, "Cadastros padrão carregados: categorias, tipos de renda, credores, formas de pagamento e tipos de despesa.")}>
        {action.busy ? "Carregando…" : "Carregar cadastros padrão"}</button>} /> :
      view === "list" ? <ListFrame>{items.map(item => <li key={item.code} className={`flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${item.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Icon size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{item.name}</span>
          {parentName(item) && <span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">Dentro de {parentName(item)}</span>}</span>
        {status(item)}
        {canEdit && <EditButton name={item.name} onClick={() => setEditing(item)} />}
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{items.map(item => <Card key={item.code} muted={!item.isActive}>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0"><p className="flex items-center gap-2 font-semibold"><Icon size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{item.name}</span></p>
            {parentName(item) && <p className="mt-0.5 text-xs text-[var(--text-secondary)]">Dentro de {parentName(item)}</p>}</div>
          {canEdit && <EditButton name={item.name} onClick={() => setEditing(item)} />}
        </div>
        <div>{status(item)}</div>
      </Card>)}</div>}

    <p className="form-hint mt-4">Inativar preserva o registro e os lançamentos que o usam.</p>

    {editing && <Modal title={current ? `Editar ${current.name}` : newLabel[kind]} onClose={() => setEditing(null)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={save}>
        <label>Nome<input name="name" required maxLength={160} defaultValue={current?.name ?? ""} autoComplete="off" /></label>
        {kind === "categories" && <label>Categoria superior<select name="parentCode" defaultValue={current?.parentCode ?? ""}>
          <option value="">Sem categoria superior</option>
          {items.filter(parent => parent.code !== current?.code && (parent.isActive || parent.code === current?.parentCode)).map(parent => <option key={parent.code} value={parent.code}>{parent.name}</option>)}
        </select></label>}
        {current && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={current.isActive} />Cadastro ativo</label>}
        <ModalActions busy={action.busy} label={current ? "Salvar" : "Cadastrar"} onCancel={() => setEditing(null)} />
      </form>
    </Modal>}
  </section>;
}
