# Organizali Finanças — implementado e trabalho restante

Atualizado em 11/10/2026. Projeto particular. Referência: RevendaPRO e Arquitetura.Global; backend Minimal API, frontend Next.js, Foundation.Base, Dapper em runtime e EF para schema. Um commit por etapa; push acumulado somente ao final. Sem deploy externo.

## O que já funciona

- Provisionamento administrativo idempotente de casa/assinatura, ativação e suspensão manual com histórico.
- Login, renovação, revogação, cookies HttpOnly e proxy autenticado. Recuperação testada com adaptador falso; envio real ainda indisponível.
- Titular/editor/leitor, menu e rotas protegidas, autorização na API, convites manuais de uso único e proteção do último titular.
- Moradores sem login, inativação, fuso IANA configurável, BRL, português e data inicial.
- Categorias/subcategorias, tipos de renda, credores, formas de pagamento e revisões históricas.
- Tipos de despesa com campos de texto, decimal, data, booleano e lista, definições versionadas e prévia validada no servidor.
- Contas com saldo recomposto e extrato; transferências atômicas; fontes de renda fixas ou variáveis com vigência; previsões até o mês atual + 12; recebimentos com ou sem previsão; estornos compensatórios; idempotência; média mensal do ano e comparativo previsto × recebido (M4).
- Despesas avulsas, recorrentes (fixas ou estimadas, “esta” ou “esta e próximas”) e parceladas com soma exata; pagamentos parciais com juros, multa e desconto separados; cancelamento; estorno de pagamento; rateio por valor ou percentual com acerto sugerido; saldo das contas com as saídas (M5).
- Cartões sem número, compras à vista ou parceladas nas faturas pelos ciclos, prévia com correção de um ciclo, pagamento de fatura que reduz o caixa uma vez, estorno da loja e limite disponível estimado (M6).
- Painel da casa com saldo real, previsto × realizado, saldo projetado de 60 dias com primeira data de déficit e causas; fluxo de caixa diário e mensal; orçamento por categoria com hierarquia; central de alertas deduplicada; agenda; análise filtrada com o mesmo total no gráfico e na lista (M7).
- Pilha Docker de produção com HTTPS (Caddy), banco em rede interna, migration em processo único, imagens versionadas por commit, backup cifrado e restauração ensaiada, rotina diária do horizonte com trava, recuperação de senha por SMTP configurável, cabeçalhos de segurança, limites por sessão e logs estruturados (M8).
- Interface igual ao RevendaPRO (D26): menu recolhível com grupos, tema claro/escuro, cadastros em mosaico ou lista com formulários e ações em modal, telas públicas no layout de login do RevendaPRO.
- Base técnica, migrations, Swagger e telas integradas; testes em MariaDB efêmero e navegador isolado.

**O piloto está tecnicamente pronto para um servidor com Docker (M4–M8).** Faltam as escolhas de hospedagem, domínio e e-mail (D16), a política de retenção (D17) e o aceite da família piloto. Complementos (M9) e comercialização (M10) não foram iniciados.

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
| M6 | Cartões e faturas implementados; D13 com premissa A06 | [Entrega](../deliveries/M6.md) |
| M7 | Painel, fluxo, orçamento, alertas e análise implementados | [Entrega](../deliveries/M7.md); premissas A09–A12 |
| M8 | Produção e publicação Docker implementadas; D16/D17 e aceite da família piloto pendentes | [Entrega](../deliveries/M8.md) · [Runbook](../operations/runbook.md) |
| UI | Visual e cadastros iguais ao RevendaPRO (D26) | [Entrega](../deliveries/UI-RevendaPRO.md) |
| M9 | Parcial: metas (RF38) e comprovantes (RF34) implementados | [Entrega](../deliveries/M9-M10-complementos.md); faltam RF35, RF39, RF40, RF42, RF45, RF46 |
| M10 | Parcial: importação CSV/OFX (RF44) implementada; D14 aberta | [Entrega](../deliveries/M9-M10-complementos.md); falta RF43 |

## Piloto na rede local (13/10/2026)

