# M3.3 — Tipos de despesa e campos parametrizados

Tipos de despesa versionados implementados: nome, estado, comportamento (avulso, recorrente, parcelado ou cartão), categoria sugerida e até 30 campos adicionais. A escolha do comportamento prepara a definição; criação de obrigações/parcelas/faturas continua nos marcos M5/M6.

## Definições e formulário

Campos de texto (até 2000 caracteres), decimal (mínimo/máximo), data sem horário, sim/não e lista (até 50 opções) têm rótulo e obrigatoriedade. Identificadores técnicos são gerados pelo formulário e permanecem estáveis quando o rótulo muda. Não há scripts, SQL ou fórmulas cadastráveis.

ExpenseType segue TenantEntity/ExpenseType do RevendaPRO. ExpenseTypeVersion guarda um snapshot imutável da definição; campos tipados são serializados como JSON nessa versão, sem tabela de valores ainda, pois não há lançamento financeiro nesta etapa. Categoria sugerida tem FK composta da mesma casa. IDs são resolvidos dentro da mesma transação Dapper, sem commit intermediário.

API valida tanto definições quanto valores; exige versão explícita ao experimentar campos. Campos desconhecidos, tipos errados, limite excedido, data inválida e obrigatório ausente são recusados. Booleano false e número zero são valores válidos. Histórico de versão não é reescrito após edição. A UI gera os controles conforme a definição e permite experimentar sem criar dívida.

Titular/editor mantêm tipos; leitor consulta e experimenta campos. Migration ParameterizedExpenseTypes adicionada; nenhum banco persistente foi alterado.

## Evidência

38 testes .NET aprovados com MariaDB descartável (TestResults/m3-3.trx). Incluem nove casos novos de domínio e integração para Água com instalação obrigatória, Luz e um tipo particular sem mudança de código. Após alterar Água, versão 1 continua aceitando instalação; versão 2 exige leitura decimal. Acesso cruzado, escrita do leitor e edição desatualizada são recusados.

Build/lint Next e teste HTTP web aprovados. Revisão integrada em navegador pertence ao M3.4. Não houve atualização de pacotes, envio externo ou push.
