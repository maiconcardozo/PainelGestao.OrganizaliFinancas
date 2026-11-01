# Entrega — Compras do cartão em Despesas, aviso de exclusão em despesa paga e estorno da loja como ícone

Data: 01/11/2026 (noite). Estado: implementado; validação registrada abaixo. Pedido da família piloto, em três pontos: "quando eu lançar uma compra na aba Cartões, ela precisa aparecer automaticamente na aba Despesas, no mês da fatura"; "quero poder apagar lançamentos que eu criei manualmente em Despesas, sem depender de cancelamento"; "remover o rótulo 'Estorno da loja' das compras parceladas novas, que aparece em todas e não é estorno".

## Objetivo e funcionalidades

1. **Compras do cartão em Despesas.** Cada compra lançada em **Cartões** aparece em **Despesas** no mês em que a fatura vence, uma linha por parcela ("Televisão · parcela 2/3"), com categoria, quem comprou, o cartão e a data da compra. A linha mostra o estado da fatura (**Na fatura**, **Paga na fatura**, **Fatura paga em parte**, **Fatura vencida**) e o botão **Ver em Cartões**. Essas linhas somam em "Despesas do mês" e "Falta pagar" e passam a "Pago" quando a fatura é paga. A fatura em si continua fora de Despesas (A18), então nada é contado duas vezes: a despesa paga no cartão (A17) aparece uma vez, como despesa, e a compra que a liquidou não vira linha.
2. **Excluir despesa lançada à mão.** O botão **Excluir** (entrega da tarde, A18) já apaga de vez despesa aberta ou cancelada. Nesta entrega ele passa a aparecer também em despesa paga ou paga em parte: em vez de sumir, abre um aviso explicando que o pagamento precisa ser estornado antes, em **Pagamentos recentes**, e que depois a exclusão é liberada. Assim a família não fica procurando o botão.
3. **"Estorno da loja" deixa de parecer um rótulo.** Em **Compras recentes** o texto "Estorno da loja", que aparecia como botão em toda compra ainda não estornada, virou o ícone de seta de volta ao lado do lápis (editar) e da lixeira (excluir), com a dica "Registrar estorno da loja de …". A janela de estorno é a mesma. O rótulo informativo "estornado R$ X" continua só nas compras que de fato tiveram estorno.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Linha de compra | `GET /api/expenses` devolve, além das obrigações da casa, um item por `CardItem` ativo cuja fatura vence no período (`i.DueDate`), com `cardPurchaseCode`, `cardName`, `purchaseDate` e `cardInvoiceCode` preenchidos. Competência = mês de referência da fatura; vencimento = vencimento da fatura. Parcelas canceladas por estorno não aparecem. |
| Liquidação | O pagamento da fatura (alocações vivas, estornos ignorados) liquida as linhas na ordem dos itens da fatura: fatura paga = todas "Paga na fatura"; paga em parte = primeiras parcelas pagas, a seguinte "Fatura paga em parte", o resto "Na fatura"; a soma de pago e em aberto das linhas é igual à da fatura. Vencida quando a fatura venceu e a linha ainda tem saldo. |
| Crédito da loja | Item `credit` entra como linha negativa ("Compra (crédito da loja)"), paga quando a fatura está paga, em aberto (negativa) quando não. |
| Despesa paga no cartão | A compra que liquida uma despesa (A17, `Payment.IdCardPurchase`) não gera linha: a despesa já é a linha. |
| Ações | A linha não tem Pagar, Detalhes, Cancelar nem Excluir: `DELETE /api/expenses/{code}` de um código de item responde 404. Editar, estornar e excluir ficam em **Cartões** (A16). |
| Vencidas de meses anteriores | Com `includeOverdue`, linhas de faturas anteriores ao período ainda com saldo entram no total "Vencidas em aberto", como as despesas. |
| Painel da casa | "Despesas do mês" do painel usa o mesmo cálculo de **Despesas** (sem fatura, com as compras): antes somava a fatura e a despesa paga no cartão. |
| Excluir despesa paga | Sem mudança de regra (409 na API); só a tela mostra o aviso em vez de esconder o botão. |

Premissa registrada em product/decisions.md (**A19**).

## Modelo e contratos

Sem migration. `ICardRepository.ListItemsDueAsync` (SQL `CardSql.ItemsDue`, que reutiliza as somas de ajuste e pagamento de `ExpenseSql` sobre a obrigação da fatura); `ExpenseMapper.CardItems` monta as linhas; `ExpenseListing.ReadAsync` (Application/Expenses/ExpenseSupport.cs) é a listagem única usada por `ListExpensesHandler` e pelo painel (`GetDashboardHandler`). `ExpenseDto` ganhou `CardPurchaseCode`, `CardName` e `PurchaseDate` (nulos nas despesas).

Frontend: `components/expenses/ExpenseList.tsx` (linha `CardPurchaseRow`, aviso de exclusão em despesa paga), `components/cards/Invoices.tsx` (ícone de estorno), `lib/expenses.ts`. Tutorial (3.3 e 3.5) atualizado e `content/tutorial.ts` regenerado.

## Validação e reprodução

- `tests/Helpers/CardAssertions.cs` (A19, executado por `PersistenceTests` em banco descartável): no mês da fatura do "Visa da casa", a lista traz "Mercado do mês" 300,00 paga (fatura paga), o crédito de -50,00 pago, nenhuma parcela da "Televisão" (estornada) e não traz a obrigação da fatura; "Farmácia" na fatura paga em parte do "Master" aparece com 100,00 pagos e 200,00 em aberto; a compra que liquidou "Internet" (A17) não aparece; `DELETE` do código de uma linha de compra responde 404.
- Fumaça na tela (com o login do piloto): abrir **Despesas** no mês de vencimento da fatura Bradesco e conferir as compras uma a uma; abrir **Cartões → Compras recentes** e conferir que "Estorno da loja" virou o ícone de seta.

## Execução

(preenchido após a validação no servidor)

## Limitações e pendências

- A linha de compra não abre Detalhes nem comprovante em Despesas; o caminho é **Ver em Cartões**.
- Pagamento parcial da fatura liquida as parcelas na ordem dos itens, não proporcionalmente.
- Orçamento e análise por categoria não mudaram: continuam contando a compra do cartão na competência da fatura, como já faziam (M7).

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
