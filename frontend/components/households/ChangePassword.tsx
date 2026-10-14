"use client";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { KeyRound } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { ModalActions } from "@/components/common/Ui";

/** Minha conta: own password change. Success closes every session, so the person is sent back to the login screen. */
export function ChangePassword() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [done, setDone] = useState(false);

  async function change(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    if (form.get("newPassword") !== form.get("confirmation")) { setError("As senhas precisam ser iguais."); return; }
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/change-password", { method: "POST", headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ currentPassword: form.get("currentPassword"), newPassword: form.get("newPassword") }) });
      if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.detail ?? "Não foi possível trocar a senha."); return; }
      setDone(true);
      setTimeout(() => { router.replace("/login"); router.refresh(); }, 4000);
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }

  return <>
    <button type="button" className="secondary-button" onClick={() => { setError(""); setOpen(true); }}><KeyRound size={16} />Trocar senha</button>
    {open && <Modal title={done ? "Senha alterada" : "Trocar senha"} onClose={() => !done && setOpen(false)} error={error} width="max-w-md">
      {done ? <div className="data-form">
        <p>Sua senha foi trocada e todas as sessões abertas foram encerradas, inclusive esta. Entre de novo com a nova senha.</p>
        <div className="modal-actions"><button type="button" className="primary-button" onClick={() => { router.replace("/login"); router.refresh(); }}>Entrar de novo</button></div>
      </div> : <form className="data-form" onSubmit={change}>
        <label>Senha atual<input name="currentPassword" type="password" required maxLength={128} autoComplete="current-password" /></label>
        <label>Nova senha<input name="newPassword" type="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label>
        <label>Confirmar nova senha<input name="confirmation" type="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label>
        <p className="form-hint">Use de 12 a 128 caracteres. Depois da troca, todas as sessões abertas são encerradas e você entra de novo com a nova senha.</p>
        <ModalActions busy={busy} label="Salvar nova senha" onCancel={() => setOpen(false)} />
      </form>}
    </Modal>}
  </>;
}
