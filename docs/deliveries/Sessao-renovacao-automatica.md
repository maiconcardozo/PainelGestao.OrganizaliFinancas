# Entrega — Sessão renovada sozinha

Data: 14/10/2026. Estado: implementado e validado no escopo abaixo. Pedido do usuário: "o usuário está reclamando que está voltando direto para a tela de login, como se caísse o login".

## Causa

O acesso (token JWT no cookie `organizali_access`) valia 15 minutos. Vencido, qualquer navegação caía em `requireSession` sem token e redirecionava para `/session-renew`, uma tela com o botão **Continuar**; qualquer clique em botão de ação recebia 401 e ia para a mesma tela. Se a renovação falhasse (por exemplo, duas abas renovando ao mesmo tempo: o refresh token era de uso único e a segunda tentativa recebia 401), a tela mandava para `/login`. Para quem usa, parecia que o login caía a cada 15 minutos.

## O que mudou

1. **Renovação silenciosa** (`frontend/proxy.ts`, convenção `proxy` do Next 16): em toda requisição de página ou de `/api/backend` sem o cookie de acesso mas com o cookie de sessão, o proxy chama `POST /api/auth/refresh` na API, grava os dois cookies novos na resposta e injeta o acesso novo na própria requisição, que segue normalmente. Prefetches não renovam. Se a API responder 401 (sessão realmente encerrada), os cookies são apagados e a pessoa vai direto ao login.
2. **Tolerância a renovações paralelas** (`AuthenticationSql.LockRefresh`): um refresh token já rotacionado continua aceito por 60 segundos, para abas ou requisições simultâneas do mesmo navegador não derrubarem a sessão. Depois disso continua inválido.
3. **Tempos de vida em produção** (`deploy/compose.production.yml`): acesso de 60 minutos (`ORGANIZALI_ACCESS_MINUTES`, máximo 60) e sessão de 30 dias de uso contínuo (`ORGANIZALI_SESSION_DAYS`, máximo 30). Antes: 15 minutos e 7 dias.
4. **Tela `/session-renew`** renova sozinha ao abrir e volta para a página de origem (`?next=`, só caminhos do próprio site); o botão **Continuar** fica para o caso de falha.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Quando a pessoa volta ao login | Só se ficar 30 dias sem usar, se sair em outro aparelho (**Sair** encerra todas as sessões), se trocar a senha, se o titular bloquear o acesso ou se a chave JWT for trocada. |
| Rotação | Cada renovação emite um par novo e marca o anterior como revogado; o anterior ainda vale 60 s. |
| Limite | A renovação usa o mesmo limite do login: 10 por minuto por endereço. Uma renovação atende todas as requisições seguintes daquele navegador. |

## Validação e reprodução

- `dotnet build` aprovado; frontend `tsc`, `eslint` e `next build` aprovados (`ƒ Proxy` presente na saída do build).
- Servidor da LAN: ver seção de validação ao final (fumaça pela API: página carregada sem o cookie de acesso devolve 200 e novos cookies; segunda renovação com o mesmo token dentro de 60 s aceita; token inventado recusado com cookies limpos).

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Runbook](../operations/runbook.md).