- Pilha publicada no servidor Windows da casa (Docker, `deploy/compose.lan.yml` + `deploy/Caddyfile.lan`, certificado interno) em https://192.168.1.7:3300, versão `cfe690c` (14/10/2026). Casa piloto provisionada e ativa; moradores cadastrados pelo titular.
- Cadastros iniciais carregados pela API com `deploy/seed-catalogs.py`: 69 categorias em 15 grupos, 15 tipos de renda, 29 credores, 9 formas de pagamento e 47 tipos de despesa (45 ativos) com campos e categoria padrão. Os quatro tipos criados pelo titular foram enriquecidos; dois duplicados ficaram inativos.
- [Tutorial de uso](../product/tutorial.md) escrito com os rótulos reais da interface.
- 14/10/2026: [complementos](../deliveries/M9-M10-complementos.md) implementados e publicados na LAN: Ajuda dentro do sistema, catálogo padrão pelo próprio sistema (provisionamento, operação `SeedCatalog` e botão na tela), Primeiros passos em Minha casa, troca de senha e link de nova senha gerado pelo titular, metas de reserva (RF38), comprovantes (RF34) e importação de extrato OFX/CSV com prévia (RF44).
- 14/10/2026 (tarde): titular cadastrou 5 cartões e 13 compras de teste. [Excluir e editar em cartões](../deliveries/Cartoes-excluir-editar.md): ativar/desativar e excluir cartão, editar e excluir compra (premissa A16); as compras de teste foram excluídas pela API nova e os cartões mantidos. A conta de teste da fumaça ficou inativa com saldo zero.
- 14/10/2026 (tarde): reclamação de "cai para o login" resolvida: [sessão renovada sozinha](../deliveries/Sessao-renovacao-automatica.md) pelo proxy do Next, acesso de 60 min, sessão de 30 dias e tolerância de 60 s a renovações paralelas.

## Próximos passos

1. **Usuário:** responder D16 (servidor, domínio, provedor SMTP), D17 (retenção/exclusão) e revisar as premissas A01–A16; validar a jornada com a família piloto (cartões cadastrados; contas e fontes de renda ainda não).
2. **Publicar fora da LAN:** preencher `deploy/.env.production` com domínio e certificado público, rodar `deploy/publish.sh` e subir a pilha conforme o [runbook](../operations/runbook.md).
3. **M9 — complementos da V1:** comprovantes privados, exportações, metas de reserva, conciliação manual, renegociação, encerramento/exportação/exclusão (depende de D17), resumo mensal e simulação.
4. **M10:** comercialização (depende de D14) e importação CSV/OFX.

## Trabalho restante por marco

| Marco | Funcionalidades e regras essenciais | Aceite |
|---|---|---|
| M9 | Comprovantes privados, exportações autorizadas, metas, conciliação manual justificada, renegociação vinculada, encerramento/exportação/exclusão, resumo mensal e simulação | Arquivos isolados por casa; reserva não cria renda; simulação não cria obrigação; exclusão segue política aprovada |
| M10 | Planos/preços/trial/cancelamento após decisão, cobrança automática sandbox, webhook idempotente e eventos fora de ordem; CSV/OFX com prévia | Sem cobrança real em testes; importar somente após confirmação da prévia |

## Rastreabilidade dos requisitos

| Requisitos | Situação |
|---|---|
| RF01, RF03, RF37 | Implementados no escopo de piloto manual |
| RF02 | Sessões e recuperação por SMTP configurável implementadas; provedor real depende de D16 |
| RF04–05 | Cadastro/configuração implementados; fuso da casa aplicado às datas financeiras do M4 |
| RF06–11 | Implementados no M4 (contas, extrato, transferência, fontes, previsões e recebimentos) |
| RF12–15 | Cadastros/parametrização implementados; integração com lançamento/orçamento depende dos módulos respectivos |
| RF16–20, RF22, RF41 | Implementados no M5 |
| RF21 | Estornos de recebimento, transferência (M4) e pagamento (M5) implementados |
| RF23–25 | Implementados no M6 |
| RF26–33 | Implementados no M7 (alertas sem envio externo) |
| RF36 | Autoria e históricos implementados nos módulos atuais; auditoria integral e consulta operacional ainda evoluem com os módulos |
| RF34, RF38 | Implementados (comprovantes e metas de reserva) |
| RF35, RF39–40, RF42, RF45–46 | M9 pendente |
| RF44 | Implementado (importação OFX/CSV com prévia) |
| RF43 | M10 pendente |

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
