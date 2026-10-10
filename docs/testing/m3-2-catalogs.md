# M3.2 — Catálogos financeiros

Categorias/subcategorias, tipos de renda, credores e formas de pagamento implementados em tabelas próprias, isoladas por casa. Titular/editor mantêm os cadastros; leitor consulta. Nenhuma lista de credores ou tipos de renda está fixa no código.

## Padrão e invariantes

Entidades seguem ExpenseType do RevendaPRO: TenantEntity, factory, setters privados, métodos de domínio e auditoria. ICatalogItem reúne somente o contrato comum desses quatro cadastros; não existe tabela genérica substituindo as entidades. SQL escolhe nomes de tabelas por enum fechado, nunca por texto de requisição. As camadas e versões permanecem as aprovadas.

Categorias têm FK composta para o pai na mesma casa. Validação impede ciclos, categoria ativa sob pai inativo e inativação de pai com filhos ativos. Não há cascata silenciosa. Registros são inativados, preservando Code e referências.

Cada criação/alteração grava CatalogRevision no mesmo commit, com rótulo, versão, categoria superior, estado, casa e autor. Isso preserva o significado anterior quando os lançamentos financeiros passarem a referenciar uma versão. Não há endpoint de edição/exclusão de revisões. Os lançamentos financeiros ainda não foram implementados.

Frontend tem abas dos quatro catálogos, formulários, categoria superior, estado e mensagens de conflito. Menu vem da sessão; API valida permissões novamente. Migration FinancialCatalogs adiciona as tabelas.

## Validação

29 testes .NET aprovados em MariaDB descartável (TestResults/m3-2.trx), incluindo CatalogAssertions: CRUD/inativação dos quatro catálogos, acesso cruzado recusado, leitor bloqueado para escrita, versão antiga recusada, nomes anteriores preservados, ciclo e inativação inválida recusados. Build/lint Next e teste HTTP web também aprovados.

Sem banco persistente, envio externo, atualização NuGet ou push. Revisão visual permanece pendente.
