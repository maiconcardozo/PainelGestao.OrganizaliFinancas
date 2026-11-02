# Entrega — Faturas anteriores em Cartões e histórico da despesa paga no cartão

Data: 02/11/2026 (tarde). Estado: implementado; validação registrada abaixo. Pedido da Carolina em PDF ("Pedido: remover compras de cartão lançadas por erro", 02/11/2026): três compras com data de julho (Mercado Livre e Amazon Prime no Hiper, Canva no Itaú Tim, R$ 312,99 no total) caíram nas faturas de ago/26, que aparecem vencidas, e não havia como vê-las nem excluí-las pela tela: Cartões só mostrava faturas a partir de dois meses atrás, "Compras recentes" não chegava a julho e o "Ver compras" da linha de agosto em Despesas não abria nada. Além disso, a despesa paga no cartão (escola, Alfa) sumia da lista do mês, e a família perdia o histórico.

## Objetivo e funcionalidades

1. **Despesa paga no cartão continua no mês dela.** Em **Despesas**, a despesa paga com "Pagar com cartão" (A17) volta a aparecer no mês da competência com o selo **Paga · cartão Bradesco · fatura dez/26** (ícone de cartão). Ela **não soma de novo** em "Despesas do mês" nem em "Pago": a parte paga no cartão conta uma vez, na linha da fatura. Com o pagamento estornado, o selo some e a despesa volta a contar como aberta.
2. **Categoria nos itens da fatura.** "Ver compras" já mostrava a categoria de cada item; a compra que liquida uma despesa no cartão recebe a categoria da despesa. Em **Orçamento** e **Análise** o gasto já contava na categoria original (Escola, Mercado…) e a compra que o liquida ficava de fora, sem dupla contagem — confirmado, sem mudança.
3. **Faturas e compras anteriores em Cartões.** As listas **Faturas** e **Compras recentes** começam dois meses atrás; **Ver faturas anteriores** e **Ver compras anteriores** recuam 12 meses por vez, até 23 meses atrás, e **Só as recentes** volta ao padrão. A nota sob cada lista diz desde quando ela mostra.
4. **"Ver compras" funciona em qualquer fatura.** O link de Despesas (`/cards?invoice=<código>`) passa a recuar o período até o mês da fatura, mesmo vencida há meses, e abre a fatura com as compras na tela.
5. **Aviso ao lançar compra em fatura antiga.** Ao lançar compra cuja primeira parcela cai em fatura anterior ao mês da **Data de início de uso** da casa (ou, sem data, em fatura já vencida), aparece "Compra em fatura antiga": diz em qual fatura cai, por que é suspeito e oferece **Voltar e corrigir** ou **Lançar mesmo assim**.
6. **As 3 compras do piloto.** Não foram excluídas nesta entrega: a leitura e a escrita no banco de produção foram negadas ao agente nesta sessão. Com esta versão a família exclui pela tela: **Cartões → Compras recentes → Ver compras anteriores** (uma vez chega a julho) → lixeira em cada compra. Conferência esperada depois: "Vencidas em aberto" R$ 0,00; agosto/26 sem "Fatura Hiper – ago/26" e "Fatura Itau Tim – ago/26"; Hiper comprometido R$ 2.126,71; Itaú Tim R$ 193,32. Se preferir por SQL, o roteiro da limpeza de 01/11 serve (prévia, backup, exclusão de `CardItem` e `CardPurchase`, recálculo das faturas e remoção das vazias), com autorização expressa.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Despesa paga no cartão | `ObligationRecord.CardPaid` (soma das alocações vivas de pagamentos com `IdCardPurchase`), `PaidCardName` e `PaidInvoiceMonth` (mês de referência da fatura que carrega o item ativo). `ExpenseDto.cardPayment` = {cartão, fatura yyyy-MM, valor} quando `CardPaid > 0`. |
| Totais do mês | `ExpenseListing.ReadAsync`: Total = Σ(valor + ajustes − pago no cartão); Pago = Σ(pago − pago no cartão); em aberto e vencidas como antes. O painel usa a mesma listagem. `ExpenseListing.Shown` só esconde fatura vazia. |
| Período em Cartões | `GET /api/card-invoices?from&to` e `GET /api/card-purchases?from&to` já existiam (36 meses e 731 dias no máximo). A página lê `?from=yyyy-MM`; `from` mínimo = mês atual − 23; `to` das faturas = mês atual + 12; das compras = hoje. `?invoice=` consulta `GET /api/card-invoices/{code}` e recua `from` até o mês de referência. |
| Aviso de fatura antiga | Só no frontend, ao lançar: usa a prévia (`POST /api/card-purchases/preview`, chamada automaticamente se o usuário não a pediu) e compara `referenceMonth` da primeira parcela com o mês de `startDate` de `GET /api/household-settings`; sem `startDate`, compara `dueDate` com hoje. A API não bloqueia. |

Premissa em product/decisions.md: **A21**.

## Modelo e contratos

Sem migration. `ExpenseSql.SelectObligation` troca `PaidOnCard` por `CardPaid`, `PaidCardName`, `PaidInvoiceMonth`; `ObligationRecord` idem; `ExpenseDto` ganha `cardPayment` (`CardPaymentDto`). `ExpenseListing.Shown`/`OnCard`. Frontend: `app/(panel)/cards/page.tsx` (período, `ListRange`, `startDate`), `components/cards/CardsView.tsx`, `Invoices.tsx` (`RangeNote`), `PurchaseForm.tsx` (confirmação), `components/expenses/ExpenseList.tsx` (selo), `lib/expenses.ts`, `lib/cards.ts`, `lib/finance.ts` (`shortMonth`). Tutorial (3.2 e 3.5) atualizado e `content/tutorial.ts` regenerado.

## Validação e reprodução

- `tests/Helpers/CardAssertions.cs` (A21, `PersistenceTests` em banco descartável): total e pago do mês antes de pagar "Internet" no cartão; depois do pagamento a despesa continua na lista do mês com `cardPayment` = ("Elo da casa", mês da fatura, 120,00) e o total do mês cai exatamente 120,00 com o pago igual; depois do estorno a despesa volta aberta, sem `cardPayment`, e o total volta ao de antes.
- Na tela (login do piloto): Despesas → novembro/26 mostra Escola Mensalidade 26 (R$ 1.646,21), Integral Alfa (R$ 375,51), Material escolar (R$ 259,40) e Festival (R$ 38,88) como "Paga · cartão Bradesco · fatura dez/26" e o total do mês continua R$ 19.890,29; Despesas → agosto/26 → "Ver compras" da Fatura Hiper – ago/26 abre a fatura em Cartões; Cartões → Ver compras anteriores chega às compras de julho; lançar compra com data de julho avisa "Compra em fatura antiga".

## Execução

(preenchido após a validação no servidor)

## Limitações e pendências

- As 3 compras de julho continuam no banco até a família excluí-las pela tela (ou por SQL autorizado).
- O aviso de fatura antiga vale ao lançar; editar a data de uma compra (A20) não avisa.
- "Ver anteriores" recua até 23 meses, limite das consultas da API.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
