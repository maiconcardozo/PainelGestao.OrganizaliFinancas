# Marcos de início do desenvolvimento

Data: 10/10/2026. Fundação, acesso e cadastros implementados até M3.4; contas e rendas entregues no M4 ([entrega](../deliveries/M4.md)) despesas no M5 ([entrega](../deliveries/M5.md)) cartões no M6 ([entrega](../deliveries/M6.md)) painel/orçamento/alertas no M7 ([entrega](../deliveries/M7.md)) e produção com Docker no M8 ([entrega](../deliveries/M8.md)). Escolhas D16/D17, aceite da família piloto e complementos (M9–M10) pendentes. [Estado completo](../status/PROJECT_STATUS.md) · [Entregas](../deliveries/README.md).

Este detalhamento desdobra M1–M3 do [plano geral](implementation.md), preservando seus identificadores e dependências. O primeiro objetivo demonstrável é entrar em uma casa isolada, navegar com permissões e manter seus cadastros. Indicadores financeiros reais entram após receitas, despesas e cartões.

## 1. Referências obrigatórias

| Decisão | Fonte a seguir | Aplicação |
|---|---|---|
| Camadas e dependências | Arquitetura.Global/docs/standards/architecture.md e RevendaPRO/docs/architecture/PADRAO-GLOBAL.md | Cinco projetos, sem dependência Application → Infrastructure |
| Código e pastas por contexto | RevendaPRO/docs/architecture/PADRAO-GLOBAL.md e src/ | Inglês no código, português na interface, Application por contexto |
| Pacotes | RevendaPRO/Directory.Packages.props e frontend/package.json/lockfile | Mesmas versões aprovadas; sem atualização automática |
| Base de entidade | RevendaPRO/src/RevendaPro.Domain/Entities/TenantEntity.cs | Foundation Entity; Id interno, Code público e IdTenant |
| Comportamento de entidade | RevendaPRO/src/RevendaPro.Domain/Entities/User.cs | Factory, setters privados e métodos de domínio |
| Mapeamentos | RevendaPRO/src/RevendaPro.Infrastructure/Persistence/Mappings/UserMaps.cs | EntityMap, nomes iguais no código/banco e FKs com prefixo Id |
| Transações | RevendaPRO/src/RevendaPro.Infrastructure/UnitOfWork/RevendaProUnitOfWork.cs | Foundation DapperUnitOfWork e contrato no Domain |
| Schema MariaDB | RevendaPRO/src/RevendaPro.Infrastructure/Database/SchemaMigrator.cs | Conferir mecanismo existente e validar compatibilidade |
| Validação | RevendaPRO/src/RevendaPro.Application/Behaviors/ValidationBehavior.cs | Pipeline FluentValidation antes do handler |
| Respostas HTTP | RevendaPRO/src/RevendaPro.Api/Contracts/SuccessDetails.cs e Middleware/ExceptionHandlingMiddleware.cs | SuccessDetails e ProblemDetails |
| Permissões | RevendaPRO/src/RevendaPro.Api/Authorization/RequireScreenAttribute.cs | Mesmo conceito de guarda no servidor; operações específicas conforme requisitos |
| Sessão frontend | RevendaPRO/frontend/lib/session.ts e lib/server.ts | Cookie HttpOnly, sessão no servidor e proxy autenticado |
| Testes de arquitetura | RevendaPRO/tests/RevendaPro.Tests/Unit/ArchitectureTests.cs | Usar regras reais de dependência; não mover arquivos sem motivo |
| README | Authentication fornecido pelo usuário e skill readme-projetos | Preservar apresentação aprovada e atualizar estado real |

Raiz das referências: `C:/Users/maicon.cardozo/source/repos/`. Caminhos conferidos nesta sessão; os arquivos devem ser lidos integralmente quando a etapa for implementada. Um padrão não torna copiáveis dados, credenciais ou módulos específicos de revenda.

