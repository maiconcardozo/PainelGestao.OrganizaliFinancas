"use client";
import { AuthShell } from "@/components/layout/AuthShell";

import Link from "next/link";
import { useState, type FormEvent } from "react";

export default function ForgotPassword() {
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");
  async function request(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/recovery", { method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: new FormData(event.currentTarget).get("email") }) });
      if (response.status === 202) { setSent(true); return; }
      const body = await response.json().catch(() => null);
      setError(response.status === 503 ? "A redefinição por e-mail ainda não está disponível. Fale com quem organizou o piloto." : body?.detail ?? "Não foi possível solicitar.");
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <AuthShell><h1>{sent ? "Confira seu e-mail" : "Redefinir senha"}</h1>
    {sent ? <><p>Se houver um acesso com esse e-mail, enviamos um link válido por 15 minutos. Ele só pode ser usado uma vez.</p><Link href="/login">Voltar para entrar</Link></> :
      <form className="auth-form" onSubmit={request} aria-busy={busy}><p>Informe o e-mail do seu acesso. Enviaremos um link para escolher uma nova senha.</p>
        {error && <p role="alert" className="form-error">{error}</p>}
        <label>E-mail<input name="email" type="email" required maxLength={180} autoComplete="username" /></label>
        <button className="primary-button" disabled={busy}>{busy ? "Enviando…" : "Enviar link"}</button>
        <p className="form-hint"><Link href="/login">Voltar para entrar</Link></p>
      </form>}
  </AuthShell>;
}
