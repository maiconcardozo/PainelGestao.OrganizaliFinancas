"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money } from "@/lib/finance";
import type { Payment } from "@/lib/expenses";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

export function Payments({ payments, canReverse }: { payments: Payment[]; canReverse: boolean }) {
  if (payments.length === 0) return <p className="form-hint">Nenhum pagamento nos últimos meses.</p>;
  return <ul className="entry-list">{payments.map(payment => <PaymentRow key={payment.code} payment={payment} canReverse={canReverse} />)}</ul>;
}

function PaymentRow({ payment, canReverse }: { payment: Payment; canReverse: boolean }) {
  const action = useFinanceAction();
  const [open, setOpen] = useState(false);
  async function reverse(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await action.send(`payments/${payment.code}/reversals`, "POST", { reason: new FormData(event.currentTarget).get("reason") },
      "Pagamento estornado. O saldo da conta e o da despesa foram restaurados.", true);
  }
  return <li className={payment.isReversed ? "muted-row" : ""}>
    <div><strong>{money(payment.amount)}</strong> · {payment.allocations.map(item => item.expenseDescription).join(", ")}
      <span className="entry-meta">{civilDate(payment.paymentDate)} de {payment.accountName}{payment.payerPersonName && ` · pago por ${payment.payerPersonName}`}</span>
      {payment.isReversed && <span className="entry-meta">Estornado: {payment.reversalReason}</span>}</div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {canReverse && !payment.isReversed && (open
      ? <form className="inline-form" onSubmit={reverse}><label>Motivo do estorno<input name="reason" required maxLength={300} /></label>
        <button className="secondary-button" disabled={action.busy}>Confirmar estorno</button></form>
      : <button type="button" className="secondary-button" onClick={() => setOpen(true)}>Estornar</button>)}
  </li>;
}
