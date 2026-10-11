"use client";
import { type FormEvent } from "react";
import { CreditCard } from "lucide-react";
import { Modal } from "@/components/common/Modal";
import { Card as Tile, EditButton, Empty, Figure, ModalActions, Pill } from "@/components/common/Ui";
import { ListBar, ListFrame, useViewMode } from "@/components/common/ViewSwitch";
import { money, toApiMoney } from "@/lib/finance";
import type { Card } from "@/lib/cards";
import type { Person } from "@/components/households/HouseholdForms";
import { useFinanceAction } from "@/components/finance/useFinanceAction";

/** RF23: the household's cards, as a RevendaPRO mosaic or list. The card number is never shown or asked. */
export function CardList({ cards, canEdit, onEdit }: { cards: Card[]; canEdit: boolean; onEdit: (card: Card) => void }) {
  const [view, chooseView] = useViewMode("organizali.cards.view");
  const limitHint = (card: Card) => card.creditLimit ? `Disponível ${money(card.available)} de ${money(card.creditLimit)}` : "Sem limite informado";
  return <>
    <ListBar count={cards.length} singular="cartão" plural="cartões" view={view} onChange={chooseView} label="Como mostrar os cartões" />
    {cards.length === 0 ? <Empty title={canEdit ? "Nenhum cartão ainda. Cadastre pelo apelido, com os dias de fechamento e vencimento; o número do cartão não é necessário." : "Nenhum cartão cadastrado. Um titular ou editor pode cadastrar os cartões."} /> :
      view === "list" ? <ListFrame>{cards.map(card => <li key={card.code} className={`card-item flex items-center gap-3 px-3 py-2.5 transition hover:bg-[var(--surface-2)] sm:gap-4 sm:px-4 ${card.isActive ? "" : "opacity-60"}`}>
        <span className="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-[var(--surface-2)] text-[var(--signal)]"><CreditCard size={17} /></span>
        <span className="min-w-0 flex-1"><span className="block truncate font-semibold">{card.name}</span>
          <span className="mt-0.5 block truncate text-xs text-[var(--text-muted)]">{card.holderName} · fecha dia {card.closingDay}, vence dia {card.dueDay}</span></span>
        <span className="hidden shrink-0 text-right sm:block"><span className="num block text-sm font-semibold">{money(card.committed)}</span>
          <span className="block text-xs text-[var(--text-muted)]">{limitHint(card)}</span></span>
        {canEdit && <EditButton name={card.name} onClick={() => onEdit(card)} />}
      </li>)}</ListFrame> :
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{cards.map(card => <div key={card.code} className="card-item"><Tile muted={!card.isActive}>
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0"><p className="flex items-center gap-2 font-semibold"><CreditCard size={16} className="shrink-0 text-[var(--signal)]" /><span className="truncate">{card.name}</span></p>
            <p className="mt-0.5 text-xs text-[var(--text-secondary)]">{card.holderName}</p></div>
          {canEdit && <EditButton name={card.name} onClick={() => onEdit(card)} />}
        </div>
        <Figure label="Comprometido" value={money(card.committed)} hint={limitHint(card)} />
        <dl className="grid gap-y-1.5 text-sm">
          <div className="flex justify-between gap-3"><dt className="text-[var(--text-muted)]">Fechamento</dt><dd className="num">dia {card.closingDay}</dd></div>
          <div className="flex justify-between gap-3"><dt className="text-[var(--text-muted)]">Vencimento</dt><dd className="num">dia {card.dueDay}</dd></div>
        </dl>
        <div className="mt-auto"><Pill tone={card.isActive ? "success" : "muted"}>{card.isActive ? "Ativo" : "Inativo"}</Pill></div>
      </Tile></div>)}</div>}
    {cards.some(card => card.creditLimit) && <p className="form-hint mt-3">O disponível é uma estimativa a partir das compras lançadas aqui e pode diferir do banco.</p>}
  </>;
}

export function CardModal({ card, people, onClose, onSaved }: { card: Card | null; people: Person[]; onClose: () => void; onSaved: (message: string) => void }) {
  const action = useFinanceAction();
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const success = card ? "Cartão atualizado. Os novos dias valem para as próximas faturas." : "Cartão cadastrado.";
    const saved = await action.send(card ? `cards/${card.code}` : "cards", card ? "PUT" : "POST", {
      holderPersonCode: data.get("holder"), name: data.get("name"), closingDay: Number(data.get("closingDay")), dueDay: Number(data.get("dueDay")),
      creditLimit: data.get("creditLimit") ? toApiMoney(data.get("creditLimit")) : null, closingDayRule: data.get("closingDayRule"),
      isActive: card ? data.has("isActive") : true, version: card?.version ?? 0,
    }, success);
    if (saved) onSaved(success);
  }
  return <Modal title={card ? `Editar ${card.name}` : "Novo cartão"} onClose={onClose} error={action.error}>
    <form className="data-form" onSubmit={save}>
      <div className="form-row">
        <label>Apelido<input name="name" required maxLength={60} defaultValue={card?.name ?? ""} placeholder="Nubank da Ana, Visa da casa" autoComplete="off" /></label>
        <label>Titular<select name="holder" required defaultValue={card?.holderPersonCode ?? ""}>
          {people.filter(person => person.isActive || person.code === card?.holderPersonCode).map(person => <option key={person.code} value={person.code}>{person.name}</option>)}</select></label>
      </div>
      <div className="form-row">
        <label>Dia do fechamento<input type="number" name="closingDay" required min={1} max={31} defaultValue={card?.closingDay ?? 25} /></label>
        <label>Dia do vencimento<input type="number" name="dueDay" required min={1} max={31} defaultValue={card?.dueDay ?? 5} /></label>
        <label>Limite (opcional)<input name="creditLimit" inputMode="decimal" defaultValue={card?.creditLimit?.replace(".", ",") ?? ""} placeholder="5.000,00" /></label>
      </div>
      <fieldset className="choice-group"><legend>Compra feita no dia do fechamento entra em qual fatura?</legend>
        <label className="checkbox-label"><input type="radio" name="closingDayRule" value="next" defaultChecked={(card?.closingDayRule ?? "next") === "next"} />Na próxima fatura (o mais comum)</label>
        <label className="checkbox-label"><input type="radio" name="closingDayRule" value="current" defaultChecked={card?.closingDayRule === "current"} />Na fatura que fecha nesse dia</label>
      </fieldset>
      {card && <label className="checkbox-label"><input type="checkbox" name="isActive" defaultChecked={card.isActive} />Cartão ativo</label>}
      <p className="form-hint">Não informe o número nem o código de segurança do cartão. Mudar os dias não move compras de faturas já criadas.</p>
      <ModalActions busy={action.busy} label={card ? "Salvar cartão" : "Cadastrar cartão"} onCancel={onClose} />
    </form>
  </Modal>;
}
