# Organizali Finanças — implementado e trabalho restante

Atualizado em 10/10/2026. Projeto particular. Referência: RevendaPRO e Arquitetura.Global; backend Minimal API, frontend Next.js, Foundation.Base, Dapper em runtime e EF para schema. Um commit por etapa; push acumulado somente ao final. Sem deploy externo.

## O que já funciona

- Provisionamento administrativo idempotente de casa/assinatura, ativação e suspensão manual com histórico.
- Login, renovação, revogação, cookies HttpOnly e proxy autenticado. Recuperação testada com adaptador falso; envio real ainda indisponível.
- Titular/editor/leitor, menu e rotas protegidas, autorização na API, convites manuais de uso único e proteção do último titular.
- Moradores sem login, inativação, fuso IANA configurável, BRL, português e data inicial.
- Categorias/subcategorias, tipos de renda, credores, formas de pagamento e revisões históricas.
- Tipos de despesa com campos de texto, decimal, data, booleano e lista, definições versionadas e prévia validada no servidor.
- Contas com saldo recomposto e extrato; transferências atômicas; fontes de renda fixas ou variáveis com vigência; previsões até o mês atual + 12; recebimentos com ou sem previsão; estornos compensatórios; idempotência; média mensal do ano e comparativo previsto × recebido (M4).
- Despesas avulsas, recorrentes (fixas ou estimadas, “esta” ou “esta e próximas”) e parceladas com soma exata; pagamentos parciais com juros, multa e desconto separados; cancelamento; estorno de pagamento; rateio por valor ou percentual com acerto sugerido; saldo das contas com as saídas (M5).
- Base técnica, migrations, Swagger e telas integradas; testes em MariaDB efêmero e navegador isolado.

**Ainda não há cartão, orçamento ou dashboard financeiro.** O saldo atual considera rendas, transferências e pagamentos; compras no cartão entram no M6. O comportamento “cartão” dos tipos de despesa ainda é recusado.

## Entregas e situação

| Marco | Situação | Evidência / próximo trabalho |
|---|---|---|
| M0 | Planejamento documentado e desenvolvimento autorizado | [Entrega](../deliveries/M0.md); decisões externas ainda abertas |
| M1.1–M1.4 | Fundação implementada | [Entregas](../deliveries/README.md); revisão visual integrada no M3.4 |
| M2.1 | Piloto administrativo implementado | [Entrega](../deliveries/M2.1.md) |
| M2.2 | Autenticação implementada; transporte de recuperação pendente | [Entrega](../deliveries/M2.2.md) |
| M2.3 | Permissões e integrantes implementados | [Entrega](../deliveries/M2.3.md) |
| M2.4 | Interface integrada; aceite visual no M3.4 | [Entrega](../deliveries/M2.4.md) |
| M3.1–M3.3 | Cadastros e parametrização implementados | [Entregas](../deliveries/README.md) |
| M3.4 | Revisão integrada | [Evidências](../testing/m3-4-integrated.md) |
| M4 | Contas e rendas implementadas | [Entrega](../deliveries/M4.md); agendador do horizonte fica para M8 |
| M5 | Despesas e liquidação implementadas | [Entrega](../deliveries/M5.md); premissas A01–A05 a revisar |
| M6 | A implementar; regra D13 aberta | Cartões, compras, faturas e pagamento sem duplicidade |
| M7 | A implementar | Dashboard, fluxo, orçamento, agenda, filtros e alertas |
| M8 | A implementar; D16/D17 abertas | Segurança operacional, recuperação, homologação e aceite da família piloto |
| M9 | A implementar | Complementos da V1 |
| M10 | A implementar; D14 aberta | Comercialização e importação |

## Próximo marco: M6 — cartões e faturas

Implementar sobre despesas e pagamentos do M5, sem reorganizar as camadas:

1. Cartão com apelido, titular, dia de fechamento, dia de vencimento e limite informado; nunca PAN/CVV.
2. Compra no cartão (à vista ou parcelada) gera itens nas faturas pelos ciclos; não movimenta conta bancária.
3. Fatura agregadora (aberta, fechada, paga em parte, paga) com prévia da fatura de destino; alterar fechamento afeta só ciclos futuros.
4. Pagamento da fatura reduz o caixa uma única vez; pagamento parcial mantém o restante em aberto; estorno de compra reduz a despesa.
5. Regra do dia de corte (D13): sem resposta, aplicar a premissa registrada e manter a prévia editável.

