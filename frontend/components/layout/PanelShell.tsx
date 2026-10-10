"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useState, type ReactNode } from "react";
import { House, LogOut, Menu, UserRound, Users, type LucideIcon } from "lucide-react";
import type { Session } from "@/lib/session";

/** RevendaPRO shell structure: sidebar, current screen, identity and content. */
export function PanelShell({ session, children }: { session: Session; children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [menuOpen, setMenuOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const icons: Record<string, LucideIcon> = { House, UserRound, Users };
  const items = session.menu.map(item => ({ ...item, Icon: icons[item.icon] ?? House }));
  async function signOut() {
    setBusy(true); setError("");
    try {
      const response = await fetch("/api/auth/logout", { method: "POST" });
      if (!response.ok) { setError("Não foi possível sair. Tente novamente."); return; }
      router.replace("/login"); router.refresh();
    } catch { setError("Servidor indisponível. Tente novamente."); }
    finally { setBusy(false); }
  }
  return <div className="panel-shell">
    <a className="skip-link" href="#content">Ir para o conteúdo</a>
    <header className="panel-mobile-header"><span className="brand">Organizali</span>
      <button aria-expanded={menuOpen} aria-controls="panel-sidebar" onClick={() => setMenuOpen(!menuOpen)}><Menu aria-hidden="true" />{menuOpen ? "Fechar menu" : "Abrir menu"}</button>
    </header>
    <aside id="panel-sidebar" className={`panel-sidebar ${menuOpen ? "is-open" : ""}`}>
      <div className="brand"><House aria-hidden="true" /><span>Organizali<span className="brand-subtitle">Finanças da casa</span></span></div>
      <nav aria-label="Navegação principal">{items.map(({ route, name, Icon }) =>
        <Link key={route} href={route} onClick={() => setMenuOpen(false)} aria-current={pathname === route ? "page" : undefined}><Icon aria-hidden="true" size={20} />{name}</Link>)}
      </nav>
      <div className="panel-identity"><p>{session.name}</p><button disabled={busy} onClick={signOut}><LogOut aria-hidden="true" size={20} />{busy ? "Saindo…" : "Sair de todas as sessões"}</button>
        {error && <p role="alert">{error}</p>}
      </div>
    </aside>
    <div className="panel-main"><header className="panel-header">{items.find(item => item.route === pathname)?.name}</header>
      <main id="content" tabIndex={-1}>{children}</main>
    </div>
  </div>;
}
