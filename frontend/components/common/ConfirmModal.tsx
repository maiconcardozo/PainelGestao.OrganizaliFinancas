"use client";
import type { ReactNode } from "react";
import { Modal } from "@/components/common/Modal";

/** Destructive confirmation: says what goes away and what stays before the red button. */
export function ConfirmModal({ title, label, busy, error, onConfirm, onClose, children }: {
  title: string; label: string; busy: boolean; error?: string | null; onConfirm: () => void; onClose: () => void; children: ReactNode;
}) {
  return <Modal title={title} onClose={onClose} error={error} width="max-w-md">
    <div className="data-form">
      {children}
      <div className="modal-actions">
        <button type="button" onClick={onClose} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm font-medium text-[var(--text-secondary)] hover:bg-[var(--surface-2)]">Cancelar</button>
        <button type="button" disabled={busy} onClick={onConfirm} className="rounded-md bg-[var(--critical)] px-4 py-2 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60">{busy ? "Excluindo..." : label}</button>
      </div>
    </div>
  </Modal>;
}
