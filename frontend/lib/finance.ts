export type Account = { code: string; name: string; isActive: boolean; version: number; openingDate: string; openingBalance: string; balance: string; hasMovements: boolean };
export type AccountsOverview = { asOf: string; total: string; accounts: Account[] };
export type StatementLine = { code: string; date: string; amount: string; kind: string; description: string; balance: string; transferCode: string | null; receiptCode: string | null; reversedMovementCode: string | null };
export type Statement = { accountCode: string; accountName: string; from: string; to: string; openingBalance: string; credits: string; debits: string; closingBalance: string; lines: StatementLine[] };
export type Transfer = { code: string; sourceAccountCode: string; sourceAccountName: string; targetAccountCode: string; targetAccountName: string; transferDate: string; amount: string; description: string; isReversed: boolean; reversalReason: string | null };
export type IncomeSource = { code: string; name: string; isActive: boolean; version: number; personCode: string; personName: string; incomeTypeCode: string | null; validFrom: string; endDate: string | null; frequency: string; amountMode: string; expectedNetAmount: string | null; grossAmount: string | null; monthlyAverage: string | null };
export type Forecast = { code: string; sourceCode: string; sourceName: string; personCode: string; personName: string; amountMode: string; occurrenceDate: string; competence: string; expectedAmount: string | null; estimatedAmount: string | null; receivedAmount: string; difference: string | null; residual: string | null; status: string; closeReason: string | null; sourceVersion: number; version: number };
export type Receipt = { code: string; accountCode: string; accountName: string; personCode: string; personName: string; sourceCode: string | null; sourceName: string | null; receiptDate: string; amount: string; allocatedAmount: string; description: string; isReversed: boolean; reversalReason: string | null };
export type IncomeAverage = { code: string | null; name: string; personName: string | null; total: string; average: string };
export type MonthComparison = { month: string; personCode: string; personName: string; expected: string; received: string; difference: string };
export type IncomeSummary = { year: number; averageFrom: string; averageTo: string; averageMonths: number; householdTotal: string; householdAverage: string; people: IncomeAverage[]; sources: IncomeAverage[]; months: MonthComparison[] };

export const frequencies: Record<string, string> = { weekly: "Semanal", biweekly: "A cada 14 dias", monthly: "Mensal", quarterly: "Trimestral", semiannual: "Semestral", annual: "Anual" };
export const movementKinds: Record<string, string> = { receipt: "Recebimento", transfer_in: "Transferência recebida", transfer_out: "Transferência enviada", reversal: "Estorno" };

const brl = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
/** Official amounts arrive as decimal strings; formatting never feeds a calculation back to the API. */
export function money(value: string | null | undefined) { return value == null ? "—" : brl.format(Number(value)); }
export function signOf(value: string | null | undefined) { return value == null ? 0 : Math.sign(Number(value)); }
export function civilDate(value: string) { const [year, month, day] = value.split("-"); return `${day}/${month}/${year}`; }
export function monthLabel(value: string) {
  const [year, month] = value.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { month: "long", year: "numeric", timeZone: "UTC" }).format(new Date(Date.UTC(year, month - 1, 1)));
}
/** Accepts "1.250,50" or "1250.50" from the form and sends the invariant API format. */
export function toApiMoney(input: FormDataEntryValue | null) {
  const text = String(input ?? "").trim().replace(/\s|R\$/g, "");
  return text.includes(",") ? text.replace(/\./g, "").replace(",", ".") : text;
}
