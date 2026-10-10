import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { People, type Person } from "@/components/households/HouseholdForms";
export default async function PeoplePage() {
  const session = await requirePermission("household.read");
  return <People people={await fetchFromApi<Person[]>("people")} canEdit={session.permissions.includes("household.manage")} />;
}
