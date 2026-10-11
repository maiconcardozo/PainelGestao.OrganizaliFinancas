"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import {
  Bell, ChartColumn, ChartLine, ChevronDown, CircleHelp, CreditCard, House, Landmark, LayoutDashboard, LogOut, Menu, Moon,
  PanelLeft, PiggyBank, Receipt, Settings, Sun, Tags, TrendingUp, UserRound, Users, X, type LucideIcon,
} from "lucide-react";
import { Confirmation } from "@/components/common/Confirmation";
import type { Session } from "@/lib/session";

type MenuItem = Session["menu"][number];

const ICONS: Record<string, LucideIcon> = {
  House, UserRound, Users, Settings, Tags, Landmark, TrendingUp, Receipt, CreditCard, LayoutDashboard, ChartLine, PiggyBank, ChartColumn, Bell,
};

/**
 * Menu groups. The API sends a flat menu filtered by permission; the grouping is presentation only,
 * the same sections RevendaPRO shows over its sidebar. A screen outside the map falls in "Casa".
 */
const GROUPS: { name: string; keys: string[] }[] = [
  { name: "Visão geral", keys: ["dashboard", "household"] },
  { name: "Dinheiro", keys: ["accounts", "income", "expenses", "cards"] },
  { name: "Planejamento", keys: ["cash-flow", "budget", "analysis", "alerts"] },
  { name: "Casa", keys: [] },
];

function groupOf(key: string): string {
  return GROUPS.find(group => group.keys.includes(key))?.name ?? "Casa";
}

function isActive(item: MenuItem, pathname: string): boolean {
  return pathname === item.route || pathname.startsWith(`${item.route}/`);
}

