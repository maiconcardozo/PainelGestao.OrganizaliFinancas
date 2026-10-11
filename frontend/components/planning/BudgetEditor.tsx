"use client";
import { useState, type FormEvent } from "react";
import { money, monthLabel, toApiMoney } from "@/lib/finance";
import { addMonths } from "@/lib/expenses";
import { bandLabels, type Budget } from "@/lib/planning";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { BudgetBar, BudgetFigures } from "./Widgets";

/** RF28: empty input means "Sem orçamento"; 0 is a real limit. Copying the previous month only fills the form for review. */
export function BudgetEditor({ budget, canEdit }: { budget: Budget; canEdit: boolean }) {
  const action = useFinanceAction();
  const lines = budget.lines.filter(line => line.categoryCode);
  const initial = Object.fromEntries(lines.map(line => [line.categoryCode!, line.limit?.replace(".", ",") ?? ""]));
  const [values, setValues] = useState<Record<string, string>>(initial);
  const [copied, setCopied] = useState(false);
  async function copyPrevious() {
    const previous = await action.read<Budget>(`budgets?month=${addMonths(budget.month, -1)}-01`);
    if (!previous) return;
    const limits = Object.fromEntries(previous.lines.filter(line => line.categoryCode && line.limit !== null).map(line => [line.categoryCode!, line.limit!.replace(".", ",")]));
    setValues(Object.fromEntries(lines.map(line => [line.categoryCode!, limits[line.categoryCode!] ?? values[line.categoryCode!] ?? ""])));
    setCopied(true);
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const items = lines.filter(line => (values[line.categoryCode!] ?? "") !== (initial[line.categoryCode!] ?? ""))
      .map(line => ({ categoryCode: line.categoryCode, amount: values[line.categoryCode!]?.trim() ? toApiMoney(values[line.categoryCode!]) : null }));
    const saved = await action.send(`budgets/${budget.month}-01`, "PUT", { items }, items.length ? "Orçamento salvo." : "Nada mudou.");
    if (saved) setCopied(false);
  }
  if (lines.length === 0) return <div className="empty-state"><h2>Nenhuma categoria</h2><p>Cadastre categorias em Cadastros financeiros para definir limites por categoria.</p></div>;
  return <form onSubmit={save}>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    {canEdit && <div className="row-actions">
      <button type="button" className="secondary-button" disabled={action.busy} onClick={copyPrevious}>Copiar limites de {monthLabel(addMonths(budget.month, -1))}</button>
      {copied && <span className="pending">Limites copiados: revise e salve.</span>}</div>}
    <ul className="budget-list">{lines.map(line => <li key={line.categoryCode} className={line.depth > 0 ? "child" : ""} style={{ marginLeft: `${line.depth * 1.25}rem` }}>
      <div className="budget-head">
        <div><strong>{line.name}</strong><BudgetFigures line={line} />
          {Number(line.card) !== 0 && <span className="entry-meta">inclui {money(line.card)} no cartão</span>}
          <span className="entry-meta">Pago {money(line.paid)} · {bandLabels[line.band]}{line.limit !== null && !line.countsInTotal && " · coberto pelo limite da categoria pai"}</span></div>
        {canEdit ? <label className="limit-input">Limite<input inputMode="decimal" value={values[line.categoryCode!] ?? ""} placeholder="Sem orçamento"
          onChange={event => setValues({ ...values, [line.categoryCode!]: event.target.value })} /></label>
          : <span className="number">{line.limit ? money(line.limit) : "Sem orçamento"}</span>}
      </div>
      <BudgetBar line={line} />
    </li>)}</ul>
    {canEdit && <button className="primary-button" disabled={action.busy}>Salvar orçamento</button>}
  </form>;
}
