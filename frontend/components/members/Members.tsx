"use client";

import { useState, type FormEvent } from "react";
import { KeyRound, Mail, UserRound, X } from "lucide-react";
import { Confirmation } from "@/components/common/Confirmation";
import { Modal } from "@/components/common/Modal";
import { EditButton, Empty, IconAction, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

type Member = { code: string; name: string; email: string; isBlocked: boolean; roleKey: string; roleName: string };
type Invitation = { code: string; name: string; email: string; roleName: string; expiresAt: string };
export type MembersData = { members: Member[]; invitations: Invitation[] };
function RoleOptions() { return <><option value="owner">Titular</option><option value="editor">Editor</option><option value="reader">Leitor</option></>; }

/** Integrantes: who has a login. Invite, change the role or block, all in modals as in RevendaPRO's Usuários. */
export function Members({ initial }: { initial: MembersData }) {
  const action = useFinanceAction();
  const [inviting, setInviting] = useState(false);
  const [inviteLink, setInviteLink] = useState("");
  const [editing, setEditing] = useState<Member | null>(null);
  const [revoking, setRevoking] = useState<Invitation | null>(null);
  const [recovery, setRecovery] = useState<{ member: Member; link: string } | null>(null);

  async function invite(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const created = await action.send<{ token?: string }>("members/invitations", "POST",
      { name: form.get("name"), email: form.get("email"), roleKey: form.get("roleKey") }, "Convite criado. O link é válido por 48 horas.");
    if (created && typeof created === "object" && created.token) setInviteLink(`${window.location.origin}/join#token=${encodeURIComponent(created.token)}`);
  }
  async function saveAccess(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!editing) return;
    const form = new FormData(event.currentTarget);
    if (await action.send(`members/${editing.code}`, "PUT", { roleKey: form.get("roleKey"), isBlocked: form.has("isBlocked") }, "Acesso atualizado.")) setEditing(null);
  }
  async function revoke() {
    if (!revoking) return;
    if (await action.send(`members/invitations/${revoking.code}`, "DELETE", undefined, "Convite revogado.")) setRevoking(null);
  }
  function closeInvite() { setInviting(false); setInviteLink(""); }
  async function createRecoveryLink(member: Member) {
    const created = await action.send<{ token?: string }>(`members/${member.code}/recovery`, "POST", undefined, "Link de nova senha gerado.");
    if (created && typeof created === "object" && created.token) setRecovery({ member, link: `${window.location.origin}/reset-password#token=${encodeURIComponent(created.token)}` });
  }

  const modalOpen = inviting || editing !== null || revoking !== null || recovery !== null;
  return <section className="page-content">
    <PageHeader title="Integrantes" description="Todos compartilham os dados da casa. O perfil define o que cada pessoa pode alterar."
      action={<NewButton label="Convidar pessoa" onClick={() => { setInviteLink(""); setInviting(true); }} />} />
    <Notices error={modalOpen ? undefined : action.error} message={modalOpen ? undefined : action.message} />

    <p className="mb-3 text-xs text-[var(--text-muted)]"><span className="num font-semibold text-[var(--text-secondary)]">{initial.members.length}</span> {initial.members.length === 1 ? "pessoa com acesso" : "pessoas com acesso"}</p>
    <ListFrame>{initial.members.map(member => <li key={member.code} className="member-row-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4">
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-full bg-[var(--primary)] text-sm font-bold text-white">{member.name.trim().charAt(0).toUpperCase()}</span>
      <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{member.name}</span><span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">{member.email}</span></span>
      <span className="hidden sm:inline-flex"><Pill tone="primary">{member.roleName}</Pill></span>
      {member.isBlocked && <Pill tone="critical">Bloqueado</Pill>}
      <IconAction label={`Gerar link de nova senha para ${member.name}`} disabled={action.busy || member.isBlocked} onClick={() => void createRecoveryLink(member)}><KeyRound size={15} /></IconAction>
      <EditButton name={`o acesso de ${member.name}`} onClick={() => setEditing(member)} />
    </li>)}</ListFrame>

    <h2>Convites pendentes</h2>
    {initial.invitations.length === 0 ? <Empty title="Nenhum convite pendente." /> :
      <ListFrame>{initial.invitations.map(invitation => <li key={invitation.code} className="flex items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4">
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Mail size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{invitation.name}</span><span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">{invitation.email} · {invitation.roleName}</span></span>
        <IconAction label={`Revogar convite de ${invitation.name}`} danger onClick={() => setRevoking(invitation)}><X size={15} /></IconAction>
      </li>)}</ListFrame>}

    <p className="form-hint mt-4">Alterar o acesso encerra as sessões da pessoa. A casa sempre precisa de pelo menos um titular ativo. O ícone de chave gera um link de nova senha para quem esqueceu a sua.</p>

    {inviting && <Modal title={inviteLink ? "Convite criado" : "Convidar pessoa"} onClose={closeInvite} error={action.error} width="max-w-md">
      {inviteLink ? <div className="data-form">
        <label>Link do convite<textarea readOnly value={inviteLink} onFocus={event => event.currentTarget.select()} /></label>
        <p className="form-hint">Compartilhe somente com a pessoa convidada. O link permite definir a senha do acesso e vale por 48 horas.</p>
        <div className="modal-actions"><button type="button" className="primary-button" onClick={closeInvite}>Concluir</button></div>
      </div> : <form className="data-form" onSubmit={invite}>
        <label>Nome<input name="name" required maxLength={160} autoComplete="off" /></label>
        <label>E-mail<input name="email" type="email" required maxLength={180} autoComplete="off" /></label>
        <label>Perfil<select name="roleKey" defaultValue="reader"><RoleOptions /></select></label>
        <ModalActions busy={action.busy} label="Criar convite" onCancel={closeInvite} />
      </form>}
    </Modal>}

    {editing && <Modal title={`Acesso de ${editing.name}`} onClose={() => setEditing(null)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={saveAccess}>
        <p className="flex items-center gap-2 text-sm text-[var(--text-secondary)]"><UserRound size={15} />{editing.email}</p>
        <label>Perfil<select name="roleKey" defaultValue={editing.roleKey}><RoleOptions /></select></label>
        <label className="checkbox-label"><input type="checkbox" name="isBlocked" defaultChecked={editing.isBlocked} />Acesso bloqueado</label>
        <ModalActions busy={action.busy} label="Salvar acesso" onCancel={() => setEditing(null)} />
      </form>
    </Modal>}

    {recovery && <Modal title={`Nova senha para ${recovery.member.name}`} onClose={() => setRecovery(null)} width="max-w-md">
      <div className="data-form">
        <label>Link de nova senha<textarea readOnly value={recovery.link} onFocus={event => event.currentTarget.select()} /></label>
        <p className="form-hint">Entregue somente a {recovery.member.name}. O link vale por 24 horas, só pode ser usado uma vez e encerra as sessões abertas da pessoa ao definir a nova senha.</p>
        <div className="modal-actions"><button type="button" className="primary-button" onClick={() => setRecovery(null)}>Concluir</button></div>
      </div>
    </Modal>}
    {revoking && <Confirmation title="Revogar convite" danger busy={action.busy} error={action.error} confirmLabel="Revogar convite"
      message={<>O link enviado para {revoking.email} deixa de funcionar. Você pode convidar de novo depois.</>}
      onConfirm={() => void revoke()} onCancel={() => setRevoking(null)} />}
  </section>;
}
