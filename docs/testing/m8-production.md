# M8 — Produção, segurança operacional e publicação Docker

Validação em 10/10/2026 com dados sintéticos, bancos descartáveis e nenhum serviço externo real: sem deploy, sem registro de imagens, sem e-mail real.

## Testes .NET

`ORGANIZALI_DATABASE_TESTS=1 dotnet test tests/PainelGestao.OrganizaliFinancas.Tests` → **89 aprovados, 0 falhas**.

| Teste | Resultado |
|---|---|
| `SmtpRecoveryDeliveryTests` | Configuração exige host, remetente e origem; envio para um servidor SMTP falso em loopback com destinatário correto e link `…/reset-password#token=` com token escapado; mensagem sem valores financeiros |
| `ApiFoundationTests` | Respostas `/api` com `X-Frame-Options: DENY`, `nosniff` e `no-store`; com limite 3/min a quarta chamada recebe 429 e `/health` não é limitado; fora de Development o Swagger responde 404 |
| `OperationsAssertions` (banco descartável) | Trava exclusiva entre conexões e liberada no descarte, livre também para uma conexão sem pool; rotina do horizonte processa todas as casas ativas; segunda execução 0/0/0; com a trava ocupada responde “Skipped” |

## Frontend

`node --test tests/frontend/auth-flow.test.mjs` → aprovado: CSP com `frame-ancestors 'none'`, `X-Frame-Options`, `nosniff`, sem `X-Powered-By`; recuperação e redefinição recusam outra origem (403); recuperação responde 202; o login repassa o endereço do cliente (`X-Forwarded-For`) à API. Jornada no Chrome (`pilot-browser.test.mjs`) aprovada com a CSP ativa. Lint, `tsc` e `next build` aprovados.

## Pilha de produção descartável

`ORGANIZALI_DOCKER_TESTS=1 node --test tests/deploy/production-stack.test.mjs` → **aprovado** (projeto Compose aleatório, portas livres, segredos gerados na hora, removido ao final com volumes).

| Verificação | Resultado |
|---|---|
| Imagens | `organizali-api` e `organizali-frontend` construídas a partir dos Dockerfiles |
| Portas | Só o proxy publica; API, frontend e banco sem porta no host |
| HTTPS | Certificado interno do Caddy; `Strict-Transport-Security`, CSP, sem cabeçalho `Server`; HTTP redireciona (308) |
| Swagger | 404 em produção |
| Provisionamento | `--Operation=ProvisionHousehold` e `ChangeSubscriptionStatus` pelo serviço `migrate` |
| Sessão | Login por HTTPS com cookies seguros; conta criada pelo proxy autenticado; sem sessão, 401 |
| Rotina | `--Operation=RollHorizon` processa 1 casa (não “Skipped”) |
| Backup | Arquivo `.sql.gz.enc` cifrado e verificado |
| Restauração | Conta criada depois do backup desaparece; a conta anterior volta com saldo 1234.56 |
| Logs | JSON (`Organizali.Http`); sem senha do banco, chave JWT, senha do usuário ou valores |

A primeira execução encontrou um defeito real: a trava da rotina ficava presa numa conexão do pool (o Dapper fechava a conexão depois do `GET_LOCK`), e a rotina manual respondia “Skipped”. A trava passou a manter a conexão aberta; o teste de banco agora confere que a trava fica livre para uma conexão sem pool.

## Reprodução

Pré-requisitos: Docker com Compose v2, Git Bash (para os scripts `deploy/*.sh`) e acesso à internet para baixar imagens base, pacotes e fontes durante o build. O teste da pilha leva cerca de 5 minutos com cache vazio.
