"use client";
import { AuthShell } from "@/components/layout/AuthShell";

import Link from "next/link";
import { useEffect, useRef, useState, type FormEvent } from "react";

/** The token arrives in the URL fragment (never sent to servers or logs) and is removed from the address bar at once. */
export default function ResetPassword() {
  const token = useRef("");
  const [busy, setBusy] = useState(false);
  const [complete, setComplete] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    const value = new URLSearchParams(window.location.hash.slice(1)).get("token");
    if (value) token.current = value;
    window.history.replaceState(null, "", "/reset-password");
  }, []);
  async function reset(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (!token.current) { setError("Abra o link recebido por e-mail."); return; }
    if (form.get("password") !== form.get("confirmation")) { setError("As senhas precisam ser iguais."); return; }
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/reset-password", { method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token: token.current, password: form.get("password") }) });
      if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.detail ?? "Link inválido ou expirado. Peça um novo."); return; }
      token.current = ""; setComplete(true);
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <AuthShell><h1>{complete ? "Senha alterada" : "Escolha uma nova senha"}</h1>
    {complete ? <><p>As sessões abertas foram encerradas. Entre com a nova senha.</p><Link href="/login">Entrar no Organizali</Link></> :
      <form className="auth-form" onSubmit={reset} aria-busy={busy}><p>Use de 12 a 128 caracteres.</p>
        {error && <p role="alert" className="form-error">{error}</p>}
        <label>Nova senha<input name="password" type="password" minLength={12} maxLength={128} autoComplete="new-password" required /></label>
        <label>Confirmar senha<input name="confirmation" type="password" minLength={12} maxLength={128} autoComplete="new-password" required /></label>
        <button className="primary-button" disabled={busy}>{busy ? "Salvando…" : "Salvar nova senha"}</button>
        <p className="form-hint"><Link href="/forgot-password">Pedir outro link</Link></p>
      </form>}
  </AuthShell>;
}
