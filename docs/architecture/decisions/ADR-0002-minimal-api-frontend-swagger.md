# ADR-0002 — Minimal API, frontend e Swagger

Estado: decidido pelo usuário em 10/10/2026.

## Decisão

Backend ASP.NET Core .NET 10 em **Minimal API**, com frontend **Next.js/React separado**, como esclarecido pelo usuário após a organização dos marcos. Esta decisão substitui as menções anteriores a controllers para o Organizali.

Manter Api, Application, Domain, Infrastructure e Shared, MediatR, FluentValidation, Foundation.Base, Dapper e EF somente para schema. Endpoints são entrada HTTP fina: recebem, autorizam, encaminham command/query ao MediatR e retornam o contrato padronizado. Regras continuam no domínio/aplicação. Program.cs apenas compõe a aplicação; não concentrar nele todas as rotas ou regras.

A referência RevendaPRO consultada usa controllers. Portanto Minimal API é uma adaptação explicitamente solicitada para este produto, não uma característica atribuída à referência. Antes de implementar M1.3, conferir exemplos Minimal API existentes nos projetos do usuário e registrar o arquivo usado para organização das rotas; não inventar um framework de endpoints. Usar as pastas de composição existentes e manter a organização concreta documentada ao implementá-la.

## Frontend

Seguir o padrão de organização, sessão HttpOnly, proxy, navegação e permissões do RevendaPRO, usando frontend-design para a identidade do Organizali. Interface em português e responsiva. O frontend é a aplicação usada pela família; Swagger documenta a API para desenvolvimento.

## Swagger

Aplicar a skill swagger-global: estilo Windows dark aprovado, cinza grafite, Segoe UI, texto claro e azul de ação, com opção clara. Descrever endpoints e propriedades em tabelas, com contratos reais e exemplos aplicáveis à API própria. Servir CSS/JS como assets locais e conferir sua inclusão no build/container.

Adaptar título, links, logo e favicon ao Organizali, por ser projeto particular; não transferir a marca Global. Preservar comportamento visual da referência. Não copiar exceções de compatibilidade da Facade nem seus contratos.

Documentar autenticação, permissões, payloads e respostas SuccessDetails/ProblemDetails nos endpoints Minimal API. Validar o OpenAPI servido e a renderização do Swagger. Nenhuma atualização de NuGet é autorizada por essa escolha.
