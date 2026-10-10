# Camadas planejadas

Prefixo técnico: `PainelGestao.OrganizaliFinancas`. Nome público provisório: Organizali Finanças.

| Camada | Responsabilidades | Proibido |
|---|---|---|
| Api | Endpoints Minimal API com IMediator, autenticação, policies, middleware, OpenAPI e DI | SQL, cálculo de saldo ou repository no endpoint |
| Application | Commands, queries, handlers, validators, autorização de caso de uso e transações coordenadas | DbContext, conexão SQL, SDK S3 ou HTTP externo |
| Domain | Household, IncomeForecast, Obligation, Payment, Statement, Budget e invariantes | Dependência de API/Infrastructure |
| Infrastructure | Repositories, SQL, migrations, Unit of Work, armazenamento e segurança técnica | Decidir regra financeira dentro de SQL ou adaptador |
| Shared | Settings e contratos técnicos mínimos | Tornar-se depósito de regras de negócio |
| Frontend | Telas, formulários, sessão/proxy e apresentação | Fonte oficial de cálculos ou autorização |

```text
src/
  PainelGestao.OrganizaliFinancas.Api/
    Middleware/ Authorization/ Security/ Contracts/ Configuration/ Background/
  PainelGestao.OrganizaliFinancas.Application/
    Behaviors/ Configuration/
    Income/ Expenses/ Accounts/ Cards/ Budgets/ Alerts/
      Commands/ Queries/ Handlers/ Validators/ DTOs/
  PainelGestao.OrganizaliFinancas.Domain/
    Entities/ ValueObjects/ Enums/ Services/ Exceptions/
    Interfaces/Repositories/ Interfaces/Services/ Interfaces/IUnitOfWork.cs
  PainelGestao.OrganizaliFinancas.Infrastructure/
    Configuration/ Data/MySql/ Database/Contexts/ Database/Migrations/
    Persistence/Mappings/ Queries/ Repositories/ UnitOfWork/ Security/ Services/
  PainelGestao.OrganizaliFinancas.Shared/
    Settings/ Constants/ Common/ Exceptions/
frontend/
  app/ components/ lib/ public/
tests/
  PainelGestao.OrganizaliFinancas.Tests/
    Unit/ Integration/ Architecture/
```

Árvore alvo; cinco projetos e testes criados no M1.1. Subpastas funcionais serão adicionadas conforme uso. `Contracts`, `Configuration` e `Background` da Api foram conferidos no diretório real do RevendaPRO. Estrutura de Infrastructure segue seu PADRAO-GLOBAL. Testes atuais ficam em Unit, como no RevendaPRO. Pastas de contextos financeiros serão criadas conforme cada funcionalidade. Não copiar pastas específicas de veículos/FIPE sem uso neste domínio.

Entidades usam factory/métodos de comportamento e setters privados. `Money`, `Competence`, `DateRange` e `Allocation` são candidatos a value objects conforme invariantes, sem abstrações vazias. Validação de payload não substitui invariantes do domínio.
