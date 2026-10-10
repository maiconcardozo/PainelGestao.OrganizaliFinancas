# Organizali Finanças

Planejamento de um SaaS web para gestão das finanças da casa: pessoas, renda prevista, recebimentos reais, despesas, dívidas, cartões e orçamento.

**Estado em 10/10/2026: escopo e documentação para revisão. Nenhuma funcionalidade foi implementada.** Projeto particular, uma casa por assinatura, sem vínculo operacional com a empresa e sem publicação na Wiki. O usuário confirmou o **RevendaPRO como referência técnica principal**.

## Sobre

O produto deve responder quanto a casa espera receber, quanto efetivamente recebeu, quanto deve, quando precisa pagar e quanto terá disponível. Pessoas cadastradas podem existir sem conta de acesso. Previsões e movimentações reais são registros distintos e relacionados.

Leia primeiro o [escopo completo](docs/product/scope.md), os [requisitos funcionais](docs/requirements/functional.md) e as [decisões pendentes](docs/product/decisions.md). As propostas ainda não aprovadas estão identificadas; não autorizam iniciar desenvolvimento.

## Funcionalidades planejadas

- Casa, moradores, usuários, convites e permissões.
- Fontes de renda com vigência e previsão mensal; recebimentos efetivos totais ou parciais.
- Contas bancárias, dinheiro em carteira, saldos iniciais e transferências.
- Categorias, tipos de dívida, credores, formas de pagamento e campos adicionais configuráveis.
- Despesas avulsas, recorrentes e parceladas; pagamentos, descontos, encargos e estornos.
- Cartões, compras, parcelas, fechamento de fatura, vencimento e pagamento.
- Visão mensal prevista e realizada, compromissos futuros, orçamento, alertas e relatórios.
- Administração das assinaturas, auditoria, exportação e operação do SaaS.

## Tecnologias planejadas

As versões abaixo são **observadas na referência local**, não pacotes instalados neste repositório. Não houve instalação nem atualização de NuGet. A fixação e a compatibilidade devem ser verificadas no marco técnico inicial.

| Área | Escolha baseada no RevendaPRO |
|---|---|
| Backend | C# / .NET 10 / ASP.NET Core Web API com controllers |
| Casos de uso | MediatR 14.2.0, FluentValidation 12.1.1 |
| Banco | MariaDB 11.8 |
| Leitura e escrita | Dapper 2.1.79, SQL parametrizado em query objects |
| Schema | EF Core 10.0.5, MySql.EntityFrameworkCore 10.0.1, MySql.Data 9.6.0; somente migrations e mapeamento |
| Base compartilhada | Foundation.Base 3.2.0-rc.5 na referência; uso autorizado, restore independente a validar |
| Frontend | Next.js 16.3.3, React 19.2.8, TypeScript 5, Tailwind CSS 4 |
| Sessão | Cookie HttpOnly no Next.js, proxy para API, JWT e refresh token no backend |
| Interface | Lucide React; identidade própria do Organizali |
| Testes | xUnit, Moq, FluentAssertions 7.2.2, NetArchTest e Testcontainers.MariaDb |
| Entrega futura | Docker / Docker Compose; hospedagem a definir |
| Anexos e exportação | API S3, ClosedXML e QuestPDF, seguindo referência e validação de licenças |

Detalhes, evidências e diferenças entre as referências: [arquitetura](docs/architecture/overview.md) e [ADR-0001](docs/architecture/decisions/ADR-0001-revenda-pro.md).

## Estrutura atual

```text
README.md
docs/
  product/          escopo, telas e decisões pendentes
  requirements/     requisitos funcionais e não funcionais
  domain/           regras financeiras e exemplos
  architecture/     camadas, dependências, decisões e referências
  database/         modelo conceitual
  api/              contratos planejados
  plans/            marcos e critérios de entrega
  testing/          cenários de aceite
  operations/       configuração e operação planejadas
  agent/            contexto e instruções para continuidade
```

`src/`, `frontend/`, solution, projetos de teste e Compose serão criados no desenvolvimento, após revisão do escopo. Não existem comandos de execução válidos nesta etapa.

## Arquitetura planejada

Monólito modular com frontend separado. Cinco camadas: `Api`, `Application`, `Domain`, `Infrastructure`, `Shared`. Contratos de repositório e Unit of Work no Domain; implementações na Infrastructure. Domain e Application não acessam SQL, HTTP externo ou detalhes de persistência.

