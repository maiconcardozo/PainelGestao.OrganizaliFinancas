"use client";
import { AuthShell } from "@/components/layout/AuthShell";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useRef, useState } from "react";

/** Only same-site paths are accepted as the return address. */
function safeNext(value: string | null): string {
  return value && value.startsWith("/") && !value.startsWith("//") ? value : "/household";
}

function Renewal() {
  const router = useRouter();
  const next = safeNext(useSearchParams().get("next"));
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState("");
  const started = useRef(false);
  async function renew() {
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/refresh", { method: "POST" });
      if (response.status === 401) { router.replace("/login"); return; }
      if (!response.ok) { setError("Não foi possível renovar o acesso. Tente novamente."); return; }
      router.replace(next); router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  // Renews by itself on arrival; the button only matters if that attempt fails.
  useEffect(() => { if (!started.current) { started.current = true; void renew(); } }, []); // eslint-disable-line react-hooks/exhaustive-deps
  return <AuthShell><h1>Continuar no Organizali</h1>
    <p>{busy ? "Renovando seu acesso…" : "Renove sua sessão para voltar à casa."}</p>
    {error && <p role="alert" className="form-error">{error}</p>}
    <button className="primary-button" disabled={busy} onClick={renew}>{busy ? "Renovando…" : "Continuar"}</button>
    <Link href="/login">Entrar com e-mail e senha</Link>
  </AuthShell>;
}

export default function RenewSession() {
  return <Suspense fallback={<AuthShell><h1>Continuar no Organizali</h1><p>Renovando seu acesso…</p></AuthShell>}><Renewal /></Suspense>;
}