Skills: **clean-ddd-hexagonal** nas fronteiras/invariantes; **clean-code** na implementação e revisão; **frontend-design** nas telas; **readme-projetos** na documentação de entrada. Estas skills complementam as convenções das referências, sem substituí-las por outra arquitetura.

## 2. Quadro de execução inicial

**M1–M3 implementados no escopo documentado.** Consulte as entregas para limitações, incluindo recuperação externa. Um commit por etapa; push somente ao final.

| Etapa | Entrega demonstrável | Dependência | Aceite principal |
|---|---|---|---|
| M1.1 | Solução e dependências restauráveis | Documentação base | Build das cinco camadas e testes de dependência |
| M1.2 | Persistência técnica validada | M1.1 | Migration e transação Dapper verificadas em ambiente isolado autorizado |
| M1.3 | Base HTTP e configuração | M1.1 | Respostas, validação, erros e health checks consistentes |
| M1.4 | Base Next.js e ambiente local | M1.1/M1.3 | Build/lint, conexão com API local e instruções reproduzíveis |
| M2.1 | Casa, titular e ativação manual | M1.2/M1.3 | Provisionamento idempotente e isolamento entre duas casas |
| M2.2 | Autenticação e sessão | M2.1/M1.4 | Login, logout, refresh e revogação verificados |
| M2.3 | Perfis, integrantes e permissões | M2.2 | Menu/rota/API obedecem permissões; revogação imediata conforme contrato |
| M2.4 | Interface autenticada utilizável | M2.3 | Login → casa → navegação → cadastros, em desktop e celular |
| M3.1 | Pessoas e configuração da casa | M2.4 | Morador sem login e manutenção autorizada |
| M3.2 | Catálogos financeiros | M3.1 | Tipos e categorias mantidos pela casa sem deploy |
| M3.3 | Tipos parametrizados e campos | M3.2 | Tipo Água com campo obrigatório validado pela API e formulário |
| M3.4 | Revisão da primeira entrega funcional | M3.3 | Jornada completa de acesso/cadastro e evidências sem falhas críticas |

## 3. M1 — Fundação técnica

### M1.1 — Solução e convenções

- [x] Ler as referências completas relevantes e registrar revisão/pacotes utilizados.
- [x] Criar solution com Api, Application, Domain, Infrastructure e Shared, mais o projeto de testes, conforme RevendaPRO.
- [x] Centralizar versões e build conforme os arquivos reais de referência; habilitar nullable/analyzers.
- [x] Validar restore do Foundation.Base autorizado, sem expor credenciais de feed.
- [x] Criar apenas pastas utilizadas; não gerar todos os módulos financeiros vazios.
- [x] Aplicar testes de dependência equivalentes aos do RevendaPRO.

**Saída:** solução compilando e referências entre projetos corretas. **Evidência:** comandos executados, versões e resultado dos testes. Se restore/compatibilidade falhar, diagnosticar e perguntar antes de substituir versão ou biblioteca.

### M1.2 — Persistência e transação

- [x] Usar Entity/TenantEntity e contratos Foundation conforme a referência.
- [x] Configurar conexão, query objects, repositories e Unit of Work nas pastas previstas.
- [x] Preparar contexto EF/mapeamentos exclusivamente para schema.
- [x] Validar geração de migration e comportamento do SchemaMigrator no MariaDB escolhido.
- [x] Verificar IDs gerados dentro da transação, commit, rollback e concorrência, sem commits intermediários para uma mesma operação.
- [x] Garantir que os contratos usados por casos de uso não permitam acesso financeiro sem contexto de casa.

**Saída:** prova técnica da persistência. Ensaios que gravam dados exigem banco descartável isolado e autorização específica conforme a regra do usuário para testes; não rodar migrations ou escrita em banco existente nesta fase de planejamento. **Evidência:** isolamento do ambiente, scripts/migrations revisados e resultados efetivos quando executados. Não afirmar que o filtro global do EF protege queries Dapper.

