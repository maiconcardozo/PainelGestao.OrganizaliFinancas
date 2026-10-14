# Entrega — Complementos da V1 (parte de M9/M10) e apoio ao piloto

Data: 14/10/2026. Estado: implementado e validado no escopo abaixo. Pedido do usuário em 13/10/2026: "faça todos tópicos" sobre as cinco sugestões apresentadas após a carga dos cadastros iniciais.

## Objetivo e funcionalidades

1. **Ajuda dentro do sistema** (`/help`, item "Ajuda" do menu para todos os perfis): o tutorial de uso (`docs/product/tutorial.md`) renderizado no painel, com navegação por seção. O texto é copiado para `frontend/content/tutorial.ts` por `node frontend/scripts/sync-tutorial.mjs`, porque a pasta `docs` não entra na imagem do frontend.
2. **Catálogo padrão pelo próprio sistema**: `DefaultCatalogSeeder` (Application/Catalogs) com categorias e subcategorias, tipos de renda, credores, formas de pagamento e tipos de despesa parametrizados. Executado automaticamente ao provisionar uma casa nova (`ProvisionHousehold`), pela operação `--Operation=SeedCatalog` (`Pilot__HouseholdCode`) e por `POST /api/catalogs/defaults` (titular ou editor), que a tela usa no botão **Carregar cadastros padrão**. Idempotente por nome; nunca altera ou inativa o que existe.
3. **Primeiros passos** em Minha casa: quadro com Moradores → Contas → Cadastros financeiros e tipos de despesa → Fontes de renda → Cartões (opcional) → Orçamento (opcional), com progresso, botão **Abrir** e **Carregar cadastros padrão**; some quando tudo estiver feito.
4. **Senha sem e-mail**: `POST /api/auth/change-password` (senha atual + nova, encerra todas as sessões) e botão **Trocar senha** em Minha conta; `POST /api/members/{code}/recovery` (titular) gera um link de nova senha de uso único válido por 24 h, exibido em Integrantes pelo ícone de chave.
5. **Complementos**:
   - **Metas de reserva** (RF38, `/goals`): nome, valor alvo, prazo opcional, conta vinculada e observação. Progresso = saldo da conta vinculada; a tela mostra guardado, restante, percentual, meses até o prazo e quanto guardar por mês. Reservar não cria renda nem despesa.
   - **Comprovantes** (RF34, `/api/attachments`): PDF ou imagem de até 10 MB, até 20 por lançamento, em despesas (Detalhes), pagamentos e recebimentos (ícone de clipe). Arquivos num volume privado da API (`/app/attachments`), chave `{casa}/{código}`; download só pela casa dona.
   - **Importação de extrato** (RF44, `/imports`): OFX (SGML/XML) e CSV com prévia, detecção de duplicidade (linhas já importadas e movimentos com a mesma data e valor), sugestão por linha (renda, despesa paga ou pagamento de despesa em aberto quando valor e data batem) e confirmação tudo-ou-nada com `Idempotency-Key`. Histórico das importações.

## Regras de negócio

| Regra | Comportamento |
|---|---|
| Troca de senha | Exige a senha atual; nova com 12–128 caracteres e diferente da atual; `SessionVersion` avança e todas as sessões (inclusive a atual) são encerradas. O proxy Next apaga os cookies no sucesso. |
| Link de nova senha | Só titular, só para integrante não bloqueado; token no fragmento da URL, hash no banco, 24 h, uso único; mesma tela `/reset-password` do e-mail. |
| Catálogo padrão | Três transações curtas: catálogos planos e categorias-pai, depois subcategorias (precisam do `Id` do pai), depois tipos de despesa (precisam do `Id` da categoria). Comparação por nome sem diferenciar maiúsculas. |
| Meta | Valor alvo positivo em centavos; conta ativa ao criar; prazo não pode ser anterior a hoje ao criar; guardado = saldo da conta (nunca negativo); percentual limitado a 100; meses restantes contam meses inteiros até o prazo; mensal necessário = restante ÷ meses (restante inteiro se o prazo já chegou). |
| Comprovante | Tipos `application/pdf`, `image/jpeg`, `image/png`, `image/webp`, `image/heic` (jpg/pjpeg/heif normalizados); o lançamento precisa pertencer à casa; o arquivo é gravado antes da linha e apagado se a linha falhar; remover é exclusão lógica mais remoção do arquivo. |
| Importação — prévia | Nada é gravado. Impressão digital = conta + data + valor + id do banco (ou descrição normalizada). Linha com impressão já aplicada ou com movimento igual na conta vem como **Ignorar**. Débito que bate com saldo em aberto de uma despesa (±15 dias) sugere **Pagar despesa existente**. |
| Importação — confirmação | Tudo ou nada numa transação com trava da casa. Erros por linha (`Lines[i]`): data futura, linha repetida, já importada, renda exige morador ativo e valor positivo, despesa paga exige valor negativo, pagamento não pode superar o saldo em aberto (considerando outras linhas do mesmo arquivo), data anterior à abertura da conta. Confirmar exige `payments.settle`. |
| Importação — efeitos | Renda → `Receipt` + movimento `receipt` (sem fonte, tipo de renda opcional). Despesa paga → `Obligation` (competência = mês da data) + pagamento pelo mesmo `PaymentWriter` do M5. Pagar existente → pagamento alocado à despesa. Lote e linhas gravados com a ação e o código resultante. |
| Isolamento | Todas as consultas filtram `IdTenant`; FKs compostas com a casa; metas e lotes referenciam a conta pela chave alternativa `(IdTenant, Id)`. |

