"use client";
import { useCallback, useEffect, useRef, useState, type ChangeEvent } from "react";
import { useRouter } from "next/navigation";
import { FileText, Paperclip, Trash2 } from "lucide-react";
import { civilDate } from "@/lib/finance";
import { fileSize, type Attachment, type AttachmentKind } from "@/lib/complements";
import { IconAction } from "./Ui";

const ACCEPT = "application/pdf,image/jpeg,image/png,image/webp,image/heic,.pdf,.jpg,.jpeg,.png,.webp,.heic";

/**
 * Comprovantes of one expense, payment or receipt. Files go through the authenticated proxy as multipart and are
 * opened from the same origin, so the browser cookie is the only credential involved.
 */
export function Attachments({ kind, target, canEdit, title = "Comprovantes" }: { kind: AttachmentKind; target: string; canEdit: boolean; title?: string }) {
  const router = useRouter();
  const [items, setItems] = useState<Attachment[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const input = useRef<HTMLInputElement>(null);

  const load = useCallback(async () => {
    try {
      const response = await fetch(`/api/backend/attachments?kind=${kind}&target=${target}`);
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = await response.json().catch(() => null);
      if (response.ok) setItems(body?.data ?? []); else setError(body?.detail ?? "Não foi possível carregar os comprovantes.");
    } catch { setError("Servidor indisponível. Tente novamente."); }
  }, [kind, target, router]);
  // The list comes from the API (external system); state is set in the response callbacks, never synchronously.
  // eslint-disable-next-line react-hooks/set-state-in-effect
  useEffect(() => { void load(); }, [load]);

  async function upload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    if (file.size > 10 * 1024 * 1024) { setError("O arquivo precisa ter até 10 MB."); return; }
    setBusy(true); setError("");
    try {
      const form = new FormData();
      form.append("file", file, file.name);
      const response = await fetch(`/api/backend/attachments?kind=${kind}&target=${target}`, { method: "POST", body: form });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(Object.values(body?.errors ?? {}).flat().join(" ") || body?.detail || "Não foi possível anexar o arquivo."); return; }
      await load();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  async function remove(item: Attachment) {
    setBusy(true); setError("");
    try {
      const response = await fetch(`/api/backend/attachments/${item.code}`, { method: "DELETE" });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.detail ?? "Não foi possível remover."); return; }
      await load();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }

  return <div className="attachments">
    <div className="flex flex-wrap items-center justify-between gap-2">
      <h3 className="!mt-0 flex items-center gap-2 text-sm"><Paperclip size={15} className="text-[var(--signal)]" />{title}{items && items.length > 0 && <span className="num text-xs font-normal text-[var(--text-muted)]">({items.length})</span>}</h3>
      {canEdit && <>
        <input ref={input} type="file" accept={ACCEPT} className="sr-only" onChange={upload} aria-label="Escolher arquivo do comprovante" />
        <button type="button" className="secondary-button" disabled={busy} onClick={() => input.current?.click()}>{busy ? "Enviando…" : "Anexar arquivo"}</button>
      </>}
    </div>
    {error && <p className="form-error" role="alert">{error}</p>}
    {items === null ? <p className="text-xs text-[var(--text-muted)]">Carregando…</p> :
      items.length === 0 ? <p className="text-xs text-[var(--text-muted)]">Nenhum comprovante. {canEdit ? "PDF ou foto (JPG, PNG, WEBP, HEIC) de até 10 MB." : ""}</p> :
      <ul className="entry-list compact">{items.map(item => <li key={item.code}>
        <a href={`/api/backend/attachments/${item.code}/content`} target="_blank" rel="noreferrer" className="flex min-w-0 items-center gap-2 text-[var(--primary)] hover:underline">
          <FileText size={15} className="shrink-0" /><span className="truncate">{item.fileName}</span>
          <span className="shrink-0 text-xs font-normal text-[var(--text-muted)]">{fileSize(item.size)} · {civilDate(item.createdAt.slice(0, 10))}</span></a>
        {canEdit && <IconAction label={`Remover ${item.fileName}`} danger disabled={busy} onClick={() => void remove(item)}><Trash2 size={14} /></IconAction>}
      </li>)}</ul>}
  </div>;
}

/** Paperclip toggle for list rows: opens the attachments of that row below it. */
export function AttachmentsToggle({ kind, target, canEdit }: { kind: AttachmentKind; target: string; canEdit: boolean }) {
  const [open, setOpen] = useState(false);
  return <>
    <IconAction label={open ? "Fechar comprovantes" : "Comprovantes"} onClick={() => setOpen(value => !value)}><Paperclip size={15} /></IconAction>
    {open && <div className="basis-full rounded-md border border-dashed border-[var(--border)] bg-[var(--surface-2)] p-3"><Attachments kind={kind} target={target} canEdit={canEdit} /></div>}
  </>;
}
