export const catalogs = {
  categories: "Categorias",
  "income-types": "Tipos de renda",
  creditors: "Credores",
  "payment-methods": "Formas de pagamento",
} as const;
export type CatalogKey = keyof typeof catalogs;
export type CatalogItem = { code: string; name: string; isActive: boolean; version: number; parentCode: string | null };
