export type Goal = { code: string; name: string; targetAmount: string; targetDate: string | null; note: string | null; isActive: boolean; version: number; accountCode: string;
  accountName: string; accountActive: boolean; saved: string; remaining: string; percent: number; monthsLeft: number | null; monthlyNeeded: string | null; reached: boolean };

export type Attachment = { code: string; kind: string; targetCode: string; fileName: string; contentType: string; size: number; createdAt: string };
export type AttachmentKind = "expense" | "payment" | "receipt";

export type ImportCandidate = { code: string; description: string; dueDate: string; openBalance: string };
export type ImportLine = { index: number; date: string; amount: string; description: string; externalId: string | null; fingerprint: string; alreadyImported: boolean;
  similarMovement: boolean; suggestedAction: ImportAction; suggestedExpenseCode: string | null };
export type ImportPreview = { accountCode: string; accountName: string; fileName: string; format: string; lines: ImportLine[]; openExpenses: ImportCandidate[] };
export type ImportAction = "ignore" | "receipt" | "expense" | "pay";
export type ImportResult = { code: string; applied: number; ignored: number; receipts: number; payments: number; expenses: number; receiptsTotal: string; paymentsTotal: string; expensesTotal: string };
export type ImportBatch = { code: string; fileName: string; format: string; accountName: string; lineCount: number; appliedCount: number; ignoredCount: number; createdAt: string };

export const importActions: Record<ImportAction, string> = { ignore: "Ignorar", receipt: "Entrada de renda", expense: "Despesa já paga", pay: "Pagar despesa existente" };

/** "12,3 KB", "1,2 MB". */
export function fileSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toLocaleString("pt-BR", { maximumFractionDigits: 1 })} KB`;
  return `${(bytes / 1024 / 1024).toLocaleString("pt-BR", { maximumFractionDigits: 1 })} MB`;
}
