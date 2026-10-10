"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

type Member = { code: string; name: string; email: string; isBlocked: boolean; roleKey: string; roleName: string };
type Invitation = { code: string; name: string; email: string; roleName: string; expiresAt: string };
export type MembersData = { members: Member[]; invitations: Invitation[] };
function RoleOptions() { return <><option value="owner">Titular</option><option value="editor">Editor</option><option value="reader">Leitor</option></>; }

export function Members({ initial }: { initial: MembersData }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [inviteLink, setInviteLink] = useState("");

  async function mutate(path: string, method: string, payload?: unknown) {
    setBusy(true); setError(""); setMessage("");
    try {
      const response = await fetch(`/api/backend/members${path}`, {
        method, headers: { "Content-Type": "application/json" }, body: payload === undefined ? undefined : JSON.stringify(payload),
      });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = response.status === 204 ? null : await response.json().catch(() => null);
      if (!response.ok) { setError(body?.detail ?? "Não foi possível salvar. Confira os dados."); return; }
      if (body?.data?.token) {
        setInviteLink(`${window.location.origin}/join#token=${encodeURIComponent(body.data.token)}`);
        setMessage("Convite criado. Copie o link abaixo; ele é válido por 48 horas.");
      } else setMessage("Alteração salva.");
      router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  async function invite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    await mutate("/invitations", "POST", { name: form.get("name"), email: form.get("email"), roleKey: form.get("roleKey") });
  }
  return <section className="page-content"><h1>Integrantes</h1>
    <p className="intro">Todos compartilham os dados da casa. O perfil define o que cada pessoa pode alterar.</p>
    {error && <p className="form-error" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    <form className="data-form" onSubmit={invite}><h2>Convidar uma pessoa</h2>
      <label>Nome<input name="name" required maxLength={160} autoComplete="off" /></label>
      <label>E-mail<input name="email" type="email" required maxLength={180} autoComplete="off" /></label>
      <label>Perfil<select name="roleKey" defaultValue="reader"><RoleOptions /></select></label>
      <button className="primary-button" disabled={busy}>Criar convite</button>
    </form>
    {inviteLink && <div className="data-form"><label>Link do convite<textarea readOnly value={inviteLink} onFocus={event => event.currentTarget.select()} /></label>
      <p className="form-hint">Compartilhe somente com a pessoa convidada. O link permite definir a senha do acesso.</p>
      <button type="button" className="secondary-button" onClick={() => setInviteLink("")}>Ocultar link</button></div>}
    <h2>Pessoas com acesso</h2><div className="member-list">
      {initial.members.map(member => <form className="member-row" key={`${member.code}-${member.roleKey}-${member.isBlocked}`} onSubmit={event => {
        event.preventDefault(); const form = new FormData(event.currentTarget);
        void mutate(`/${member.code}`, "PUT", { roleKey: form.get("roleKey"), isBlocked: form.has("isBlocked") });
      }}><div><h3>{member.name}</h3><p>{member.email}</p></div>
        <label>Perfil de {member.name}<select name="roleKey" defaultValue={member.roleKey}><RoleOptions /></select></label>
        <label className="checkbox-label"><input type="checkbox" name="isBlocked" defaultChecked={member.isBlocked} />Acesso bloqueado</label>
        <button disabled={busy} className="secondary-button">Salvar acesso</button>
      </form>)}
    </div>
    <h2>Convites pendentes</h2>{initial.invitations.length === 0 ? <p>Nenhum convite pendente.</p> :
      <ul className="invitation-list">{initial.invitations.map(invitation => <li key={invitation.code}>
        <div><strong>{invitation.name}</strong><p>{invitation.email} · {invitation.roleName}</p></div>
        <button className="secondary-button" disabled={busy} onClick={() => void mutate(`/invitations/${invitation.code}`, "DELETE")}>Revogar convite</button>
      </li>)}</ul>}
    <p className="form-hint">Alterar o acesso encerra as sessões da pessoa. A casa sempre precisa de pelo menos um titular ativo.</p>
  </section>;
}