Aceite: compra de 300 + pagamento de 300 não vira despesa de 600; parcial de 100 em fatura de 300 deixa 200; limite disponível estimado.

## Trabalho restante por marco

| Marco | Funcionalidades e regras essenciais | Aceite |
|---|---|---|
| M6 | Cartão sem PAN/CVV; corte/vencimento; compras não reduzem banco; parcelas em faturas; pagamento reduz caixa uma vez; estornos e parcial reconciliados | Compra R$ 300 + pagamento R$ 300 não vira despesa R$ 600; regra de corte confirmada antes do código dependente |
| M7 | Saldo real/projetado rastreável, fluxo diário/mensal, previsto/realizado, orçamento por categoria, subcategorias sem dupla soma, alertas configuráveis/deduplicados, agenda e filtros | Mostrar origem do total, excesso de orçamento e primeira data de déficit; mesmo total em gráfico/lista; revisão mobile |
| M8 | Recuperação real após provedor escolhido, configuração de produção, observabilidade, backup/restauração, segurança, limites, concorrência, jornada completa e política de retenção | Restauração ensaiada e família piloto valida jornada; nenhuma liberação antecipada por cadastro funcionar |
| M9 | Comprovantes privados, exportações autorizadas, metas, conciliação manual justificada, renegociação vinculada, encerramento/exportação/exclusão, resumo mensal e simulação | Arquivos isolados por casa; reserva não cria renda; simulação não cria obrigação; exclusão segue política aprovada |
| M10 | Planos/preços/trial/cancelamento após decisão, cobrança automática sandbox, webhook idempotente e eventos fora de ordem; CSV/OFX com prévia | Sem cobrança real em testes; importar somente após confirmação da prévia |

## Rastreabilidade dos requisitos

| Requisitos | Situação |
|---|---|
| RF01, RF03, RF37 | Implementados no escopo de piloto manual |
| RF02 | Sessões implementadas; recuperação externa pendente |
| RF04–05 | Cadastro/configuração implementados; fuso da casa aplicado às datas financeiras do M4 |
| RF06–11 | Implementados no M4 (contas, extrato, transferência, fontes, previsões e recebimentos) |
| RF12–15 | Cadastros/parametrização implementados; integração com lançamento/orçamento depende dos módulos respectivos |
| RF16–20, RF22, RF41 | Implementados no M5 |
| RF21 | Estornos de recebimento, transferência (M4) e pagamento (M5) implementados |
| RF23–25 | M6 pendente |
| RF26–33 | M7 pendente |
| RF36 | Autoria e históricos implementados nos módulos atuais; auditoria integral e consulta operacional ainda evoluem com os módulos |
| RF34–35, RF38–40, RF42, RF45–46 | M9 pendente |
| RF43–44 | M10 pendente |

Os 22 [requisitos não funcionais](../requirements/non-functional.md) continuam como gates de implementação e homologação. Build, isolamento, transações e sessão já têm evidências; capacidade, disponibilidade, recuperação operacional, acessibilidade ampla e segurança externa não estão homologadas.

## Decisões pendentes e limites de operação

- **D13:** tratamento de compra no dia de corte do cartão — antes do M6.
- **D14:** planos, valores, trial e inadimplência — antes do M10.
- **D16:** hospedagem, domínio, e-mail, storage e metas de disponibilidade — antes de homologação externa.
- **D17:** retenção, exclusão e acesso excepcional de suporte — antes de receber usuários externos.
- Licença de distribuição ainda não definida. Não copiar licença MIT da referência.
- Não houve publicação em produção nem envio real de e-mail/WhatsApp. O piloto não está pronto para finanças reais.

## Regra permanente de entrega

Cada etapa deve atualizar um documento em `docs/deliveries/` com funcionalidades, regras de negócio, migrations/contratos afetados, evidências, limitações e continuidade. Atualizar este quadro e README; copiar a documentação destinada à revisão para `C:/Codex/Upload`; criar um commit por etapa. Documentos de testes preservam evidência histórica e devem indicar claramente quando uma pendência foi resolvida em marco posterior.
