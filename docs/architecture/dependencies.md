# Dependências permitidas

| Origem | Referências permitidas |
|---|---|
| Api | Application, Shared; Infrastructure somente na composição |
| Application | Domain, Shared |
| Infrastructure | Domain, Shared e bibliotecas técnicas |
| Domain | Shared limitado a abstrações puras; Foundation.Domain |
| Shared | Nenhuma camada do projeto |
| Frontend | Contratos HTTP; sem acesso direto ao banco |
| Tests | Camadas testadas |

Foundation é uma dependência autorizada; verificar que as classes efetivamente usadas pelo Domain não obrigam dependência de EF/Dapper. Evitar dependências circulares entre contextos. Comunicação entre contexto de cartão e contas passa por caso de uso/contratos explícitos e transação, sem controller chamando outro controller.

Testes de arquitetura devem falhar se Domain/Application referenciarem Infrastructure/Api ou se repositories/controllers ocuparem camada incorreta. SQL é versionado exclusivamente em Queries. Toda consulta de negócio recebe contexto de tenant seguro; não disponibilizar um repository genérico sem tenant em handlers.