| Camada | Responsabilidade | Dependências permitidas |
|---|---|---|
| Api | Controllers, autenticação, respostas HTTP e composição | Application, Shared; Infrastructure para DI |
| Application | Casos de uso, commands, queries e validação de entrada | Domain, Shared |
| Domain | Entidades, regras financeiras e contratos | Abstrações puras de Shared e Foundation.Domain |
| Infrastructure | Dapper, migrations, arquivos e integrações | Domain, Shared |
| Shared | Configurações e recursos transversais mínimos | Nenhuma camada do projeto |

```text
Navegador → Next.js / sessão e proxy → API → Application → Domain
                                         ↓ composição
                                  Infrastructure → MariaDB / arquivos
```

Código, rotas e banco em inglês; interface, mensagens e documentação em português. Identificadores internos `Id`; identificadores públicos `Code` UUID v7; FKs com prefixo `Id`. Ver [camadas](docs/architecture/layers.md) e [dependências](docs/architecture/dependencies.md).

## Fluxos principais

1. Criar casa → cadastrar moradores → cadastrar contas e saldos iniciais.
2. Definir fonte de renda → gerar previsão → registrar o recebido → comparar diferença.
3. Escolher tipo de dívida → cadastrar obrigação/parcelas → registrar pagamento → atualizar saldo.
4. Cadastrar cartão → registrar compra → compor fatura → pagar fatura sem duplicar despesa.
5. Consultar mês → revisar vencimentos → acompanhar orçamento e projeção.

## API, jobs e integrações

Contratos propostos em [endpoints](docs/api/endpoints.md) e [respostas](docs/api/responses.md). Sucesso com `SuccessDetails<T>`; erro com `ProblemDetails`; controllers finos com `IMediator`. Recorrências e alertas terão processamento idempotente com controle persistente no MariaDB. Cobrança automática depende da decisão comercial.

## Autenticação e permissões

Sessão de navegador em cookie HttpOnly/Secure e proxy no Next.js, conforme RevendaPRO. A API valida usuário, vínculo ativo com a casa e permissão a cada operação. O menu vem filtrado do servidor. Guardas de tela não substituem autorização no endpoint. Detalhes em [requisitos não funcionais](docs/requirements/non-functional.md).

## Cache e serviços externos

Redis não é obrigatório para a primeira versão. Caso necessário por medição, será introduzido por ADR, com chave por ambiente/casa/usuário, TTL, invalidação após commit e fallback para o banco. Não será fonte de saldos nem garantia exclusiva de idempotência.

Serviços de e-mail, arquivos e pagamento serão próprios deste produto. Não há WCF, banco jCobranca, autenticação corporativa ou WhatsApp da empresa no escopo.

## Configuração e variáveis de ambiente

Planejados em [operação](docs/operations/plan.md). SDK, Node, feed de pacotes, variáveis, portas, Dockerfiles e comandos serão confirmados no marco M1 e documentados com evidência de execução. Até lá, **não há aplicação para iniciar**. Exemplos futuros conterão placeholders, sem credenciais.

## Rodando localmente

Nesta etapa, o repositório contém documentação. Comece pelo [índice de docs](docs/README.md). Comandos de restore, build e execução serão acrescentados no M1 após validação na solução real.

## Docker

Docker e Docker Compose estão planejados conforme o RevendaPRO. Os arquivos de containers ainda não existem; sua criação e validação fazem parte do M1.

## Testes

Nesta entrega: revisão documental, rastreabilidade e conferência de links. Não há build ou testes de aplicação executados, pois ainda não há código. O [plano de testes](docs/testing/test-patterns.md) define os cenários financeiros, de isolamento, permissão, concorrência e operação que condicionarão as entregas.

## Documentação complementar

- [Plano de implementação e marcos](docs/plans/implementation.md)
- [Regras financeiras](docs/domain/financial-rules.md)
- [Modelo de dados](docs/database/mappings.md)
- [Telas e experiência](docs/product/experience.md)
- [Contexto para agentes](docs/agent/context.md)
- [Instruções para desenvolvimento](docs/agent/instructions.md)
- [Checklist de entrega](docs/agent/checklist.md)
- [Fontes consultadas](docs/architecture/references.md)

## Decisões importantes

RevendaPRO é a referência prioritária por decisão do usuário. Regras domésticas são específicas do Organizali. O documento define o escopo proposto completo e suas etapas, sem afirmar que recursos existem. Dúvidas devem ser apresentadas ao usuário antes de implementar a parte dependente. As decisões abertas estão concentradas em um [registro único](docs/product/decisions.md).
