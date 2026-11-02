# Entrega — Fatura por cartão em Despesas, reativar despesa, corrigir motivos e editar compra

Data: 02/11/2026. Estado: implementado; validação registrada abaixo. Pedido do Maicon em PDF ("Pedido de ajuste: Despesas mostrar só o total da fatura do cartão", 01/11/2026): a entrega da noite de 01/11 (A19) listava cada compra e cada parcela do cartão em Despesas, e a fatura de dezembro do Bradesco virava 29 linhas. O pedido é **uma linha por cartão por mês, com o valor total da fatura**, e quatro ajustes opcionais: reativar despesa cancelada, editar o motivo do cancelamento ou do estorno, editar compra no cartão (valor, data, parcelas) e tirar o rótulo "Estorno da loja" (já feito em `49a87db`, virou ícone).

## Objetivo e funcionalidades

1. **Uma linha por fatura em Despesas.** No mês em que a fatura vence, **Despesas** mostra "Fatura Bradesco – dez/26" com o total das compras e parcelas daquela fatura, "Vence 10/12 · 29 compras no cartão Bradesco", o estado (Em aberto, Paga em parte, Paga em dd/mm, Vencida) e os botões **Pagar fatura** (só com conta), **Ver compras** (abre a fatura em **Cartões** com a lista de compras já aberta) e **Detalhes** (pagamentos e ajustes da fatura). A linha soma em "Despesas do mês" e "Falta pagar". As compras individuais existem só em Cartões.
2. **Sem duplicar a despesa paga no cartão.** Escola, Alfa e qualquer despesa paga com "Pagar com cartão" (A17) entram só dentro do total da fatura: enquanto estão pagas, não aparecem na lista nem nos totais de Despesas. Se o pagamento no cartão for estornado, a despesa volta à lista em aberto.
3. **Desfazer cancelamento.** Despesa cancelada ganha o botão **Desfazer cancelamento**: volta a ficar em aberto com os mesmos valores, datas e responsáveis.
4. **Corrigir motivos.** Ao lado de "Motivo: …" na despesa cancelada e de "Estornado: …" em **Pagamentos recentes** há um lápis que abre a correção do texto (motivo do cancelamento: titular ou editor; justificativa do estorno: titular).
5. **Editar compra no cartão por completo.** Em **Compras recentes**, o lápis passa a editar também **Valor total**, **Parcelas** e **Data da compra** (com a escolha da primeira fatura): as parcelas são refeitas nas faturas do cartão. Recusado se uma fatura com a compra já recebeu pagamento ou se a compra teve estorno da loja; a compra que é pagamento de despesa no cartão continua só com descrição editável.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Linha de fatura | `GET /api/expenses` devolve a obrigação da fatura como item com `cardInvoiceCode`, `cardName`, `cardItemCount` (itens ativos) e `description` = "Fatura {cartão} – {mmm/aa}" (mês de referência da fatura). Vencimento e competência são os da fatura. Fatura com valor, pago e ajustes zerados (todas as compras excluídas ou estornadas) não aparece. |
| Valor e atualização | O valor da linha é `Obligation.Amount` da fatura, recalculado a cada lançamento, edição, exclusão ou estorno de compra (já era assim); a lista reflete na hora. |
| Pagamento | A fatura é a própria obrigação: pagar em Despesas ou em Cartões é o mesmo `POST /api/payments` com o `code` da linha; a linha fica "Paga em dd/mm" (`paidOn` = data do último pagamento vivo). Fatura não se paga com cartão (A17). |
| Despesa paga no cartão | `ObligationRecord.PaidOnCard` (alocação viva de pagamento com `IdCardPurchase`); a despesa sai da lista quando `PaidOnCard` e saldo aberto ≤ 0 e não cancelada (`ExpenseListing.Shown`). Com o pagamento estornado, volta. |
| Painel da casa | "Despesas do mês" do painel usa `ExpenseListing.ReadAsync`, o mesmo cálculo da tela. |
| Reativar | `POST /api/expenses/{code}/reactivate` `{version}`: só despesa cancelada; `Status=open`, `CancelReason=null`, `Version+1`. Ocorrência de recorrente reativada volta ao controle da recorrência (não está travada). |
| Motivo do cancelamento | `PUT /api/expenses/{code}/cancel-reason` `{reason, version}`: só despesa cancelada; `finance.write`. |
| Justificativa do estorno | `PUT /api/payments/{code}/reversal-reason` `{reason}`: só pagamento estornado; `payments.reverse` (titular). Nada mais muda. |
| Editar compra | `PUT /api/card-purchases/{code}` aceita `purchaseDate`, `total`, `count` e `firstInvoiceShift` (opcionais). Se algum difere da compra: recusa 409 se a compra é pagamento de despesa no cartão, se alguma fatura com suas parcelas recebeu pagamento (mesmo estornado) ou se houve estorno da loja; senão apaga os itens, replaneja com `CardSchedule.Plan` (regra do cartão), grava nas faturas (criando as que faltam), recalcula as faturas do cartão e remove faturas que ficaram vazias. `Version+1`. |

Premissas em product/decisions.md: **A19** (revista) e **A20**.

## Modelo e contratos

