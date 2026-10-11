"use client";

import { useState, type FormEvent, type ReactNode } from "react";
import { Undo2 } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { useFinanceAction } from "./useFinanceAction";

/**
 * Reversal with a mandatory reason, in a modal as every destructive step in RevendaPRO.
 * The trigger keeps the text "Estornar" so the action reads the same on every list.
 */
export function ReverseButton({ path, title, summary, success, onDone }: { path: string; title: string; summary: ReactNode; success: string; onDone?: (message: string) => void }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  async function reverse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await action.send(path, "POST", { reason: new FormData(event.currentTarget).get("reason") }, success, true)) { setOpen(false); onDone?.(success); }
  }
  return <>
    <button type="button" onClick={() => setOpen(true)}
      className="inline-flex items-center gap-1.5 rounded-md border border-[var(--border)] px-3 py-1.5 text-xs font-semibold text-[var(--text-secondary)] transition hover:bg-[var(--surface-2)] hover:text-[var(--critical)]">
      <Undo2 size={14} aria-hidden />Estornar
    </button>
    {open && <Modal title={title} onClose={() => setOpen(false)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={reverse}>
        <p className="text-sm leading-relaxed text-[var(--text-secondary)]">{summary}</p>
        <label>Motivo do estorno<input name="reason" required maxLength={300} autoComplete="off" /></label>
        <div className="modal-actions">
          <button type="button" onClick={() => setOpen(false)} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm font-medium text-[var(--text-secondary)] hover:bg-[var(--surface-2)]">Cancelar</button>
          <button type="submit" disabled={action.busy} className="rounded-md bg-[var(--critical)] px-4 py-2 text-sm font-semibold text-white transition hover:brightness-110 disabled:opacity-60">{action.busy ? "Aguarde..." : "Confirmar estorno"}</button>
        </div>
      </form>
    </Modal>}
  </>;
}
