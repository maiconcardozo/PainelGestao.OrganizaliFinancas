# Contexto para continuidade

Desenvolvimento autorizado pelo usuário após os marcos. Iniciar por M1.1. Backend em Minimal API e frontend Next.js separado; Swagger conforme swagger-global, com identidade Organizali. A decisão substitui controllers sem alterar as demais camadas; ver ADR-0002.

Início organizado em M1.1–M3.4 em plans/development-milestones.md. D19/D20 confirmados: português, America/Sao_Paulo configurável por casa; cada login pertence a uma casa no piloto. Modelo de usuário segue User : TenantEntity do RevendaPRO, sem identidade global/múltiplos vínculos.

Organizali Finanças é projeto particular de SaaS doméstico. Usuário quer escopo completo antes do desenvolvimento e exige perguntas quando houver dúvidas. RevendaPRO é referência técnica principal; Arquitetura.Global orienta camadas; CP Comunica foi apenas comparação inicial.

Confirmado: uma casa por assinatura; dados compartilhados entre integrantes; permissões de edição; piloto manual; Foundation.Base autorizado; BRL; renda com bruto opcional, líquido previsto e recebido real; rateio por valor ou percentual; dashboard moderno com saldo, fluxo de caixa, orçamento e alertas. Wiki recusada. Não há chamado Azure/Redmine associado.

Estado: M1.1 publicado e M1.2 concluído. Usuário autorizou escrita de teste somente em MariaDB descartável; 22 testes aprovados. Ver testing/m1-2-persistence.md. M1.3 implementado com 29 testes aprovados, publish e assets HTTP conferidos; inspeção visual pendente porque não há navegador conectado. M1.4 concluído tecnicamente: frontend compila, lint aprovado, imagens Docker e comunicação web/API verificadas sem banco persistente. Visual RevendaPRO confirmado. Próximo M2.1; ver testing/m1-4-web.md. Commit por marco concluído; push somente ao final.

Skills usadas: clean-ddd-hexagonal, clean-code e frontend-design. Usar skills adicionais só quando correspondam à etapa real. Referências técnicas e versões em architecture/references.md e overview.md. Dúvidas bloqueiam apenas a implementação dependente, não a documentação independente.

Arquivos de entrega em C:/Codex/Upload; documentação de desenvolvimento também permanece neste repositório. A pasta .vs preexistia e não foi alterada.


Continuação autorizada pelo usuário para todos os marcos, mantendo commits por marco e push ao final. M2.1 implementado: provisionamento administrativo, assinatura e histórico; 29 testes aprovados incluindo migration incremental e isolamento por FK. M2.2 é o próximo. Perguntas sobre matriz de permissões e corte de cartão enviadas; sem resposta ainda, não fazer seed nem presumir decisão. Visual RevendaPRO para Organizali confirmado (D15).

M2.2 implementado e validado: 29 testes .NET com MariaDB descartável e teste HTTP do Next aprovados. JWT, refresh, revogação e recuperação simulada; documentação em testing/m2-2-authentication.md. APP_ORIGIN exata protege mutações web atrás de proxy. Matriz de perfis ainda sem resposta; avançar apenas nas partes independentes da interface, sem declarar M2.3/M2.4 completos.

M2.4 parcialmente entregue: login, shell responsivo, Minha conta, renovação explícita e logout; build/lint/teste HTTP aprovados. Ver testing/m2-4-web-access.md. D15 confirmado no registro. M2.3 depende da resposta D21 (matriz enviada); não presumir poderes nem marcar M2.4/M3 concluídos. Não houve push. Próximo passo é aplicar matriz confirmada e continuar M2.3/M3; M6 também aguarda corte D13, e operação externa aguarda D16/D17.

D21 aprovada na continuação após a matriz apresentada. M2.3 implementado: Role/UserRole por casa, titulares/editor/leitor, convites manuais de 48h, menu servidor, bloqueio/revogação e proteção concorrente do último titular. 29 testes .NET e testes Next aprovados. Ver testing/m2-3-members.md. M2.4 integrado com permissões; visual ainda pendente. Próximo M3.1. Sem push.

M3.1 implementado e testado: moradores sem login, inativação, configurações pt-BR/BRL/fuso IANA/data inicial, controle de versão e autoria. 29 testes .NET em banco descartável, build/lint/teste HTTP web aprovados. Ver testing/m3-1-households.md. Próximo M3.2; sem push.

M3.2 implementado: Category/IncomeType/Creditor/PaymentMethod em tabelas próprias, hierarquia por casa e versões imutáveis CatalogRevision. 29 testes .NET com novas asserções, build/lint/teste HTTP Next aprovados. Ver testing/m3-2-catalogs.md. Próximo M3.3 (tipos de despesa/campos parametrizados). Sem push.
