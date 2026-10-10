"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { catalogs, type CatalogKey, type CatalogItem } from "@/lib/catalogs";

export function CatalogForm({ kind, items, canEdit }: { kind: CatalogKey; items: CatalogItem[]; canEdit: boolean }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  function parentPicker(item?: CatalogItem) {
    return kind === "categories" && <label>Categoria superior<select name="parentCode" defaultValue={item?.parentCode ?? ""} disabled={!canEdit}>
      <option value="">Sem categoria superior</option>{items.filter(parent => parent.code !== item?.code && (parent.isActive || parent.code === item?.parentCode))
        .map(parent => <option key={parent.code} value={parent.code}>{parent.name}</option>)}
    </select></label>;
  }
  async function save(event: FormEvent<HTMLFormElement>, item?: CatalogItem) {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    setBusy(true); setError(""); setMessage("");
    try {
      const response = await fetch(`/api/backend/catalogs/${kind}${item ? `/${item.code}` : ""}`, {
        method: item ? "PUT" : "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name: data.get("name"), isActive: item ? data.has("isActive") : true, version: item?.version ?? 0, parentCode: data.get("parentCode") || null }),
      });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(body?.detail ?? "Não foi possível salvar o cadastro."); return; }
      setMessage("Cadastro salvo."); if (!item) form.reset(); router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <section className="page-content"><h1>{catalogs[kind]}</h1>
    <nav className="catalog-tabs" aria-label="Cadastros financeiros">{Object.entries(catalogs).map(([key, name]) =>
      <Link key={key} href={`/catalogs/${key}`} aria-current={kind === key ? "page" : undefined}>{name}</Link>)}</nav>
    <p className="intro">Organize os nomes usados nos lançamentos da casa.</p>
    {error && <p role="alert" className="form-error">{error}</p>}{message && <p role="status">{message}</p>}
    {canEdit && <form className="data-form" onSubmit={event => void save(event)}><h2>Novo cadastro</h2>
      <label>Nome<input name="name" required maxLength={160} /></label>{parentPicker()}<button className="primary-button" disabled={busy}>Cadastrar</button></form>}
    {items.length === 0 ? <div className="empty-state"><h2>Nenhum cadastro por aqui</h2><p>{canEdit ? "Adicione os nomes que fazem sentido para a sua casa." : "Um titular ou editor pode adicionar os cadastros da casa."}</p></div> :
      <div className="member-list">{items.map(item => <form className="data-form" key={`${item.code}-${item.version}`} onSubmit={event => void save(event, item)}>
        <label>Nome<input name="name" defaultValue={item.name} readOnly={!canEdit} required maxLength={160} /></label>{parentPicker(item)}
        <p>{item.isActive ? "Ativo" : "Inativo"}</p>{canEdit && <><label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={item.isActive} />Ativo</label>
          <button className="secondary-button" disabled={busy}>Salvar cadastro</button></>}
      </form>)}</div>}
    <p className="form-hint">Inativar preserva o registro. Alterações de nome mantêm as versões anteriores para o histórico financeiro.</p>
  </section>;
}
