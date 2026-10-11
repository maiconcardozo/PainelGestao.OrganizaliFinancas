import type { ReactNode } from "react";
import { LockKeyhole } from "lucide-react";

/** RevendaPRO sign-in layout: the brand panel on the left (desktop only), the form centred on the right. */
export function AuthShell({ children }: { children: ReactNode }) {
  return <main className="grid min-h-screen bg-[var(--canvas)] lg:grid-cols-2">
    <section className="instrument-grid hidden flex-col justify-between p-14 text-white lg:flex" style={{ backgroundColor: "var(--sidebar-bg)" }}>
      <div className="flex items-center gap-3">
        <div className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--primary)] text-xs font-black">OF</div>
        <div>
          <p className="font-display text-xl font-bold">Organizali</p>
          <p className="text-xs text-white/60">Finanças da casa</p>
        </div>
      </div>
      <div>
        <p className="hero-title max-w-lg text-4xl font-bold leading-[1.1]">Cada conta.<br />Cada renda.<br /><span className="text-[var(--primary)]">O mês inteiro à vista.</span></p>
        <p className="mt-5 max-w-md text-sm leading-relaxed text-white/60">Rendas, despesas, cartões e orçamento da família no mesmo lugar: quanto entra, quanto sai e quando o saldo aperta.</p>
      </div>
      <div className="flex items-start gap-2.5 text-xs leading-relaxed text-white/45">
        <LockKeyhole size={14} className="mt-0.5 shrink-0" />
        <p className="max-w-sm">Todos da casa veem os mesmos dados; o perfil de cada pessoa define o que ela pode alterar.</p>
      </div>
    </section>
    <section className="grid place-items-center p-8">
      <div className="session-message w-full max-w-sm">{children}</div>
    </section>
  </main>;
}
