# M1.2 — Persistência e transações

Estado: concluído. Usuário autorizou os testes com gravação exclusivamente no container descartável. Execução final: 22 testes aprovados, zero falhas e zero ignorados. Migration, commit, rollback, Id gerado, exclusão lógica e concorrência exercitados no MariaDB 11.8. Push somente ao final.

## Implementado

- Conexão MySql.Data, registro DI e Unit of Work conforme RevendaPRO.
- Tenant com nome, identidade e auditoria Foundation. É a raiz da casa, de escopo de plataforma; ativação, assinatura e usuários continuam em M2.
- Repository e query object com busca pelo Id da casa e filtro IsActive.
- Contexto EF exclusivo de schema, EntityMap Foundation e migration InitialTenant.
- Factory de design offline aponta para porta 1; não usa conexão corporativa nem banco local existente.
- SchemaMigrator adaptado da referência: lock próprio do Organizali; falha ao não adquirir o lock, em vez de continuar sem exclusividade.
- Nenhuma migration automática no startup da API nesta etapa.

Versões novas no manifesto são as do RevendaPRO: EF Core 10.0.5, MySql.EntityFrameworkCore 10.0.1, MySql.Data 9.6.0, Extensions.Options.ConfigurationExtensions 10.0.11 e Testcontainers.MariaDb 4.14.0. Geração offline executada com a ferramenta dotnet-ef 10.0.11 já instalada, sem atualizar pacote.

## Teste preparado para aprovação

Arquivo: tests/PainelGestao.OrganizaliFinancas.Tests/Integration/PersistenceTests.cs.

O teste cria um container novo mariadb:11.8 via Testcontainers, banco organizali_tests e senha aleatória em memória. Não aceita conexão externa nem monta dados de bancos existentes. O container é descartado ao encerrar.

Operações previstas:

1. Aplicar a migration inicial com dois migradores concorrentes e verificar reaplicação sem alterações.
2. Inserir casas sintéticas e verificar buffer, commit, consulta por Id e exclusão lógica.
3. Provocar duplicidade de Code para verificar rollback integral e descarte do buffer.
4. Executar uma instrução dependente do Id gerado no mesmo commit, resolvendo-o por Code.
5. Concorrer duas inserções do mesmo Code e verificar que apenas uma persiste.

Não há pagamento real, envio de mensagem ou banco de produção. A execução padrão ignora esse teste; somente ORGANIZALI_DATABASE_TESTS=1 o habilita, após autorização.

## Validação sem escrita em banco

Build e testes de arquitetura/validação/modelo executados. Migration gerada offline. Primeira execução: 21 aprovados e integração ignorada. Após autorização, execução com ORGANIZALI_DATABASE_TESTS=1: 22 aprovados, incluindo integração, em TestResults/m1-2.trx. Container descartado ao encerrar.

## Limites registrados

- Foundation não preenche o Id na entidade após inserção em buffer. Isso está explícito na prova; não usar dois commits para obter o Id em uma operação financeira.
- A transação financeira completa será validada quando houver pagamentos e alocações; a prova atual usa Tenant.
- A busca de Tenant não comprova isolamento de tabelas financeiras. Repositories financeiros futuros exigem contexto de casa em cada operação.
- MariaDB pode executar commit implícito em DDL; migration não tem a atomicidade de uma transação de negócio.
- O runner herdado separa instruções simples por ponto e vírgula. SQL procedural ou literais com esse caractere exigem revisão antes da inclusão.
- A segunda migration real terá sua evolução incremental testada quando o próximo schema for implementado.
