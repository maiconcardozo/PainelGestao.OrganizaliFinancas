"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { House } from "lucide-react";

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

  return <main className="welcome">
    <section className="welcome-brand" aria-label="Organizali Finanças">
      <div className="brand"><House aria-hidden="true" /><span>Organizali<span className="brand-subtitle">Finanças da casa</span></span></div>
      <div className="welcome-message"><h1>Mais clareza para cuidar do que é de todos.</h1><p>Rendas, contas e planos da família em um só lugar.</p></div>
      <p className="brand-footer">Organização para o dia a dia.</p>
    </section>
    <section className="welcome-content">
      <form className="auth-form" onSubmit={signIn} aria-busy={busy}>
        <h2>Entrar na sua casa</h2><p className="intro">Use o acesso liberado para você no piloto.</p>
        {error && <p role="alert" className="form-error">{error}</p>}
        <label>E-mail<input name="email" type="email" required maxLength={180} autoComplete="username" /></label>
        <label>Senha<input name="password" type="password" required maxLength={128} autoComplete="current-password" /></label>
        <button className="primary-button" disabled={busy}>{busy ? "Entrando…" : "Entrar"}</button>
        <p className="form-hint">Ainda não tem acesso? Combine a liberação com quem organizou o piloto.</p>
      </form>
    </section>
  </main>;
}
