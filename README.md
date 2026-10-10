# 🏡 Organizali Finanças — Gestão financeira da casa

<!-- Estado do projeto -->
[![Estado](https://img.shields.io/badge/Estado-M1%20base%20tecnica-blue.svg)](docs/testing/m1-1-foundation.md)
[![Arquitetura](https://img.shields.io/badge/Arquitetura-Clean%20Architecture%20%2B%20DDD-6A3FA0.svg)](docs/architecture/overview.md)
[![Referência](https://img.shields.io/badge/Padr%C3%A3o-RevendaPRO-34495E.svg)](docs/architecture/decisions/ADR-0001-revenda-pro.md)

<!-- Stack definida para implementação -->
[![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg?logo=dotnet)](docs/architecture/overview.md)
[![C#](https://img.shields.io/badge/C%23-implementado-239120.svg)](docs/architecture/overview.md)
[![EF Core](https://img.shields.io/badge/EF%20Core-10%20%7C%20schema-blue.svg?logo=nuget)](docs/architecture/overview.md)
[![JWT](https://img.shields.io/badge/JWT-implementado-green.svg?logo=jsonwebtokens)](docs/testing/m2-2-authentication.md)
[![Next.js](https://img.shields.io/badge/Next.js-16.3.3-000000.svg?logo=nextdotjs)](docs/architecture/overview.md)
[![React](https://img.shields.io/badge/React-19.2.8-149ECA.svg?logo=react)](docs/architecture/overview.md)
[![MariaDB](https://img.shields.io/badge/MariaDB-11.8-003545.svg?logo=mariadb)](docs/architecture/overview.md)
[![Dapper](https://img.shields.io/badge/Dapper-SQL%20parametrizado-0078D4.svg)](docs/architecture/overview.md)

<!-- Documentação disponível -->
[![Requisitos funcionais](https://img.shields.io/badge/Requisitos%20funcionais-46-informational.svg)](docs/requirements/functional.md)
[![Requisitos não funcionais](https://img.shields.io/badge/Requisitos%20n%C3%A3o%20funcionais-22-informational.svg)](docs/requirements/non-functional.md)
[![Cenários de aceite](https://img.shields.io/badge/Cen%C3%A1rios%20de%20aceite-32%20definidos-informational.svg)](docs/testing/test-patterns.md)

## 📋 Sobre

O **Organizali Finanças** é um projeto de SaaS web para quem coordena as finanças de uma casa. Reúne moradores, rendas, contas, dívidas, cartões e orçamento para acompanhar **quanto deveria entrar, quanto realmente entrou, o que precisa ser pago e qual será o saldo nos próximos dias**.

A arquitetura segue o **RevendaPRO**, com **Clean Architecture**, **DDD**, **CQRS com MediatR** e **Foundation.Base**. Cada assinatura atende uma casa, com dados compartilhados entre os integrantes e permissões para controlar as alterações.

> **Estado atual:** M1.1–M1.4 implementados: cinco camadas, persistência base, Minimal API e Swagger. 29 testes aprovados, incluindo MariaDB descartável autorizado. Revisão visual do Swagger pendente por indisponibilidade do navegador. Base Next.js disponível; login e funcionalidades financeiras abaixo seguem planejados. Piloto com liberação manual.

### ✨ Funcionalidades principais

- 📊 **Dashboard da casa** — saldo atual, previsto e realizado, vencimentos e ações pendentes.
- 💸 **Fluxo de caixa** — entradas, saídas e projeção diária e mensal, com alerta de saldo negativo.
- 👥 **Moradores e rendas** — bruto opcional, líquido esperado e recebimentos reais em BRL.
- 🏦 **Contas financeiras** — saldos iniciais, extrato, carteira e transferências entre contas.
- 🧾 **Dívidas parametrizadas** — água, luz, aluguel, compras e outros tipos criados pela casa.
- 🔁 **Recorrências e parcelas** — contas fixas ou variáveis, compromissos futuros e prévia de vencimentos.
- 💳 **Cartões e faturas** — compras, parcelas, fechamento e pagamentos sem duplicar despesas.
- 🤝 **Rateio entre moradores** — divisão por valor ou percentual, separando responsabilidade e pagamento.
- 🎯 **Orçamento por categoria** — limites, consumo, saldo restante e excesso em reais.
- 🔔 **Alertas financeiros** — estouro de orçamento, déficit futuro, contas vencidas e renda atrasada.
- 🛡️ **Permissões e auditoria** — controle de edição, histórico e isolamento entre casas.
- 📁 **Evolução da V1** — comprovantes, exportações, metas de reserva e simulações.

## 🛠️ Tecnologias

**Stack definida a partir dos arquivos do RevendaPRO.** As versões completas e a validação técnica prevista estão no [guia de arquitetura](docs/architecture/overview.md).

- **C# / .NET 10 / ASP.NET Core** — backend em Minimal API, separado do frontend Next.js.
- **MediatR / FluentValidation** — commands, queries, handlers e validação de entrada.
- **Foundation.Base** — entidades, UUID v7, auditoria e base de persistência; uso autorizado.
- **Dapper / MariaDB 11.8** — leitura e escrita com SQL parametrizado.
- **EF Core 10 / MySql.EntityFrameworkCore** — migrations e mapeamento de schema.
- **Next.js 16 / React 19 / TypeScript 5** — aplicação web e sessão no servidor.
- **Tailwind CSS 4 / Lucide React** — interface responsiva e ícones.
- **JWT / cookies HttpOnly** — autenticação e proxy entre frontend e API.
- **xUnit / Moq / FluentAssertions / NetArchTest** — testes e validação das dependências.
- **Testcontainers.MariaDb** — integração com banco isolado.
- **Docker / Docker Compose** — execução e empacotamento planejados.
- **S3 / ClosedXML / QuestPDF** — anexos e exportação na evolução da V1.

## 🏗️ Estrutura do projeto — DDD e Clean Architecture

A estrutura de implementação seguirá as camadas e convenções do **RevendaPRO**, conforme a [decisão arquitetural](docs/architecture/decisions/ADR-0001-revenda-pro.md).

**Árvore alvo:** as cinco camadas em `src/`, o projeto em `tests/` e `docs/` já existem. O frontend também existe; subpastas funcionais serão acrescentadas nos respectivos marcos. A árvore abaixo mostra a organização completa prevista. [Estrutura efetivamente criada no M1.1](docs/testing/m1-1-foundation.md).

```text
PainelGestao.OrganizaliFinancas/
├── src/
│   ├── PainelGestao.OrganizaliFinancas.Domain/
│   │   ├── Entities/               # Pessoas, rendas, obrigações, faturas e orçamento
│   │   ├── ValueObjects/           # Valores e invariantes do domínio financeiro
│   │   ├── Enums/                 # Estados e comportamentos suportados
│   │   └── Interfaces/
│   │       ├── Repositories/      # Contratos de persistência
│   │       ├── Services/          # Contratos usados pelos casos de uso
│   │       └── IUnitOfWork.cs     # Coordenação transacional
│   │
│   ├── PainelGestao.OrganizaliFinancas.Application/
│   │   ├── Behaviors/             # Validação, logs e transações
│   │   ├── Configuration/         # Registro de dependências
│   │   └── {Context}/             # Income, Expenses, Cards, Budgets, Alerts...
│   │       ├── Commands/          # Operações de escrita
│   │       ├── Queries/           # Consultas
│   │       ├── Handlers/          # Orquestração dos casos de uso
│   │       ├── Validators/        # Validação de entrada
│   │       └── DTOs/              # Contratos de saída
│   │
│   ├── PainelGestao.OrganizaliFinancas.Infrastructure/
│   │   ├── Data/MySql/            # Conexões
│   │   ├── Database/              # Contexto de schema e migrations
│   │   ├── Persistence/Mappings/  # Mapeamento das entidades
│   │   ├── Queries/               # SQL versionado por contexto
│   │   ├── Repositories/          # Implementações Dapper
│   │   ├── UnitOfWork/            # Transações
│   │   ├── Security/              # Implementações de segurança
│   │   ├── Services/              # Adaptadores externos
│   │   └── Configuration/         # Composição da infraestrutura
│   │
│   ├── PainelGestao.OrganizaliFinancas.Api/
│   │   ├── Configuration/         # Composição e registro de endpoints Minimal API
│   │   ├── Contracts/             # Respostas padronizadas
│   │   ├── Middleware/            # Erros e correlação
│   │   ├── Authorization/         # Permissões
│   │   ├── Security/              # Contexto autenticado
│   │   └── Background/            # Gatilhos de rotinas da aplicação
│   │
│   └── PainelGestao.OrganizaliFinancas.Shared/
│       ├── Common/                # Recursos transversais mínimos
│       ├── Constants/             # Constantes
│       ├── Exceptions/            # Exceções transversais
│       └── Settings/              # Configurações tipadas
│
├── frontend/                      # Next.js: app, components, lib e public
├── tests/                         # Unidade, integração e arquitetura
├── docs/                          # Documentação disponível
├── .gitignore
└── README.md
```

**Princípios da arquitetura:**

- 🧩 **Domínio protegido** — cálculos e invariantes sem dependência de HTTP ou banco.
- 🔄 **Dependências para dentro** — Application usa contratos; Infrastructure os implementa.
- 💾 **Persistência explícita** — Dapper no acesso a dados, EF Core para schema.
- 🔐 **Isolamento por casa** — autorização e contexto de tenant em cada operação.
- 🧪 **Regras verificáveis** — parcelas, rateios, pagamentos e estornos com cenários de aceite.

## 🚀 Primeiros passos

**Comece pelo produto e siga para o planejamento técnico:**

1. **[Escopo completo](docs/product/scope.md)** — módulos, limites e jornada da família.
2. **[Decisões do projeto](docs/product/decisions.md)** — escolhas confirmadas e questões abertas.
3. **[Dashboard e experiência](docs/product/experience.md)** — indicadores, navegação e ações.
4. **[Plano de implementação](docs/plans/implementation.md)** — marcos, dependências e critérios de entrega.

**[Marcos iniciais M1.1–M3.4](docs/plans/development-milestones.md)** — tarefas de fundação, acesso e cadastros, com referências do RevendaPRO e aceite por entrega.

**Base backend — comandos validados com SDK .NET 10.0.401:**

```powershell
dotnet restore PainelGestao.OrganizaliFinancas.slnx
dotnet build PainelGestao.OrganizaliFinancas.slnx --no-restore -c Release
dotnet test PainelGestao.OrganizaliFinancas.slnx --no-build --no-restore -c Release
```

O restore requer acesso ao Foundation.Base 3.2.0-rc.5. Nenhuma credencial de feed é versionada. [Evidências e limites do M1.1](docs/testing/m1-1-foundation.md).

**[Executar API e Swagger](docs/testing/m1-3-api.md)** · **[Executar frontend e conferir Docker](docs/testing/m1-4-web.md)**

## 🐳 Docker

**Preparação para execução em containers:**

- **[Plano operacional](docs/operations/plan.md)** — ambientes, configuração, publicação e recuperação.
- **[Fundação técnica — M1](docs/plans/implementation.md)** — criação e validação de Dockerfiles e Compose.

Dockerfiles e Compose disponíveis. [Builds e execução local](docs/testing/m1-4-web.md). Imagens e comunicação web/API verificadas; Compose persistente não iniciado.

## 📖 Guias de desenvolvimento

- **[Contexto do projeto](docs/agent/context.md)** — decisões e estado atual para continuidade.
- **[Instruções de desenvolvimento](docs/agent/instructions.md)** — convenções e regras de trabalho.
- **[Checklist de entrega](docs/agent/checklist.md)** — verificações por etapa.
- **[Regras financeiras](docs/domain/financial-rules.md)** — cálculos, datas, rateios, faturas e histórico.

## 🌐 Documentação da API

**Contratos planejados para a aplicação:**

- **[Endpoints](docs/api/endpoints.md)** — operações por contexto, filtros e idempotência.
- **[Respostas HTTP](docs/api/responses.md)** — `SuccessDetails<T>`, `ProblemDetails` e códigos de retorno.
- **[Segurança e isolamento](docs/requirements/non-functional.md)** — sessão, permissões e proteção de dados.

Swagger/OpenAPI seguirá o estilo Windows dark da skill `swagger-global`, adaptado ao Organizali. Backend Minimal API e frontend Next.js permanecem separados, conforme [ADR-0002](docs/architecture/decisions/ADR-0002-minimal-api-frontend-swagger.md).

## 📬 Coleção Postman

Ainda não há coleção publicada. Os [contratos planejados](docs/api/endpoints.md) definem as operações que serão documentadas e verificadas durante a implementação da API.

## 🏗️ Arquitetura e design

- **[Visão geral e stack](docs/architecture/overview.md)** — tecnologias, versões de referência e decisões.
- **[Camadas](docs/architecture/layers.md)** — localização e responsabilidade de cada componente.
- **[Dependências](docs/architecture/dependencies.md)** — referências permitidas e limites entre camadas.
- **[ADR-0001 — RevendaPRO](docs/architecture/decisions/ADR-0001-revenda-pro.md)** — padrão adotado.
- **[Modelo de dados](docs/database/mappings.md)** — entidades, relações e invariantes.
- **[Experiência do dashboard](docs/product/experience.md)** — interface para coordenar as finanças da casa.

## 🔄 CI/CD

**Gates previstos para o desenvolvimento:** build, lint, testes, validação de dependências e documentação atualizada.

- **[Plano de implementação](docs/plans/implementation.md)** — aceite de cada marco.
- **[Operação e publicação](docs/operations/plan.md)** — imagens versionadas, migrations, backup e recuperação.

O pipeline ainda não foi criado.

## 📚 Referências

- **[Fontes consultadas](docs/architecture/references.md)** — projetos e documentos que orientam a implementação.
- **[Requisitos funcionais](docs/requirements/functional.md)** — 46 requisitos com prioridade e critério de aceite.
- **[Requisitos não funcionais](docs/requirements/non-functional.md)** — 22 requisitos de qualidade e operação.
- **[Decisões e dúvidas](docs/product/decisions.md)** — registro das definições do produto.

## 🧪 Testes

**32 cenários de aceite definidos** para os fluxos financeiros, isolamento, permissões, concorrência e operação.

- **[Plano de testes](docs/testing/test-patterns.md)** — cenários, ferramentas e resultados esperados.
- **[Regras financeiras](docs/domain/financial-rules.md)** — exemplos para validar cálculos.

**29 testes técnicos aprovados localmente:** dependências entre camadas, referências dos projetos, Foundation, ausência de controllers e validação. Não representam os 32 cenários funcionais nem medição de cobertura. [Execução do M1.1](docs/testing/m1-1-foundation.md).

## 📊 Estado do projeto

| Etapa | Estado |
|---|---|
| Escopo e requisitos | Documentados para revisão |
| Arquitetura e tecnologias | Definidas com base no RevendaPRO |
| Dashboard e jornadas | Especificados |
| Modelo de dados e API | Contratos propostos |
| Plano de implementação e testes | Documentado |
| M1.1 — solução backend | Concluído; build e 18 testes aprovados |
| M1.2 — persistência | Concluído; [22 testes aprovados, incluindo banco descartável](docs/testing/m1-2-persistence.md) |
| M1.3 — endpoints e Swagger | [Implementado e testado](docs/testing/m1-3-api.md); revisão visual pendente |
| M1.4 — base Next.js e Docker | [Build, lint e comunicação aprovados](docs/testing/m1-4-web.md) |
| M2.1 — piloto | [Provisionamento e histórico implementados](docs/testing/m2-1-pilot.md) |
| M2.2 — autenticação | [JWT, cookies, renovação e revogação testados](docs/testing/m2-2-authentication.md); transporte externo de recuperação pendente |
| M2.3 — permissões | Matriz proposta aguardando confirmação, sem seed presumido |
| M2.4 — interface | [Login, shell e Minha conta implementados](docs/testing/m2-4-web-access.md); jornada por permissões e revisão visual pendentes |

## 📋 Documentação por público

### Para quem coordena a casa

1. [O que o sistema vai oferecer](docs/product/scope.md)
2. [Dashboard, fluxo de caixa e alertas](docs/product/experience.md)
3. [Rendas, despesas, cartões e rateios](docs/domain/financial-rules.md)

### Para desenvolvedores

1. [Instruções do projeto](docs/agent/instructions.md)
2. [Arquitetura e stack](docs/architecture/overview.md)
3. [Modelo de dados](docs/database/mappings.md)
4. [Contratos da API](docs/api/endpoints.md)
5. [Testes e aceite](docs/testing/test-patterns.md)

### Para operação do SaaS

1. [Ambientes e recuperação](docs/operations/plan.md)
2. [Requisitos de segurança e disponibilidade](docs/requirements/non-functional.md)
3. [Marcos e liberação do piloto](docs/plans/implementation.md)

### Para consumidores da API

1. [Operações e payloads](docs/api/endpoints.md)
2. [Respostas de sucesso e erro](docs/api/responses.md)
3. [Requisitos de autorização](docs/requirements/non-functional.md)

## 🔍 Links rápidos

| Quero... | Documento |
|---|---|
| Revisar o escopo | [Escopo completo](docs/product/scope.md) |
| Entender o dashboard | [Experiência e indicadores](docs/product/experience.md) |
| Conferir saldo e projeção | [Regras financeiras](docs/domain/financial-rules.md) |
| Consultar tecnologias e versões | [Arquitetura](docs/architecture/overview.md) |
| Ver o que será feito primeiro | [Plano de implementação](docs/plans/implementation.md) |
| Consultar as decisões | [Registro de decisões](docs/product/decisions.md) |
| Navegar por todos os documentos | [Índice de documentação](docs/README.md) |

## 📁 Estrutura da documentação

```text
docs/
├── README.md                      # Índice e ordem de leitura
├── product/                       # Escopo, decisões e experiência
├── requirements/                  # Requisitos funcionais e não funcionais
├── domain/                        # Regras financeiras e exemplos
├── architecture/                  # Stack, camadas, dependências e referências
│   └── decisions/                 # Decisões arquiteturais
├── database/                      # Modelo conceitual
├── api/                           # Endpoints e respostas planejados
├── plans/                         # Marcos de implementação
├── testing/                       # Cenários de aceite
├── operations/                    # Ambientes, publicação e recuperação
└── agent/                         # Contexto, instruções e checklist
```

## 💡 Como usar esta documentação

1. Comece pelo escopo para entender o produto e suas prioridades.
2. Consulte as decisões antes de implementar uma regra de negócio.
3. Use os links por público para encontrar os documentos da sua etapa.
4. Confira os cenários de aceite ao implementar cada funcionalidade.

## 🤝 Desenvolvimento e revisão

Mudanças devem seguir as [instruções do projeto](docs/agent/instructions.md), atualizar a documentação afetada e preservar os [critérios de aceite](docs/testing/test-patterns.md). Decisões de negócio em aberto devem ser discutidas antes da implementação correspondente.

## 📄 Licença

A licença de distribuição do projeto ainda não foi definida. As condições de uso das dependências serão verificadas na fundação técnica.

## 👨‍💻 Autor

**Maicon Cardozo** — [@maiconcardozo](https://github.com/maiconcardozo)

## 📞 Suporte e sugestões

- **[Issues do projeto](https://github.com/maiconcardozo/PainelGestao.OrganizaliFinancas/issues)** — sugestões e problemas na documentação.
- **[Índice de documentação](docs/README.md)** — acesso aos documentos disponíveis.
- **[Decisões pendentes](docs/product/decisions.md)** — questões que precisam de definição.

## 📖 Recursos externos

- [Authentication — referência de apresentação deste README](https://github.com/maiconcardozoglobal/Authentication)
- [.NET e ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Next.js](https://nextjs.org/docs)
- [React](https://react.dev/)
- [Dapper](https://github.com/DapperLib/Dapper)
- [MariaDB](https://mariadb.com/docs/)
