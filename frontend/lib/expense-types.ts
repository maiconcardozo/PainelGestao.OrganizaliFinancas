export type FieldDefinition = { key: string; label: string; type: string; required: boolean; maxLength: number | null; min: number | null; max: number | null; options: string[] | null };
export type ExpenseType = { code: string; name: string; isActive: boolean; version: number; behavior: string; defaultCategoryCode: string | null; fields: FieldDefinition[] };
export const behaviors: Record<string, string> = { single: "Conta avulsa", recurring: "Conta recorrente", installments: "Compra parcelada", card: "Cartão" };
export const fieldTypes: Record<string, string> = { text: "Texto", decimal: "Número", date: "Data", boolean: "Sim ou não", list: "Lista de opções" };
