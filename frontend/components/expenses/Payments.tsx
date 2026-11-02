"use client";
import { useState, type FormEvent } from "react";
import { HandCoins, Pencil } from "lucide-react";
import { Empty, ModalActions } from "@/components/common/Ui";
import { Modal } from "@/components/common/Modal";
import { ListFrame } from "@/components/common/ViewSwitch";
import { ReverseButton } from "@/components/finance/ReverseDialog";
import { AttachmentsToggle } from "@/components/common/Attachments";
import { useFinanceAction } from "@/components/finance/useFinanceAction";
import { civilDate, money } from "@/lib/finance";
import { paidWith, type Payment } from "@/lib/expenses";

type Props = { payments: Payment[]; canReverse: boolean; canEdit?: boolean };

export function Payments({ payments, canReverse, canEdit = false }: Props) {
  if (payments.length === 0) return <Empty title="Nenhum pagamento nos últimos meses." />;
  return <ListFrame>{payments.map(payment => <PaymentRow key={`${payment.code}-${payment.reversalReason ?? ""}`} payment={payment} canReverse={canReverse} canEdit={canEdit} />)}</ListFrame>;
}

function PaymentRow({ payment, canReverse, canEdit }: { payment: Payment; canReverse: boolean; canEdit: boolean }) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState(false);
  // A20: the owner corrects the reason of a reversal already made; the reversal itself stays.
  async function editReason(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await action.send(`payments/${payment.code}/reversal-reason`, "PUT", { reason: new FormData(event.currentTarget).get("reason") }, "Justificativa do estorno atualizada.")) setEditing(false);
  }
  return <li className={`payment-item flex flex-wrap items-center gap-3 px-3 py-2.5 sm:gap-4 sm:px-4 ${payment.isReversed ? "opacity-60" : ""}`}>
    <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><HandCoins size={17} /></span>
    <span className="min-w-0 flex-1"><span className="block font-semibold">{payment.allocations.map(item => item.expenseDescription).join(", ")}</span>
      <span className="mt-0.5 block text-xs text-[var(--text-muted)]">{civilDate(payment.paymentDate)} {payment.cardName ? "no" : "de"} {paidWith(payment)}{payment.payerPersonName && ` · pago por ${payment.payerPersonName}`}
        {payment.isReversed && <> · Estornado: {payment.reversalReason}
          {canReverse && <button type="button" onClick={() => setEditing(true)} aria-label="Corrigir a justificativa do estorno" title="Corrigir justificativa"
            className="ml-1 inline-flex rounded p-0.5 align-middle text-[var(--text-secondary)] hover:text-[var(--primary)]"><Pencil size={12} /></button>}</>}</span></span>
    <span className="num shrink-0 text-sm font-semibold">{money(payment.amount)}</span>
    <AttachmentsToggle kind="payment" target={payment.code} canEdit={canEdit && !payment.isReversed} />
    {canReverse && !payment.isReversed && <ReverseButton path={`payments/${payment.code}/reversals`} title="Estornar pagamento"
      success={payment.cardName ? "Pagamento estornado. O valor saiu da fatura e a despesa voltou a ficar em aberto." : "Pagamento estornado. O saldo da conta e o da despesa foram restaurados."}
      summary={payment.cardName
        ? <>{money(payment.amount)} pago em {civilDate(payment.paymentDate)} sai da fatura do {payment.cardName}, e a despesa fica em aberto de novo. Se essa fatura já foi paga, estorne antes o pagamento da fatura.</>
        : <>{money(payment.amount)} pago em {civilDate(payment.paymentDate)} volta para {payment.accountName}, e a despesa fica em aberto de novo.</>} />}
    {editing && <Modal title="Corrigir a justificativa do estorno" onClose={() => setEditing(false)} error={action.error} width="max-w-md">
      <form className="data-form" onSubmit={editReason}>
        <label>Justificativa<input name="reason" required maxLength={300} autoComplete="off" defaultValue={payment.reversalReason ?? ""} /></label>
        <ModalActions busy={action.busy} label="Salvar justificativa" onCancel={() => setEditing(false)} />
      </form>
    </Modal>}
  </li>;
}
