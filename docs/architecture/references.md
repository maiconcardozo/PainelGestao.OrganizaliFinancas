# Referências e alinhamento inicial

Consulta local em 10/10/2026. Não houve alteração nos projetos de referência. Não foram copiados segredos ou código para este projeto.

## Referências locais

| Repositório sob C:/Users/maicon.cardozo/source/repos | HEAD observado |
|---|---|
| RevendaPRO | ac90857193504020918f1ee031b62e08c3c17523 |
| Arquitetura.Global | d8a52f90e60b17469accae3939de0cf668a3919a |
| PainelGestao.CPComunica | f646aafb00704f31d863bedcb24a354ed6c38739 |

HEAD identifica a revisão do clone; a análise foi dos arquivos locais, sem afirmar que o working tree de cada referência corresponde exatamente ao commit.

RevendaPRO: README.md; Directory.Packages.props; frontend/package.json; frontend/AGENTS.md; src/RevendaPro.Api/RevendaPro.Api.csproj; docs/architecture/PADRAO-GLOBAL.md; ADR-0003-padrao-global.md; overview.md; UnitOfWork/RevendaProUnitOfWork.cs; imagem de banco no docker-compose.yml.

CP Comunica (comparação secundária): AGENTS.md; README.md; Directory.Build.props; frontend/package.json; Infrastructure.csproj. A divergência EF/Pomelo foi registrada; não adotada.

Arquitetura.Global: README.md, AGENTS.md, docs/agents.md; visão geral, estratégia de repositório, estrutura de pastas e uso com agentes. Consultados standards de architecture, tech-stack, nomenclatura, patterns, best-practices, api-controllers, api-responses, operability, quality-gates, redis-cache, readme, code-inspection, driver-spec e cli-refactoring. Algumas leituras em lote tiveram saída truncada; trechos centrais foram consultados em chamadas adicionais. Antes de implementar uma área, reler integralmente seu padrão específico, especialmente fluxos de refatoração/cache quando aplicáveis. Esta entrega não executa refatoração nem cache.

Projeto alvo: inventário da raiz e status Git; havia apenas .git e .vs, sem README, src, frontend ou tests. `.vs` foi preservada. Não há Work Item, chamado Redmine ou Wiki associado.

## Skills aplicadas

- clean-ddd-hexagonal: limites entre domínio, aplicação e adaptadores; leitura complementar de LAYERS.md.
- clean-code: convenções de qualidade para o desenvolvimento planejado.
- frontend-design: direção da experiência e identidade do dashboard.
- perguntar-wiki-por-tarefa: pergunta feita e recusa registrada; encerrada a aplicação nesta tarefa.

Skill de marca cp-comunica foi consultada durante identificação das referências, mas suas regras comerciais/WordPress não se aplicam ao produto particular. Skills de relatório jCobranca, branches Neocob, publicação, anúncios e automações não correspondem ao escopo atual. Skills de arquivos Office/PDF serão usadas se esses formatos forem solicitados; a entrega atual é documentação Markdown de repositório.

## Referência de apresentação do README

O usuário definiu o README completo do Authentication como padrão visual/documental em 10/10/2026. Conteúdo obtido também pela API autenticada do GitHub em `maiconcardozoglobal/Authentication`, blob `03a6d8f169e92490e3d1f51e22d0db5728afacd3`; o endereço antigo informado, `maiconcardozo/Authentication`, retornou 404. Aplicar título com ícone, badges, árvore comentada e navegação temática/por público. Skill local: `readme-projetos`. Esta referência de apresentação não muda a arquitetura RevendaPRO nem transfere métricas, licença ou funcionalidades do Authentication.

## Documentação oficial consultada na arquitetura

- [Next.js — Authentication](https://nextjs.org/docs/app/guides/authentication): cookies/sessão e autorização.
- [ASP.NET Core — Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0): policies no servidor.
- [Connector/NET — EF Core support](https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html): matriz do provider; validar MariaDB separadamente.

As páginas oficiais podem representar versões diferentes das fixadas no clone. Não substituem manifestos nem autorizam atualizar pacotes. Não foi feita auditoria integral de segurança/licenças dos projetos existentes.
