"use client";
import { useState, type FormEvent } from "react";
import { ArrowLeftRight, FileText, Landmark } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card, EditButton, Empty, Figure, IconAction, ModalActions, NewButton, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { civilDate, money, movementKinds, signOf, toApiMoney, type Account, type AccountsOverview, type Statement, type Transfer } from "@/lib/finance";
import { ReverseButton } from "./ReverseDialog";
import { useFinanceAction } from "./useFinanceAction";

type Props = { overview: AccountsOverview; transfers: Transfer[]; canEdit: boolean; canReverse: boolean };

export function Accounts({ overview, transfers, canEdit, canReverse }: Props) {
  const action = useFinanceAction();
  const [view, chooseView] = useViewMode("organizali.accounts.view");
  const [editing, setEditing] = useState<Account | "new" | null>(null);
  const [transferring, setTransferring] = useState(false);
  const [statement, setStatement] = useState<Statement | null>(null);
  const [notice, setNotice] = useState("");
  const current = editing === "new" ? null : editing;
  const active = overview.accounts.filter(account => account.isActive);

  async function saveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send(current ? `accounts/${current.code}` : "accounts", current ? "PUT" : "POST", {
      name: data.get("name"), openingDate: data.get("openingDate"), openingBalance: toApiMoney(data.get("openingBalance")),
      isActive: current ? data.has("isActive") : true, version: current?.version ?? 0,
    }, current ? "Conta atualizada." : "Conta criada.");
    if (saved) { setEditing(null); setNotice(current ? "Conta atualizada." : "Conta criada."); }
  }
  async function transfer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const saved = await action.send("transfers", "POST", {
      sourceAccountCode: data.get("source"), targetAccountCode: data.get("target"), transferDate: data.get("date"),
      amount: toApiMoney(data.get("amount")), description: data.get("description") || null,
    }, "Transferência registrada. O saldo total da casa não mudou.", true);
    if (saved) { setTransferring(false); setNotice("Transferência registrada. O saldo total da casa não mudou."); }
  }
  async function openStatement(account: Account) {
    const loaded = await action.read<Statement>(`accounts/${account.code}/statement`);
    if (loaded) setStatement(loaded);
  }

  const status = (account: Account) => <Pill tone={account.isActive ? "success" : "muted"}>{account.isActive ? "Ativa" : "Inativa"}</Pill>;
  const modalOpen = editing !== null || transferring;

  return <section className="page-content">
    <PageHeader title="Contas" description="O saldo de cada conta é o saldo inicial somado às entradas e saídas registradas até hoje."
      action={canEdit && <div className="flex flex-wrap gap-2">
        {active.length >= 2 && <button type="button" className="secondary-button" onClick={() => setTransferring(true)}><ArrowLeftRight size={16} />Transferir</button>}
        <NewButton label="Nova conta" onClick={() => setEditing("new")} />
      </div>} />
    <Notices error={modalOpen || statement ? undefined : action.error} message={notice} />

    <div className="money-summary"><span>Saldo da casa em {civilDate(overview.asOf)}</span><strong>{money(overview.total)}</strong></div>

    <ListBar count={overview.accounts.length} singular="conta" plural="contas" view={view} onChange={chooseView} label="Como mostrar as contas" />
    {overview.accounts.length === 0 ? <Empty title={canEdit ? "Nenhuma conta ainda. Cadastre a conta corrente, a poupança ou a carteira com o saldo do dia em que começar a usar o Organizali." : "Nenhuma conta cadastrada. Um titular ou editor pode cadastrar as contas da casa."} /> :
      view === "list" ? <ListFrame>{overview.accounts.map(account => <li key={account.code} className={`account-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${account.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Landmark size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{account.name}</span>
          <span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">Desde {civilDate(account.openingDate)} com {money(account.openingBalance)}</span></span>
        <span className="num shrink-0 text-right text-sm font-semibold">{money(account.balance)}</span>
        <span className="flex shrink-0 gap-1">
          <IconAction label={`Ver extrato de ${account.name}`} onClick={() => void openStatement(account)}><FileText size={15} /></IconAction>
          {canEdit && <EditButton name={account.name} onClick={() => setEditing(account)} />}
        </span>
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{overview.accounts.map(account => <div key={account.code} className="account-item"><Card muted={!account.isActive}>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0"><p className="flex items-center gap-2 font-semibold"><Landmark size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{account.name}</span></p>
            <p className="mt-0.5 text-xs text-[var(--text-secondary)]">Desde {civilDate(account.openingDate)}</p></div>
          {canEdit && <EditButton name={account.name} onClick={() => setEditing(account)} />}
        </div>
        <Figure label="Saldo atual" value={money(account.balance)} hint={`Saldo inicial ${money(account.openingBalance)}`} />
        <div>{status(account)}</div>
        <button type="button" onClick={() => void openStatement(account)}
          className="mt-auto inline-flex items-center justify-center gap-2 rounded-md border border-[var(--border)] px-3 py-2 text-sm font-semibold text-[var(--primary)] transition hover:bg-[var(--surface-2)]">
          <FileText size={15} />Ver extrato
        </button>
      </Card></div>)}</div>}

    <h2>Transferências</h2>
    {transfers.length === 0 ? <Empty title="Nenhuma transferência entre contas." /> :
      <ListFrame>{transfers.map(item => <li key={item.code} className={`flex flex-wrap items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4 ${item.isReversed ? "opacity-60" : ""}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><ArrowLeftRight size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block font-semibold">{item.sourceAccountName} → {item.targetAccountName}</span>
          <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(item.transferDate)}{item.description && ` · ${item.description}`}{item.isReversed && ` · Estornada: ${item.reversalReason}`}</span></span>
        <span className="num shrink-0 text-sm font-semibold">{money(item.amount)}</span>
        {canReverse && !item.isReversed && <ReverseButton path={`transfers/${item.code}/reversals`} title="Estornar transferência" success="Transferência estornada." onDone={setNotice}
          summary={<>{money(item.amount)} de {item.sourceAccountName} para {item.targetAccountName} em {civilDate(item.transferDate)} volta para a conta de origem.</>} />}
      </li>)}</ListFrame>}

    {editing && <Modal title={current ? `Editar ${current.name}` : "Nova conta"} onClose={() => setEditing(null)} error={action.error} width="max-w-lg">
      <form className="data-form" onSubmit={saveAccount}>
        <label>Nome<input name="name" required maxLength={160} defaultValue={current?.name ?? ""} placeholder="Conta corrente, Poupança, Carteira" autoComplete="off" /></label>
        <div className="form-row">
          <label>Data de abertura<input type="date" name="openingDate" required defaultValue={current?.openingDate ?? overview.asOf} readOnly={current?.hasMovements} /></label>
          <label>Saldo nessa data<input name="openingBalance" required inputMode="decimal" defaultValue={current?.openingBalance ?? "0.00"} readOnly={current?.hasMovements} /></label>
        </div>
        {current?.hasMovements && <p className="form-hint">A conta já tem movimentos; a data e o saldo inicial ficam travados para não alterar o histórico.</p>}
        {current && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={current.isActive} />Conta ativa</label>}
        <ModalActions busy={action.busy} label={current ? "Salvar conta" : "Cadastrar conta"} onCancel={() => setEditing(null)} />
      </form>
    </Modal>}

    {transferring && <Modal title="Transferir entre contas" onClose={() => setTransferring(false)} error={action.error} width="max-w-lg">
      <form className="data-form" onSubmit={transfer}>
        <p className="form-hint">Transferência não é renda nem despesa. Tarifa bancária deve ser lançada como despesa à parte.</p>
        <div className="form-row">
          <label>De<select name="source" required>{active.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
          <label>Para<select name="target" required defaultValue={active[1]?.code}>{active.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
        </div>
        <div className="form-row">
          <label>Data<input type="date" name="date" required defaultValue={overview.asOf} max={overview.asOf} /></label>
          <label>Valor<input name="amount" required inputMode="decimal" placeholder="500,00" /></label>
        </div>
        <label>Descrição<input name="description" maxLength={200} placeholder="Opcional" /></label>
        <ModalActions busy={action.busy} label="Transferir" onCancel={() => setTransferring(false)} />
      </form>
    </Modal>}

    {statement && <StatementModal key={`${statement.accountCode}-${statement.from}-${statement.to}`} statement={statement} onChange={setStatement} />}
  </section>;
}

function StatementModal({ statement, onChange }: { statement: Statement; onChange: (value: Statement | null) => void }) {
  const action = useFinanceAction();
  async function reload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    const next = await action.read<Statement>(`accounts/${statement.accountCode}/statement?from=${data.get("from")}&to=${data.get("to")}`);
    if (next) onChange(next);
  }
  return <Modal title={`Extrato · ${statement.accountName}`} onClose={() => onChange(null)} error={action.error} width="max-w-4xl">
    <section className="statement-body" aria-label={`Extrato de ${statement.accountName}`}>
      <form className="inline-form" onSubmit={reload}>
        <label>De<input type="date" name="from" defaultValue={statement.from} required /></label>
        <label>Até<input type="date" name="to" defaultValue={statement.to} required /></label>
        <button className="secondary-button" disabled={action.busy}>Atualizar período</button></form>
      <dl className="statement-totals"><div><dt>Saldo anterior</dt><dd>{money(statement.openingBalance)}</dd></div>
        <div><dt>Entradas</dt><dd className="positive">{money(statement.credits)}</dd></div><div><dt>Saídas</dt><dd className="negative">{money(statement.debits)}</dd></div>
        <div><dt>Saldo final</dt><dd>{money(statement.closingBalance)}</dd></div></dl>
      {statement.lines.length === 0 ? <p className="form-hint">Nenhum movimento no período.</p> :
        <div className="table-scroll"><table className="finance-table"><caption className="sr-only">Movimentos</caption>
          <thead><tr><th scope="col">Data</th><th scope="col">Descrição</th><th scope="col" className="number">Valor</th><th scope="col" className="number">Saldo</th></tr></thead>
          <tbody>{statement.lines.map(line => <tr key={line.code}><td className="num">{civilDate(line.date)}</td>
            <td>{line.description}<span className="entry-meta">{movementKinds[line.kind] ?? line.kind}</span></td>
            <td className={`number ${signOf(line.amount) < 0 ? "negative" : "positive"}`}>{money(line.amount)}</td><td className="number">{money(line.balance)}</td></tr>)}</tbody>
        </table></div>}
    </section>
  </Modal>;
}
