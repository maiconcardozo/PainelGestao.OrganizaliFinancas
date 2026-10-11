import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import type { Alert, AlertPreference } from "@/lib/planning";
import { AlertList, AlertPreferences } from "@/components/planning/AlertCenter";

export default async function AlertsPage() {
  const session = await requirePermission("household.read");
  const [alerts, preference] = await Promise.all([fetchFromApi<Alert[]>("alerts"), fetchFromApi<AlertPreference>("alert-preferences")]);
  return <section className="page-content"><h1>Alertas</h1>
    <p className="intro">Orçamento perto ou acima do limite, saldo projetado negativo, contas e faturas vencendo, rendas atrasadas e valores a confirmar. Nenhum alerta movimenta dinheiro.</p>
    <AlertList alerts={alerts} canAct={session.permissions.includes("finance.write")} />
    <AlertPreferences preference={preference} canEdit={session.permissions.includes("budgets.write")} />
  </section>;
}
