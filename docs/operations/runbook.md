# Operação em produção (Docker)

Procedimentos validados no M8 com uma pilha descartável (`tests/deploy/production-stack.test.mjs`). A hospedagem, o domínio e o provedor de e-mail continuam em aberto (D16); os comandos abaixo valem para qualquer servidor Linux com Docker e Compose v2, ou para Windows com Git Bash.

## Arquitetura

| Serviço | Imagem | Rede | Exposição |
|---|---|---|---|
| `proxy` | `caddy:2-alpine` | edge | Únicas portas publicadas (80 → redireciona, 443 com HTTPS e HSTS) |
| `frontend` | `organizali-frontend:<versão>` | edge, app | Interna; usuário `nextjs` sem privilégios |
| `api` | `organizali-api:<versão>` | app, data | Interna; usuário `app`; Swagger desligado fora de Development |
| `migrate` | mesma imagem da API | data | Execução única antes da API (`--Operation=Migrate`) |
| `database` | `mariadb:11.8` | data (interna, sem saída) | Nenhuma; volume `organizali_data` |

A API roda a rotina diária do horizonte (D23) com trava no banco, segura com mais de uma réplica. Logs em JSON com método, rota, status, tempo e correlação; nunca corpo, consulta, valores ou segredos.

## Primeira instalação

1. Copie `deploy/.env.production.example` para `deploy/.env.production` e preencha: domínio (`ORGANIZALI_SITE_ADDRESS`, `ORGANIZALI_PUBLIC_ORIGIN`), e-mail do certificado (`ORGANIZALI_TLS`), senhas do banco, chave JWT com 32+ caracteres e senha de backup. O arquivo é ignorado pelo Git.
2. Gere as imagens da versão: `deploy/publish.sh` (tag = commit atual e `latest`). Com `ORGANIZALI_REGISTRY=registro/usuario/` as imagens também são enviadas ao registro.
3. Informe a versão no `.env.production` (`ORGANIZALI_VERSION=<commit>`) e suba: `docker compose -f deploy/compose.production.yml --env-file deploy/.env.production up -d`.
4. Crie a casa piloto (operador, nunca HTTP):
   `docker compose -f deploy/compose.production.yml --env-file deploy/.env.production run --rm --no-deps -e Pilot__Key=<uuid> -e "Pilot__HouseholdName=<casa>" -e "Pilot__OwnerName=<titular>" -e Pilot__Email=<e-mail> -e "Pilot__Password=<senha>" -e Pilot__Actor=<operador> migrate --Operation=ProvisionHousehold`
   e ative com `--Operation=ChangeSubscriptionStatus` (`Pilot__HouseholdCode`, `Pilot__Status=Active`, `Pilot__Reason`). Repetir o provisionamento com a mesma chave não duplica a casa.

## Cadastros iniciais da casa

`deploy/seed-catalogs.py` carrega pela API, com o login do titular, um conjunto pronto de categorias com subcategorias, tipos de renda, credores, formas de pagamento e tipos de despesa parametrizados (campos e categoria padrão). Compara pelo nome e só cria o que falta; nunca apaga nem inativa. Sem `--apply` apenas lista o que faltaria.

```bash
ORGANIZALI_ORIGIN=https://financas.exemplo.com.br ORGANIZALI_EMAIL=<titular> ORGANIZALI_PASSWORD=<senha> \
  python -I deploy/seed-catalogs.py --apply
```

Na rede local com certificado interno, acrescente `ORGANIZALI_INSECURE=1`. Carregado na casa piloto em 13/10/2026.

O mesmo conjunto está embutido no sistema: uma casa nova recebe o catálogo ao ser provisionada, o titular pode carregá-lo pelo botão **Carregar cadastros padrão** e o operador pode rodar `... run --rm --no-deps -e Pilot__HouseholdCode=<código> -e Pilot__Actor=<operador> migrate --Operation=SeedCatalog`.

## Comprovantes

Os arquivos anexados ficam no volume `organizali_attachments` (montado em `/app/attachments` na API). `deploy/backup.sh` gera um `.tar.gz` desse volume ao lado do dump; restaure com `docker run --rm -v organizali_organizali_attachments:/data alpine sh -c 'cd /data && tar -xzf -' < arquivo.tar.gz` (decifre antes com `openssl enc -d` quando houver senha).

## Servidor da rede local

`deploy/compose.lan.yml` e `deploy/Caddyfile.lan` substituem o Caddy público por certificado interno (`local_certs`) e redirecionamento manual da porta HTTP: `docker compose -f deploy/compose.production.yml -f deploy/compose.lan.yml --env-file deploy/.env.production up -d`. Cada aparelho instala a raiz uma vez (`http://<IP>:<porta HTTP>/raiz-organizali.crt`). Os contêineres usam `restart: unless-stopped`: voltam sozinhos após reinício do servidor, mas **não** depois de um `docker compose stop`/`down`; nesse caso, repita o `up -d`.

## Atualização

Backup → `deploy/publish.sh` na nova versão → trocar `ORGANIZALI_VERSION` → `up -d` (o `migrate` aplica as migrations uma vez, com trava, antes da API) → conferir `https://<domínio>/api/health` → jornada de fumaça (entrar, painel, uma despesa). DDL do MariaDB não é transacional: se a migration falhar, restaure o backup ou corrija para a frente; não repita às cegas. Voltar só a imagem exige confirmar que o schema anterior é compatível.

## Backup e restauração

- Backup: `deploy/backup.sh` gera `deploy/backups/organizali-<UTC>.sql.gz.enc` (dump `--single-transaction`, compactado, cifrado com AES-256 se houver `ORGANIZALI_BACKUP_PASSPHRASE`) e confere que o arquivo abre. Agende diariamente (cron ou Agendador de Tarefas) e copie para fora do servidor. Meta proposta: RPO 24 h, RTO 4 h (RNF10).
- Restauração: `deploy/restore.sh --yes <arquivo>` confere o arquivo antes de apagar qualquer coisa, para API e frontend, recria o banco, importa e religa. Depois, confira saldos e quantidade de lançamentos.
- Ensaio validado: conta criada → backup cifrado → nova conta → restauração → apenas a primeira conta, com o mesmo saldo.

## Recuperação de senha

Com `ORGANIZALI_SMTP_HOST`, `ORGANIZALI_SMTP_FROM` e `ORGANIZALI_PUBLIC_ORIGIN`, a tela “Esqueci minha senha” envia por SMTP um link de uso único válido por 15 minutos (token no fragmento da URL). Sem provedor, a tela informa que o recurso está indisponível. Testes usam servidor SMTP falso em loopback; nenhum e-mail real foi enviado.

## Rotina manual e diagnóstico

- Avançar o horizonte na hora: `... run --rm --no-deps migrate --Operation=RollHorizon` (idempotente; se outra instância estiver rodando, responde `Skipped`).
- Logs: `docker compose -f deploy/compose.production.yml --env-file deploy/.env.production logs -f api`.
- Limites: 600 requisições/min por sessão (`Jobs__ApiRequestsPerMinute`) e 10 tentativas de login/min por endereço; o endereço real vem do proxy (`X-Forwarded-For`) e só é aceito da rede interna.
- Rotação da chave JWT: trocar `ORGANIZALI_JWT_KEY` e reiniciar a API encerra todas as sessões.
