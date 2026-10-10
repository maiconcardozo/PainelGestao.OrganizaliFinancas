# Testes e aceite planejados

Nenhum teste de aplicação executado nesta entrega documental. Seguir xUnit/Moq/FluentAssertions 7.2.2/NetArchTest do RevendaPRO; integração com MariaDB descartável, nunca bancos da empresa ou usuários reais. Testes não enviam e-mail, WhatsApp ou cobranças; usar adaptadores falsos e fixtures isoladas. Testes transacionais precisam escrever apenas no banco efêmero de testes, a provisionar explicitamente na etapa de desenvolvimento.

| ID | Cenário | Resultado esperado | RF/RNF |
|---|---|---|---|
| T01 | Usuário A consulta/edita Code da casa B | Nenhum dado de B; 404; nenhuma mutação | RF01, RNF01 |
| T02 | Leitor chama endpoint de pagamento diretamente | 403 e nenhum efeito | RF03, RNF02 |
| T03 | Previsão 4.000, recebimentos 2.000 + 1.800 | Real 3.800, residual 200, diferença -200 | RF06–08 |
| T04 | Alterar salário futuro | Histórico e recebimentos anteriores preservados | RF07 |
| T05 | 100 divididos em 3 parcelas | 33,33 + 33,33 + 33,34 | RF18, RNF04 |
| T06 | Conta 250 + 10 − 5 paga em 100 | Aberto 155, caixa -100 | RF19–20 |
| T07 | Estornar pagamento do T06 | Aberto 255; compensação +100; original preservado | RF21 |
| T08 | Transferir 500 entre contas | Duas pontas; consolidado inalterado | RF11 |
| T09 | Duas baixas concorrentes sobre mesma dívida | Limite respeitado; conflito/rejeição controlada | RNF05 |
| T10 | Repetir requisição após timeout | Uma única baixa; mesmo resultado pela chave | RNF05 |
| T11 | Reusar chave com outro valor | 409; nenhuma nova baixa | RNF05 |
| T12 | Falhar entre Payment e Movement | Rollback de todos os registros | RNF04 |
| T13 | Job repetido e executado em duas instâncias | Uma ocorrência por regra/data/casa | RF17 |
| T14 | Recorrência dia 31 atravessa fevereiro | Ajuste ao último dia e retorno ao dia-base | RF17–18 |
| T15 | Compra cartão 300 e pagamento 300 | Despesa 300; saída de caixa 300, não 600 | RF24–27 |
| T16 | Fatura 300 paga em 100 e saldo rolado | Dívida residual 200 contada uma vez | RF25 |
| T17 | Estorno de compra em fatura fechada | Crédito vinculado, sem alterar silenciosamente fatura | RF21/25 |
| T18 | Orçamento 1.000, comprometido 900/1.100 | Aproximação/excesso 100; notificações sem duplicação | RF28–31 |
| T19 | Orçamento pai e filhos | Consolidado sem dupla soma | RF28 |
| T20 | Saldo 500, receber 300, pagar 900 | Projeção -100; saldo real continua 500 | RF26–30 |
| T21 | Tipo Água com campo leitura obrigatório | Validação servidor; sem alterar tipos existentes | RF13–15 |
| T22 | Inativar categoria/pessoa com histórico | Novo uso bloqueado, relatórios históricos preservados | RF04/12 |
| T23 | Rateio 100 por 50%/50%, A paga tudo | Despesa 100, cotas 50/50, acerto B→A 50 | RF41 |
| T24 | Rateio 100 em três cotas percentuais | Centavos distribuídos deterministicamente; soma 100 | RF41 |
| T25 | Revogar integrante e renovar token | Acesso negado inclusive download e exportação | RF03/RNF03 |
| T26 | CSRF, payload de outra casa e SQL malicioso | Nenhuma ação/dado indevido | RNF01/17 |
| T27 | Uso em 360 px/teclado/zoom | Registro completo, foco e mensagens acessíveis | RNF07/08 |
| T28 | Banco com 100 mil movimentos, 50 usuários | Medir metas p95 e documentar ambiente/resultados | RNF06 |
| T29 | Backup restaurado em ambiente isolado | Saldos, anexos e vínculos reconciliados no RPO/RTO | RNF10 |
| T30 | Migration banco vazio e versão anterior | Schema/histórico consistentes, sem perda | RNF12/13 |
| T31 | Recebimento esperado encerrado a menor | Residual não entra novamente na projeção | RF08 |
| T32 | Falha de transporte de alerta | Financeiro preservado, retentativa sem duplicidade | RF31 |

Unidade: invariantes monetárias, datas, parcelas, rateios e estados. Integração: SQL, isolamento, transações, concorrência e migrations. API: permissões, envelopes, idempotência. UI: jornadas e acessibilidade com ferramenta de automação a fixar no M1, seguindo a stack existente. Arquitetura: dependências e localização de contratos.

Matriz mínima por endpoint: não autenticado, leitor, colaborador com permissão, colaborador sem permissão, administrador, integrante revogado, casa diferente e operador da plataforma. Operador não herda acesso financeiro.

Sem meta arbitrária de cobertura global: cenários críticos T01–T26 e T29–T32 são gates funcionais/operacionais conforme recurso implementado. T27/T28 exigem evidência no ambiente alvo. Falha de cálculo ou isolamento bloqueia piloto.
