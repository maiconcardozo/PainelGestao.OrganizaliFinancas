# Entrega — Despesa paga com cartão de crédito

Data: 01/11/2026. Estado: implementado; validação registrada abaixo. Pedido da Carolina: "quando uma despesa for paga com cartão de crédito, mostrar a opção Cartão de crédito na forma de pagamento; o valor deve ser somado automaticamente à fatura do mês correspondente; a fatura deve refletir o total de todas as compras no cartão; usar a data de fechamento do cartão para decidir em qual fatura entra".

## Objetivo e funcionalidades

Até aqui uma despesa só podia ser paga a partir de uma **conta** (dinheiro saindo na hora). Quem pagava uma conta de luz ou de internet no cartão precisava escolher entre lançar a despesa (e vê-la em aberto) ou lançar uma compra no cartão (e perder a despesa). Agora:

1. **Pagar** (lista de despesas) e **Já foi paga** (Nova despesa → Conta avulsa) ganharam o campo **Pagar com**, com as contas e, abaixo, o grupo **Cartão de crédito** com os cartões ativos da casa.
2. Ao escolher um cartão, a tela mostra **em qual fatura o valor entra** (mês, fechamento e vencimento), calculado pelo servidor com a regra do cartão; nada sai de conta nenhuma.
3. Ao confirmar, a despesa fica **Paga**, e a fatura do cartão passa a somar esse valor junto com as outras compras. Em **Cartões → Ver compras**, o item aparece com a descrição da despesa; em **Compras recentes**, marcado como "despesa paga no cartão".
4. Em **Pagamentos recentes** e nos **Detalhes** da despesa o pagamento aparece como "no cartão X" em vez de "de conta Y". **Estornar** o pagamento tira o item da fatura e reabre a despesa.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Origem do pagamento | `POST /api/payments` e o pagamento imediato de `POST /api/expenses` aceitam `accountCode` **ou** `cardCode`; informar os dois, ou nenhum, é recusado (400: "Escolha a conta de saída ou o cartão de crédito"). |
| Qual fatura | Mesma regra das compras (`CardSchedule`): compra até a data de fechamento entra na fatura que fecha naquele mês; depois do fechamento, na seguinte. No dia do fechamento vale a regra configurada no cartão ("próxima" ou "atual"). A fatura é identificada pelo mês do vencimento. Exemplo com fechamento dia 3 e vencimento dia 10: pagamento em 02/11 → fatura de novembro (vence 10/11); pagamento em 04/11 → fatura de dezembro (vence 10/12). |
| O que é gravado | Um `Payment` sem conta e com `IdCardPurchase`; uma `CardPurchase` de 1 parcela com a descrição e a categoria da despesa (comprador = quem pagou); um `CardItem` na fatura; a obrigação da fatura é recalculada. Nenhum `AccountMovement`. Tudo na mesma transação com a trava da casa e a mesma idempotência dos pagamentos. |
| Despesa | Recebe a alocação normal: fica paga (ou paga em parte), some das vencidas e do "falta pagar". Juros, multa e desconto continuam funcionando no mesmo pagamento. |
| Fatura | Soma todas as compras e todos os pagamentos de despesas no cartão. O dinheiro sai da conta uma única vez, ao pagar a fatura. |
| Fatura com cartão | Pagar a obrigação de uma fatura com cartão é recusado (409): "A fatura de um cartão não pode ser paga com cartão de crédito". |
| Cartão inativo | Recusado, como nas compras. |
| Orçamento e análise | A despesa continua contando na sua categoria e competência (como sempre). O item da fatura que nasceu desse pagamento **não** conta de novo (`PlanningSql` ignora compras ligadas a pagamento). A fatura nunca contou como despesa; nada muda aí. |
| Estorno do pagamento | `POST /api/payments/{code}/reversals` (titular): o pagamento fica estornado; como não há movimento de conta, o item sai da fatura e a compra fica "estornada" (mesmo mecanismo do estorno da loja); a despesa volta a ficar em aberto. Recusado (409) se a fatura que recebeu o item já tem pagamento registrado — estorne antes o pagamento da fatura. |
| Compra vinculada | `DELETE /api/card-purchases/{code}` de uma compra ligada a pagamento é recusado (409): o caminho é estornar o pagamento na tela de despesas. A tela de cartões esconde Excluir e Estorno da loja nessas compras; editar descrição/categoria continua permitido. |
| Filtro por conta na análise (A12) | Despesa paga no cartão não tem conta de pagamento; fica fora do filtro por conta, como as compras no cartão. |
| Permissões e isolamento | `payments.settle` para pagar, `payments.reverse` para estornar, `finance.write` para excluir compra; todas as consultas e escritas filtram `IdTenant`; o cartão precisa ser da casa. |

Premissa registrada em product/decisions.md (**A17**).

## Modelo e contratos

- Migration `20261101150000_CardPayments`: `Payment.IdFinancialAccount` passa a aceitar nulo; nova coluna `Payment.IdCardPurchase` (nula) com índice `(IdTenant, IdCardPurchase)` e FK composta para `CardPurchase`. Nenhum dado existente muda.
- `PaymentPayload` e `ImmediatePaymentPayload`: `accountCode` opcional e `cardCode` opcional (um dos dois).
- `PaymentDto`: `accountCode`/`accountName` nulos no cartão; novos `cardCode`, `cardName` e `purchaseCode`. `PaymentAllocationDto`: `accountName` nulo no cartão e novo `cardName`.
- `PurchaseDto`: novo `paymentCode` quando a compra é o pagamento de uma despesa.
- `ICardRepository.FindPurchaseByIdAsync`; `IExpenseRepository.LinkPaymentToPurchase` (resolve as duas linhas por `Code` dentro da transação, sem dividir para obter id).
- Domínio: `Payment.CreateOnCard` e `Payment.IsOnCard`; `CardCharges.ChargeAsync`/`UndoAsync` (Application/Cards) concentram a entrada e a saída do item na fatura.

