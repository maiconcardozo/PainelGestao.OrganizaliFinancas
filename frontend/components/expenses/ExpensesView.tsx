"use client";
import { useState, type ReactNode } from "react";
import { NewButton, Notices, PageHeader } from "@/components/common/Ui";
import type { Account } from "@/lib/finance";
import type { Catalogs } from "./DetailInputs";
import { ExpenseModal } from "./ExpenseForm";

type Props = { catalogs: Catalogs; accounts: Account[]; today: string; canEdit: boolean; canSettle: boolean; children: ReactNode };

/** Despesas: the header carries "Nova despesa"; the month content stays server-rendered below. */
export function ExpensesView({ catalogs, accounts, today, canEdit, canSettle, children }: Props) {
  const [creating, setCreating] = useState(false);
  const [notice, setNotice] = useState("");
  return <section className="page-content">
    <PageHeader title="Despesas" description="Contas a pagar da casa: o que vence no mês, o que já foi pago e quem é responsável por cada parte."
      action={canEdit && <NewButton label="Nova despesa" onClick={() => setCreating(true)} />} />
    <Notices message={notice} />
    {children}
    {creating && <ExpenseModal catalogs={catalogs} accounts={accounts} today={today} canSettle={canSettle} onClose={() => setCreating(false)}
      onSaved={message => { setCreating(false); setNotice(message); }} />}
  </section>;
}
