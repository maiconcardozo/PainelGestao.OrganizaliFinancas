# Entrega — Fatura fora de Despesas, excluir despesa e limpeza dos lançamentos cancelados

Data: 01/11/2026. Estado: implementado; validação registrada abaixo. Pedido da família piloto (01/11/2026), em três pontos: "remover a Fatura Bradesco automática, que soma as parcelas que já lancei como compras no cartão e dobra o total"; "apagar as despesas que cancelei e estornei (motivos como 'erro', 'duplicado', 'cartao Bradesco estimado, nao usar')"; "permitir excluir despesas e compras no cartão em vez de só cancelar, e deixar eu lançar compras no cartão sem problema".

## Objetivo e funcionalidades

1. **A fatura do cartão saiu de Despesas.** A fatura continua existindo (é por ela que a fatura é paga em **Cartões → Pagar fatura**), mas não aparece mais na lista de **Despesas** nem soma em "Despesas do mês", "Pago", "Falta pagar" e "Vencidas em aberto". Com a entrega de 01/11 (despesa paga no cartão, A17), uma conta de R$ 120 paga no cartão aparecia duas vezes no mês: como a própria despesa (paga) e dentro da "Fatura Bradesco" — era esse o total dobrado. Fluxo de caixa, agenda e alertas continuam mostrando a fatura como saída de dinheiro ("Fatura vence em…"), porque ali a compra do cartão não aparece e o dinheiro sai de fato ao pagar a fatura.
2. **Excluir despesa.** Cada despesa **em aberto** ou **cancelada** ganhou o botão **Excluir** (lixeira, com confirmação) ao lado de **Cancelar despesa**. Excluir apaga de vez: a despesa some da lista, do total, do orçamento e do histórico. Despesa paga ou paga em parte não tem o botão: o titular estorna o pagamento antes e então exclui. **Cancelar** continua existindo para quem quer manter o registro com o motivo.
3. **Compras no cartão** já tinham **Excluir** e **Editar** desde 14/10 (A16) e continuam iguais. O "problema" para lançar compras era o efeito do item 1: cada compra fazia a fatura crescer e a fatura entrava como despesa, duplicando o total. Com a fatura fora de Despesas, lançar compras no cartão não altera a lista de despesas.
4. **Limpeza dos dados do piloto**: ver seção "Limpeza no servidor" abaixo.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Fatura em Despesas | `GET /api/expenses` não devolve obrigações de fatura (`IdCardInvoice` preenchido); os totais do período são calculados sem elas. `GET /api/expenses/{code}` da fatura continua respondendo (é usado ao pagar a fatura). Fluxo de caixa, agenda, alertas e a tela **Cartões** não mudaram. Orçamento e análise já ignoravam a fatura (M7). |
| Excluir despesa | `DELETE /api/expenses/{code}` (204): despesa aberta ou cancelada, sem pagamento ativo e sem comprovante anexado. Apaga fisicamente rateio, ajustes (juros, multa, desconto), alocações de pagamentos **estornados** e a própria obrigação. O pagamento estornado continua em **Pagamentos recentes** e no extrato da conta (movimento original e estorno), porque o dinheiro saiu e voltou de verdade. |
| Pagamento ativo | 409 "Esta despesa tem pagamento ativo. Estorne o pagamento antes de excluir." A tela nem mostra o botão para despesa paga ou paga em parte. |
| Comprovante | 409 "Esta despesa tem comprovante anexado. Remova o comprovante antes de excluir." (evita arquivo órfão no volume de anexos; mesma trava do cartão, A16). |
| Fatura | 409 "A fatura não é excluída por aqui: exclua ou estorne as compras do cartão." A fatura some sozinha quando fica sem compras (regra de 14/10). |
| Ocorrência de recorrência | Não é apagada: fica **oculta** (`IsActive=0`) e **travada** (`IsDetached=1`). Some de todas as listas e totais, e a geração de recorrências (diária e **Atualizar vencimentos**) não a recria nem a reativa, porque ocorrência travada nunca é reescrita (regra de M5). A recorrência e as outras ocorrências continuam. |
| Parcela de compra parcelada | Apaga só a parcela; as outras continuam. Quando a última parcela sai, o plano da compra sai junto. |
| Permissão | `finance.write` (titular ou editor). Leitor não vê o botão e recebe 403. Casa diferente recebe 404. Trava da casa e transação única, como nas demais escritas. |

Premissa registrada em product/decisions.md (**A18**).

## Modelo e contratos

Sem migration: nenhuma coluna nova. `IExpenseRepository` ganhou `CountAttachmentsAsync`, `Remove` e `HideOccurrence`; `Obligation` ganhou `EnsureRemovable` e `IsRecurringOccurrence`. `ListExpensesHandler` filtra `CardInvoiceCode is null` antes de montar a lista e os totais.

| Rota | Permissão | Uso |
|---|---|---|
| `DELETE /api/expenses/{code}` | `finance.write` | Exclui despesa aberta ou cancelada sem pagamento ativo |
| `GET /api/expenses` | `household.read` | Passa a não listar faturas de cartão |

