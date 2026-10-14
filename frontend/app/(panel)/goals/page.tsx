import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview } from "@/lib/finance";
import type { Goal } from "@/lib/complements";
import { Goals } from "@/components/planning/Goals";

export default async function GoalsPage() {
  const session = await requirePermission("household.read");
  const [goals, accounts] = await Promise.all([fetchFromApi<Goal[]>("goals"), fetchFromApi<AccountsOverview>("accounts")]);
  return <Goals goals={goals} accounts={accounts.accounts} today={accounts.asOf} canEdit={session.permissions.includes("finance.write")} />;
}