Sem migration. `ExpenseSql.SelectObligation` ganhou `CardName`, `CardItemCount`, `PaidOnCard` e `LastPaymentDate`; `ObligationRecord` idem. `ExpenseDto` troca `cardPurchaseCode`/`purchaseDate` por `cardItemCount` e `paidOn` (`cardName` fica). `CardItemDueRecord`, `ListItemsDueAsync`, `CardSql.ItemsDue` e `ExpenseMapper.CardItems` (A19 de 01/11) foram removidos. Novos: `Obligation.Reactivate`/`EditCancelReason`, `Payment.EditReversalReason`, `CardPurchase.Reschedule`, `ICardRepository.UpdatePurchaseSchedule`/`RemovePurchaseItems`, `IExpenseRepository.UpdateReversalReason`, handlers `ReactivateExpense`, `EditCancelReason`, `EditReversalReason`; `UpdatePurchasePayload` com `PurchaseDate`, `Total`, `Count`, `FirstInvoiceShift`.

Frontend: `components/expenses/ExpenseList.tsx` (`InvoiceRow`, Desfazer cancelamento, lápis do motivo, "Paga em"), `components/expenses/Payments.tsx` (lápis da justificativa), `app/(panel)/cards/page.tsx` + `components/cards/CardsView.tsx` + `Invoices.tsx` (`?invoice=` seleciona o cartão e abre as compras da fatura), `components/cards/PurchaseForm.tsx` (valor, parcelas, data e primeira fatura no editar), `lib/expenses.ts`. Tutorial (3.3 e 3.5) atualizado e `content/tutorial.ts` regenerado.

## Validação e reprodução

- `tests/Helpers/CardAssertions.cs` (A19/A20, executado por `PersistenceTests` em banco descartável): no mês da fatura do Elo a lista traz a linha "Fatura Elo da casa – mmm/aa" com 120,00, 1 compra, em aberto, e não traz "Internet" (paga no cartão); no mês do Visa, a linha da fatura com 289,90 (300 + 39,90 − 50 de crédito; parcela estornada fora), 3 compras, paga em `today`, e nenhuma linha "Mercado do mês" ou "Televisão"; fatura do Master paga em parte com 100,00/200,00; após estornar o pagamento no cartão, "Internet" volta à lista em aberto; editar valor da "Assinatura de vídeo" (fatura paga) responde 409; "Tênis" 90,00 em 1x no Amex editado para 120,00 em 2x com data de ontem gera duas faturas de 60,00 e comprometido 120,00.
- `tests/Helpers/ExpenseAssertions.cs` (A20): justificativa do estorno corrigida pelo titular e recusada ao leitor; motivo do cancelamento corrigido; reativar com versão velha responde 409, pelo leitor 403, pelo titular reabre com 30,00 em aberto e sem motivo; reativar despesa não cancelada responde 409.
- Critérios de aceite do PDF na tela (login do piloto): Despesas → novembro/26 com 1 linha do Bradesco (R$ 8.742,18, vence 10/11), dezembro/26 com 1 linha do Bradesco (R$ 5.731,51), uma linha cada para Hiper, Itaú Tim, Nubank e Amazon Mastercard em novembro; lançar compra muda o total na hora; Ver compras abre a fatura certa; Pagar fatura em Cartões deixa a linha Paga; o total do mês não muda em relação à versão anterior (só a forma de mostrar).

## Execução

Validação em 02/11/2026 no servidor 192.168.1.7 (PowerShell Remoting, `git archive` do commit `3c7284b` expandido em `C:\apps\organizali-work\repo`, contêineres descartáveis): `dotnet test` em Release com MariaDB descartável (Testcontainers) resultou em 96 de 97 aprovados, só a falha conhecida de `SmtpRecoveryDeliveryTests.Delivery_SendsOneTimeLinkWithTokenInFragment` (não depende desta entrega); frontend `npm ci`, `tsc --noEmit`, `eslint` e `next build` com saída 0.

Publicação: zip expandido sobre `C:\apps\OrganizaliFinancas`, `ORGANIZALI_VERSION=3c7284b` em `deploy\.env.production`, tarefa agendada `OrganizaliDeploy` (`UP_EXIT=0`, `FIM` às 11:48); contêineres `organizali-api:3c7284b` e `organizali-frontend:3c7284b` no ar (frontend healthy). Smoke do notebook: `GET /api/health` 200 `{"status":"Healthy"}`, `GET /api/backend/expenses` sem sessão 401, `/login` 200. Os critérios de aceite na tela (valores do Bradesco, Hiper, Itaú Tim, Nubank, Amazon; Ver compras; Pagar fatura) ficam para a conferência da família com o login do piloto.

## Limitações e pendências

- A despesa paga no cartão some da lista enquanto está paga (regra 8 do pedido); seus Detalhes e comprovantes ficam acessíveis pelo estorno em Pagamentos recentes ou reabrindo a despesa. Alternativa (linha informativa sem somar) registrada em A19.
- Despesa paga em parte no cartão e em parte na conta some ao ficar totalmente paga (o valor pago na conta deixa de aparecer em "Pago" de Despesas).
- Editar compra já em fatura paga continua recusado; o caminho é o estorno da loja.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
