import Link from "next/link";
import { Bell, CreditCard, Landmark, LayoutDashboard, Receipt, TrendingUp, type LucideIcon } from "lucide-react";
import { requireSession } from "@/lib/session";

const shortcuts: Record<string, { icon: LucideIcon; text: string }> = {
  dashboard: { icon: LayoutDashboard, text: "Saldo de hoje, projeção e o mês da casa." },
  expenses: { icon: Receipt, text: "Contas a pagar, pagas e vencidas." },
  income: { icon: TrendingUp, text: "Previsto x recebido de cada morador." },
  accounts: { icon: Landmark, text: "Saldo e extrato de cada conta." },
  cards: { icon: CreditCard, text: "Faturas e compras no cartão." },
  alerts: { icon: Bell, text: "O que precisa de atenção." },
};

/** Minha casa: the RevendaPRO greeting, with the shortcuts this profile can open. */
export default async function Household() {
  const session = await requireSession();
  const firstName = session.name.trim().split(" ")[0];
  const items = session.menu.filter(item => shortcuts[item.key]);
  return <section className="page-content">
    <div className="mb-6">
      <h1>Olá, {firstName}.</h1>
      <p className="mt-1 text-sm text-[var(--text-secondary)]">Seu acesso ao Organizali está ativo. Por onde quer começar?</p>
    </div>
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{items.map(item => {
      const { icon: Icon, text } = shortcuts[item.key];
      return <Link key={item.key} href={item.route} className="group flex items-start gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)] transition hover:border-[var(--primary)]">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[var(--surface-2)] text-[var(--signal)]"><Icon size={19} /></span>
        <span><span className="block font-semibold group-hover:text-[var(--primary)]">{item.name}</span><span className="mt-0.5 block text-sm text-[var(--text-secondary)]">{text}</span></span>
      </Link>;
    })}</div>
  </section>;
}
