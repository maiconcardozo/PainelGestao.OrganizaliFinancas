# Requisitos não funcionais

Metas de engenharia propostas, ainda não medidas nem compromissos contratuais. Validar capacidade e custo antes de assumir disponibilidade comercial.

| ID | Requisito | Critério verificável |
|---|---|---|
| RNF01 | Isolamento entre casas | 100% dos endpoints, queries, jobs, arquivos e exportações testados com duas casas; nenhum dado cruzado |
| RNF02 | Autorização por operação | Acesso direto à API sem permissão recebe 403; recurso de outra casa responde 404 sem revelar existência |
| RNF03 | Sessão segura | Cookie HttpOnly/Secure/SameSite, validação de origem/CSRF em mutações, expiração, rotação e revogação de refresh token |
| RNF04 | Integridade financeira | Valores em decimal; atomicidade entre liquidação, alocação e extrato; falha não deixa lançamento incompleto |
| RNF05 | Idempotência e concorrência | Repetição de pagamento/job não duplica movimento; duas baixas concorrentes não excedem saldo permitido |
| RNF06 | Desempenho | Meta API p95 até 500 ms em consultas simples e 1,5 s no dashboard, com 50 usuários simultâneos e 100 mil movimentos/casa em ambiente medido |
| RNF07 | Usabilidade e responsividade | Uso a partir de 360 px, navegação por teclado, campos com rótulo, foco visível, erros próximos ao campo e gráficos com alternativa textual |
| RNF08 | Acessibilidade | Meta WCAG 2.2 AA; verificar contraste, foco, zoom 200%, leitor de tela e não depender só de cor |
| RNF09 | Disponibilidade | Meta inicial proposta 99,5% mensal no piloto; monitorar disponibilidade e documentar manutenção, sem SLA comercial presumido |
| RNF10 | Recuperação | RPO proposto até 24 h e RTO até 4 h; backup diário criptografado e restauração ensaiada antes do piloto |
| RNF11 | Observabilidade | Logs estruturados, correlation id, métricas de erro/latência/jobs e health checks sem dados financeiros ou segredos |
| RNF12 | Manutenibilidade | Dependências de camada testadas; regra de negócio no domínio; versão central de pacotes; documentação acompanha alteração |
| RNF13 | Portabilidade | Containers não root; ambientes separados; banco e arquivos persistentes; segredos externos à imagem |
| RNF14 | Precisão temporal | Instantes de auditoria em UTC; competência/vencimento como data civil; conversão de fuso explícita |
| RNF15 | Proteção de dados | Minimização de dados, exportação controlada, política de retenção, revogação e exclusão definitiva definida antes do público externo |
| RNF16 | Segurança de arquivos | Tipos permitidos, tamanho limitado, validação de assinatura, quarentena/verificação e download autenticado; sem bucket público |
| RNF17 | Segurança de aplicação | SQL parametrizado, validação de entrada, limites por usuário/casa, rate limit, CORS restrito, proteção XSS e secrets fora do Git |
| RNF18 | Qualidade de entrega | Build, lint, testes críticos e análise de dependências aprovados; sem defeitos críticos de cálculo/isolamento conhecidos |
| RNF19 | Auditabilidade | Histórico financeiro não apagado por edição comum; alterações sensíveis rastreáveis e estornos vinculados |
| RNF20 | Escalabilidade controlada | Paginação e limites de exportação; job com claim persistente e lease; isolamento de consumo por casa |
| RNF21 | Consistência de leitura | Após confirmar pagamento, detalhe e saldo oficial refletem commit; caches derivados invalidam após commit |
| RNF22 | Independência operacional | Execução em ambiente próprio, sem credenciais, endpoints ou banco corporativo |

## Segurança e privacidade por desenho

`IdTenant` é derivado do contexto autenticado, não confiado a partir do corpo da requisição. Validar o pertencimento de todas as FKs, inclusive conta, pessoa e categoria. Índices/FKs compostos por tenant protegem relações quando aplicáveis. Operador da plataforma consulta metadados operacionais; eventual suporte financeiro exige fluxo explícito, temporário e auditado, ainda a definir.

Excluir logicamente um cadastro não equivale a atender uma solicitação de exclusão definitiva dos dados. A política deve tratar banco ativo, arquivos, logs e expiração de backups; restaurar backup deve reaplicar solicitações válidas de exclusão. A definição jurídica/comercial da política permanece pendente.

## Orçamento de carga para validação

Dataset sintético de ao menos duas casas para segurança e uma casa com 100 mil movimentos para desempenho. Relatórios extensos passam a fila e possuem limite de linhas/tamanho e expiração. Medir frontend em celular intermediário/rede móvel simulada; meta inicial de conteúdo útil em até 3 s no cenário documentado, sem afirmar resultado antes do teste.
