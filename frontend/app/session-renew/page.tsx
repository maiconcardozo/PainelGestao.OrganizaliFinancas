"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function RenewSession() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  async function renew() {
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/refresh", { method: "POST" });
      if (response.status === 401) { router.replace("/login"); return; }
      if (!response.ok) { setError("Não foi possível renovar o acesso. Tente novamente."); return; }
      router.replace("/household"); router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <main className="session-message"><h1>Continuar no Organizali</h1>
    <p>Renove sua sessão para voltar à casa.</p>
    {error && <p role="alert" className="form-error">{error}</p>}
    <button className="primary-button" disabled={busy} onClick={renew}>{busy ? "Renovando…" : "Continuar"}</button>
    <Link href="/login">Entrar com e-mail e senha</Link>
  </main>;
}