/** RevendaPRO shell: collapsible dark sidebar, translucent topbar, theme switch, user menu and the instrument grid behind the content. */
export function PanelShell({ session, children }: { session: Session; children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [dark, setDark] = useState(false);
  const [userOpen, setUserOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const [confirmingSignOut, setConfirmingSignOut] = useState(false);
  const [signOutError, setSignOutError] = useState("");
  const userMenuRef = useRef<HTMLDivElement>(null);
  const userButtonRef = useRef<HTMLButtonElement>(null);
  const sidebarRef = useRef<HTMLElement>(null);
  const drawerButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    // Reads what the pre-hydration script already applied to <html>.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setCollapsed(document.documentElement.dataset.sidebar === "collapsed");
    setDark(document.documentElement.classList.contains("dark"));
  }, []);

  useEffect(() => { document.documentElement.dataset.sidebar = collapsed ? "collapsed" : "expanded"; }, [collapsed]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      if (mobileOpen) { setMobileOpen(false); drawerButtonRef.current?.focus(); }
      if (userOpen) { setUserOpen(false); userButtonRef.current?.focus(); }
    }
    addEventListener("keydown", onKeyDown);
    return () => removeEventListener("keydown", onKeyDown);
  }, [mobileOpen, userOpen]);

  useEffect(() => {
    if (!mobileOpen) return;
    const focusable = sidebarRef.current?.querySelectorAll<HTMLElement>("a[href], button:not([disabled])");
    focusable?.[0]?.focus();
    function keepFocusInside(event: KeyboardEvent) {
      if (event.key !== "Tab" || !focusable?.length) return;
      const first = focusable[0], last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
    addEventListener("keydown", keepFocusInside);
    return () => removeEventListener("keydown", keepFocusInside);
  }, [mobileOpen]);

  useEffect(() => {
    if (!userOpen) return;
    function onClick(event: MouseEvent) { if (!userMenuRef.current?.contains(event.target as Node)) setUserOpen(false); }
    addEventListener("mousedown", onClick);
    return () => removeEventListener("mousedown", onClick);
  }, [userOpen]);

  const toggleSidebar = useCallback(() => {
    setCollapsed(current => {
      const next = !current;
      try { localStorage.setItem("organizali-sidebar", next ? "1" : "0"); } catch { /* preference only for this visit */ }
      return next;
    });
  }, []);

  function toggleTheme() {
    const next = !dark;
    setDark(next);
    try { localStorage.setItem("organizali-theme", next ? "dark" : "light"); } catch { /* preference only for this visit */ }
    document.documentElement.classList.toggle("dark", next);
    document.querySelector('meta[name="theme-color"]')?.setAttribute("content", next ? "#1a1c1f" : "#edf1f4");
  }

  function askSignOut() { setUserOpen(false); setMobileOpen(false); setSignOutError(""); setConfirmingSignOut(true); }

  async function signOut() {
    setSigningOut(true); setSignOutError("");
    try {
      const response = await fetch("/api/auth/logout", { method: "POST" });
      if (!response.ok) { setSignOutError("Não foi possível sair. Tente novamente."); return; }
      router.replace("/login"); router.refresh();
    } catch { setSignOutError("Servidor indisponível. Tente novamente."); }
    finally { setSigningOut(false); }
  }

  const expanded = !collapsed || mobileOpen;
  const current = session.menu.find(item => isActive(item, pathname));
  const groups = GROUPS.map(group => ({ name: group.name, items: session.menu.filter(item => groupOf(item.key) === group.name) })).filter(group => group.items.length > 0);
  const roleNames: Record<string, string> = { owner: "Titular", editor: "Editor", reader: "Leitor" };
  const roleName = roleNames[session.roles[0]] ?? session.roles[0] ?? "Sem perfil";
  // The eyebrow over each page title is the menu group, as in RevendaPRO; CSS reads it from here.
  const pageGroup = { "--page-group": JSON.stringify(current ? groupOf(current.key) : "Organizali") } as CSSProperties;

  return <div className="flex h-screen overflow-hidden bg-[var(--canvas)]" data-collapsed={collapsed}>
    <a href="#content" className="skip-link">Ir para o conteúdo</a>

    <aside id="sidebar-navigation" ref={sidebarRef} aria-label="Navegação principal"
      className={["fixed inset-y-0 left-0 z-50 flex h-screen w-[264px] shrink-0 flex-col",
        "transition-transform duration-200 lg:static lg:translate-x-0 lg:transition-[width]",
        mobileOpen ? "translate-x-0" : "-translate-x-full", collapsed ? "lg:w-[76px]" : "lg:w-[264px]"].join(" ")}
      style={{ backgroundColor: "var(--sidebar-bg)", color: "var(--sidebar-ink)" }}>
      <div className="relative flex h-16 shrink-0 items-center gap-3 border-b px-4" style={{ borderColor: "var(--sidebar-border)" }}>
        <span className="absolute inset-x-0 bottom-0 h-px" style={{ background: "linear-gradient(90deg,var(--primary),transparent)" }} />
        <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[var(--primary)] text-xs font-black text-white">OF</div>
        {expanded && <div className="min-w-0">
          <p className="font-display truncate text-[15px] font-bold leading-tight">Organizali</p>
          <p className="truncate text-[10px] font-semibold uppercase leading-tight tracking-[.18em] text-[var(--sidebar-ink-muted)]">Finanças da casa</p>
        </div>}
        <button type="button" onClick={() => { setMobileOpen(false); drawerButtonRef.current?.focus(); }} className="ml-auto grid h-8 w-8 place-items-center lg:hidden" aria-label="Fechar menu"><X size={20} /></button>
      </div>

      <nav className="flex-1 space-y-5 overflow-y-auto px-3 py-4">
        {groups.map(group => <div key={group.name}>
          {expanded && <p className="font-display mb-2 px-3 text-[10px] font-bold uppercase tracking-[.2em] text-[var(--sidebar-ink-muted)]">{group.name}</p>}
          <div className="space-y-1">{group.items.map(item => <SidebarItem key={item.key} item={item} active={isActive(item, pathname)} expanded={expanded} onNavigate={() => setMobileOpen(false)} />)}</div>
        </div>)}
      </nav>

      <div className="shrink-0 border-t px-3 py-3" style={{ borderColor: "var(--sidebar-border)" }}>
        <div className={`flex items-center gap-3 px-2 py-2 ${expanded ? "" : "justify-center px-0"}`}>
          <Initial name={session.name} />
          {expanded && <div className="min-w-0">
            <p className="truncate text-[13px] font-medium">{session.name}</p>
            <p className="truncate text-[11px] text-[var(--sidebar-ink-muted)]">{roleName}</p>
          </div>}
        </div>
        <button type="button" onClick={askSignOut} title={!expanded ? "Sair" : undefined} aria-label="Sair"
          className={["flex h-10 w-full items-center gap-3 rounded-md text-sm font-medium transition-colors",
            "text-[var(--sidebar-ink-muted)] hover:bg-[var(--sidebar-hover)] hover:text-white", expanded ? "px-3" : "justify-center px-0"].join(" ")}>
          <LogOut className="h-[19px] w-[19px] shrink-0" />{expanded && <span>Sair</span>}
        </button>
      </div>
    </aside>

    {mobileOpen && <button type="button" aria-label="Fechar menu" onClick={() => { setMobileOpen(false); drawerButtonRef.current?.focus(); }} className="fixed inset-0 z-40 bg-[#07152c]/60 lg:hidden" />}

    <div className="flex h-screen min-w-0 flex-1 flex-col">
      <header className="sticky top-0 z-30 flex h-16 shrink-0 items-center gap-2 border-b border-[var(--border)] bg-[color-mix(in_srgb,var(--canvas)_88%,transparent)] px-4 backdrop-blur">
        <button ref={drawerButtonRef} type="button" onClick={() => setMobileOpen(true)} aria-label="Abrir menu" aria-expanded={mobileOpen} aria-controls="sidebar-navigation"
          className="grid h-10 w-10 place-items-center rounded-md text-[var(--text-secondary)] lg:hidden"><Menu size={23} /></button>
        <button type="button" onClick={toggleSidebar} aria-label={collapsed ? "Expandir menu" : "Recolher menu"} aria-expanded={!collapsed} aria-controls="sidebar-navigation"
          className="hidden h-10 w-10 place-items-center rounded-md text-[var(--text-secondary)] hover:bg-[var(--surface-2)] lg:grid"><PanelLeft size={21} /></button>
        {current && <p className="font-display ml-1 truncate text-sm font-semibold text-[var(--text-primary)]">{current.name}</p>}
        <div className="flex-1" />
        <button type="button" onClick={toggleTheme} aria-label={dark ? "Ativar tema claro" : "Ativar tema escuro"}
          className="grid h-10 w-10 place-items-center rounded-md border border-[var(--border)] bg-[var(--surface)] text-[var(--text-secondary)] transition hover:border-[var(--primary)] hover:text-[var(--primary)]">
          {dark ? <Sun size={18} /> : <Moon size={18} />}
        </button>
        <div className="relative" ref={userMenuRef}>
          <button ref={userButtonRef} type="button" onClick={() => setUserOpen(value => !value)} aria-label="Menu do usuário" aria-expanded={userOpen} aria-haspopup="menu"
            className="flex h-10 w-10 items-center justify-center rounded-full border border-[var(--border)] bg-[var(--surface)] sm:w-auto sm:justify-start sm:gap-2 sm:pl-1 sm:pr-2">
            <Initial name={session.name} size={32} />
            <ChevronDown size={15} className="hidden text-[var(--text-muted)] sm:block" />
          </button>
          {userOpen && <div role="menu" className="absolute right-0 z-50 mt-2 w-64 overflow-hidden rounded-xl border border-[var(--border)] bg-[var(--surface)] shadow-[var(--shadow-lg)]">
            <div className="border-b border-[var(--border)] px-4 py-3">
              <p className="truncate text-sm font-semibold">{session.name}</p>
              <p className="truncate text-xs text-[var(--text-muted)]">{session.email}</p>
            </div>
            <div className="px-2 py-2">
              <Link role="menuitem" href="/my-account" onClick={() => setUserOpen(false)} className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm hover:bg-[var(--surface-2)]"><UserRound size={16} />Minha conta</Link>
              <button type="button" role="menuitem" onClick={askSignOut} className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-[var(--critical)] hover:bg-[var(--surface-2)]"><LogOut size={16} />Sair</button>
            </div>
          </div>}
        </div>
      </header>

      <main id="content" tabIndex={-1} className="instrument-grid flex-1 overflow-y-auto" style={pageGroup}>
        <div className="mx-auto max-w-screen-xl p-4 sm:p-6">{children}</div>
      </main>
    </div>

    {confirmingSignOut && <Confirmation title="Sair do sistema" danger busy={signingOut} error={signOutError} confirmLabel="Sair de todas as sessões"
      message={<>Deseja realmente sair? Todas as suas sessões abertas serão encerradas, e você vai precisar entrar de novo com e-mail e senha.</>}
      onConfirm={signOut} onCancel={() => setConfirmingSignOut(false)} />}
  </div>;
}

