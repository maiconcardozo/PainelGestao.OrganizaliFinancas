# Contexto para continuidade

## Estado vigente — 13/10/2026

M1–M8 e UI RevendaPRO concluídos. Piloto rodando na LAN (servidor Windows 192.168.1.7, Docker, https://192.168.1.7:3300, versão 1eb0951; operação por PowerShell Remoting, detalhes em operations/runbook.md). Cadastros iniciais carregados na casa piloto por `deploy/seed-catalogs.py`; tutorial de uso em product/tutorial.md. Em 14/10/2026 entregues os complementos pedidos (Ajuda, catálogo padrão no sistema, primeiros passos, troca de senha e link de nova senha, metas, comprovantes, importação OFX/CSV; ver deliveries/M9-M10-complementos.md). Cartões da família cadastrados pelo titular em 14/10/2026; contas e fontes de renda ainda por cadastrar. Na tarde de 14/10 entregues ativar/desativar e excluir cartão e editar/excluir compra (deliveries/Cartoes-excluir-editar.md, premissa A16); compras de teste do titular excluídas pela API nova. Restante de M9/M10: RF35, RF39, RF40, RF42, RF43, RF45, RF46. D13/D14/D16/D17 abertas. Cada marco exige documento com regras de negócio e atualização de status. [Estado completo](../status/PROJECT_STATUS.md) · [Entregas](../deliveries/README.md). Commit por etapa; sem push intermediário.

## Histórico de execução

Os registros abaixo preservam o estado de cada momento; pendências superadas não substituem o estado vigente acima.

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

M3.3 implementado: ExpenseType/ExpenseTypeVersion, definições de campos tipados serializadas por versão imutável, formulário dinâmico e validação API. 38 testes .NET, build/lint/teste HTTP aprovados; ver testing/m3-3-expense-types.md. Próximo M3.4 revisão integrada: Chrome/Edge executáveis locais detectados, tentar teste headless isolado para validar visual e jornada real sem depender do conector CUA indisponível. Sem push.

M4 implementado: decisões D22–D25 registradas no início do marco (todas as periodicidades, horizonte +12, média do ano civil por meses fechados, recebimento acima da base é um só lançamento com comparativo). 58 testes .NET e jornada Chrome/Next/API/MariaDB aprovados; ver testing/m4-accounts-income.md. Pendente confirmar se a média deve ignorar meses anteriores à data de início da casa. .gitignore igualado ao RevendaPRO (.idea/) e launchSettings.json versionado como no RevendaPRO. Sem push.

M5 implementado em execução autônoma autorizada ("fazer todos os marcos até a publicação do Docker, commit por marco, sem pedir autorização"): despesas avulsas/recorrentes/parceladas, pagamentos parciais com encargos, cancelamento, estorno de pagamento, rateio e acerto. 69 testes .NET e jornada Chrome aprovados; ver testing/m5-expenses.md. Premissas A01–A05 registradas em decisions.md. Próximo M6 (cartões; D13 sem resposta → premissa conservadora com prévia editável). Sem push.

M6 implementado (execução autônoma): cartões sem PAN, ciclos com regra do dia de fechamento por cartão (D13 → premissa A06, padrão próxima fatura), compras e parcelas como itens de fatura, fatura com obrigação própria paga pelo fluxo do M5, estorno da loja e limite estimado. 77 testes .NET e jornada Chrome aprovados; ver testing/m6-cards.md. Premissas A06–A08 em decisions.md. Próximo M7. Sem push.

M7 implementado (execução autônoma): painel com projeção de 60 dias e primeiro déficit, fluxo diário/mensal, orçamento por categoria com hierarquia e faixas, central de alertas deduplicada por chave/impressão, agenda e análise filtrada. 85 testes .NET e jornada Chrome aprovados; ver testing/m7-planning.md. Premissas A09–A12. Próximo M8 (produção e Docker). Sem push.

M8 implementado (execução autônoma): deploy/compose.production.yml (Caddy HTTPS, banco em rede interna, migrate único), publish.sh (tag do commit), backup cifrado/restore ensaiados, rotina diária do horizonte com GET_LOCK (conexão mantida aberta), recuperação por SMTP configurável, CSP e cabeçalhos, limites por sessão e X-Forwarded-For confiável, logs JSON. 89 testes .NET, testes Next/Chrome e pilha descartável aprovados; ver testing/m8-production.md e operations/runbook.md. Premissas A13–A15. Nada publicado em registro, nenhum deploy, nenhum push. Pendentes do usuário: D13 confirmação, D14, D16, D17, revisão A01–A15, aceite da família piloto; M9/M10 não iniciados.
