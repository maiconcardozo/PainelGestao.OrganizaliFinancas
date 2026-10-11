import Link from "next/link";
import { ArrowRight, CalendarDays, Users, Wallet } from "lucide-react";
import { AuthShell } from "@/components/layout/AuthShell";

export default function Home() {
  return <AuthShell>
    <h1>Finanças da casa, em conjunto</h1>
    <p>Piloto por convite. Já recebeu seu acesso? Entre para acompanhar a sua casa.</p>
    <Link href="/login" className="primary-button">Entrar no Organizali<ArrowRight size={16} /></Link>
    <ul className="mt-8 grid gap-3">
      {[[Wallet, "Saiba o que entra e o que sai", "Rendas, despesas e o saldo de cada conta."],
        [CalendarDays, "Planeje os próximos dias", "Vencimentos, parcelas, faturas e orçamento."],
        [Users, "Organize as responsabilidades", "Quem paga o quê e o acerto entre os moradores."]].map(([Icon, title, text]) => {
        const Glyph = Icon as typeof Wallet;
        return <li key={String(title)} className="flex gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-4 shadow-[var(--shadow)]">
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><Glyph size={17} /></span>
          <span><span className="block text-sm font-semibold">{String(title)}</span><span className="block text-xs text-[var(--text-secondary)]">{String(text)}</span></span>
        </li>;
      })}
    </ul>
  </AuthShell>;
}
