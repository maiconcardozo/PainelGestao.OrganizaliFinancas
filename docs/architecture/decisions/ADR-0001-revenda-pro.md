# ADR-0001 — Seguir RevendaPRO

Data: 10/10/2026. Estado: referência confirmada pelo usuário; desenho local proposto para revisão.

## Contexto

O usuário inicialmente citou Arquitetura.Global, Painel CP Comunica e RevendaPRO, depois determinou usar RevendaPRO. O padrão central usa português em conceitos de negócio e aceita outros providers por ADR. CP Comunica tem EF Core 9/Pomelo em runtime. RevendaPRO registra código em inglês e Dapper em runtime com EF Core 10 para schema.

## Decisão

Adotar as cinco camadas, convenções e tecnologias do RevendaPRO. Código, rotas, tabelas e colunas em inglês; rótulos, erros e documentos em português. `Id` numérico interno, `Code` UUID v7 público, FKs `IdTenant`, `IdPerson` etc., sem `HasColumnName` para traduzir nomes. SuccessDetails/ProblemDetails nas respostas.

MariaDB 11.8, Dapper para leitura/escrita, EF Core/provider Oracle somente schema. Foundation.Base autorizado pelo usuário em resposta específica. Preservar camadas do Arquitetura.Global; registrar divergências de idioma, provider, rotas `/api/...` e política operacional independente neste ADR.

Projeto particular: nome não recebe sufixo corporativo `.Global`; não utiliza serviços corporativos. Acesso privado ao feed precisa ser verificado; autorização de uso não comprova disponibilidade técnica.

## Consequências

Queries Dapper exigem filtros explícitos de tenant/atividade, testados. Confirmar suporte do conjunto de versões em M1. O README antigo de RevendaPRO e seu overview não descrevem todo o estado atual; manifestos, PADRAO-GLOBAL e código da Unit of Work têm prioridade para esta análise.

Financeiro confirmado usa reversão por lançamento, não soft delete destrutivo do saldo. Foundation fornece auditoria básica, mas não substitui histórico financeiro, autorização nem separação entre casas. Redis opcional; checkout posterior ao piloto; não adicionar infraestrutura sem caso de uso.
