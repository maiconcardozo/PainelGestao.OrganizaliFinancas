# Operação e configuração planejadas

Documento de planejamento. Não existem Dockerfiles, Compose, variáveis carregadas ou pipeline implementados no Organizali nesta etapa.

## Ambientes

Desenvolvimento, teste efêmero, homologação e produção com bancos, buckets e segredos diferentes. Imagens derivadas de Dockerfiles versionados e identificadas por commit/digest. API e frontend não root; banco/storage sem exposição pública direta; HTTPS no proxy reverso. Portas locais serão escolhidas sem colidir com os projetos existentes.

## Configuração proposta

| Grupo de configuração futuro | Conteúdo | Proteção |
|---|---|---|
| ConnectionStrings / Database | Host, banco, usuário e segredo próprios | Secret local/CI; nenhum valor corporativo |
| Jwt / Session | Emissor, audiência, chaves, validade e cookies | Segredo externo; rotação e revogação |
| Cors / Frontend | Origins e URL de retorno | Allowlist por ambiente |
| Storage | Endpoint S3, bucket, região e credenciais | Bucket privado e acesso mínimo |
| Mail | Provedor, remetente, token e habilitação | Transporte falso em teste |
| Jobs | Horizonte, frequência, lease e retentativas | Defaults documentados e limites |
| ApiDocs | Habilitação de OpenAPI | Desabilitado/restrito fora de desenvolvimento |
| Subscription | Ativação manual e limites do piloto | Alteração somente por operador |

Nomes exatos das variáveis e precedência serão definidos com as Options reais no M1. Versionar apenas exemplos `.env.example`, `.env.Development.example`, `.env.Staging.example`; ignorar arquivos com valores reais. Não fornecer comandos fictícios de execução antes de criá-los e validá-los.

## Publicação futura

Build/teste → imagem versionada → backup verificado → migration controlada em processo único → atualização da aplicação → readiness → jornada de fumaça → observação. Não rodar migration concorrente automaticamente em cada réplica. Documentar compatibilidade da versão anterior antes de permitir rollback só de imagem.

DDL pode não reverter transacionalmente no MariaDB; uma falha requer avaliação e restauração/forward fix controlado, não repetição cega. Backup antes da mudança deve ter restauração ensaiada. Esta tarefa não autoriza publicação ou contratação de infraestrutura.

## Backup, retenção e restauração

Proposta: backup diário criptografado de banco/arquivos e teste mensal de restauração; metas RPO 24 h, RTO 4 h, retenção a confirmar. Backup deve abranger chaves de criptografia necessárias e metadados de anexos. Conferir saldos e quantidade de movimentos após restauração. Acesso e exportação de backup são restritos e auditados.

## Observabilidade

Health live para processo, ready para banco/dependências obrigatórias. Logs sem salários, descrições financeiras, tokens ou payload completo. Correlation id, tempo de execução, status, erro técnico sanitizado e identificadores mínimos. Alertar backlog de job, falhas repetidas, erro de backup e indisponibilidade. Canal externo a escolher, sem serviço da empresa.

## Piloto manual

Operador provisiona assinatura/casa, define titular e entrega convite por fluxo próprio após transporte escolhido. Estados propostos: PendingActivation, Active, Suspended e Closed. Política de leitura/exportação durante suspensão e período de encerramento precisa de decisão antes do piloto externo; suspensão não apaga dados.

## Custos e dependências

Dimensionar compute, MariaDB, armazenamento/backup, domínio/TLS, e-mail e licenças após escolha da hospedagem. Nenhum preço é presumido. Foundation.Base foi autorizado, mas feed/restore independente e requisitos transitivos continuam sendo validação técnica. Licenças de bibliotecas devem ser registradas antes da distribuição comercial, sem atualização automática de pacotes.
