"use client";

import { useState, type FormEvent } from "react";
import { UserRound } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card, EditButton, Empty, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export type Person = { code: string; name: string; isActive: boolean; version: number };
export type Settings = { timeZone: string; language: string; currency: string; startDate: string | null; version: number };

/** Moradores: the RevendaPRO cadastro flow — header with "Novo", mosaic or list, the form in a modal. */
export function People({ people, canEdit }: { people: Person[]; canEdit: boolean }) {
  const action = useFinanceAction();
  const [view, chooseView] = useViewMode("organizali.people.view");
  const [editing, setEditing] = useState<Person | "new" | null>(null);
  const current = editing === "new" ? null : editing;

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(current ? `people/${current.code}` : "people", current ? "PUT" : "POST",
      { name: data.get("name"), isActive: current ? data.has("isActive") : true, version: current?.version ?? 0 },
      current ? "Morador atualizado." : "Morador cadastrado.");
    if (saved) setEditing(null);
  }

  const status = (person: Person) => <Pill tone={person.isActive ? "success" : "muted"}>{person.isActive ? "Ativo" : "Inativo"}</Pill>;

  return <section className="page-content">
    <PageHeader title="Moradores" description="Quem participa das finanças da casa, mesmo sem acesso ao sistema. Morador e usuário de acesso são cadastros independentes."
      action={canEdit && <NewButton label="Novo morador" onClick={() => setEditing("new")} />} />
    <Notices error={editing ? undefined : action.error} message={action.message} />

    <ListBar count={people.length} singular="morador" plural="moradores" view={view} onChange={chooseView} label="Como mostrar os moradores" />
    {people.length === 0 ? <Empty title={canEdit ? "Nenhum morador ainda. Comece pelas pessoas que recebem renda ou dividem as contas da casa." : "Nenhum morador cadastrado. Um titular pode cadastrar os moradores da casa."} /> :
      view === "list" ? <ListFrame>{people.map(person => <li key={person.code} className={`member-row-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${person.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><UserRound size={17} /></span>
        <span className="min-w-0 flex-1 truncate font-semibold">{person.name}</span>
        {status(person)}
        {canEdit && <EditButton name={person.name} onClick={() => setEditing(person)} />}
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{people.map(person => <Card key={person.code} muted={!person.isActive}>
        <div className="member-row-item flex items-start justify-between gap-3">
          <p className="flex min-w-0 items-center gap-2 font-semibold"><UserRound size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{person.name}</span></p>
          {canEdit && <EditButton name={person.name} onClick={() => setEditing(person)} />}
        </div>
        <div>{status(person)}</div>
      </Card>)}</div>}

    <p className="form-hint mt-4">Inativar preserva o cadastro e todas as referências financeiras.</p>

    {editing && <Modal title={current ? `Editar ${current.name}` : "Novo morador"} onClose={() => setEditing(null)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={save}>
        <label>Nome da pessoa<input name="name" required maxLength={160} defaultValue={current?.name ?? ""} autoComplete="off" /></label>
        {current && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={current.isActive} />Morador ativo</label>}
        <ModalActions busy={action.busy} label={current ? "Salvar morador" : "Cadastrar morador"} onCancel={() => setEditing(null)} />
      </form>
    </Modal>}
  </section>;
}

export function HouseholdSettings({ settings, canEdit }: { settings: Settings; canEdit: boolean }) {
  const action = useFinanceAction();
  return <section className="page-content">
    <PageHeader title="Configurações da casa" description="Preferências para o calendário e a organização financeira." />
    <Notices error={action.error} message={action.message} />
    <form className="data-form max-w-2xl" key={settings.version} onSubmit={event => {
      event.preventDefault(); const data = new FormData(event.currentTarget);
      void action.send("household-settings", "PUT", { timeZone: data.get("timeZone"), startDate: data.get("startDate") || null, version: settings.version }, "Configurações salvas.");
    }}>
      <div className="form-row">
        <label>Fuso horário<input name="timeZone" defaultValue={settings.timeZone} list="timezones" maxLength={100} required readOnly={!canEdit} /></label>
        <label>Data de início de uso<input type="date" name="startDate" min="1000-01-01" defaultValue={settings.startDate ?? ""} readOnly={!canEdit} /></label>
      </div>
      <datalist id="timezones">{["America/Sao_Paulo", "America/Manaus", "America/Rio_Branco", "America/Noronha", "Europe/Lisbon", "Etc/UTC"].map(zone => <option key={zone} value={zone} />)}</datalist>
      <p className="form-hint">Use o nome IANA, como America/Sao_Paulo. Você pode informar outro fuso válido.</p>
      <dl className="statement-totals"><div><dt>Idioma</dt><dd>Português (Brasil)</dd></div><div><dt>Moeda</dt><dd>Real brasileiro (BRL)</dd></div></dl>
      {canEdit && <div><button className="primary-button" disabled={action.busy}>Salvar configurações</button></div>}
    </form>
  </section>;
}
