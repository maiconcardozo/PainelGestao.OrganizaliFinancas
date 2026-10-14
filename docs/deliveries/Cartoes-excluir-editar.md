# Entrega — Excluir e editar em cartões e compras

Data: 14/10/2026. Estado: implementado e validado no escopo abaixo. Pedido do usuário: "tudo que a Carolina registrou do cartão de crédito pode excluir também; faça um botão que consegue excluir e editar, mas apenas em ativo e inativo".

## Objetivo e funcionalidades

Até aqui cartões e compras só podiam ser criados, editados (cartão) ou estornados (compra); nada saía da lista. A tela **Cartões** passou a ter:

1. **No cartão** (mosaico e lista, titular ou editor): **Editar** (janela já existente), **Ativar/Desativar** com um clique (ícone de energia) e **Excluir** (lixeira, com confirmação). Desativar mantém faturas e histórico e só impede novas compras; excluir apaga o cartão de vez e só é aceito sem compras lançadas e sem fatura com pagamento, ajuste, rateio ou comprovante.
2. **Na compra** (Compras recentes, titular ou editor): **Editar** descrição, tipo de despesa (tipos "Cartão"), campos extras, categoria e quem comprou; **Excluir** (lixeira, com confirmação), que tira as parcelas das faturas; **Estorno da loja** continua para compras ainda não estornadas.
3. **Limpeza pedida**: as 13 compras de teste lançadas pelo titular no cartão Bradesco (e as 7 faturas que elas criaram) foram excluídas pela própria API nova; os 5 cartões da família (Nubank, Itau Tim, Hiper, Nubank Maicon, Bradesco) ficaram.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Excluir cartão | `DELETE /api/cards/{code}`: recusado (409) se houver compra lançada ("exclua as compras antes ou apenas desative") ou fatura travada (pagamento, mesmo estornado; ajuste; rateio; comprovante ativo). Faturas vazias restantes somem junto com suas obrigações. Exclusão física, dentro da transação com trava da casa. |
| Ativar/desativar | Usa o `PUT /api/cards/{code}` existente com os dados atuais e `isActive` invertido; respeita a versão (409 se o cartão mudou em outra aba). Cartão inativo não recebe compras (regra já existente). |
| Editar compra | `PUT /api/card-purchases/{code}`: só descrição, tipo de despesa, campos, categoria e comprador. Valor, data e parcelas são fixos porque os itens já estão nas faturas; para mudá-los, excluir e lançar de novo. Exige `version`; tipo precisa ser "Cartão"; morador inativo só se já era o comprador. |
| Excluir compra | `DELETE /api/card-purchases/{code}`: recusado (409) se alguma fatura que contém item da compra já recebeu pagamento (mesmo estornado), para não alterar o caixa registrado; nesse caso usar o estorno da loja. Caso contrário apaga itens (parcelas e créditos) e a compra, recalcula o valor das faturas do cartão e remove as faturas que ficaram sem itens e sem pagamento, ajuste, rateio ou comprovante. |
| Permissão | Todas as operações exigem `finance.write` (titular ou editor). Leitor não vê os botões. |
| Isolamento | Todas as consultas e exclusões filtram `IdTenant`. |

Premissa registrada em product/decisions.md (A16): exclusão física de cartão e compra só quando nada financeiro depende deles; com pagamento registrado, o caminho é desativar/estornar.

## Modelo e contratos

Sem migration: nenhuma coluna nova. `PurchaseDto` passou a expor `expenseTypeCode`, `expenseTypeVersion` e `fields` para a edição. `ICardRepository` ganhou `GetCardUsageAsync`, `RemoveCard`, `PurchaseHasPaidInvoiceAsync`, `UpdatePurchaseDetails`, `RemovePurchase`, `RecalculateCardInvoices` e `RemoveEmptyInvoices`.

| Rota | Permissão | Uso |
|---|---|---|
| `DELETE /api/cards/{code}` | `finance.write` | Exclui cartão sem dependências |
| `PUT /api/card-purchases/{code}` | `finance.write` | Edita descrição, tipo, campos, categoria e comprador |
| `DELETE /api/card-purchases/{code}` | `finance.write` | Exclui compra e tira as parcelas das faturas |

Frontend: `components/common/ConfirmModal.tsx` (confirmação destrutiva reutilizável), `CardActions` em `components/cards/Cards.tsx`, `PurchaseEditModal` em `components/cards/PurchaseForm.tsx`, ações na linha da compra em `components/cards/Invoices.tsx`.

## Validação e reprodução

- `dotnet build` e testes de unidade: novo `UpdateDetails_ChangesDescriptionOnlyAndKeepsAmountDateAndInstallments` em `CardScheduleTests`.
- Frontend: `tsc`, `eslint` e `next build` aprovados.
- Servidor da LAN (versão `1eb0951`, 14/10/2026): fumaça pela API com a conta piloto aprovada: 13 compras de teste excluídas, nenhuma compra ou fatura restante, 5 cartões mantidos com comprometido zero; cartão temporário criado, exclusão recusada com compra lançada (409), compra editada (descrição, categoria e comprador; valor e parcelas iguais; versão avançou), edição com versão velha recusada (409), compra excluída com as 3 faturas removidas, cartão desativado pelo clique e depois excluído. Página Cartões renderizada. A primeira publicação (`9cedca6`) falhou ao excluir cartão porque o Dapper não materializava as contagens BIGINT no registro posicional `CardUsage`; corrigido em `1eb0951`.
- A trava de exclusão por fatura paga não foi exercitada no servidor (pagar deixaria uma fatura permanente na casa piloto); a regra está no SQL `PurchasePaidInvoices` e no `InvoiceLocked`.

## Limitações e pendências

- Comprovante ainda não existe em compra no cartão; quando existir, entra na mesma regra de trava.
- Excluir cartão com fatura paga continua impossível por desenho; o caminho é desativar.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
