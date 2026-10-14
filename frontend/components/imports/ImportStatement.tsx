"use client";
import { useRouter } from "next/navigation";
import { useState, type ChangeEvent, type FormEvent } from "react";
import { Upload } from "lucide-react";
import { Empty, Notices, PageHeader, Pill } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { civilDate, money, type Account } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { importActions, type ImportAction, type ImportBatch, type ImportPreview, type ImportResult } from "@/lib/complements";

type Decision = { action: ImportAction; expenseCode: string; personCode: string; categoryCode: string; incomeTypeCode: string; description: string };
type Props = { accounts: Account[]; people: Person[]; categories: CatalogItem[]; incomeTypes: CatalogItem[]; history: ImportBatch[]; canEdit: boolean; canSettle: boolean };

/**
 * Importar extrato (RF44): choose the account and the file, review the preview line by line (duplicates are flagged and
 * ignored by default), decide what each line becomes, confirm. Nothing is written before the confirmation.
 */
export function ImportStatement({ accounts, people, categories, incomeTypes, history, canEdit, canSettle }: Props) {
  const router = useRouter();
  const action = useFinanceAction();
  const [file, setFile] = useState<File | null>(null);
  const [accountCode, setAccountCode] = useState(accounts.find(account => account.isActive)?.code ?? "");
  const [preview, setPreview] = useState<ImportPreview | null>(null);
  const [decisions, setDecisions] = useState<Decision[]>([]);
  const [result, setResult] = useState<ImportResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const activePeople = people.filter(person => person.isActive);
  const activeAccounts = accounts.filter(account => account.isActive);

  async function loadPreview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!file || !accountCode) return;
    setBusy(true); setError(""); setResult(null);
    try {
      const form = new FormData();
      form.append("accountCode", accountCode);
      form.append("file", file, file.name);
      const response = await fetch("/api/backend/imports/preview", { method: "POST", body: form });
      if (response.status === 401) { router.replace("/session-renew"); return; }
      const body = await response.json().catch(() => null);
      if (!response.ok) { setError(Object.values(body?.errors ?? {}).flat().join(" ") || body?.detail || "Não foi possível ler o arquivo."); return; }
      const loaded = body.data as ImportPreview;
      setPreview(loaded);
      setDecisions(loaded.lines.map(line => ({ action: line.suggestedAction, expenseCode: line.suggestedExpenseCode ?? "", personCode: activePeople[0]?.code ?? "",
        categoryCode: "", incomeTypeCode: "", description: line.description })));
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }

  function update(index: number, patch: Partial<Decision>) { setDecisions(current => current.map((item, position) => position === index ? { ...item, ...patch } : item)); }

  async function confirm() {
    if (!preview) return;
    const payload = {
      accountCode: preview.accountCode, fileName: preview.fileName, format: preview.format,
      lines: preview.lines.map((line, index) => {
        const decision = decisions[index];
        return { date: line.date, amount: line.amount, description: decision.description.trim() || line.description, externalId: line.externalId, fingerprint: line.fingerprint,
          action: decision.action, expenseCode: decision.action === "pay" ? decision.expenseCode || null : null,
          personCode: decision.action === "receipt" ? decision.personCode || null : null, categoryCode: decision.action === "expense" ? decision.categoryCode || null : null,
          incomeTypeCode: decision.action === "receipt" ? decision.incomeTypeCode || null : null };
      }),
    };
    const saved = await action.send<ImportResult>("imports", "POST", payload, "Importação concluída.", true);
    if (saved && typeof saved === "object") { setResult(saved); setPreview(null); setFile(null); }
  }

  const counts = decisions.reduce((total, decision) => ({ ...total, [decision.action]: (total[decision.action] ?? 0) + 1 }), {} as Record<string, number>);
  const incoming = (amount: string) => Number(amount) > 0;

  return <section className="page-content">
    <PageHeader title="Importar extrato" description="Leia o extrato OFX ou CSV do banco, confira a prévia e escolha o que cada lançamento vira. Nada é gravado antes da confirmação." />
    <Notices error={preview ? undefined : error || action.error} message={action.message} />

    {result && <div className="mb-6 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)]">
      <h2 className="!mt-0 text-base">Importação concluída</h2>
      <dl className="money-summary money-grid">
        <div><dt>Linhas gravadas</dt><dd className="num">{result.applied}</dd></div>
        <div><dt>Ignoradas</dt><dd className="num">{result.ignored}</dd></div>
        <div><dt>Entradas de renda</dt><dd>{result.receipts} · {money(result.receiptsTotal)}</dd></div>
        <div><dt>Despesas pagas e pagamentos</dt><dd>{result.expenses + result.payments} · {money((Number(result.expensesTotal) + Number(result.paymentsTotal)).toFixed(2))}</dd></div>
      </dl>
      <p className="form-hint">Confira em Contas (extrato), Rendas e Despesas. Importar o mesmo arquivo de novo não duplica: as linhas já gravadas aparecem marcadas.</p>
    </div>}

    {!preview && <form className="data-form max-w-2xl" onSubmit={loadPreview}>
      {!canEdit ? <p className="form-hint">Um titular ou editor pode importar extratos.</p> : activeAccounts.length === 0 ? <p className="form-hint">Cadastre uma conta antes de importar o extrato dela.</p> : <>
        <div className="form-row">
          <label>Conta do extrato<select value={accountCode} onChange={event => setAccountCode(event.target.value)} required>
            {activeAccounts.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
          <label>Arquivo (OFX ou CSV)<input type="file" accept=".ofx,.csv,.txt,text/csv,application/x-ofx" required onChange={(event: ChangeEvent<HTMLInputElement>) => setFile(event.target.files?.[0] ?? null)} /></label>
        </div>
        <p className="form-hint">No internet banking, exporte o extrato do período em OFX (Money/Quicken) ou CSV. Linhas com data futura e lançamentos já importados são ignorados por padrão.</p>
        <div><button className="primary-button" disabled={busy || !file}><Upload size={16} />{busy ? "Lendo o arquivo…" : "Ler o extrato"}</button></div>
      </>}
    </form>}

    {preview && <>
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm text-[var(--text-secondary)]"><strong>{preview.fileName}</strong> · {preview.accountName} · <span className="num">{preview.lines.length}</span> linhas ·
          {" "}{Object.entries(importActions).map(([key, label]) => `${counts[key] ?? 0} ${label.toLowerCase()}`).join(", ")}</p>
        <div className="flex gap-2">
          <button type="button" className="secondary-button" onClick={() => { setPreview(null); setDecisions([]); }}>Escolher outro arquivo</button>
          <button type="button" className="primary-button" disabled={action.busy || !canSettle} onClick={() => void confirm()}>{action.busy ? "Gravando…" : "Confirmar importação"}</button>
        </div>
      </div>
      {!canSettle && <p className="form-error">Confirmar a importação exige permissão para pagar e receber (titular ou editor).</p>}
      {(error || action.error) && <p className="form-error" role="alert">{error || action.error}</p>}
      <div className="table-scroll"><table className="finance-table import-table">
        <thead><tr><th>Data</th><th>Descrição no extrato</th><th className="number">Valor</th><th>Situação</th><th>O que fazer</th><th>Detalhes</th></tr></thead>
        <tbody>{preview.lines.map((line, index) => {
          const decision = decisions[index];
          const candidates = preview.openExpenses;
          return <tr key={line.fingerprint + index} className={decision.action === "ignore" ? "muted-row" : ""}>
            <td className="whitespace-nowrap">{civilDate(line.date)}</td>
            <td><span className="block max-w-xs truncate" title={line.description}>{line.description}</span>{line.externalId && <span className="block text-xs text-[var(--text-muted)]">id {line.externalId}</span>}</td>
            <td className={`number whitespace-nowrap ${incoming(line.amount) ? "positive" : "negative"}`}>{money(line.amount)}</td>
            <td>{line.alreadyImported ? <Pill tone="warning">Já importada</Pill> : line.similarMovement ? <Pill tone="warning">Possível duplicado</Pill> : <Pill tone="success">Nova</Pill>}</td>
            <td><select value={decision.action} aria-label={`Ação para ${line.description}`} onChange={event => update(index, { action: event.target.value as ImportAction })}>
              <option value="ignore">{importActions.ignore}</option>
              {incoming(line.amount) ? <option value="receipt">{importActions.receipt}</option> : <>
                <option value="expense">{importActions.expense}</option>
                <option value="pay">{importActions.pay}</option></>}
            </select></td>
            <td className="min-w-[220px]">
              {decision.action === "receipt" && <div className="grid gap-1">
                <select value={decision.personCode} aria-label="Quem recebeu" onChange={event => update(index, { personCode: event.target.value })}>
                  {activePeople.map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select>
                <select value={decision.incomeTypeCode} aria-label="Tipo de renda" onChange={event => update(index, { incomeTypeCode: event.target.value })}>
                  <option value="">Sem tipo de renda</option>{incomeTypes.filter(item => item.isActive).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select>
              </div>}
              {decision.action === "expense" && <div className="grid gap-1">
                <input value={decision.description} maxLength={200} aria-label="Descrição da despesa" onChange={event => update(index, { description: event.target.value })} />
                <select value={decision.categoryCode} aria-label="Categoria" onChange={event => update(index, { categoryCode: event.target.value })}>
                  <option value="">Sem categoria</option>{categories.filter(item => item.isActive).map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select>
              </div>}
              {decision.action === "pay" && <select value={decision.expenseCode} aria-label="Despesa a pagar" onChange={event => update(index, { expenseCode: event.target.value })}>
                <option value="">Escolha a despesa em aberto</option>
                {candidates.map(item => <option key={item.code} value={item.code}>{civilDate(item.dueDate)} · {item.description} · {money(item.openBalance)}</option>)}</select>}
              {decision.action === "ignore" && <span className="text-xs text-[var(--text-muted)]">Nada será gravado.</span>}
            </td>
          </tr>;
        })}</tbody>
      </table></div>
      <p className="form-hint mt-3">Entrada de renda cria um recebimento na conta; despesa já paga cria a despesa e o pagamento juntos; pagar despesa existente baixa uma conta já lançada. Tudo é gravado de uma vez ou nada é gravado.</p>
    </>}

    <h2>Importações anteriores</h2>
    {history.length === 0 ? <Empty title="Nenhum extrato importado ainda." /> : <ListFrame>{history.map(batch => <li key={batch.code} className="flex flex-wrap items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4">
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Upload size={17} /></span>
      <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{batch.fileName}</span>
        <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(batch.createdAt.slice(0, 10))} · {batch.accountName} · {batch.format.toUpperCase()}</span></span>
      <span className="num shrink-0 text-xs text-[var(--text-secondary)]">{batch.appliedCount} gravadas · {batch.ignoredCount} ignoradas</span>
    </li>)}</ListFrame>}
  </section>;
}
