# Plano de implementação

Plano antes do código. Marcos com dependências e aceite; desenvolvimento autorizado após revisão, com M1.1 concluído em 10/10/2026. P0 é o piloto completo, incluindo dashboard moderno, orçamento, alertas e rateio.

**Execução inicial detalhada:** [M1.1 a M3.4 — tarefas, referências e aceite](development-milestones.md). Esse documento desdobra os marcos abaixo sem mudar a arquitetura aprovada.

| Marco | Entrega | Depende | Requisitos | Gate de aceite |
|---|---|---|---|---|
| M0 | Escopo, decisões, arquitetura e documentação | — | Todos | Revisão do usuário; decisões fundamentais registradas |
| M1 | Prova técnica da stack e fundação | M0 | RNF04/12/13/18 | Foundation restaura; schema MariaDB cria/atualiza; Dapper transaciona e reverte; build das camadas |
| M2 | Identidade, casa, permissões e liberação manual | M1 | RF01–05, RF36–37 | Duas casas isoladas; recuperação/convite em transporte falso; matriz de acesso aprovada |
| M3 | Cadastros e parametrização | M2 | RF04–05, RF12–15; preparação de RF16 | Tipo de dívida e campos criados sem deploy; histórico preservado |
| M4 | Pessoas, rendas, contas e extrato | M2/M3 | RF06–11, RF21 | Previsto/realizado separados; transferência conserva consolidado |
| M5 | Dívidas, recorrência, parcelas, baixa e rateio | M3/M4 | RF16–22, RF41 | Centavos exatos; concorrência/idempotência; rateio não duplica despesa |
| M6 | Cartões e faturas | M5 | RF23–25 | Compra→fatura→pagamento sem duplicação; parcial/estorno reconciliados |
| M7 | Dashboard, fluxo, orçamento, agenda e alertas | M4–M6 | RF26–33 | Totais rastreáveis; estouro e déficit detectados; revisão mobile |
| M8 | Homologação do piloto e operação | M2–M7 | Todos P0, RNF01–22 | Restauração ensaiada, segurança/calculadoras aprovadas, aceite de família piloto |
| M9 | Complementos V1 | M8 | RF34–35, RF38–40, RF42, RF45–46 | Anexos/exportações privados; metas sem renda fictícia; simulação isolada |
| M10 | Comercialização e evolução | M8 + decisões comerciais | RF43–44 e V2 | Provedor/planos definidos; webhooks sandbox testados; importação com prévia |

## Sequência de construção por funcionalidade

Regra e exemplo de aceite → domínio → schema/query/repository → handler/validação → endpoint → tela → teste integrado → documentação. Cada fatia deve funcionar de ponta a ponta; não concluir módulo apenas com cadastro visual.

M7 pode receber desenho/protótipo documental desde M0, mas indicadores oficiais dependem de M4–M6. Não construir gráficos com dados fictícios e apresentá-los como integração concluída. O painel do piloto inclui ações, não apenas leitura.

## Tarefas técnicas do M1

1. Conferir autorização/acesso ao Foundation e demais licenças para o produto; preservar versões, sem atualização de NuGet.
2. Criar solution/projetos, packages centralizados, analyzers e testes de arquitetura.
3. Validar Entity, Code UUID v7, auditoria, query objects, Dapper Unit of Work e obtenção de IDs na mesma transação.
4. Testar provider Oracle + MariaDB com migration inicial e incremental; verificar workaround do lock descrito no RevendaPRO.
5. Preparar frontend nas versões fixadas, sessão/proxy e tokens de design após revisão visual.
6. Definir ambientes locais isolados, placeholders de configuração, Compose e comandos comprovados no README.

Solução, restore, build e testes de arquitetura concluídos no M1.1. Persistência e frontend pendentes; ver [evidências](../testing/m1-1-foundation.md).

## Entregáveis por marco

Código e migrations revisáveis, documentação atualizada, exemplos de uso, testes do risco do marco, evidências de validação e pendências explícitas. Commit/push autorizados no repositório particular; deploy não solicitado. Não criar chamados corporativos ou inventar identificadores para este projeto particular.

## Estimativa inicial de esforço

Faixa de planejamento para um desenvolvedor experiente, ainda sujeita à capacidade disponível: M1 3–5 dias úteis; M2 5–8; M3 3–5; M4 5–8; M5 7–11; M6 6–10; M7 6–10; M8 4–7. Total P0 após M0: **39–64 dias úteis de esforço**, com contingência proposta de 20%: aproximadamente **47–77**. Não é prazo contratado; depende de decisões, experiência, reaproveitamento efetivo e feedback. M9/M10 serão estimados separadamente.

## Riscos e respostas

| Risco | Resposta / momento |
|---|---|
| Documentação antiga da referência | Priorizar padrão consolidado, manifestos e código; ADR local |
| Provider e versão prerelease do Foundation | Prova M1, teste de transação e restauração; perguntar se houver incompatibilidade |
| Misturar caixa/competência/cartão | Exemplos de aceite e reconciliação M4–M7 |
| Falta de filtro de tenant em Dapper | Contratos seguros, teste com duas casas e revisão de JOINs em todo marco |
| Dashboard amplo tornar escopo indefinido | P0/P1/P2 explícitos e rastreabilidade por RF |
| Regra de rateio ambígua | Valor/percentual confirmado; arredondamento e base explícitos |
| Indisponibilidade de e-mail/storage | Adaptadores falsos em teste; escolha antes de piloto externo |
| Custos de hospedagem/licença não definidos | Registrar e validar antes de contratar; nenhuma contratação nesta fase |

## Pronto para piloto

Todas as capacidades P0 concluídas; cenários financeiros/segurança sem falhas críticas; migrações reproduzíveis; restauração realizada; contas de demonstração sem segredos reutilizados; onboarding de uma família revisado; política operacional/retenção e contatos definidos. Liberação continua manual.
