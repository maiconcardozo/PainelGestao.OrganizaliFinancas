import Link from "next/link";
import { Bell, CircleHelp, CreditCard, Landmark, LayoutDashboard, Receipt, TrendingUp, type LucideIcon } from "lucide-react";
import { requireSession } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { FirstSteps, type Step } from "@/components/households/FirstSteps";

const shortcuts: Record<string, { icon: LucideIcon; text: string }> = {
  dashboard: { icon: LayoutDashboard, text: "Saldo de hoje, projeção e o mês da casa." },
  expenses: { icon: Receipt, text: "Contas a pagar, pagas e vencidas." },
  income: { icon: TrendingUp, text: "Previsto x recebido de cada morador." },
  accounts: { icon: Landmark, text: "Saldo e extrato de cada conta." },
  cards: { icon: CreditCard, text: "Faturas e compras no cartão." },
  alerts: { icon: Bell, text: "O que precisa de atenção." },
  help: { icon: CircleHelp, text: "Tutorial de uso, passo a passo." },
};

async function count(path: string, query?: Record<string, string>): Promise<number> {
  try {
    const data = await fetchFromApi<unknown>(path, query);
    if (Array.isArray(data)) return data.length;
    if (data && typeof data === "object") {
      const record = data as { accounts?: unknown[]; lines?: { limit: string | null }[] };
      if (Array.isArray(record.accounts)) return record.accounts.length;
      if (Array.isArray(record.lines)) return record.lines.filter(line => line.limit !== null).length;
    }
  } catch { /* a failed count only hides the hint; the screens stay reachable */ }
  return 0;
}

/** Minha casa: the RevendaPRO greeting, the guided first steps while the household is empty, and the shortcuts this profile can open. */
export default async function Household() {
  const session = await requireSession();
  const firstName = session.name.trim().split(" ")[0];
  const items = session.menu.filter(item => shortcuts[item.key]);
  let steps: Step[] = [];
  if (session.permissions.includes("household.read")) {
    const [people, accounts, categories, types, sources, cards, budgets] = await Promise.all([
      count("people"), count("accounts"), count("catalogs/categories"), count("expense-types"), count("income-sources"), count("cards"), count("budgets")]);
    const manage = session.permissions.includes("household.manage") ? "" : "Um titular faz este passo.";
    const edit = session.permissions.includes("finance.write") ? "" : "Um titular ou editor faz este passo.";
    steps = [
      { key: "people", title: "Moradores", text: "Quem participa das contas, com ou sem login.", route: "/people", done: people > 0, who: manage },
      { key: "accounts", title: "Contas", text: "Conta corrente, poupança ou carteira com o saldo do dia em que começar.", route: "/accounts", done: accounts > 0, who: edit },
      { key: "catalogs", title: "Cadastros financeiros e tipos de despesa", text: "Categorias, credores, formas de pagamento e os tipos de conta da casa. Dá para carregar um conjunto pronto.", route: "/catalogs", done: categories > 0 && types > 0, who: edit },
      { key: "income", title: "Fontes de renda", text: "O que cada morador espera receber; gera as previsões do ano.", route: "/income", done: sources > 0, who: edit },
      { key: "cards", title: "Cartões", text: "Apelido, fechamento e vencimento; o número não é pedido.", route: "/cards", done: cards > 0, optional: true, who: edit },
      { key: "budget", title: "Orçamento do mês", text: "Um limite nas categorias que quer controlar.", route: "/budget", done: budgets > 0, optional: true, who: edit },
    ];
  }
  return <section className="page-content">
    <div className="mb-6">
      <h1>Olá, {firstName}.</h1>
      <p className="mt-1 text-sm text-[var(--text-secondary)]">Seu acesso ao Organizali está ativo. Por onde quer começar?</p>
    </div>
    {steps.length > 0 && <FirstSteps steps={steps} canLoadDefaults={session.permissions.includes("finance.write")} />}
    <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{items.map(item => {
      const { icon: Icon, text } = shortcuts[item.key];
      return <Link key={item.key} href={item.route} className="group flex items-start gap-3 rounded-xl border border-[var(--border)] bg-[var(--surface)] p-5 shadow-[var(--shadow)] transition hover:border-[var(--primary)]">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[var(--surface-2)] text-[var(--signal)]"><Icon size={19} /></span>
        <span><span className="block font-semibold group-hover:text-[var(--primary)]">{item.name}</span><span className="mt-0.5 block text-sm text-[var(--text-secondary)]">{text}</span></span>
      </Link>;
    })}</div>
  </section>;
}
