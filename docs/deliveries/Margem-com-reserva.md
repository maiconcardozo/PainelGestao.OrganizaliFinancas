# Entrega — Margem do mês com reserva mínima

Data: 02/11/2026 (noite). Estado: implementado e validado no servidor (commits `aea2914` e `d68d99e`); **publicação na LAN pendente** (ver Execução). Pedido da Carolina em PDF ("Pedido: previsão de margem com reserva mínima", 02/11/2026): a casa quer manter R$ 15.000 de reserva e saber, mês a mês, o que sai da conta, o que vai para o cartão e sai no mês seguinte e quanto ainda dá para gastar sem entrar na reserva.

## Objetivo e funcionalidades

1. **Reserva mínima** em **Casa → Configurações da casa** (inicial R$ 15.000,00; titular edita). Mudar a reserva muda todas as margens.
2. **Margem do mês** no Painel (completa) e no alto de Despesas (compacta): Saldo no início do mês + Entradas previstas − Sai da conta este mês (contas + faturas que vencem no mês) = Saldo previsto no fim do mês; − Reserva mínima = **Margem para gastar**. Verde; amarela com margem abaixo de R$ 1.000; vermelha quando fecha abaixo da reserva, com a falta. Faixa com a margem dos próximos seis meses; "Sobra de meses anteriores entra na margem" pelo saldo.
3. **Vai para o cartão (sai em m/aa)**: contas do mês marcadas para pagar no cartão, agrupadas por categoria (ou descrição), com a fatura em que saem.
4. **Pago normalmente com** (Conta ou um cartão) na conta avulsa, na recorrente e em Detalhes. Conta marcada para o cartão continua no mês dela, mas o dinheiro só sai no vencimento da fatura em que uma compra no vencimento cairia (margem, fluxo de caixa, projeção de 60 dias, déficit e agenda usam a mesma data). **Pagar** já abre com esse cartão.
5. **E se eu gastar…** (Painel): valor + À vista ou um cartão; a API recalcula os mesmos meses e a tela diz de qual mês a margem cai, para quanto e se algum mês fica vermelho.
6. **Hoje, em qual fatura cai?** (Painel): por cartão ativo, o dia do fechamento, a fatura em que uma compra de hoje cai, o vencimento e até que dia vale; depois, a seguinte.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Reserva | `HouseholdSettings.MinimumReserve` (decimal ≥ 0, padrão 15000). `PUT /api/household-settings` com `minimumReserve`; omitido mantém. |
| Sai da conta | `HouseholdPlanner.MonthsAsync`: por mês, saídas = contas (`Bills`) + faturas (`Invoices`) com data de caixa no mês; entradas = rendas fixas previstas; `Other` = saldo inicial de contas abertas no mês. Fluxo de caixa mensal usa a mesma série. |
| Conta no cartão | `Obligation.IdCreditCard` / `RecurringExpense.IdCreditCard` (ocorrências herdam). Data de caixa = `CardSchedule.LandingFor(max(vencimento, hoje))`.DueDate; a data de vencimento continua a da conta (agenda, alertas, lista). Cartão inativo não pode ser escolhido de novo (409), mas edição mantém o atual. |
| Margem | `ReserveMargin`: margem = fechamento − reserva; red se < 0 (shortfall), yellow se < 1.000, green. `GET /api/margin` devolve 7 meses (atual + 6), grupos "vai para o cartão", tabela de cartões e, com `?amount=&cardCode=`, a simulação (evento `simulation` em hoje ou no vencimento da fatura em que cai). |

Premissa em product/decisions.md: **A22**.

## Modelo e contratos

Migration `20261102120000_ReserveAndDefaultCard`: `HouseholdSettings.MinimumReserve` decimal(19,2) NOT NULL default 15000; `Obligation.IdCreditCard` e `RecurringExpense.IdCreditCard` int null com índice e FK para `CreditCard (IdTenant, Id)` Restrict. Contratos: `SettingsDto.minimumReserve`, `ExpenseDetailsPayload.cardCode`, `ExpenseDto`/`RecurrenceDto` `payCardCode`/`payCardName`, `MarginDto` (`PlanningContracts.cs`). Frontend: `components/planning/MarginCard.tsx` (`MarginCard`, `CardLandingTable`), `PayCardSelect` em `DetailInputs.tsx`, páginas Painel e Despesas, `HouseholdForms.tsx`.

## Validação e reprodução

- `tests/Helpers/MarginAssertions.cs` (chamado de `PlanningAssertions`): conta "Mensalidade A22" marcada para o cartão sai na fatura; margem = fechamento − reserva com identidade de saldo e consistência com o fluxo de caixa; simulação à vista baixa todas as margens e no cartão só a partir da fatura; reserva 10.000 soma 5.000 em todas; recorrência com cartão herdada pela ocorrência; cartão inativo recusado como novo padrão. Unitários: `CardScheduleTests.LandingFor_*` e `PlanningTests.ReserveMargin_*` (exemplos do pedido: nov 13.525,73 → 1.474,27 abaixo; dez 18.342,72 → 3.342,72).
- Na tela: Painel → "Margem de novembro/26" com as linhas do pedido; Despesas → cartão compacto no alto; Nova despesa → "Pago normalmente com"; simular R$ 1.000 à vista baixa a margem de todos os meses em 1.000; Configurações da casa → reserva 10.000 soma 5.000 a todas.

## Execução

Validação em 02/11/2026 (noite) no servidor 192.168.1.7 (PowerShell Remoting, `git archive` do commit `aea2914` expandido em `C:\apps\organizali-work\repo`, contêineres descartáveis). Primeira rodada: o código não compilava (regra de validação da reserva fora da classe e `$"` perdido na mensagem do cartão inativo, ambos efeito dos scripts de patch) e, corrigido isso, a edição de despesa com cartão inativo recusava o cartão já gravado (o `previous` não levava `IdCreditCard`) e dois exemplos de `LandingFor` estavam escritos com a regra do dia de fechamento invertida (com "next" o próprio dia do fechamento já cai na fatura seguinte; com "current" ainda fica na atual, que é o caso "até 03/11" do pedido). Correções no commit `d68d99e`. Rodada final: `dotnet test` em Release com MariaDB descartável (Testcontainers) resultou em 105 de 106 aprovados, só a falha conhecida de `SmtpRecoveryDeliveryTests.Delivery_SendsOneTimeLinkWithTokenInFragment` (não depende desta entrega); `PersistenceTests` passou com `MarginAssertions` inteiro. Frontend na imagem `organizali-frontend:b7f7271`: `npm ci`, `tsc --noEmit`, `eslint` e `next build` sem erro (36 rotas).

Publicação: **pendente**. A expansão do commit `d68d99e` sobre `C:\apps\OrganizaliFinancas`, `ORGANIZALI_VERSION=d68d99e` e a tarefa `OrganizaliDeploy` foram negadas ao agente nesta sessão pelo classificador de modo automático ("Production Deploy"); a LAN continua em `b7f7271`. A migration `ReserveAndDefaultCard` roda sozinha na subida da API.

## Limitações e pendências

- Publicação na LAN pendente: a LAN continua em `b7f7271` até a tarefa `OrganizaliDeploy` rodar com `ORGANIZALI_VERSION=d68d99e`.
- A ordem das colunas no snapshot do EF foi ajustada à mão; o migrador não confere o modelo, mas o `Designer` deve ser conferido no próximo `dotnet ef migrations add`.
- Faturas antigas erradas, estornos e transferências já ficam fora da margem pelas regras existentes; nenhum filtro novo foi criado.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
