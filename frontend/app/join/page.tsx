"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type FormEvent } from "react";

export default function Join() {
  const token = useRef("");
  const [busy, setBusy] = useState(false);
  const [complete, setComplete] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    const value = new URLSearchParams(window.location.hash.slice(1)).get("token");
    if (value) token.current = value;
    window.history.replaceState(null, "", "/join");
  }, []);
  async function accept(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (!token.current) { setError("Abra o link de convite enviado pelo titular."); return; }
    if (form.get("password") !== form.get("confirmation")) { setError("As senhas precisam ser iguais."); return; }
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/accept-invitation", { method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token: token.current, password: form.get("password") }) });
      if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.detail ?? "Convite inválido ou expirado."); return; }
      token.current = ""; setComplete(true);
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <main className="session-message"><h1>{complete ? "Acesso criado" : "Bem-vindo à casa"}</h1>
    {complete ? <><p>Entre com o e-mail que recebeu o convite e a senha que você definiu.</p><Link href="/login">Entrar no Organizali</Link></> :
      <form className="auth-form" onSubmit={accept} aria-busy={busy}><p>Defina sua senha para aceitar o convite. Use de 12 a 128 caracteres.</p>
        {error && <p role="alert" className="form-error">{error}</p>}
        <label>Senha<input name="password" type="password" minLength={12} maxLength={128} autoComplete="new-password" required /></label>
        <label>Confirmar senha<input name="confirmation" type="password" minLength={12} maxLength={128} autoComplete="new-password" required /></label>
        <button className="primary-button" disabled={busy}>{busy ? "Criando acesso…" : "Aceitar convite"}</button>
      </form>}
  </main>;
}
