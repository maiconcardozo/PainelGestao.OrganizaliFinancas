import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { Members, type MembersData } from "@/components/members/Members";

export default async function MembersPage() {
  await requirePermission("members.manage");
  const data = await fetchFromApi<MembersData>("members");
  return <Members initial={data} />;
}
