# M1.4 — Base web e execução local

**Atualização após M2:** login e cookies foram implementados; consulte [M2.2](m2-2-authentication.md) para `Jwt__Key`, `APP_ORIGIN` e `COOKIE_SECURE`, e [M2.4](m2-4-web-access.md) para as telas disponíveis. As evidências abaixo descrevem a execução original de M1.4.

Estado: base técnica concluída. Identidade visual baseada no RevendaPRO, confirmada pelo usuário. Revisão em navegador permanece pendente porque o conector não disponibilizou navegador nesta sessão.

## Implementação

- Next.js 16.3.3, React/React DOM 19.2.8 e lockfile copiado do RevendaPRO, sem atualizar versões.
- App Router em app/, código compartilhado em lib/ e assets em public/, com configuração standalone.
- Tailwind, Lucide, Source Sans 3 e Barlow Condensed seguindo o frontend de referência.
- Página de entrada em português, com indicação de piloto em preparação. Sem formulário inoperante, indicadores financeiros fictícios ou login simulado.
- lib/config.ts, session.ts e server.ts preparam a leitura de cookie no servidor e requisições autenticadas. Emissão/renovação de cookie, sessão verificada e proxy autenticado completo pertencem ao M2; helpers ainda não consumidos pela tela.
- /api/health faz uma chamada do servidor Next para /health/live. Sem credencial no navegador, cache ou redirecionamento a outro servidor.
- Dockerfiles seguem os estágios das referências, com usuários sem privilégios; Compose próprio, portas locais 5189/3110 e volume organizali_data.
- Documentação instalada de Next.js sobre layouts e route handlers lida antes da implementação, conforme frontend/AGENTS.md.

## Validações executadas

| Verificação | Resultado |
|---|---|
| npm ci | Concluído com o lockfile da referência |
| npm run build | Build de produção e TypeScript aprovados |
| npm run lint | Aprovado |
| docker compose config --quiet | Configuração válida com valores sintéticos de validação |
| Docker API | Imagem organizali-api:m1-4 compilada |
| Docker frontend | Imagem organizali-frontend:m1-4 compilada |
| Página em container | HTTP 200 e conteúdo Organizali |
| Asset local | SVG HTTP 200 |
| Next → API em rede isolada | /api/health respondeu 200 Healthy |
| API desligada | /api/health respondeu 503 Unavailable |
| Limpeza | Dois containers e rede de teste descartados |

A verificação de containers não montou volumes nem iniciou banco. A persistência foi validada separadamente no MariaDB descartável autorizado do M1.2. Não houve acesso a banco existente. O Compose completo com volume persistente não foi iniciado.

O npm emitiu aviso de obsolescência do ESLint da referência; sua versão foi preservada. Não foi apresentada medição de cobertura, auditoria de segurança ou aprovação visual.

## Desenvolvimento local

```powershell
cd frontend
npm ci
npm run build
npm run lint
npm run start -- --port 3110
```

Para desenvolvimento, npm run dev -- --port 3110. INTERNAL_API_URL tem padrão http://127.0.0.1:5189; em Docker usa http://api:8080. Inicie a API conforme [M1.3](m1-3-api.md). Login ainda não está implementado.

## Containers

Comandos de build efetivamente executados na raiz:

```powershell
docker build -f src/PainelGestao.OrganizaliFinancas.Api/Dockerfile -t organizali-api:m1-4 .
docker build -t organizali-frontend:m1-4 frontend
```

O Compose exige duas senhas locais em .env, conforme .env.example. Não há valores reais versionados. A configuração expõe serviços apenas em loopback e não expõe a porta do banco.

Migrations não rodam automaticamente no startup. Um banco recém-criado pelo Compose fica sem schema e /health/ready responde 503; o provisionamento explícito entra no M2. A página web e /health/live podem funcionar nesse estado. Não tratar esse Compose como publicação de produção ou piloto pronto.

## Próximo marco

M2: casa, assinatura manual, provisionamento, login e permissões. O dashboard financeiro segue no M7, após os lançamentos que alimentam seus indicadores.
