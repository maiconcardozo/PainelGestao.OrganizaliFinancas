# Experiência e dashboard

Aplicação da skill frontend-design ao planejamento; identidade ainda proposta, sem telas implementadas. O dashboard deve ajudar quem coordena a casa a perceber problemas e agir.

## Painel principal

No desktop, navegação lateral e conteúdo em duas áreas: fluxo de caixa ocupa a maior largura; agenda/ações ficam ao lado. No celular, saldo e ações urgentes vêm antes dos gráficos, seguidos de vencimentos e orçamento. Todos os valores têm período e data de atualização.

```text
Organizali       Casa / Mês                       Novo lançamento
----------------------------------------------------------------
Saldo atual          Entrou no mês       Saiu no mês
Projeção fim do mês   A receber           Compromissos abertos
----------------------------------------------------------------
Fluxo de caixa diário: realizado + previsto  | Precisa de atenção
Saldo acumulado e primeira data de déficit  | Orçamento excedido
Filtro: conta / pessoa / categoria          | Vencimentos próximos
----------------------------------------------------------------
Orçamento por categoria                    | Cartões e faturas
Usado / limite / restante / excesso         | Valor / vencimento
----------------------------------------------------------------
Contribuição dos moradores                  | Metas de reserva
Previsto / recebido / cota / pago            | Progresso (P1)
----------------------------------------------------------------
Últimos movimentos e pendências, com link para detalhe
```

## Indicadores e ações

| Bloco | Informação | Ação |
|---|---|---|
| Saldo atual | Contas incluídas e saldo disponível real | Abrir extrato por conta |
| Fluxo de caixa | Entradas/saídas realizadas e futuras, saldo acumulado | Abrir lançamentos do dia e simular cenário em P1 |
| Projeção | Saldo previsto ao fim do mês e pior saldo no caminho | Ver obrigações causadoras |
| Rendas | Líquido esperado, recebido, residual e diferença | Registrar recebimento |
| Contas a pagar | Vencidas, hoje, 7 dias e mês; confirmado/estimado | Registrar pagamento ou ajustar previsão |
| Orçamento | Comprometido, limite, percentual e excesso em reais | Abrir categoria ou revisar limite |
| Cartões | Fatura atual, próxima e compromisso futuro | Conferir compras ou registrar pagamento |
| Contribuição | Cota de cada morador e pagamento efetivo | Abrir rateio e acertos |
| Alertas | Severidade, motivo, valor, data e origem | Resolver causa, adiar ou marcar lido |
| Metas (P1) | Valor reservado e falta até objetivo | Registrar alocação/transferência |

Receita esperada e limite do cartão não são somados ao saldo real. “Disponível após compromissos” é projeção identificada, nunca apresentado como saldo bancário. Dashboards não têm gráficos decorativos sem possibilidade de explicar o número.

## Gráficos e comparações

Fluxo diário com barras de entradas/saídas e linha de saldo; realizado sólido, previsto tracejado e legenda textual. Orçamento em barras horizontais por categoria, com limite/excesso legíveis. Comparativo mensal e composição de gastos complementam análise, com tabela acessível e opção de ocultar valores na tela.

Ao filtrar pessoa, compras compartilhadas podem usar cota atribuída ou valor pago, selecionados e rotulados; nunca alternar sem informar. Consolidado da casa usa despesa original uma única vez. Vencimento e competência são filtros distintos.

## Alertas que ajudam a agir

1. Categoria próxima do limite ou acima dele.
2. Saldo previsto negativo em determinada data.
3. Conta vencida ou próxima do vencimento.
4. Recebimento esperado atrasado.
5. Fatura próxima de vencer ou parcialmente paga.
6. Parcela/conta variável ainda sem valor confirmado.
7. Categoria com gasto e sem orçamento configurado.
8. Contribuição do morador pendente, se houver rateio acordado.

Cada ocorrência explica cálculo e origem; limiares e antecedência configuráveis. Informar estimativas e dados incompletos. Mensagens diretas: “Alimentação passou R$ 120 do orçamento”, “O saldo previsto fica negativo em 18/11”. Sem conclusões causais ou aconselhamento gerado sem evidência.

## Navegação

Visão geral; Fluxo de caixa; Agenda; Pessoas e rendas; Contas; Despesas e dívidas; Cartões; Orçamento; Rateios; Alertas; Relatórios; Configurações. Metas entra em P1. Menu vem filtrado do servidor; cada módulo oferece estados vazio, carregando, erro recuperável e sem permissão.

Cadastro rápido oferece natureza, valor, data, pessoa e conta, expandindo parcelamento/recorrência/campos próprios conforme escolha. Prévia mostra efeitos antes de criar parcelas ou alterar próximas ocorrências. Botões específicos: “Registrar recebimento”, “Registrar pagamento”, “Estornar pagamento”.

## Direção visual proposta

Identidade própria, moderna e legível, pendente de aprovação: superfície clara `#F6F8FC`, texto `#17243A`, ação `#3159C7`, entrada `#147D64`, atenção `#946200`, saída/erro `#B42332`. Contrastes precisam ser medidos nas combinações finais. Fonte proposta Source Sans 3 com números tabulares, hospedada localmente; confirmar fonte/licença antes de usar.

Hierarquia prioriza fluxo e pendências; não transformar todas as informações em cartões idênticos. Valores alinhados à direita em tabelas, rótulos à esquerda, números de destaque com unidade. Espaçamento consistente e controles de formulário com altura uniforme. Animações só para resposta a ações, respeitando redução de movimento.

Tema escuro pode entrar em P1 após validar o claro. Modais em portal no body, foco preso durante abertura e retorno ao disparador. Telas de celular devem permitir registrar uma conta sem depender de tabela horizontal extensa. Aparência final será revisada com o usuário antes de implementar o frontend completo.
