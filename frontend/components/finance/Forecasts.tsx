"use client";
import { useState, type FormEvent } from "react";
import { civilDate, money, signOf, type Forecast } from "@/lib/finance";
import { useFinanceAction } from "./useFinanceAction";

export function Forecasts({ forecasts, canEdit }: { forecasts: Forecast[]; canEdit: boolean }) {
  const action = useFinanceAction();
  return <section aria-labelledby="forecasts-title"><div className="statement-header"><h2 id="forecasts-title">Previsto x recebido</h2>
    {canEdit && <button type="button" className="secondary-button" disabled={action.busy}
      onClick={() => void action.send("income-forecasts/generate", "POST", undefined, "Previsões atualizadas até o mês atual + 12.")}>Atualizar previsões</button>}</div>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}{action.message && <p role="status">{action.message}</p>}
    {forecasts.length === 0 ? <p className="form-hint">Nenhuma previsão neste período. Cadastre uma fonte de renda abaixo para gerar as previsões.</p> :
      <ul className="forecast-list">{forecasts.map(item => <ForecastRow key={`${item.code}-${item.version}`} forecast={item} canEdit={canEdit} />)}</ul>}
  </section>;
}

function ForecastRow({ forecast, canEdit }: { forecast: Forecast; canEdit: boolean }) {
  const action = useFinanceAction();
  const [closing, setClosing] = useState(false);
  const target = forecast.expectedAmount ?? forecast.estimatedAmount;
  const received = Number(forecast.receivedAmount);
  const ratio = target && Number(target) > 0 ? Math.min(received / Number(target), 1) : 0;
  const closed = forecast.status === "closed";
  async function close(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await action.send(`income-forecasts/${forecast.code}/close`, "POST", { reason: new FormData(event.currentTarget).get("reason"), version: forecast.version }, "Previsão encerrada.");
  }
  return <li className={closed ? "muted-row" : ""}>
    <div className="forecast-head"><div><strong>{forecast.sourceName}</strong><span className="entry-meta">{forecast.personName} · {civilDate(forecast.occurrenceDate)}</span></div>
      <div className="number"><strong>{money(forecast.receivedAmount)}</strong><span className="entry-meta">
        {forecast.expectedAmount ? `de ${money(forecast.expectedAmount)} previstos` : forecast.estimatedAmount ? `estimativa pela média: ${money(forecast.estimatedAmount)}` : "renda variável sem média ainda"}</span></div></div>
    {target && <div className="progress" role="img" aria-label={`Recebido ${Math.round(ratio * 100)}% do ${forecast.expectedAmount ? "previsto" : "estimado"}`}>
      <span style={{ width: `${ratio * 100}%` }} className={forecast.expectedAmount ? "" : "estimate"} /></div>}
    <p className="forecast-figures">
      {signOf(forecast.difference) > 0 && <span className="positive">+{money(forecast.difference)} acima do {forecast.expectedAmount ? "previsto" : "estimado"}</span>}
      {!closed && signOf(forecast.residual) > 0 && <span className="pending">Falta receber {money(forecast.residual)}</span>}
      {closed && <span>Encerrada abaixo do previsto: {forecast.closeReason}</span>}
      {!closed && signOf(forecast.residual) === 0 && received > 0 && signOf(forecast.difference) <= 0 && <span className="positive">Recebida</span>}
    </p>
    {action.error && <p className="form-error" role="alert">{action.error}</p>}
    {canEdit && !closed && received > 0 && signOf(forecast.residual) > 0 && (closing
      ? <form className="inline-form" onSubmit={close}><label>Por que o valor ficou menor?<input name="reason" required maxLength={300} /></label>
        <button className="secondary-button" disabled={action.busy}>Encerrar com valor menor</button></form>
      : <button type="button" className="text-button" onClick={() => setClosing(true)}>Encerrar com valor menor</button>)}
  </li>;
}
