"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, movementKinds, signOf, toApiMoney, type Account, type AccountsOverview, type Statement, type Transfer } from "@/lib/finance";
import { useFinanceAction } from "./useFinanceAction";

type Props = { overview: AccountsOverview; transfers: Transfer[]; canEdit: boolean; canReverse: boolean };

export function Accounts({ overview, transfers, canEdit, canReverse }: Props) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState<Account | null>(null);
  const [statement, setStatement] = useState<Statement | null>(null);
  const active = overview.accounts.filter(account => account.isActive);

  async function saveAccount(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget; const data = new FormData(form);
    const saved = await action.send(editing ? `accounts/${editing.code}` : "accounts", editing ? "PUT" : "POST", {
      name: data.get("name"), openingDate: data.get("openingDate"), openingBalance: toApiMoney(data.get("openingBalance")),
      isActive: editing ? data.has("isActive") : true, version: editing?.version ?? 0,
    }, editing ? "Conta atualizada." : "Conta criada.");
    if (saved) { setEditing(null); form.reset(); }
  }
  async function transfer(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget; const data = new FormData(form);
    const saved = await action.send("transfers", "POST", {
      sourceAccountCode: data.get("source"), targetAccountCode: data.get("target"), transferDate: data.get("date"),
      amount: toApiMoney(data.get("amount")), description: data.get("description") || null,
    }, "Transferência registrada. O saldo total da casa não mudou.", true);
    if (saved) form.reset();
  }

  return <section className="page-content"><h1>Contas</h1>
    <p className="intro">O saldo de cada conta é o saldo inicial somado às entradas e saídas registradas até hoje.</p>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    <div className="money-summary"><span>Saldo da casa em {civilDate(overview.asOf)}</span><strong>{money(overview.total)}</strong></div>

    {overview.accounts.length === 0 ? <div className="empty-state"><h2>Nenhuma conta cadastrada</h2>
      <p>{canEdit ? "Cadastre a conta corrente, a poupança ou a carteira com o saldo do dia em que começar a usar o Organizali." : "Um titular ou editor pode cadastrar as contas da casa."}</p></div> :
      <div className="table-scroll"><table className="finance-table stack-table"><caption>Contas da casa</caption>
        <thead><tr><th scope="col">Conta</th><th scope="col" className="number">Saldo atual</th><th scope="col">Abertura</th><th scope="col"><span className="sr-only">Ações</span></th></tr></thead>
        <tbody>{overview.accounts.map(account => <tr key={account.code} className={account.isActive ? "" : "muted-row"}>
          <th scope="row">{account.name}{!account.isActive && <span className="tag">Inativa</span>}</th>
          <td className="number">{money(account.balance)}</td>
          <td><span className="entry-meta">Desde {civilDate(account.openingDate)} com {money(account.openingBalance)}</span></td>
          <td className="row-actions"><button type="button" className="secondary-button" onClick={async () => setStatement(await action.read<Statement>(`accounts/${account.code}/statement`))}>Ver extrato</button>
            {canEdit && <button type="button" className="secondary-button" onClick={() => setEditing(account)}>Editar</button>}</td>
        </tr>)}</tbody></table></div>}

    {statement && <StatementPanel key={`${statement.accountCode}-${statement.from}-${statement.to}`} statement={statement} onChange={setStatement} />}

    {canEdit && <form className="data-form" key={`${editing?.code ?? "new"}-${editing?.version ?? 0}`} onSubmit={saveAccount}>
      <h2>{editing ? `Editar ${editing.name}` : "Nova conta"}</h2>
      <label>Nome<input name="name" required maxLength={160} defaultValue={editing?.name ?? ""} placeholder="Conta corrente, Poupança, Carteira" /></label>
      <div className="form-row">
        <label>Data de abertura<input type="date" name="openingDate" required defaultValue={editing?.openingDate ?? overview.asOf} readOnly={editing?.hasMovements} /></label>
        <label>Saldo nessa data<input name="openingBalance" required inputMode="decimal" defaultValue={editing?.openingBalance ?? "0.00"} readOnly={editing?.hasMovements} /></label>
      </div>
      {editing?.hasMovements && <p className="form-hint">A conta já tem movimentos; a data e o saldo inicial ficam travados para não alterar o histórico.</p>}
      {editing && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={editing.isActive} />Conta ativa</label>}
      <div className="row-actions"><button className="primary-button" disabled={action.busy}>{editing ? "Salvar conta" : "Cadastrar conta"}</button>
        {editing && <button type="button" className="secondary-button" onClick={() => setEditing(null)}>Cancelar edição</button>}</div>
    </form>}

    {canEdit && active.length >= 2 && <form className="data-form" onSubmit={transfer}><h2>Transferir entre contas</h2>
      <p className="form-hint">Transferência não é renda nem despesa. Tarifa bancária deve ser lançada como despesa à parte.</p>
      <div className="form-row">
        <label>De<select name="source" required>{active.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
        <label>Para<select name="target" required defaultValue={active[1].code}>{active.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>Data<input type="date" name="date" required defaultValue={overview.asOf} max={overview.asOf} /></label>
        <label>Valor<input name="amount" required inputMode="decimal" placeholder="500,00" /></label>
      </div>
      <label>Descrição<input name="description" maxLength={200} placeholder="Opcional" /></label>
      <button className="primary-button" disabled={action.busy}>Transferir</button>
    </form>}

    {transfers.length > 0 && <><h2>Transferências</h2><ul className="entry-list">{transfers.map(item => <TransferRow key={item.code} transfer={item} canReverse={canReverse} />)}</ul></>}
  </section>;
}

function TransferRow({ transfer, canReverse }: { transfer: Transfer; canReverse: boolean }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  async function reverse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await action.send(`transfers/${transfer.code}/reversals`, "POST", { reason: new FormData(event.currentTarget).get("reason") }, "Transferência estornada.", true);
  }
  return <li className={transfer.isReversed ? "muted-row" : ""}>
    <div><strong>{money(transfer.amount)}</strong> de {transfer.sourceAccountName} para {transfer.targetAccountName}
      <span className="entry-meta">{civilDate(transfer.transferDate)}{transfer.description && ` · ${transfer.description}`}</span>
      {transfer.isReversed && <span className="entry-meta">Estornada: {transfer.reversalReason}</span>}</div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {canReverse && !transfer.isReversed && (open
      ? <form className="inline-form" onSubmit={reverse}><label>Motivo do estorno<input name="reason" required maxLength={300} /></label>
        <button className="secondary-button" disabled={action.busy}>Confirmar estorno</button></form>
      : <button type="button" className="secondary-button" onClick={() => setOpen(true)}>Estornar</button>)}
  </li>;
}

