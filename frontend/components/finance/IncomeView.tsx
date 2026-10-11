"use client";
import { useState } from "react";
import { Plus } from "lucide-react";
import { Notices, PageHeader } from "@/components/common/Ui";
import type { AccountsOverview, Forecast, IncomeSource, IncomeSummary, Receipt } from "@/lib/finance";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";
import { IncomeOverview, MonthlyComparison } from "./IncomeOverview";
import { Forecasts } from "./Forecasts";
import { ReceiptList, ReceiptModal } from "./Receipts";
import { IncomeSourceList, IncomeSourceModal } from "./IncomeSources";

type Props = {
  summary: IncomeSummary; forecasts: Forecast[]; receipts: Receipt[]; sources: IncomeSource[]; people: Person[];
  accounts: AccountsOverview; incomeTypes: CatalogItem[]; canEdit: boolean; canReverse: boolean;
};

/** Rendas: the overview on top, the two entries (a source, a receipt) as modals opened from the header. */
export function IncomeView({ summary, forecasts, receipts, sources, people, accounts, incomeTypes, canEdit, canReverse }: Props) {
  const [receiving, setReceiving] = useState(false);
  const [source, setSource] = useState<IncomeSource | "new" | null>(null);
  const [notice, setNotice] = useState("");
  function done(message: string) { setReceiving(false); setSource(null); setNotice(message); }
  return <section className="page-content">
    <PageHeader title="Rendas" description="O que cada morador espera receber, o que de fato entrou e a média mensal do ano."
      action={canEdit && <div className="flex flex-wrap gap-2">
        <button type="button" className="secondary-button" onClick={() => setSource("new")}><Plus size={16} />Nova fonte de renda</button>
        <button type="button" onClick={() => setReceiving(true)} className="inline-flex items-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-[var(--primary-strong)]"><Plus size={17} />Registrar recebimento</button>
      </div>} />
    <Notices message={notice} />
    <IncomeOverview summary={summary} />
    <Forecasts forecasts={forecasts} canEdit={canEdit} />
    <h2>Recebimentos do ano</h2>
    <ReceiptList receipts={receipts} canReverse={canReverse} onDone={setNotice} />
    <MonthlyComparison summary={summary} />
    <h2>Fontes de renda</h2>
    <IncomeSourceList sources={sources} canEdit={canEdit} onEdit={setSource} />
    {receiving && <ReceiptModal accounts={accounts.accounts} people={people} sources={sources} forecasts={forecasts} incomeTypes={incomeTypes} today={accounts.asOf}
      onClose={() => setReceiving(false)} onSaved={done} />}
    {source && <IncomeSourceModal source={source === "new" ? null : source} people={people} incomeTypes={incomeTypes} today={accounts.asOf}
      onClose={() => setSource(null)} onSaved={done} />}
  </section>;
}
