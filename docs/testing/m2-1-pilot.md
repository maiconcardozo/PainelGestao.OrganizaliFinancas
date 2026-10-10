# M2.1 — Provisionamento do piloto

Implementado o provisionamento local de Tenant, User : TenantEntity, Subscription 1:1 e SubscriptionEvent. Login e autorização HTTP são a etapa M2.2; o isolamento de navegação ainda não está entregue.

## Operação

Com a conexão própria em OrganizaliFinancas__ConnectionString, a API aceita operações administrativas explícitas:

- --Operation=Migrate: aplica somente migrations.
- --Operation=ProvisionHousehold: lê Pilot__Key (UUID de idempotência), Pilot__HouseholdName, Pilot__OwnerName, Pilot__Email, Pilot__Password e Pilot__Actor.
- --Operation=ChangeSubscriptionStatus: lê Pilot__HouseholdCode, Pilot__Status (Pending, Active ou Suspended), Pilot__Reason e Pilot__Actor.

Não há rota HTTP de operador. Esses comandos encerram o processo após executar e não iniciam o servidor. Não versionar valores reais nem passar senhas na linha de comando; configurar ambiente protegido. Nenhum comando foi executado em banco persistente.

Provisionamento cria assinatura Pending. Ativação é explícita. Repetir a mesma chave não duplica nem altera a senha. Mesma chave com nome/e-mail diferentes gera conflito. E-mail normalizado é único entre logins para manter autenticação inequívoca no modelo de um login por casa.

Argon2 segue PasswordHasherService do RevendaPRO e Foundation.Base, com dependência explícita 1.3.1. Senha inicial exige 12–128 caracteres. Não há seed de papéis; matriz aguarda resposta à pergunta enviada.

## Persistência

Queries ficam em Infrastructure/Queries/Pilot. Provisionamento executa quatro escritas no mesmo commit; IDs são resolvidos por Code dentro da transação. Restrição composta garante que IdOwnerUser pertence ao IdTenant da assinatura. Alteração de estado bloqueia a assinatura com FOR UPDATE e registra evento no mesmo commit. Repetir o estado atual não duplica evento.

Migration PilotAccess adicionada após InitialTenant. Em teste, o schema inicial foi aplicado primeiro e a segunda migration foi aplicada pelos migradores concorrentes; isso valida a evolução incremental real.

## Validação

29 testes aprovados, zero falhas/ignorados (TestResults/m2-1.trx). A suíte de persistência passou a cobrir:

- Dois provisionamentos concorrentes da mesma chave retornam a mesma casa.
- Repetição preserva usuário e hash original.
- Chave reutilizada com payload diferente é recusada.
- Duas casas independentes e vínculo cruzado do titular recusado pela FK.
- Ativação/suspensão registram histórico, sem evento duplicado na repetição.
- Migration incremental, commit, rollback e schema continuam aprovados.

Operador de plataforma não recebe identidade doméstica automaticamente. A política detalhada de suspensão para usuários externos permanece pendente; nesta etapa só existem estado e histórico administrativo.
