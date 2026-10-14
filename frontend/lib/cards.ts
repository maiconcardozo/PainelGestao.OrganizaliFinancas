export type Card = { code: string; name: string; isActive: boolean; version: number; holderPersonCode: string; holderName: string; closingDay: number; dueDay: number;
  creditLimit: string | null; closingDayRule: "next" | "current"; committed: string; available: string | null };
export type Invoice = { code: string; cardCode: string; cardName: string; referenceMonth: string; closingDate: string; dueDate: string; expenseCode: string; amount: string;
  adjustments: string; paid: string; openBalance: string; status: "open" | "closed" | "partial" | "paid" | "credit"; isOverdue: boolean; itemCount: number };
export type CardItem = { code: string; purchaseCode: string; description: string; purchaseDate: string; kind: "installment" | "credit"; installmentNumber: number | null;
  installmentCount: number; amount: string; categoryName: string | null; isActive: boolean; note: string | null };
export type InvoiceDetail = { invoice: Invoice; items: CardItem[] };
export type Purchase = { code: string; cardCode: string; cardName: string; description: string; purchaseDate: string; total: string; count: number; refunded: string;
  categoryCode: string | null; categoryName: string | null; buyerPersonCode: string | null; buyerName: string | null; expenseTypeCode: string | null;
  expenseTypeVersion: number | null; fields: Record<string, unknown>; version: number };
export type PurchasePreview = { total: string; items: { number: number; referenceMonth: string; closingDate: string; dueDate: string; amount: string }[] };

export const invoiceStatus: Record<Invoice["status"], string> = { open: "Aberta", closed: "Fechada", partial: "Paga em parte", paid: "Paga", credit: "Com crédito" };