## Modelo e contratos

Migration `GoalsAttachmentsImports`: `SavingsGoal`, `Attachment`, `ImportBatch`, `ImportLine`. Configuração nova: `OrganizaliFinancas:AttachmentsPath` (padrão `attachments`; em produção `/app/attachments`, volume `organizali_attachments`). `deploy/backup.sh` passou a gerar também `organizali-attachments-<UTC>.tar.gz[.enc]`.

| Rota | Permissão | Uso |
|---|---|---|
| `POST /api/auth/change-password` | sessão | Troca a própria senha |
| `POST /api/members/{code}/recovery` | `members.manage` | Link de nova senha para um integrante |
| `POST /api/catalogs/defaults` | `finance.write` | Carrega o catálogo padrão |
| `GET/POST /api/goals`, `PUT /api/goals/{code}` | leitura / `finance.write` | Metas de reserva |
| `GET /api/attachments?kind&target`, `POST` (multipart `file`), `GET /{code}/content`, `DELETE /{code}` | leitura / `finance.write` | Comprovantes |
| `POST /api/imports/preview` (multipart `file`, `accountCode`), `POST /api/imports` (JSON, `Idempotency-Key`), `GET /api/imports` | `finance.write` / `payments.settle` / leitura | Importação de extrato |
| `--Operation=SeedCatalog` | operador | Catálogo padrão numa casa existente |

O proxy do Next (`/api/backend/[...path]`) passou a repassar `multipart/form-data` sem alteração e a devolver `Content-Disposition` nos downloads.

## Validação e reprodução

- `dotnet build` e `dotnet test` (unidade): 94 aprovados, 1 falha pré-existente em `SmtpRecoveryDeliveryTests` nesta máquina (o servidor SMTP falso recebe só o `EHLO`; a mesma falha ocorre no commit anterior) e 1 pulado (banco). Novos testes: `StatementParserTests` (OFX, CSV com cabeçalho e números brasileiros, crédito/débito em colunas, sem cabeçalho, arquivo inválido, impressão digital).
- Frontend: `tsc`, `eslint` e `next build` aprovados (rotas `/help`, `/goals`, `/imports`, `/api/auth/change-password`).
- Servidor da LAN (versão `dc29b68`, 14/10/2026): imagens construídas, migration aplicada e pilha no ar. Fumaça pela API com a conta piloto, aprovada: catálogo padrão idempotente (0 categorias novas), meta criada com progresso e mensal necessário, prévia de OFX com sugestões, importação confirmada (1 renda + 1 despesa paga), repetição com a mesma chave devolvendo o mesmo lote, segunda prévia marcando as linhas como já importadas, comprovante PDF anexado, listado, baixado com `Content-Disposition`, removido (404 depois) e `.exe` recusado, troca de senha recusando a senha atual errada sem derrubar a sessão, link de nova senha gerado, páginas Ajuda, Minha casa (Primeiros passos), Metas, Importar extrato e Minha conta renderizadas com os novos itens de menu. Os lançamentos de teste foram estornados/cancelados e a conta de teste inativada em seguida.
- Observação operacional: em sessão WinRM o credential helper do Docker falha; `deploy.ps1` no servidor passou a usar `DOCKER_CONFIG=C:\painel\dockercfg` e `build` sem `--pull`, e é disparado por tarefa agendada para sobreviver ao fim da sessão remota.

## Limitações e pendências

- Importação não cria transferências entre contas nem compras no cartão; linhas desse tipo devem ser ignoradas ou lançadas como despesa.
- Compras no cartão ainda não aceitam comprovante (só despesa, pagamento e recebimento).
- O progresso da meta é o saldo da conta inteira; para acompanhar reservas separadas, use uma conta por meta.
- Exportações (RF35), conciliação manual (RF39), renegociação (RF40), encerramento da casa (RF42), resumo mensal (RF45), simulação (RF46) e cobrança (RF43) continuam pendentes de M9/M10.
- O link de nova senha e o convite continuam entregues à mão pelo titular enquanto D16 (e-mail) estiver aberta.

## Continuidade

[Estado completo](../status/PROJECT_STATUS.md) · [Índice das entregas](README.md) · [Tutorial](../product/tutorial.md).
