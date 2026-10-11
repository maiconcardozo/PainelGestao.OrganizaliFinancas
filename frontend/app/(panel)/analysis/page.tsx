import { requirePermission } from "@/lib/session";
import { fetchFromApi } from "@/lib/server";
import { civilDate, money, monthLabel, type AccountsOverview } from "@/lib/finance";
import { analysisStatus, type Analysis } from "@/lib/planning";
import type { CatalogItem } from "@/lib/catalogs";
import type { Person } from "@/components/households/HouseholdForms";

type Search = { month?: string; category?: string; person?: string; account?: string; status?: string };
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** RF32: one GET form drives both the chart and the list, which come from the same filtered lines. */
export default async function AnalysisPage({ searchParams }: { searchParams: Promise<Search> }) {
  await requirePermission("household.read");
  const search = await searchParams;
  const query: Record<string, string> = {};
  if (search.month && /^\d{4}-(0[1-9]|1[0-2])$/.test(search.month)) query.month = `${search.month}-01`;
  if (search.category && uuid.test(search.category)) query.categoryCode = search.category;
  if (search.person && uuid.test(search.person)) query.personCode = search.person;
  if (search.account && uuid.test(search.account)) query.accountCode = search.account;
  if (search.status && Object.keys(analysisStatus).includes(search.status)) query.status = search.status;
  const [analysis, categories, people, accounts] = await Promise.all([fetchFromApi<Analysis>("analysis", query), fetchFromApi<CatalogItem[]>("catalogs/categories"),
    fetchFromApi<Person[]>("people"), fetchFromApi<AccountsOverview>("accounts")]);
  const peak = Math.max(...analysis.byCategory.map(x => Number(x.amount)), 1);
  return <section className="page-content"><h1>Análise de despesas</h1>
    <p className="intro">Despesas da competência por categoria, com os mesmos filtros no gráfico e na lista.</p>
    <form className="data-form filter-form" method="get">
      <div className="form-row">
        <label>Mês<input type="month" name="month" defaultValue={analysis.month} /></label>
        <label>Categoria<select name="category" defaultValue={search.category ?? ""}><option value="">Todas</option>
          {categories.map(item => <option key={item.code} value={item.code}>{item.parentCode ? "— " : ""}{item.name}</option>)}</select></label>
        <label>Morador responsável<select name="person" defaultValue={search.person ?? ""}><option value="">Todos</option>
          {people.map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
        <label>Conta que pagou<select name="account" defaultValue={search.account ?? ""}><option value="">Todas</option>
          {accounts.accounts.map(account => <option key={account.code} value={account.code}>{account.name}</option>)}</select></label>
        <label>Situação<select name="status" defaultValue={search.status ?? ""}><option value="">Todas</option>
          {Object.entries(analysisStatus).map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
      </div>
      <button className="primary-button">Aplicar filtros</button>
    </form>
    <dl className="money-summary money-grid">
      <div><dt>Total em {monthLabel(analysis.month)}</dt><dd>{money(analysis.total)}</dd></div>
      <div><dt>Pago</dt><dd>{money(analysis.paid)}</dd></div>
    </dl>
    {analysis.lines.length === 0 ? <p className="form-hint">Nenhuma despesa com esses filtros.</p> : <>
      <h2>Por categoria</h2>
      <ul className="category-bars">{analysis.byCategory.map(item => <li key={item.categoryCode ?? "none"}>
        <span>{item.name}</span><span className="bar-track"><span style={{ width: `${(Number(item.amount) / peak) * 100}%` }} /></span><strong className="number">{money(item.amount)}</strong></li>)}</ul>
      <h2>Lançamentos</h2>
      <div className="table-scroll"><table className="finance-table stack-table"><caption>{analysis.lines.length} lançamento(s) somando {money(analysis.total)}</caption>
        <thead><tr><th scope="col">Despesa</th><th scope="col" className="number">Valor</th><th scope="col">Situação</th></tr></thead>
        <tbody>{analysis.lines.map(line => <tr key={`${line.source}-${line.code}`}>
          <th scope="row">{line.description}<span className="entry-meta">{line.categoryName ?? "Sem categoria"} · vence {civilDate(line.dueDate)}{line.cardName && ` · ${line.cardName}`}</span></th>
          <td className="number">{money(line.amount)}</td><td><span className={`status-tag ${line.status}`}>{analysisStatus[line.status]}</span></td></tr>)}</tbody></table></div>
    </>}
    <p className="form-hint">Compras no cartão contam no mês da fatura. Faturas não aparecem aqui como despesa para não somar duas vezes. O filtro de conta considera as despesas pagas pela conta escolhida.</p>
  </section>;
}
