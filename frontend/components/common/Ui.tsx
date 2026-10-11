"use client";

import type { ReactNode } from "react";
import { Pencil, Plus } from "lucide-react";

/** RevendaPRO page header: eyebrow (the menu group, drawn by CSS), display title, description and the main action on the right. */
export function PageHeader({ title, description, action }: { title: ReactNode; description?: ReactNode; action?: ReactNode }) {
  return <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
    <div className="min-w-0">
      <h1>{title}</h1>
      {description && <p className="mt-1 max-w-2xl text-sm text-[var(--text-secondary)]">{description}</p>}
    </div>
    {action}
  </div>;
}

/** The primary "Novo …" button that opens the creation modal. */
export function NewButton({ label, onClick }: { label: string; onClick: () => void }) {
  return <button type="button" onClick={onClick}
    className="inline-flex items-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--primary-strong)]">
    <Plus size={17} />{label}
  </button>;
}

/** Edit action, the same on the card and on the row. */
export function EditButton({ name, onClick }: { name: string; onClick: () => void }) {
  return <button type="button" onClick={onClick} aria-label={`Editar ${name}`} title="Editar"
    className="rounded-md p-2 text-[var(--text-secondary)] transition hover:bg-[var(--surface-2)] hover:text-[var(--primary)]">
    <Pencil size={15} />
  </button>;
}

/** Small icon action for anything else on a row (statement, preview, revoke). */
export function IconAction({ label, onClick, children, danger = false, disabled = false }: { label: string; onClick: () => void; children: ReactNode; danger?: boolean; disabled?: boolean }) {
  return <button type="button" onClick={onClick} aria-label={label} title={label} disabled={disabled}
    className={`rounded-md p-2 text-[var(--text-secondary)] transition hover:bg-[var(--surface-2)] ${danger ? "hover:text-[var(--critical)]" : "hover:text-[var(--primary)]"}`}>
    {children}
  </button>;
}

/** Empty state: says what is there, and the next step. */
export function Empty({ title, action }: { title: string; action?: ReactNode }) {
  return <div className="grid place-items-center rounded-xl border border-dashed border-[var(--border)] bg-[var(--surface)] p-10 text-center">
    <p className="text-sm font-medium">{title}</p>
    {action && <div className="mt-3">{action}</div>}
  </div>;
}

/** Page level messages, outside any modal. */
export function Notices({ error, message }: { error?: string; message?: string }) {
  return <>
    {error && <p role="alert" className="mb-4 rounded-md border border-[color-mix(in_srgb,var(--critical)_40%,transparent)] bg-[color-mix(in_srgb,var(--critical)_8%,transparent)] px-4 py-3 text-sm text-[var(--critical)]">{error}</p>}
    {message && <p role="status" className="mb-4 rounded-md border border-[color-mix(in_srgb,var(--success)_40%,transparent)] bg-[color-mix(in_srgb,var(--success)_8%,transparent)] px-4 py-3 text-sm text-[var(--success)]">{message}</p>}
  </>;
}

/** Status pill with the dot, as RevendaPRO's StatusPill. */
export function Pill({ tone, children }: { tone: "success" | "muted" | "warning" | "critical" | "primary"; children: ReactNode }) {
  const tones = {
    success: "bg-[color-mix(in_srgb,var(--success)_14%,transparent)] text-[var(--success)]",
    muted: "bg-[var(--surface-2)] text-[var(--text-muted)]",
    warning: "bg-[color-mix(in_srgb,var(--flare)_20%,transparent)] text-[var(--warning)]",
    critical: "bg-[color-mix(in_srgb,var(--critical)_16%,transparent)] text-[var(--critical)]",
    primary: "bg-[color-mix(in_srgb,var(--primary)_14%,transparent)] text-[var(--primary)]",
  };
  return <span className={`inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full px-2.5 py-1 text-[11px] font-semibold ${tones[tone]}`}>
    <span aria-hidden className="h-1.5 w-1.5 rounded-full" style={{ background: "currentColor" }} />{children}
  </span>;
}

/** Card of the mosaic view. */
export function Card({ children, muted = false }: { children: ReactNode; muted?: boolean }) {
  return <section className={`flex h-full flex-col gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)] ${muted ? "opacity-70" : ""}`}>{children}</section>;
}

/** Labelled figure inside a card (RevendaPRO "Total comprado" block). */
export function Figure({ label, value, hint }: { label: string; value: ReactNode; hint?: ReactNode }) {
  return <div className="rounded-lg bg-[var(--surface-2)] px-4 py-3">
    <p className="text-xs font-semibold uppercase tracking-wide text-[var(--text-muted)]">{label}</p>
    <p className="num text-xl font-bold">{value}</p>
    {hint && <p className="text-xs text-[var(--text-secondary)]">{hint}</p>}
  </div>;
}

/** Cancel + submit row at the foot of a modal form. */
export function ModalActions({ busy, label, onCancel }: { busy: boolean; label: string; onCancel: () => void }) {
  return <div className="modal-actions">
    <button type="button" onClick={onCancel} className="rounded-md border border-[var(--border)] px-4 py-2 text-sm font-medium text-[var(--text-secondary)] hover:bg-[var(--surface-2)]">Cancelar</button>
    <button type="submit" disabled={busy} className="rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--primary-strong)] disabled:opacity-60">{busy ? "Salvando..." : label}</button>
  </div>;
}
