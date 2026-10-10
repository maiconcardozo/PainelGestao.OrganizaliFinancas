import type { ReactNode } from "react";
import { requireSession } from "@/lib/session";
import { PanelShell } from "@/components/layout/PanelShell";

export default async function PanelLayout({ children }: { children: ReactNode }) {
  const session = await requireSession();
  return <PanelShell session={session}>{children}</PanelShell>;
}
