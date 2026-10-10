# API planejada

Contratos propostos, não endpoints existentes. Prefixo `/api`, rotas em inglês, identificadores públicos `Code`. Toda rota financeira exige sessão, casa ativa e permissão da operação. Corpo não concede tenant, autor ou permissão.

| Contexto | Rotas propostas | Operações |
|---|---|---|
| Sessão | POST /api/auth/login, /refresh, /logout, /recover, /reset | Sessão e recuperação |
| Identidade | GET /api/me | Usuário, vínculo, permissões e menu filtrado |
| Casa | GET/PUT /api/household | Configuração da única casa da assinatura |
| Integrantes | /api/users, /api/invitations, /api/roles | Usuários da casa, CRUD autorizado e convite; D20 |
| Pessoas | /api/people | Cadastro, consulta e inativação |
| Catálogos | /api/categories, /income-types, /expense-types, /creditors, /payment-methods | Cadastro e manutenção por casa |
| Campos | /api/expense-types/{code}/fields | Definição e versão |
| Renda | /api/income-sources, /api/income-forecasts, /api/receipts | Previsão e recebimento |
| Contas | /api/accounts, /api/accounts/{code}/movements | Cadastro e extrato |
| Transferência | POST /api/transfers | Débito/crédito em transação |
| Obrigações | /api/obligations, /api/installment-plans, /api/recurring-rules | Despesa e gerações |
| Liquidação | POST /api/payments, POST /api/payments/{code}/reversals | Pagamento e estorno |
| Recebimento | POST /api/receipts/{code}/reversals | Estorno de entrada |
| Rateio | /api/obligations/{code}/shares | Divisão por valor ou percentual |
| Cartões | /api/cards, /api/card-purchases, /api/statements | Cadastro, compras e faturas |
| Faturas | POST /api/statements/{code}/close, /payments | Fechar e pagar |
| Painel | GET /api/dashboard, /api/cashflow, /api/agenda | Indicadores e detalhamento |
| Orçamento | /api/budgets, /api/budgets/{code}/lines | Limites mensais |
| Alertas | GET /api/alerts; POST /api/alerts/{code}/read, /snooze | Central e ações |
| Auditoria | GET /api/audit-events | Histórico autorizado |
| Plataforma | /api/platform/subscriptions | Ativação/suspensão manual, sem financeiro doméstico |
| P1 | /api/goals, /api/attachments, /api/exports, /api/reconciliations | Complementos |

Listagens suportam paginação (proposta 50, máximo 200), ordenação permitida, busca e filtros. Datas `YYYY-MM-DD`, instantes ISO 8601 UTC, competência `YYYY-MM`, dinheiro string decimal. Intervalos de consulta incluem início e fim civil; implementação converte corretamente para timestamps quando necessário.

## Exemplo proposto: pagamento

`POST /api/payments`, header `Idempotency-Key: <uuid>`. UUIDs abaixo são placeholders descritivos.

```json
{
  "accountCode": "<uuid-da-conta>",
  "paymentDate": "2026-11-05",
  "amount": "100.00",
  "paymentMethodCode": "<uuid-do-meio>",
  "allocations": [{ "obligationCode": "<uuid-da-obrigacao>", "amount": "100.00" }],
  "expectedVersion": 3
}
```

Validar pertencimento, saldo aberto, soma alocada, versão e data. Resposta 201 com Code e efeito confirmado. Repetição idêntica devolve resultado original; chave reutilizada com outro payload devolve 409. Não salvar duas saídas após timeout e nova tentativa.

## Rotinas

GenerateForecasts e GenerateRecurringObligations materializam horizonte configurado; EvaluateBudgetAlerts e EvaluateCashflowAlerts recalculam ocorrências. Cada execução registra casa, período, tentativa e resultado. Reexecução por chave única não duplica. Agendamentos são configuração operacional; nenhum job envia e-mail/WhatsApp real em teste.
