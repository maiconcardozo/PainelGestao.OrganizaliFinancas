export type BudgetLine = { categoryCode: string | null; name: string; parentCode: string | null; depth: number; limit: string | null; committed: string; paid: string;
  card: string; usedPercent: string | null; band: "none" | "ok" | "warning" | "limit" | "exceeded"; excess: string; countsInTotal: boolean };
export type Budget = { month: string; warningPercent: number; limitPercent: number; totalLimit: string; totalCommitted: string; totalPaid: string; unbudgeted: string; lines: BudgetLine[] };
export type AlertPreference = { warningPercent: number; limitPercent: number; dueSoonDays: number; version: number };
export type Alert = { key: string; kind: string; severity: "critical" | "warning" | "info"; title: string; detail: string; link: string; date: string; status: "new" | "read" };
export type CashEvent = { kind: string; description: string; amount: string; isEstimate: boolean; code: string | null };
export type ProjectionDay = { date: string; inflow: string; outflow: string; balance: string; events: CashEvent[] };
export type Projection = { from: string; to: string; start: string; end: string; firstDeficit: string | null; lowest: string; lowestDate: string; deficitCauses: CashEvent[]; days: ProjectionDay[] };
export type DayFlow = { date: string; inflow: string; outflow: string; balance: string; isProjected: boolean };
export type MonthFlow = { month: string; opening: string; inflow: string; outflow: string; other: string; closing: string; isProjected: boolean };
export type CashFlow = { month: string; today: string; opening: string; closing: string; days: DayFlow[]; months: MonthFlow[] };
export type AgendaItem = { date: string; kind: string; description: string; amount: string; isEstimate: boolean; isOverdue: boolean; code: string; link: string };
export type Agenda = { overdue: AgendaItem[]; today: AgendaItem[]; next7: AgendaItem[]; next30: AgendaItem[]; later: AgendaItem[] };
export type Dashboard = { month: string; today: string; balance: string; income: { expected: string; received: string; pending: string };
  expenses: { total: string; paid: string; open: string; overdue: string }; projection: Projection; budget: Budget; agenda: Agenda; alerts: Alert[]; unreadAlerts: number };
export type AnalysisLine = { source: "bill" | "card"; code: string; description: string; categoryCode: string | null; categoryName: string | null; dueDate: string; amount: string;
  paid: string; status: "open" | "paid" | "overdue" | "card"; cardName: string | null };
export type Analysis = { month: string; categoryCode: string | null; personCode: string | null; accountCode: string | null; status: string | null; total: string; paid: string;
  byCategory: { categoryCode: string | null; name: string; amount: string }[]; lines: AnalysisLine[] };

/** A22: bills of a month that go to a card, grouped by category (or description), with the invoice their money leaves on. */
export type MarginGroup = { name: string; count: number; amount: string; cardName: string; invoiceMonth: string; invoiceDueDate: string };
/** opening + inflow + outflow + other = closing; margin = closing − reserve; status red when closing is below the reserve (shortfall says by how much). */
export type MarginMonth = { month: string; opening: string; inflow: string; outflow: string; bills: string; invoices: string; other: string; closing: string; margin: string;
  status: "green" | "yellow" | "red"; shortfall: string; toCard: string; toCardGroups: MarginGroup[] };
/** "Hoje, em qual fatura cai?": a purchase today lands in referenceMonth until lastDay; after that, nextReferenceMonth. */
export type CardLanding = { cardCode: string; cardName: string; closingDay: number; dueDay: number; referenceMonth: string; closingDate: string; dueDate: string; lastDay: string;
  nextReferenceMonth: string };
export type MarginSimulation = { amount: string; date: string; cardName: string | null; invoiceMonth: string | null; invoiceDueDate: string | null; months: MarginMonth[] };
export type Margin = { today: string; reserve: string; attentionThreshold: string; months: MarginMonth[]; cards: CardLanding[]; simulation: MarginSimulation | null };

export const bandLabels: Record<BudgetLine["band"], string> = { none: "Sem orçamento", ok: "Dentro do limite", warning: "Perto do limite", limit: "No limite", exceeded: "Passou do limite" };
export const eventKinds: Record<string, string> = { bill: "Conta", invoice: "Fatura", income: "Renda prevista" };
export const analysisStatus: Record<AnalysisLine["status"], string> = { open: "Em aberto", paid: "Paga", overdue: "Vencida", card: "No cartão" };
