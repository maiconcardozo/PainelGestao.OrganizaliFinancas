import type { FieldDefinition } from "./expense-types";
import { toApiMoney } from "./finance";

export type Share = { personCode: string; personName: string; amount: string; percent: string | null };
export type Expense = {
  code: string; description: string; competence: string; dueDate: string; amount: string; adjustments: string; paid: string; openBalance: string;
  status: "open" | "partial" | "paid" | "cancelled"; isOverdue: boolean; isEstimated: boolean; cancelReason: string | null;
  expenseTypeCode: string | null; expenseTypeName: string | null; expenseTypeVersion: number | null; categoryCode: string | null; categoryName: string | null;
  creditorCode: string | null; creditorName: string | null; paymentMethodCode: string | null; payerPersonCode: string | null; payerPersonName: string | null;
  recurrenceCode: string | null; isDetached: boolean; installmentPlanCode: string | null; installmentNumber: number | null; installmentCount: number | null;
  splitMode: string | null; shares: Share[]; fields: Record<string, unknown>; version: number;
};
export type ExpenseList = { from: string; to: string; today: string; total: string; paid: string; open: string; overdue: string; items: Expense[] };
export type Adjustment = { code: string; kind: string; amount: string; date: string; reason: string };
export type PaymentAllocation = { paymentCode: string; expenseCode: string; expenseDescription: string; amount: string; paymentDate: string; accountName: string; isReversed: boolean };
export type ExpenseDetail = { expense: Expense; adjustments: Adjustment[]; payments: PaymentAllocation[] };
export type Payment = { code: string; accountCode: string; accountName: string; payerPersonCode: string | null; payerPersonName: string | null; paymentMethodCode: string | null;
  paymentDate: string; amount: string; description: string; isReversed: boolean; reversalReason: string | null; allocations: PaymentAllocation[] };
export type SharePayload = { personCode: string; value: string };
export type Recurrence = { code: string; description: string; isActive: boolean; version: number; validFrom: string; endDate: string | null; frequency: string; amount: string;
  isEstimated: boolean; expenseTypeCode: string | null; expenseTypeVersion: number | null; categoryCode: string | null; categoryName: string | null; creditorCode: string | null;
  paymentMethodCode: string | null; payerPersonCode: string | null; payerPersonName: string | null; splitMode: string | null; shares: SharePayload[]; fields: Record<string, unknown> };
export type OccurrencePreview = { date: string; amount: string; change: "new" | "update" | "keep" | "preserved" };
export type RecurrencePlan = { inserted: number; updated: number; cancelled: number; preserved: number; next: OccurrencePreview[] };
export type InstallmentPreview = { total: string; count: number; items: { number: number; dueDate: string; competence: string; amount: string }[] };
export type Contributions = { month: string; people: { personCode: string; personName: string; responsible: string; paid: string; balance: string }[];
  settlements: { fromPersonCode: string; fromPersonName: string; toPersonCode: string; toPersonName: string; amount: string }[] };

export const statusLabels: Record<Expense["status"], string> = { open: "Em aberto", partial: "Paga em parte", paid: "Paga", cancelled: "Cancelada" };
export const adjustmentKinds: Record<string, string> = { interest: "Juros", fine: "Multa", discount: "Desconto", adjustment: "Ajuste" };
export const changeLabels: Record<OccurrencePreview["change"], string> = { new: "nova", update: "muda", keep: "sem mudança", preserved: "preservada" };

/** Custom field values typed as the expense type version defines them; empty inputs are omitted. */
export function fieldValues(data: FormData, fields: FieldDefinition[]) {
  const values: Record<string, unknown> = {};
  for (const field of fields) {
    const value = String(data.get(`field_${field.key}`) ?? "");
    if (value === "") continue;
    values[field.key] = field.type === "decimal" ? Number(value) : field.type === "boolean" ? value === "true" : value;
  }
  return values;
}

/** Shares are sent only for the selected people; percent keeps the typed decimals, value goes in the invariant money format. */
export function sharesFrom(data: FormData, people: { code: string }[], mode: string): SharePayload[] | null {
  const shares = people.map(person => ({ personCode: person.code, raw: String(data.get(`share_${person.code}`) ?? "").trim() }))
    .filter(item => item.raw !== "")
    .map(item => ({ personCode: item.personCode, value: mode === "percent" ? item.raw.replace(",", ".") : toApiMoney(item.raw) }));
  return shares.length > 0 ? shares : null;
}

export function monthOf(date: string) { return date.slice(0, 7); }
export function addMonths(month: string, delta: number) {
  const [year, value] = month.split("-").map(Number);
  const date = new Date(Date.UTC(year, value - 1 + delta, 1));
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}`;
}
export function capitalize(text: string) { return text.charAt(0).toUpperCase() + text.slice(1); }
