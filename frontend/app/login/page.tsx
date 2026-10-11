"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ArrowRight } from "lucide-react";
import { AuthShell } from "@/components/layout/AuthShell";

const label = "mb-1.5 block text-xs font-semibold uppercase tracking-wide text-[var(--text-muted)]";

export default function Login() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function signIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    setBusy(true);
    setError("");
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: form.get("email"), password: form.get("password") }),
      });
      if (!response.ok) {
        const body = await response.json().catch(() => null);
        setError(body?.detail ?? "Não foi possível entrar. Confira seus dados.");
        return;
      }
      router.replace("/household");
      router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }

  return <AuthShell>
    <form onSubmit={signIn} aria-busy={busy} className="w-full">
      <p className="font-display mb-1 text-xs font-bold uppercase tracking-[.18em] text-[var(--signal)]">Organizali</p>
      <p className="hero-title mb-1 text-3xl font-bold text-[var(--text-primary)]">Entrar na sua casa</p>
      <p className="mb-7 text-sm text-[var(--text-secondary)]">Use o acesso liberado para você no piloto.</p>
      {error && <p role="alert" className="mb-4 rounded-md border border-[color-mix(in_srgb,var(--critical)_40%,transparent)] bg-[color-mix(in_srgb,var(--critical)_8%,transparent)] px-4 py-3 text-sm text-[var(--critical)]">{error}</p>}
      <label className="mb-4 block"><span className={label}>E-mail</span>
        <input name="email" type="email" required maxLength={180} autoComplete="username" className="control bg-[var(--surface)]" /></label>
      <label className="mb-6 block"><span className={label}>Senha</span>
        <input name="password" type="password" required maxLength={128} autoComplete="current-password" className="control bg-[var(--surface)]" /></label>
      <button type="submit" disabled={busy} className="flex w-full items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--primary-strong)] disabled:opacity-60">
        {busy ? "Entrando..." : "Entrar"}{!busy && <ArrowRight size={16} />}
      </button>
      <div className="mt-5 flex flex-wrap justify-between gap-2 text-sm">
        <Link href="/forgot-password" className="font-semibold text-[var(--primary)] hover:underline">Esqueci minha senha</Link>
        <span className="text-[var(--text-muted)]">Sem acesso? Fale com o titular.</span>
      </div>
    </form>
  </AuthShell>;
}
