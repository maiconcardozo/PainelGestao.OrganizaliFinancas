# M3.1 — Moradores e configurações

Implementados Person e HouseholdSettings, com TenantEntity/Foundation, runtime Dapper, migration EF, casos de uso MediatR, Minimal API e telas Next no shell aprovado.

## Comportamento

- Morador não exige usuário, senha nem e-mail. Nome e estado podem ser alterados por titular. Inativação preserva registro e Code; consultas incluem ativos e inativos.
- Toda consulta e atualização limita IdTenant. Identidade e autoria vêm da sessão; o payload não define a casa nem o autor.
- Idioma pt-BR e moeda BRL fixos no piloto; fuso inicial America/Sao_Paulo. Titular pode configurar outro identificador IANA válido e a data de início de uso opcional, sem horário.
- Preferências padrão são retornadas sem gravar no GET. Persistência ocorre no primeiro salvamento explícito. Fuso será consumido pelos módulos financeiros quando implementados; esta etapa não gera previsões.
- Leitor/editor consultam moradores/configurações; administração da casa permanece com titular conforme D21. Rotas web e API verificam suas permissões independentemente.
- Controle de versão recusa edição desatualizada com 409; serialização pela casa evita duas atualizações concorrentes com a mesma versão. Auditoria Foundation registra autor e datas.

## Referências

RevendaPRO Customer (factory, alteração, TenantEntity), RolesUseCases e repositórios por contexto. Organização existente preservada; nenhuma atualização NuGet ou biblioteca web adicionada. HouseholdPeopleAndSettings é a migration incremental deste marco.

## Validação

29 testes .NET aprovados com banco MariaDB descartável, incluindo asserções novas em HouseholdAssertions: pessoa sem usuário, isolamento, inativação preservada, autoria real da sessão, edição concorrente (200/409), fuso inicial, fuso inválido (400), alteração para Manaus, data sem deslocamento e configuração independente de outra casa. Leitor consulta e recebe 403 ao escrever.

Build/lint Next e teste HTTP de acesso aprovados. Revisão visual continua pendente. TestResults/m3-1.trx contém a execução; nenhum banco persistente foi alterado.