### M1.3 — Base da API

- [x] Preparar DI, opções tipadas e validação de configuração seguindo a referência.
- [x] Aplicar endpoints Minimal API finos com IMediator, ValidationBehavior e tratamento centralizado de erros.
- [x] Usar SuccessDetails/ProblemDetails e documentar respostas em OpenAPI.
- [x] Preparar correlação, logs sem dados sensíveis, health live/ready e configuração local segura.
- [x] Conferir resposta de erro e cancelamento de requisição com casos técnicos, sem financeiro fictício.

**Saída:** API técnica executável e contratos consistentes. **Evidência:** startup, health e exemplos reais de validação/erro. Login pertence a M2; M1 não promete autenticação pronta.

### M1.4 — Base web e execução local

- [x] Criar frontend nas versões e organização do RevendaPRO; ler o guia da versão instalada do Next.js.
- [x] Preparar integração HTTP e a estrutura de sessão/proxy a ser completada em M2.
- [x] Configurar exemplos de ambiente, Dockerfiles e Compose próprios a partir dos padrões existentes.
- [x] Conferir portas/volumes independentes, sem reutilizar bancos ou secrets das referências.
- [x] Documentar apenas comandos de restore/build/start efetivamente validados.

**Saída:** frontend compila, API responde e instruções locais são reproduzíveis. Não fabricar dados de dashboard para declarar o módulo financeiro pronto. Aparência final aguarda decisão visual.

## 4. M2 — Acesso e casa

### M2.1 — Casa e piloto manual

- [x] Aplicar D20 confirmado: User : TenantEntity; cada login pertence a uma casa no piloto e a casa pode ter vários usuários.
- [x] Criar cadastro de casa/assinatura 1:1 e titular, usando as abstrações existentes.
- [x] Implementar ativação manual, estados e histórico de alteração.
- [x] Tratar provisionamento repetido sem duplicar a casa ou o titular.
- [x] Validar duas casas e impedir referências de uma casa à outra.

**Aceite:** operador provisiona; titular só alcança sua casa. Operador de plataforma não ganha acesso automático às movimentações domésticas. Política detalhada de suspensão é definida antes de usuários externos.

### M2.2 — Login e sessão

- [x] Implementar autenticação/hash/token conforme RevendaPRO e Foundation.
- [x] Aplicar cookies protegidos, proxy servidor e proteção de mutações.
- [x] Verificar logout, expiração, refresh, bloqueio e revogação.
- [x] Preparar recuperação com transporte falso; provedor real só após escolha em D16.

**Aceite:** autenticar, renovar e encerrar sessão; token não fica acessível ao JavaScript da página. Testes não enviam mensagens reais.

### M2.3 — Perfis e integrantes

- [x] Revisar com o usuário a matriz de consulta, cadastro, liquidação, estorno, exportação e administração antes do seed.
- [x] Implementar perfis/permissões, convite e revogação seguindo os mecanismos da referência.
- [x] Entregar menu filtrado do servidor; aplicar a mesma autorização em rota e API.
- [x] Impedir exclusão do último titular e atribuição indevida de permissões de plataforma.

**Aceite:** leitor consulta, editor altera somente o permitido, integrante revogado perde acesso. Não criar privacidade por lançamento: dados compartilhados já foram confirmados.

### M2.4 — Interface autenticada

- [x] Confirmar nome/identidade visual em D15 antes de consolidar tokens e componentes.
- [x] Reutilizar o padrão de organização do shell do RevendaPRO, adaptado ao Organizali.
- [x] Implementar login, navegação comum, estado ativo, navegação mobile, mensagens de erro e estados vazios. Menu por permissões depende de M2.3.
- [ ] Conferir a jornada com dados de teste identificados e permissões reais.

**Aceite demonstrável:** login → casa → navegação autorizada → perfil/usuário → logout. O dashboard financeiro permanece identificado como etapa M7.

