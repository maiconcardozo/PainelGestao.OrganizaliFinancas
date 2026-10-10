import { money, signOf } from "@/lib/finance";
import type { Contributions as Data } from "@/lib/expenses";

/** D10/RF41: shares of the split bills, what each resident paid and the internal settlement. The settlement is not a new expense. */
export function Contributions({ data }: { data: Data }) {
  if (data.people.length === 0) return <p className="form-hint">Nenhuma despesa dividida neste mês. Ao lançar, marque “Dividir entre moradores” para acompanhar quem paga e quem é responsável.</p>;
  return <>
    <div className="table-scroll"><table className="finance-table stack-table"><caption className="sr-only">Rateio por morador</caption>
      <thead><tr><th scope="col">Morador</th><th scope="col" className="number">Responsável por</th><th scope="col" className="number">Pagou</th><th scope="col" className="number">Saldo</th></tr></thead>
      <tbody>{data.people.map(person => <tr key={person.personCode}><th scope="row">{person.personName}</th>
        <td className="number">{money(person.responsible)}</td><td className="number">{money(person.paid)}</td>
        <td className={`number ${signOf(person.balance) > 0 ? "positive" : signOf(person.balance) < 0 ? "negative" : ""}`}>{signOf(person.balance) > 0 ? "+" : ""}{money(person.balance)}</td></tr>)}</tbody>
    </table></div>
    {data.settlements.length === 0 ? <p className="form-hint">Ninguém precisa acertar valores neste mês.</p> :
      <ul className="entry-list compact" aria-label="Acertos sugeridos">{data.settlements.map(item =>
        <li key={`${item.fromPersonCode}-${item.toPersonCode}`}><span>{item.fromPersonName} repassa para {item.toPersonName}</span><strong className="number">{money(item.amount)}</strong></li>)}</ul>}
    <p className="form-hint">O acerto é um repasse entre moradores: não é uma nova despesa nem uma renda. Se for feito entre contas da casa, registre como transferência.</p>
  </>;
}
