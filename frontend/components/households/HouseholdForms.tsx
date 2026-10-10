"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";

export type Person = { code: string; name: string; isActive: boolean; version: number };
export type Settings = { timeZone: string; language: string; currency: string; startDate: string | null; version: number };

function useSave() {
  const router = useRouter();
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  async function save(path: string, method: string, payload: unknown) {
    setBusy(true); setError(""); setMessage("");
    try {
      const response = await fetch(`/api/backend/${path}`, { method, headers: { "Content-Type": "application/json" }, body: JSON.stringify(payload) });
      if (response.status === 401) { router.replace("/session-renew"); return false; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(body?.detail ?? "Não foi possível salvar. Confira os dados."); return false; }
      setMessage("Alteração salva."); router.refresh(); return true;
    } catch { setError("Servidor indisponível. Tente novamente."); return false; }
    finally { setBusy(false); }
  }
  return { busy, message, error, save };
}

export function People({ people, canEdit }: { people: Person[]; canEdit: boolean }) {
  const { busy, message, error, save } = useSave();
  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    if (await save("people", "POST", { name: new FormData(form).get("name"), isActive: true, version: 0 })) form.reset();
  }
  return <section className="page-content"><h1>Moradores</h1><p className="intro">Cadastre quem participa das finanças da casa, mesmo sem acesso ao sistema.</p>
    {error && <p className="form-error" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    {canEdit && <form className="data-form" onSubmit={create}><label>Nome da pessoa<input name="name" required maxLength={160} /></label><button className="primary-button" disabled={busy}>Cadastrar morador</button></form>}
    {people.length === 0 ? <div className="empty-state"><h2>Nenhum morador cadastrado</h2><p>{canEdit ? "Comece pelas pessoas que recebem renda ou dividem as contas da casa." : "Um titular pode cadastrar os moradores da casa."}</p></div> :
      <div className="member-list">{people.map(person => <form className="member-row" key={`${person.code}-${person.version}`} onSubmit={event => {
        event.preventDefault(); const data = new FormData(event.currentTarget);
        void save(`people/${person.code}`, "PUT", { name: data.get("name"), isActive: data.has("isActive"), version: person.version });
      }}><label>Nome<input name="name" required maxLength={160} defaultValue={person.name} readOnly={!canEdit} /></label>
        <span>{person.isActive ? "Ativo" : "Inativo"}</span>
        {canEdit && <><label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={person.isActive} />Ativo</label><button className="secondary-button" disabled={busy}>Salvar morador</button></>}
      </form>)}</div>}
    <p className="form-hint">Inativar preserva o cadastro e suas referências. Morador e usuário de acesso são cadastros independentes.</p>
  </section>;
}

export function HouseholdSettings({ settings, canEdit }: { settings: Settings; canEdit: boolean }) {
  const { busy, message, error, save } = useSave();
  return <section className="page-content"><h1>Configurações da casa</h1><p className="intro">Preferências para o calendário e a organização financeira.</p>
    {error && <p className="form-error" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    <form className="data-form" key={settings.version} onSubmit={event => {
      event.preventDefault(); const data = new FormData(event.currentTarget);
      void save("household-settings", "PUT", { timeZone: data.get("timeZone"), startDate: data.get("startDate") || null, version: settings.version });
    }}><label>Fuso horário<input name="timeZone" defaultValue={settings.timeZone} list="timezones" maxLength={100} required readOnly={!canEdit} /></label>
      <datalist id="timezones">{["America/Sao_Paulo", "America/Manaus", "America/Rio_Branco", "America/Noronha", "Europe/Lisbon", "Etc/UTC"].map(zone => <option key={zone} value={zone} />)}</datalist>
      <p className="form-hint">Use o nome IANA, como America/Sao_Paulo. Você pode informar outro fuso válido.</p>
      <label>Data de início de uso<input type="date" name="startDate" min="1000-01-01" defaultValue={settings.startDate ?? ""} readOnly={!canEdit} /></label>
      <dl><dt>Idioma</dt><dd>Português (Brasil)</dd><dt>Moeda</dt><dd>Real brasileiro (BRL)</dd></dl>
      {canEdit && <button className="primary-button" disabled={busy}>Salvar configurações</button>}
    </form>
  </section>;
}
