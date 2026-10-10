import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { AccountsOverview, Transfer } from "@/lib/finance";
import { Accounts } from "@/components/finance/Accounts";
export default async function AccountsPage() {
  const session = await requirePermission("household.read");
  const [overview, transfers] = await Promise.all([fetchFromApi<AccountsOverview>("accounts"), fetchFromApi<Transfer[]>("transfers")]);
  return <Accounts overview={overview} transfers={transfers} canEdit={session.permissions.includes("finance.write")}
    canReverse={session.permissions.includes("payments.reverse")} />;
}
