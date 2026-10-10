import { civilDate, money, monthLabel, signOf, type IncomeSummary } from "@/lib/finance";

/** D24: monthly average over the closed months of the calendar year; D25: excess is shown only as a comparison. */
export function IncomeOverview({ summary }: { summary: IncomeSummary }) {
  const period = `${civilDate(summary.averageFrom)} a ${civilDate(summary.averageTo)}`;
  return <>
    <div className="money-summary"><span>Média mensal da casa · {summary.averageMonths} {summary.averageMonths === 1 ? "mês fechado" : "meses fechados"} ({period})</span>
      <strong>{money(summary.householdAverage)}</strong><span>Total recebido no período: {money(summary.householdTotal)}</span></div>
    {(summary.people.length > 0 || summary.sources.length > 0) && <div className="average-grid">
      <div><h3>Por morador</h3><ul className="entry-list compact">{summary.people.map(item => <li key={item.code ?? item.name}>
        <div><strong>{item.name}</strong><span className="entry-meta">Recebido no período {money(item.total)}</span></div><span className="number">{money(item.average)}/mês</span></li>)}</ul></div>
      <div><h3>Por fonte</h3>{summary.sources.length === 0 ? <p className="form-hint">Ainda não há recebimentos nos meses fechados.</p> :
        <ul className="entry-list compact">{summary.sources.map(item => <li key={`${item.code}-${item.personName}`}>
          <div><strong>{item.name}</strong><span className="entry-meta">{item.personName}</span></div><span className="number">{money(item.average)}/mês</span></li>)}</ul>}</div>
    </div>}
  </>;
}

export function MonthlyComparison({ summary }: { summary: IncomeSummary }) {
  if (summary.months.length === 0) return null;
  return <>
    <h2>Comparativo de {summary.year}</h2>
      <p className="form-hint">Comparativo por mês. A diferença positiva mostra quanto entrou além da base prevista; renda variável não tem valor previsto.</p>
      <div className="table-scroll"><table className="finance-table"><caption className="sr-only">Comparativo mensal por morador</caption>
        <thead><tr><th scope="col">Mês</th><th scope="col">Morador</th><th scope="col" className="number">Previsto</th><th scope="col" className="number">Recebido</th><th scope="col" className="number">Diferença</th></tr></thead>
        <tbody>{summary.months.map(row => <tr key={`${row.month}-${row.personCode}`}><td>{monthLabel(row.month)}</td><td>{row.personName}</td>
          <td className="number">{money(row.expected)}</td><td className="number">{money(row.received)}</td>
          <td className={`number ${signOf(row.difference) > 0 ? "positive" : signOf(row.difference) < 0 ? "negative" : ""}`}>{signOf(row.difference) > 0 ? "+" : ""}{money(row.difference)}</td></tr>)}</tbody>
      </table></div>
  </>;
}
