# M1.1 — Fundação da solução

Concluído em 10/10/2026. Escopo: solução compilável, pacotes centralizados e dependências protegidas. M1 completo ainda depende de persistência, API e frontend.

## Implementação e origem

- Cinco projetos .NET 10 e um projeto xUnit na solução SLNX, como RevendaPRO.
- Directory.Packages.props usa exclusivamente versões do manifesto RevendaPRO, sem atualizações. Apenas pacotes necessários nesta etapa foram incluídos.
- Nullable, implicit usings, analyzers e code style habilitados em Directory.Build.props conforme padrão central de build.
- TenantEntity reproduz a base RevendaPRO: Entity do Foundation e IdTenant.
- IUnitOfWork e OrganizaliFinancasUnitOfWork herdam contratos e implementação Foundation; ainda sem repositories, registro de conexão ou transações executadas.
- ValidationBehavior, registro de Application, InputValidationException e SuccessDetails seguem os arquivos reais da referência.
- Program contém somente composição/startup. Sem controllers, rotas de negócio, conexão com banco ou rotinas em background.

Referências lidas: RevendaPRO/Directory.Packages.props, projetos .csproj, TenantEntity, IUnitOfWork, RevendaProUnitOfWork, Application/Configuration/ServiceCollectionExtensions, ValidationBehavior, Shared/Exceptions/BusinessExceptions, Api/Contracts/SuccessDetails e Unit/ArchitectureTests. Minimal API é decisão explícita do usuário: [ADR-0002](../architecture/decisions/ADR-0002-minimal-api-frontend-swagger.md).

## Estrutura implementada

```text
PainelGestao.OrganizaliFinancas.slnx
Directory.Build.props
Directory.Packages.props
src/
├── PainelGestao.OrganizaliFinancas.Api/
│   ├── Contracts/SuccessDetails.cs
│   └── Program.cs
├── PainelGestao.OrganizaliFinancas.Application/
│   ├── Behaviors/ValidationBehavior.cs
│   └── Configuration/ServiceCollectionExtensions.cs
├── PainelGestao.OrganizaliFinancas.Domain/
│   ├── Entities/TenantEntity.cs
│   └── Interfaces/IUnitOfWork.cs
├── PainelGestao.OrganizaliFinancas.Infrastructure/
│   └── UnitOfWork/OrganizaliFinancasUnitOfWork.cs
└── PainelGestao.OrganizaliFinancas.Shared/
    └── Exceptions/BusinessExceptions.cs
tests/PainelGestao.OrganizaliFinancas.Tests/
└── Unit/
    ├── ArchitectureTests.cs
    └── ValidationBehaviorTests.cs
```

Cada camada contém seu .csproj. Pastas sem implementação não foram criadas.

## Validação executada

Ambiente: Windows, SDK .NET 10.0.401. Comandos a partir da raiz:

```powershell
dotnet restore PainelGestao.OrganizaliFinancas.slnx
dotnet build PainelGestao.OrganizaliFinancas.slnx --no-restore -c Release
dotnet test PainelGestao.OrganizaliFinancas.slnx --no-restore -c Release --logger "trx;LogFileName=m1-1.trx" --results-directory TestResults
```

| Verificação | Resultado |
|---|---|
| Restore | Seis projetos restaurados, Foundation.Base 3.2.0-rc.5 disponível |
| Build Release | Zero erros e zero avisos |
| Testes | 18 aprovados, zero falhas, zero ignorados |
| Dependências | NetArchTest verifica código; leitura de .csproj também captura referências proibidas não utilizadas |
| Foundation | Herança da entidade e contrato transacional conferidos |
| Validação | Requisição inválida impede handler; válida preserva resposta/token; sem validators segue handler |
| Host | Processo local iniciou, respondeu HTTP 404 na raiz e foi encerrado |

O HTTP 404 é esperado: ainda não há endpoints. Isso comprova somente startup, não health, autenticação ou funcionalidade financeira. A primeira execução dos testes encontrou uma asserção que rejeitava coleção vazia em Shared; foi corrigida para verificar referências proibidas, e a execução final passou.

TRX local em TestResults/m1-1.trx (ignorado pelo Git). Nenhum banco foi acessado, nenhuma migration executada, nenhum serviço externo de envio acionado. Sem medição de cobertura.

## Próximas etapas

- M1.2: conexão, schema e prova de transações Dapper. Escrita em testes depende de banco descartável isolado e autorização específica.
- M1.3: endpoints Minimal API, tratamento HTTP de erros, OpenAPI e Swagger no padrão aprovado.
- M1.4: frontend Next.js separado e execução local completa.

O pacote Foundation foi restaurado no ambiente atual; em outra máquina é necessário acesso ao mesmo pacote. Não foram copiadas credenciais ou configurações dos projetos de referência.