function StatementPanel({ statement, onChange }: { statement: Statement; onChange: (value: Statement | null) => void }) {
  const action = useFinanceAction();
  async function reload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    const next = await action.read<Statement>(`accounts/${statement.accountCode}/statement?from=${data.get("from")}&to=${data.get("to")}`);
    if (next) onChange(next);
  }
  return <section className="statement" aria-label={`Extrato de ${statement.accountName}`}>
    <div className="statement-header"><h2>Extrato · {statement.accountName}</h2><button type="button" className="secondary-button" onClick={() => onChange(null)}>Fechar extrato</button></div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    <form className="inline-form" onSubmit={reload}>
      <label>De<input type="date" name="from" defaultValue={statement.from} required /></label>
      <label>Até<input type="date" name="to" defaultValue={statement.to} required /></label>
      <button className="secondary-button" disabled={action.busy}>Atualizar período</button></form>
    <dl className="statement-totals"><div><dt>Saldo anterior</dt><dd>{money(statement.openingBalance)}</dd></div>
      <div><dt>Entradas</dt><dd>{money(statement.credits)}</dd></div><div><dt>Saídas</dt><dd>{money(statement.debits)}</dd></div>
      <div><dt>Saldo final</dt><dd>{money(statement.closingBalance)}</dd></div></dl>
    {statement.lines.length === 0 ? <p className="form-hint">Nenhum movimento no período.</p> :
      <div className="table-scroll"><table className="finance-table"><caption className="sr-only">Movimentos</caption>
        <thead><tr><th scope="col">Data</th><th scope="col">Descrição</th><th scope="col" className="number">Valor</th><th scope="col" className="number">Saldo</th></tr></thead>
        <tbody>{statement.lines.map(line => <tr key={line.code}><td>{civilDate(line.date)}</td>
          <td>{line.description}<span className="entry-meta">{movementKinds[line.kind] ?? line.kind}</span></td>
          <td className={`number ${signOf(line.amount) < 0 ? "negative" : "positive"}`}>{money(line.amount)}</td><td className="number">{money(line.balance)}</td></tr>)}</tbody>
      </table></div>}
  </section>;
}