Frontend: `components/expenses/PaymentSource.tsx` (seletor "Pagar com" e dica da fatura via `POST /api/card-purchases/preview`), usado em `ExpenseList.PayModal` e `ExpenseForm`; a tela de despesas carrega `GET /api/cards`. Pagar fatura (tela de cartões) usa a mesma janela sem a opção de cartão.

## Validação e reprodução

- `tests/Helpers/CardAssertions.cs` ganhou o cenário A17 (executado por `PersistenceTests`, banco descartável): pagamento com conta **e** cartão → 400; sem nenhum → 400; leitor → 403; pagamento no cartão não altera o saldo da conta; despesa fica paga com `cardName` no detalhe; fatura do ciclo correto soma 120,00 com o item "Internet"; a compra lista `paymentCode`; excluir a compra → 409; pagar fatura com cartão → 409; estorno reabre a despesa (120,00 em aberto), zera a fatura e marca a compra como estornada; estorno de pagamento cuja fatura já foi paga → 409.
- Resultado da execução: ver seção "Execução" abaixo (preenchida ao rodar no servidor, onde ficam o SDK e o Docker).

## Execução

Este notebook (Carolina) não tem .NET, Node nem Docker; tudo rodou no servidor da LAN (192.168.1.7, PowerShell Remoting), em contêineres descartáveis a partir de uma cópia de trabalho em `C:\apps\organizali-work\repo`:

- `dotnet test` (SDK 10 em contêiner, Testcontainers com `host.docker.internal`): **96 aprovados, 1 falha** — `SmtpRecoveryDeliveryTests` (o servidor SMTP falso recebe só o `EHLO`; mesma falha conhecida desde M9, não relacionada). `PersistenceTests` aprovado em 44 s: migrations incrementais com a nova `CardPayments` (o snapshot escrito à mão confere com o modelo, senão o `Migrate` recusaria) e o cenário A17 inteiro de `CardAssertions`.
- `AuthenticationAssertions` tinha uma asserção de 10/10 (refresh token reutilizado → 401) contrária à tolerância de 60 s entregue em 14/10; estava derrubando `PersistenceTests` antes de chegar aos cartões. Alinhada à regra vigente (reutilização segundos depois → 200).
- Frontend (imagem `organizali-frontend` local com `NODE_ENV` vazio, `npm ci --include=dev`): `tsc --noEmit`, `eslint` (depois de trocar um `setState` síncrono no efeito da dica da fatura por uma resposta chaveada pelo cartão/data) e `next build` aprovados.
- Publicação na LAN (01/11/2026): zip do commit `d61d19d` expandido em `C:\apps\OrganizaliFinancas`, `ORGANIZALI_VERSION=d61d19d`, tarefa `OrganizaliDeploy` (build + up) com `BUILD_EXIT=0`/`UP_EXIT=0`; `migrate` aplicou `20261101150000_CardPayments` (saída 0), `Payment.IdFinancialAccount` nulo e `Payment.IdCardPurchase` conferidos no banco, `/api/health` = Healthy, página de login 200. Nenhum pagamento existente foi alterado (15 pagamentos, 0 no cartão). Fumaça funcional (pagar uma despesa com cartão e ver a fatura) fica para a Carolina, que tem o login do piloto.

## Rendas (dados do piloto, pedido do mesmo dia)

Carolina pediu também: salário do Maicon a partir de 05/11 (uma só entrada de R$ 16.000; some a duplicata de 06/11), aluguel da garagem a partir de 07/11, aluguel sem a previsão avulsa de 05/11, e outubro só com o que foi recebido (R$ 2.277,46 em Comissões; sem a previsão de R$ 4.560). Diagnóstico no banco: cada fonte ganhou vigências novas começando um dia depois (salário v1–v3 em 05/11, v4–v7 em 06/11; garagem 18/10 → 05/11 → 07/12; aluguel 05/11 → 07/11), e `Recurrence.Expected` mantém as ocorrências da vigência antiga até o início da nova — por isso "Atualizar previsões" não as remove. A correção certa é nos dados (as vigências foram erros de cadastro, não história): `C:\apps\organizali-rendas-fix-20261101.sql` ajusta `ValidFrom` das vigências (salário → 05/11; garagem e aluguel → 07/11; cp comunica → 10/11), remove de vez as duas previsões de outubro sem recebimento (inativas e encerradas, travadas contra regeneração) e fecha as duas recebidas no valor recebido; `C:\apps\organizali-aplicar-rendas-fix.ps1` aplica, roda `RollHorizon` (regenera todas as previsões) e imprime a conferência. Backup feito antes: `C:\apps\organizali-backup-rendas-20261101.sql`. **Aplicação pendente de autorização explícita** (escrita no banco de produção). A fonte "Comissões Plano Saúde" (quinzenal) ficou para depois por decisão da Carolina.

## Limitações e pendências

- Compra que nasceu de pagamento não aceita estorno da loja pela tela; se o cartão devolver o valor, estorne o pagamento (reabre a despesa) e registre o que for preciso.
- O rateio entre moradores do pagamento continua pela despesa; a compra no cartão não é rateada (A07).
- Não há como mover o item para outra fatura depois de gravado; estorne e pague de novo com a data certa.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