function SidebarItem({ item, active, expanded, onNavigate }: { item: MenuItem; active: boolean; expanded: boolean; onNavigate: () => void }) {
  const Icon = ICONS[item.icon] ?? CircleHelp;
  return <Link href={item.route} onClick={onNavigate} aria-label={item.name} aria-current={active ? "page" : undefined}
    className={["relative flex h-11 items-center gap-3 rounded-md text-sm transition-colors",
      active ? "font-semibold text-white" : "font-medium text-[var(--sidebar-ink-muted)] hover:bg-[var(--sidebar-hover)] hover:text-white",
      expanded ? "px-3" : "justify-center px-0"].join(" ")}
    style={active ? { backgroundColor: "color-mix(in srgb,var(--primary) 20%,transparent)" } : undefined}>
    {active && <span className="absolute left-0 top-1/2 h-6 w-[3px] -translate-y-1/2 rounded-r-full bg-[var(--primary)]" />}
    <Icon className="h-[21px] w-[21px] shrink-0" />
    {expanded && <span>{item.name}</span>}
  </Link>;
}

/** RevendaPRO Avatar without a photo: the initial over the brand color. */
function Initial({ name, size = 36 }: { name: string; size?: number }) {
  return <span aria-hidden className="grid shrink-0 place-items-center overflow-hidden rounded-full bg-[var(--primary)] font-bold text-white"
    style={{ height: size, width: size, fontSize: Math.round(size * 0.42) }}>{name.trim().charAt(0).toUpperCase() || "?"}</span>;
}
