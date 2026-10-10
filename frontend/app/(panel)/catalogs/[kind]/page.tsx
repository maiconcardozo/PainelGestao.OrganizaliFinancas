import { notFound } from "next/navigation";
import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { catalogs, type CatalogKey, type CatalogItem } from "@/lib/catalogs";
import { CatalogForm } from "@/components/catalogs/CatalogForm";
export default async function CatalogPage({ params }: { params: Promise<{ kind: string }> }) {
  const { kind } = await params;
  if (!Object.hasOwn(catalogs, kind)) notFound();
  const session = await requirePermission("household.read");
  return <CatalogForm kind={kind as CatalogKey} items={await fetchFromApi<CatalogItem[]>(`catalogs/${kind}`)} canEdit={session.permissions.includes("finance.write")} />;
}
