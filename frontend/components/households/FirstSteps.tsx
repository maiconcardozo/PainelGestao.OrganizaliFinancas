"use client";
import Link from "next/link";
import { CircleCheck, Circle, Sparkles } from "lucide-react";
import { Notices } from "@/components/common/Ui";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export type Step = { key: string; title: string; text: string; route: string; done: boolean; optional?: boolean; who?: string };

/**
 * Minha casa: guided preparation while the household is still empty. The order is the one the screens themselves suggest:
 * people → accounts → catalogs → income sources → cards → budget. Disappears once every required step is done.
 */
export function FirstSteps({ steps, canLoadDefaults }: { steps: Step[]; canLoadDefaults: boolean }) {
  const action = useFinanceAction();
  const required = steps.filter(step => !step.optional);
  const done = steps.filter(step => step.done).length;
  if (required.every(step => step.done) && steps.every(step => step.done)) return null;
  return <section aria-labelledby="first-steps" className="mb-6 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)]">
    <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
      <h2 id="first-steps" className="!mt-0 flex items-center gap-2 text-base"><Sparkles size={18} className="text-[var(--signal)]" />Primeiros passos</h2>
      <span className="text-xs font-semibold text-[var(--text-muted)]"><span className="num">{done}</span> de <span className="num">{steps.length}</span> prontos</span>
    </div>
    <div className="progress mb-4" role="progressbar" aria-valuemin={0} aria-valuemax={steps.length} aria-valuenow={done} aria-label="Progresso da preparação">
      <span style={{ width: `${Math.round((done / steps.length) * 100)}%` }} />
    </div>
    <Notices error={action.error} message={action.message} />
    <ol className="space-y-2">{steps.map((step, index) => <li key={step.key} className={`flex items-start gap-3 rounded-lg border border-[var(--border)] px-3 py-2.5 ${step.done ? "opacity-60" : ""}`}>
      <span className={`mt-0.5 shrink-0 ${step.done ? "text-[var(--success)]" : "text-[var(--text-muted)]"}`} aria-hidden>{step.done ? <CircleCheck size={18} /> : <Circle size={18} />}</span>
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-semibold">{index + 1}. {step.title}{step.optional && <span className="ml-2 text-xs font-normal text-[var(--text-muted)]">opcional</span>}</span>
        <span className="block text-xs text-[var(--text-secondary)]">{step.text}{step.who && ` ${step.who}`}</span>
      </span>
      {!step.done && <span className="flex shrink-0 flex-wrap gap-2">
        {step.key === "catalogs" && canLoadDefaults && <button type="button" className="secondary-button" disabled={action.busy}
          onClick={() => void action.send("catalogs/defaults", "POST", undefined, "Cadastros padrão carregados: categorias, tipos de renda, credores, formas de pagamento e tipos de despesa.")}>
          {action.busy ? "Carregando…" : "Carregar cadastros padrão"}</button>}
        <Link href={step.route} className="inline-flex items-center rounded-md border border-[var(--border)] px-3 py-1.5 text-xs font-semibold text-[var(--primary)] transition hover:bg-[var(--surface-2)]">Abrir</Link>
      </span>}
    </li>)}</ol>
  </section>;
}