Frontend: botão **Excluir** e confirmação (`ConfirmModal`) em `components/expenses/ExpenseList.tsx`; o rótulo "Fatura do cartão" saiu da linha porque a fatura não é mais listada. Tutorial (3.3 e 3.5) atualizado e `content/tutorial.ts` regenerado.

## Limpeza no servidor (dados do piloto)

Pedido: "excluir todos os lançamentos cancelados ou estornados". Executado por SQL direto no banco do piloto, com backup antes, seguindo as mesmas regras da exclusão pela tela:

- Despesas **canceladas** (`Obligation.Status='cancelled'`): avulsas e parcelas apagadas (rateio, ajustes e alocações de pagamentos estornados junto); ocorrências de recorrência ocultas e travadas, para não voltarem.
- Pagamentos **estornados** (`Payment.DtReversed` preenchido): apagados com suas alocações e com os dois movimentos de conta (saída original e estorno, que se anulam — o saldo não muda); quando o pagamento era no cartão, a compra estornada e seus itens inativos saem da fatura.
- Compras no cartão **estornadas pela loja** (`RefundedAmount>0`, sem pagamento vinculado): apagadas com seus itens (parcelas canceladas e créditos) quando nenhuma fatura envolvida recebeu pagamento; as faturas são recalculadas e as vazias somem.

Resultado da execução: ver "Execução".

## Validação e reprodução

- `tests/Helpers/ExpenseAssertions.cs` (cenário A18, executado por `PersistenceTests` em banco descartável): excluir despesa com pagamento ativo → 409; leitor → 403; outra casa → 404; despesa cancelada excluída → 204 e `GET` → 404; despesa rateada com juros e pagamento estornado excluída → 204, saldo da conta inalterado, nenhuma linha restante na tabela; ocorrência de recorrência excluída some da lista e `generate` não a recria; as três parcelas de uma compra excluídas uma a uma levam o plano junto.
- `tests/Helpers/CardAssertions.cs` (A17 estendido): a fatura que recebeu a despesa paga no cartão não aparece em `GET /api/expenses` do mês do vencimento; `DELETE` da fatura → 409.

## Execução

Tudo rodou no servidor da LAN (192.168.1.7, PowerShell Remoting), em contêineres descartáveis a partir do zip do commit `5be2004` expandido em `C:\apps\organizali-work\repo`:

- `dotnet test` (SDK 10 em contêiner, Testcontainers com `host.docker.internal`): **96 aprovados, 1 falha** — `SmtpRecoveryDeliveryTests` (quebra de linha quoted-printable no link; mesma falha conhecida desde M9, não relacionada). `PersistenceTests` aprovado com os cenários A18 de `ExpenseAssertions` e `CardAssertions`.
- Frontend (imagem `organizali-frontend:d61d19d` local, `npm ci --include=dev`): `tsc --noEmit`, `eslint` e `next build` aprovados.
- **Limpeza do piloto** (antes da publicação): backup `C:\apps\organizali-backup-cancelados-20261101.sql` (477 KB); prévia com `C:\apps\preview-cancelados-20261101.sql`: 1 casa, 29 despesas canceladas (14 ocorrências de recorrência, 15 avulsas/parcelas — entre elas as cinco "cartao Bradesco" estimadas e os "Cartão Master Carol" duplicados), 2 pagamentos estornados em conta ("Padaria Teste Fumaca" e "Dental Bradesco", 4 movimentos), nenhuma compra estornada pela loja, nenhum comprovante envolvido. Aplicada com `C:\apps\cleanup-cancelados-20261101.sql` em uma transação: 0 canceladas visíveis, 0 pagamentos estornados, despesas ativas 255 → 226, pagamentos 16 → 14, faturas 11 e compras 19 inalteradas, saldos das 11 contas idênticos antes e depois.
- **Publicação na LAN** (01/11/2026, 17:22): zip expandido em `C:\apps\OrganizaliFinancas`, `ORGANIZALI_VERSION=5be2004`, tarefa `OrganizaliDeploy` com `BUILD_EXIT=0` e `UP_EXIT=0`; `migrate` sem migrations novas; `api`, `frontend` e `migrate` na imagem `5be2004`; `/api/health` = Healthy; página de login 200; `DELETE /api/backend/expenses/{code}` responde pelo proxy (403 sem sessão, como as demais escritas). Fumaça funcional na tela (abrir Despesas sem a fatura e excluir uma despesa de teste) fica para a Carolina, que tem o login do piloto.

## Limitações e pendências

- Excluir não registra quem excluiu nem o que foi excluído (não há tabela de auditoria de exclusões); quem precisar de rastro usa **Cancelar despesa**.
- Pagamento estornado não pode ser excluído pela tela; a limpeza dos estornos do piloto foi feita por SQL. Se a família quiser isso no sistema, entra como pedido novo.
- A fatura continua no fluxo de caixa e nos alertas como saída de dinheiro. Se a família preferir vê-la também em Despesas como linha informativa (sem somar), é ajuste pequeno.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
