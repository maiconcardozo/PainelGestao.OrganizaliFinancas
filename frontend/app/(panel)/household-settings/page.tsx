import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { HouseholdSettings, type Settings } from "@/components/households/HouseholdForms";
export default async function SettingsPage() {
  const session = await requirePermission("household.read");
  return <HouseholdSettings settings={await fetchFromApi<Settings>("household-settings")} canEdit={session.permissions.includes("household.manage")} />;
}