## 5. M3 — Cadastros que sustentam o financeiro

### M3.1 — Pessoas e configuração

- [x] Separar pessoa/morador do usuário que acessa o sistema.
- [x] Cadastrar, editar e inativar moradores preservando histórico.
- [x] Configurar preferências conforme D19: português e America/Sao_Paulo inicial, configurável por casa.
- [x] Entregar cadastro com autorização e autoria obtida da sessão.

### M3.2 — Catálogos

- [x] Implementar categorias/subcategorias, tipos de renda, credores e formas de pagamento.
- [x] Aplicar escopo por casa, validação e inativação.
- [x] Garantir que alterações de rótulo não mudem o significado financeiro do histórico.

### M3.3 — Parametrização

- [x] Criar tipos de despesa/dívida que selecionem os comportamentos previstos no escopo.
- [x] Definir campos tipados, validação, obrigatoriedade e versão.
- [x] Gerar formulário a partir dessas definições com validação equivalente no servidor.
- [x] Demonstrar cadastro de Água/Luz e um tipo novo sem alteração de código.

**Limite:** M3 prepara os tipos; recorrência, parcela, liquidação e cartão são implementados em M5/M6. Não declarar esses comportamentos entregues só porque aparecem em um seletor.

### M3.4 — Demonstração integrada

Jornada: ativar casa → entrar como titular → cadastrar morador sem login → convidar editor → manter categorias/tipos → validar campo obrigatório → entrar como leitor e confirmar bloqueio de edição. Revisar também acesso cruzado entre casas e registros de auditoria.

**Aceite:** módulos M1–M3 integrados, documentação atualizada, demonstração reproduzível e pendências explicitadas. Uma despesa completa só será demonstrada após o núcleo financeiro.

## 6. Continuação até o piloto

| Marco | Entrega seguinte | Demonstração de aceite |
|---|---|---|
| M4 | Contas, renda prevista e recebimento real | Previsão 4.000, recebido 3.800, residual 200; transferência sem renda duplicada |
| M5 | Obrigações, recorrência, parcelas, pagamentos e rateio | 100 em 3 parcelas com soma exata; rateio 50/50 com um pagador |
| M6 | Cartões e faturas | Compra de 300 e pagamento de 300 sem despesa de 600 |
| M7 | Dashboard moderno, fluxo, orçamento e alertas | Saldo real/projetado rastreável, categoria excedida e primeira data de déficit |
| M8 | Piloto completo | Jornada da família, segurança, operação e recuperação verificadas |

M9/M10 permanecem no plano geral: complementos e comercialização posterior. O detalhamento futuro será feito usando o modelo real desenvolvido, sem reinventar a base.

## 7. Decisões antes das etapas dependentes

| Decisão | Momento | Tratamento |
|---|---|---|
| Login pertence a uma casa | M2.1 | D20 confirmado; aplicar User : TenantEntity do RevendaPRO |
| Matriz final de perfis | M2.3 | D21 aprovada e implementada; ver testing/m2-3-members.md |
| Identidade visual D15 | M2.4 | Confirmado: seguir RevendaPRO, marca Organizali e português |
| Idioma/fuso D19 | M3.1 | Confirmado: português e America/Sao_Paulo configurável |
| Fechamento de cartão D13 | M6 | Confirmar regra do dia de corte |
| Hospedagem/e-mail/storage D16 e retenção D17 | M8 ou recurso dependente | Escolher antes de operação externa |

## 8. Registro de conclusão por etapa

Para cada etapa, registrar: estado (não iniciado/em execução/concluído/bloqueado), tarefas entregues, arquivos, referência seguida, comandos/verificações e resultados, pendências e próximo passo. Informar commit/URL quando houver publicação autorizada. Não associar este projeto particular a chamados corporativos.

O detalhamento dos marcos não executa migrations, teste com escrita, envio, contratação ou deploy. Atualizar este arquivo conforme a execução real; checkboxes não representam implementação já realizada.
