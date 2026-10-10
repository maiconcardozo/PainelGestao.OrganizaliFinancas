# Organizali Finanças — implementado e trabalho restante

Atualizado em 10/10/2026. Projeto particular. Referência: RevendaPRO e Arquitetura.Global; backend Minimal API, frontend Next.js, Foundation.Base, Dapper em runtime e EF para schema. Um commit por etapa; push acumulado somente ao final. Sem deploy externo.

## O que já funciona

- Provisionamento administrativo idempotente de casa/assinatura, ativação e suspensão manual com histórico.
- Login, renovação, revogação, cookies HttpOnly e proxy autenticado. Recuperação testada com adaptador falso; envio real ainda indisponível.
- Titular/editor/leitor, menu e rotas protegidas, autorização na API, convites manuais de uso único e proteção do último titular.
- Moradores sem login, inativação, fuso IANA configurável, BRL, português e data inicial.
- Categorias/subcategorias, tipos de renda, credores, formas de pagamento e revisões históricas.
- Tipos de despesa com campos de texto, decimal, data, booleano e lista, definições versionadas e prévia validada no servidor.
- Base técnica, migrations, Swagger e telas integradas; testes em MariaDB efêmero e navegador isolado.

**Ainda não há saldo financeiro, renda recebida, dívida liquidada, cartão, orçamento ou dashboard financeiro.** Os seletores de comportamento preparam esses módulos; não executam seus cálculos.

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
| M4 | A implementar | Contas, extrato, transferências, fontes/previsões/recebimentos e estornos |
| M5 | A implementar | Obrigações, recorrências, parcelas, pagamentos, encargos, cancelamento e rateio |
| M6 | A implementar; regra D13 aberta | Cartões, compras, faturas e pagamento sem duplicidade |
| M7 | A implementar | Dashboard, fluxo, orçamento, agenda, filtros e alertas |
| M8 | A implementar; D16/D17 abertas | Segurança operacional, recuperação, homologação e aceite da família piloto |
| M9 | A implementar | Complementos da V1 |
| M10 | A implementar; D14 aberta | Comercialização e importação |

## Próximo marco: M4 — contas e rendas

Implementar ponta a ponta, seguindo os contextos Accounts e Income já previstos, sem reorganizar as camadas:

1. Conta financeira: nome, situação, data/valor inicial; extrato recomposto do saldo inicial e movimentos efetivos. Conta inativa preserva histórico. Não aceitar movimento anterior à abertura sem procedimento explícito de correção.
2. Transferência: débito e crédito vinculados na mesma transação, entre contas da mesma casa. Não altera renda/despesa nem saldo consolidado; tarifa é despesa separada.
3. Fontes de renda por morador: tipo, bruto opcional, líquido esperado e vigência versionada. Mudança futura não reescreve previsão histórica.
4. Previsões por competência: geração idempotente e vínculo com a versão da fonte. Definir periodicidades adicionais e horizonte antes de expor geração automática não aprovada.
5. Recebimentos: conta e data reais; renda extra sem previsão; recebimentos parciais e múltiplas alocações explícitas. Soma alocada não supera o recebido.
6. Estorno: titular, justificativa, movimento compensatório e vínculo com original; nunca apagar movimento confirmado.
7. Precisão: decimal(19,2), valores monetários como string invariável na API, nenhuma soma financeira oficial em ponto flutuante.
8. Idempotência por casa/operação/chave e hash do conteúdo; repetição divergente gera conflito. Movimento, alocação e auditoria confirmados juntos.
9. Interface RevendaPRO adaptada, permissões e isolamento testados; documentação de entrega com exemplos e regras.

Aceite: previsão de R$ 4.000, recebimentos de R$ 2.000 e R$ 1.800, realizado R$ 3.800 e residual R$ 200. Transferir R$ 500 entre contas conserva o total da casa. Repetir a mesma requisição não duplica entrada. Estornar mantém o original e recompõe o saldo.

## Trabalho restante por marco

| Marco | Funcionalidades e regras essenciais | Aceite |
|---|---|---|
| M5 | Obrigação distingue competência/vencimento/pagador/responsável; campos por versão; recorrência sem duplicar; parcelas com soma exata; pagamentos parciais atômicos; juros/multa/desconto separados; rateio por valor/percentual não gera nova despesa | R$ 100 em três parcelas soma R$ 100; dois responsáveis e um pagador sem duplicação; corrida de pagamentos não quita em excesso |
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
| RF04–05 | Cadastro/configuração implementados; aplicação em finanças entra no M4 |
| RF06–11 | M4 pendente |
| RF12–15 | Cadastros/parametrização implementados; integração com lançamento/orçamento depende dos módulos respectivos |
| RF16–20, RF22, RF41 | M5 pendente |
| RF21 | Estorno de recebimento no M4 e pagamento no M5; ambos pendentes |
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
