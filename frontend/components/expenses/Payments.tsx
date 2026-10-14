"use client";
import { HandCoins } from "lucide-react";
import { Empty } from "@/components/common/Ui";
import { ListFrame } from "@/components/common/ViewSwitch";
import { ReverseButton } from "@/components/finance/ReverseDialog";
import { AttachmentsToggle } from "@/components/common/Attachments";
import { civilDate, money } from "@/lib/finance";
import type { Payment } from "@/lib/expenses";

export function Payments({ payments, canReverse, canEdit = false }: { payments: Payment[]; canReverse: boolean; canEdit?: boolean }) {
  if (payments.length === 0) return <Empty title="Nenhum pagamento nos últimos meses." />;
  return <ListFrame>{payments.map(payment => <li key={payment.code} className={`payment-item flex flex-wrap items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4 ${payment.isReversed ? "opacity-60" : ""}`}>
    <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><HandCoins size={17} /></span>
    <span className="min-w-0 flex-1"><span className="block font-semibold">{payment.allocations.map(item => item.expenseDescription).join(", ")}</span>
      <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(payment.paymentDate)} de {payment.accountName}{payment.payerPersonName && ` · pago por ${payment.payerPersonName}`}
        {payment.isReversed && ` · Estornado: ${payment.reversalReason}`}</span></span>
    <span className="num shrink-0 text-sm font-semibold">{money(payment.amount)}</span>
    <AttachmentsToggle kind="payment" target={payment.code} canEdit={canEdit && !payment.isReversed} />
    {canReverse && !payment.isReversed && <ReverseButton path={`payments/${payment.code}/reversals`} title="Estornar pagamento"
      success="Pagamento estornado. O saldo da conta e o da despesa foram restaurados."
      summary={<>{money(payment.amount)} pago em {civilDate(payment.paymentDate)} volta para {payment.accountName}, e a despesa fica em aberto de novo.</>} />}
  </li>)}</ListFrame>;
}
