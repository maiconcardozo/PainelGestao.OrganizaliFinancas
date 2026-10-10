# M2.2 — Autenticação e sessão

Backend Minimal API com MediatR, Argon2/Foundation e JWT conforme RevendaPRO. Login exige casa e assinatura ativas e usuário não bloqueado. JWT contém casa, usuário e versão da sessão; a API confere esses dados no banco a cada acesso. Tokens de renovação e recuperação são armazenados somente por hash.

## Entrega

- Login, consulta de sessão, renovação com rotação de uso único e logout de todas as sessões.
- Suspensão invalida sessões; reativação não reabilita tokens anteriores.
- Recuperação com token de uso único e expiração de 15 minutos; troca de senha revoga sessões. Transporte simulado nos testes, sem envio real. Em execução normal, recuperação retorna 503 até configurar um provedor aprovado.
- Cookies HttpOnly, SameSite=Lax e Secure por padrão em produção. Nenhum token retorna no corpo das rotas web de login/refresh.
- Proxy autenticado não expõe rotas de autenticação, inclusive variações de maiúsculas. Mutações exigem Origin exata.
- Limite local de 10 solicitações/minuto/IP nos endpoints públicos de autenticação. Atrás do proxy Next, o IP observado é o do servidor web; a política distribuída e o encaminhamento de IP precisam ser definidos antes de operação externa.

## Configuração

Configure `Jwt__Key` com segredo aleatório de pelo menos 32 bytes, fora do código. Issuer/audience usam os padrões próprios do Organizali; acesso dura 15 minutos e refresh 7 dias. Operações administrativas locais dispensam JWT porque não iniciam HTTP.

No frontend, `APP_ORIGIN` deve ser a origem pública exata, sem barra final (por exemplo `http://localhost:3110` localmente). Ela permite validar Origin quando a URL interna do Next difere da externa. Não se confia em cabeçalhos de encaminhamento enviados pelo cliente. `INTERNAL_API_URL` aponta para a API interna. Em produção HTTPS, manter `COOKIE_SECURE=true`; o Compose local define false por servir somente HTTP em loopback.

Aplicar a migration AuthenticationSessions pelo comando administrativo Migrate antes de iniciar a API sobre um banco próprio autorizado. Nenhum banco persistente foi alterado nesta entrega.

## Evidência de execução

- `dotnet test PainelGestao.OrganizaliFinancas.slnx -c Release` com `ORGANIZALI_DATABASE_TESTS=1`: 29 aprovados, zero falhas/ignorados, em MariaDB descartável. Resultado `TestResults/m2-2.trx`.
- A integração verifica login pendente/ativo, refresh único, logout, suspensão/reativação, recuperação/consumo único, senha antiga rejeitada, JWT expirado/inválido, casa diferente e usuário bloqueado.
- `npm run build --prefix frontend` e `npm run lint --prefix frontend`.
- `node --test tests/frontend/auth-flow.test.mjs`: servidor Next real e API simulada; cookies protegidos, origem recusada, isolamento dos tokens e logout quando só resta refresh.

Não houve envio de e-mail nem deploy. Telas de login/navegação pertencem a M2.4. Perfis e integrantes (M2.3) aguardam a matriz de operações enviada ao usuário; não foi feito seed presumindo aprovação.
