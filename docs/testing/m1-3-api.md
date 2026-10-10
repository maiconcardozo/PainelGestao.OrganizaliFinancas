# M1.3 — Base da API

Implementado: endpoints Minimal API, MediatR, readiness de persistência, erros padronizados, correlação e Swagger.

## Referências

- RevendaPRO/Api/Program.cs: MapGet de health; Application/Configuration e ValidationBehavior; Api/Contracts/SuccessDetails e Middleware/ExceptionHandlingMiddleware.
- PainelGestao.CPComunica/Api/Program.cs: endpoint /health/live.
- Organização das rotas nas extensões de Configuration, mantendo Program somente como composição, conforme ADR-0002. Não há controllers nem framework adicional de endpoints.
- Assets da skill swagger-global: tema Windows dark, Segoe UI, grafite e azul. Títulos, textos, links e marca adaptados ao Organizali; símbolo local provisório de casa, sem marca Global.
- Swashbuckle.AspNetCore 10.2.3 e Microsoft.AspNetCore.Mvc.Testing 10.0.11, versões existentes no RevendaPRO.

## Contratos disponíveis

| Rota | Resposta | Efeito |
|---|---|---|
| GET /health/live | 200 SuccessDetails | Confere somente processo |
| GET /health/ready | 200 SuccessDetails ou 503 ProblemDetails | MediatR → serviço de Infrastructure; leitura da tabela Tenant sem retornar linhas |
| GET /swagger/index.html | Swagger UI | Somente Development |
| GET /swagger/v1/swagger.json | OpenAPI | Somente Development |

Readiness não aplica migrations nem certifica módulos financeiros futuros. Startup exige OrganizaliFinancas:ConnectionString, sem credenciais versionadas. Configuração ausente impede inicialização. Nenhum seed ou escrita automática.

Exceções inesperadas retornam 500 genérico; validação retorna 400 e errors por campo. Resposta e header X-Correlation-ID permitem correlação. Logs de falha inesperada registram tipo e correlação, sem mensagem potencialmente sensível. Cancelamento pelo cliente não gera 500. Endpoints técnicos não exigem login; identidade e autorização entram em M2.

## Validação

- Testes HTTP em WebApplicationFactory: respostas live/ready, falhas, erros por campo e OpenAPI real.
- Teste de cancelamento do middleware e readiness contra o banco descartável autorizado.
- Publish Release executado em TestResults/publish-m1-3.
- Host publicado: live 200, ready 503 com banco deliberadamente indisponível, Swagger/OpenAPI/CSS/JS/SVG 200.
- JavaScript conferido com node --check.
- Nenhum navegador disponível no conector: inspeção visual desktop/mobile e tema claro permanecem pendentes. Não foi declarada aprovação visual.

## Execução local

A partir da raiz, configure uma conexão própria; não reaproveite banco corporativo:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:OrganizaliFinancas__ConnectionString = "<conexão do banco local do Organizali>"
dotnet run --project src/PainelGestao.OrganizaliFinancas.Api --no-launch-profile --urls http://127.0.0.1:5189
```

Swagger: http://127.0.0.1:5189/swagger/index.html. Tema claro: acrescente ?theme=light. Migrations não são aplicadas por esse comando.

Execução final: 29 testes aprovados, zero falhas e zero ignorados, incluindo readiness real no MariaDB descartável. Resultado em TestResults/m1-3.trx.
