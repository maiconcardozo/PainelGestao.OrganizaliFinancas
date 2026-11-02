# Entregas por marco

Cada documento registra objetivo, funcionalidades, regras de negócio, validação e pendências. O código permanece no repositório; o pacote para revisão fica em `C:/Codex/Upload`.

- [M0 — Planejamento](M0.md)
- [M1.1 — Fundação da solução](M1.1.md)
- [M1.2 — Persistência](M1.2.md)
- [M1.3 — Minimal API e Swagger](M1.3.md)
- [M1.4 — Frontend e Docker](M1.4.md)
- [M2.1 — Provisionamento do piloto](M2.1.md)
- [M2.2 — Autenticação](M2.2.md)
- [M2.3 — Integrantes e permissões](M2.3.md)
- [M2.4 — Interface autenticada](M2.4.md)
- [M3.1 — Moradores e configuração](M3.1.md)
- [M3.2 — Catálogos financeiros](M3.2.md)
- [M3.3 — Tipos de despesa e campos](M3.3.md)
- [M3.4 — Demonstração integrada](M3.4.md)
- [M4 — Contas e rendas](M4.md)
- [M5 — Despesas e liquidação](M5.md)
- [M6 — Cartões e faturas](M6.md)
- [M7 — Painel, fluxo, orçamento e alertas](M7.md)
- [M8 — Produção e publicação Docker](M8.md)
- [Interface igual ao RevendaPRO](UI-RevendaPRO.md)

Próximas entregas devem usar a mesma estrutura, atualizar o [quadro consolidado](../status/PROJECT_STATUS.md) e registrar um commit por etapa. Não marcar futuro como entregue nem apresentar seletor como mecanismo financeiro implementado.

- [Complementos da V1 e apoio ao piloto (14/10/2026)](M9-M10-complementos.md): Ajuda no sistema, catálogo padrão, primeiros passos, senha sem e-mail, metas, comprovantes e importação de extrato.
- [Sessão renovada sozinha (14/10/2026)](Sessao-renovacao-automatica.md): correção do "cai para o login"; renovação silenciosa no proxy do Next, acesso de 60 min e sessão de 30 dias.
- [Excluir e editar em cartões e compras (14/10/2026)](Cartoes-excluir-editar.md): ativar/desativar e excluir cartão; editar e excluir compra; limpeza das compras de teste no piloto.
- [Despesa paga com cartão de crédito (01/11/2026)](Despesas-pagas-no-cartao.md): "Pagar com" aceita um cartão; o valor entra na fatura pelo fechamento, nada sai da conta, e o estorno do pagamento desfaz o item da fatura.
- [Fatura fora de Despesas, excluir despesa e limpeza (01/11/2026)](Despesas-excluir-e-fatura.md): a fatura do cartão deixa de ser listada e somada em Despesas; despesa aberta ou cancelada pode ser excluída de vez; lançamentos cancelados e estornados do piloto apagados.
- [Compras do cartão em Despesas, aviso ao excluir despesa paga e estorno como ícone (01/11/2026, noite)](Despesas-compras-do-cartao.md): cada compra do cartão aparece em Despesas no mês da fatura, parcela por parcela, somando no mês; Excluir em despesa paga orienta a estornar antes; "Estorno da loja" vira ícone.
- [Fatura por cartão em Despesas, reativar despesa, corrigir motivos e editar compra (02/11/2026)](Despesas-fatura-por-cartao.md): cada fatura de cartão é uma linha em Despesas com o total das compras, Pagar fatura e Ver compras; despesa paga no cartão não aparece duas vezes; Desfazer cancelamento, correção dos motivos de cancelamento e estorno, e edição de valor, data e parcelas da compra.
- [Faturas anteriores em Cartões e histórico da despesa paga no cartão (02/11/2026, tarde)](Cartoes-faturas-anteriores-e-historico.md): despesa paga no cartão continua no mês com o selo "Paga · cartão X · fatura m/aa" sem somar de novo; Ver faturas/compras anteriores em Cartões; Ver compras abre fatura antiga; aviso ao lançar compra em fatura anterior ao início do painel.
