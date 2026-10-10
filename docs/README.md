# Documentação do Organizali Finanças

Escopo v0.1 para revisão, preparado antes do desenvolvimento. Organização base de documentação segue Arquitetura.Global: `agent`, `architecture`, `api`, `database`, `testing`; `plans` e `operations` também existem no RevendaPRO. `product`, `requirements` e `domain` contêm os documentos específicos do produto doméstico, sem alterar camadas de código.

## Ordem de leitura

1. [Escopo completo](product/scope.md)
2. [Decisões confirmadas e pendências](product/decisions.md)
3. [Dashboard e experiência](product/experience.md)
4. [Requisitos funcionais](requirements/functional.md)
5. [Requisitos não funcionais](requirements/non-functional.md)
6. [Regras financeiras](domain/financial-rules.md)
7. [Arquitetura e tecnologias](architecture/overview.md)
8. [Camadas](architecture/layers.md) e [dependências](architecture/dependencies.md)
9. [Decisão de seguir RevendaPRO](architecture/decisions/ADR-0001-revenda-pro.md)
10. [Modelo de dados](database/mappings.md)
11. [Endpoints](api/endpoints.md) e [respostas](api/responses.md)
12. [Plano de implementação](plans/implementation.md)
    - [Marcos iniciais M1.1–M3.4](plans/development-milestones.md)
13. [Testes e aceite](testing/test-patterns.md)
14. [Operação](operations/plan.md)
15. [Referências consultadas](architecture/references.md)
16. [Contexto](agent/context.md), [instruções](agent/instructions.md) e [checklist](agent/checklist.md)

O [README da raiz](../README.md) resume o produto, a stack e o estado real do repositório. Não há código implementado nesta entrega.
