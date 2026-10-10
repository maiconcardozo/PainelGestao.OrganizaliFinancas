"use client";
import { useState, type FormEvent } from "react";
import { money, toApiMoney } from "@/lib/finance";
import type { Card } from "@/lib/cards";
import type { Person } from "@/components/households/HouseholdForms";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

/** RF23: nickname, holder, closing and due days and the informed limit. The card number is never asked. */
export function Cards({ cards, people, canEdit }: { cards: Card[]; people: Person[]; canEdit: boolean }) {
  const action = useFinanceAction();
  const [editing, setEditing] = useState<Card | null>(null);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget; const data = new FormData(form);
    const saved = await action.send(editing ? `cards/${editing.code}` : "cards", editing ? "PUT" : "POST", {
      holderPersonCode: data.get("holder"), name: data.get("name"), closingDay: Number(data.get("closingDay")), dueDay: Number(data.get("dueDay")),
      creditLimit: data.get("creditLimit") ? toApiMoney(data.get("creditLimit")) : null, closingDayRule: data.get("closingDayRule"),
      isActive: editing ? data.has("isActive") : true, version: editing?.version ?? 0,
    }, editing ? "Cartão atualizado. Os novos dias valem para as próximas faturas." : "Cartão cadastrado.");
    if (saved) { form.reset(); setEditing(null); }
  }
  return <>
    {cards.length === 0 ? <div className="empty-state"><h2>Nenhum cartão cadastrado</h2>
      <p>{canEdit ? "Cadastre o cartão pelo apelido, com os dias de fechamento e vencimento. O número do cartão não é necessário." : "Um titular ou editor pode cadastrar os cartões."}</p></div> :
      <div className="table-scroll"><table className="finance-table stack-table"><caption>Cartões da casa</caption>
        <thead><tr><th scope="col">Cartão</th><th scope="col" className="number">Comprometido</th><th scope="col">Ciclo</th><th scope="col"><span className="sr-only">Ações</span></th></tr></thead>
        <tbody>{cards.map(card => <tr key={card.code} className={card.isActive ? "" : "muted-row"}>
          <th scope="row">{card.name}{!card.isActive && <span className="tag">Inativo</span>}<span className="entry-meta">{card.holderName}</span></th>
          <td className="number">{money(card.committed)}<span className="entry-meta">{card.creditLimit ? `disponível ${money(card.available)} de ${money(card.creditLimit)}` : "sem limite informado"}</span></td>
          <td><span className="entry-meta">Fecha dia {card.closingDay}, vence dia {card.dueDay}</span></td>
          <td className="row-actions">{canEdit && <button type="button" className="secondary-button" onClick={() => setEditing(card)}>Editar</button>}</td>
        </tr>)}</tbody></table></div>}
    {cards.some(card => card.creditLimit) && <p className="form-hint">O disponível é uma estimativa a partir das compras lançadas aqui e pode diferir do banco.</p>}
    {canEdit && <form className="data-form" key={`${editing?.code ?? "new"}-${editing?.version ?? 0}`} onSubmit={save}>
      <h2>{editing ? `Editar ${editing.name}` : "Novo cartão"}</h2>
      {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
      <div className="form-row">
        <label>Apelido<input name="name" required maxLength={60} defaultValue={editing?.name ?? ""} placeholder="Nubank da Ana, Visa da casa" /></label>
        <label>Titular<select name="holder" required defaultValue={editing?.holderPersonCode ?? ""}>
          {people.filter(person => person.isActive || person.code === editing?.holderPersonCode).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>Dia do fechamento<input type="number" name="closingDay" required min={1} max={31} defaultValue={editing?.closingDay ?? 25} /></label>
        <label>Dia do vencimento<input type="number" name="dueDay" required min={1} max={31} defaultValue={editing?.dueDay ?? 5} /></label>
        <label>Limite (opcional)<input name="creditLimit" inputMode="decimal" defaultValue={editing?.creditLimit?.replace(".", ",") ?? ""} placeholder="5.000,00" /></label>
      </div>
      <fieldset className="choice-group"><legend>Compra feita no dia do fechamento entra em qual fatura?</legend>
        <label className="checkbox-label"><input type="radio" name="closingDayRule" value="next" defaultChecked={(editing?.closingDayRule ?? "next") === "next"} />Na próxima fatura (o mais comum)</label>
        <label className="checkbox-label"><input type="radio" name="closingDayRule" value="current" defaultChecked={editing?.closingDayRule === "current"} />Na fatura que fecha nesse dia</label>
      </fieldset>
      {editing && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={editing.isActive} />Cartão ativo</label>}
      <p className="form-hint">Não informe o número nem o código de segurança do cartão. Mudar os dias não move compras de faturas já criadas.</p>
      <div className="row-actions"><button className="primary-button" disabled={action.busy}>{editing ? "Salvar cartão" : "Cadastrar cartão"}</button>
        {editing && <button type="button" className="secondary-button" onClick={() => setEditing(null)}>Cancelar edição</button>}</div>
    </form>}
  </>;
}
