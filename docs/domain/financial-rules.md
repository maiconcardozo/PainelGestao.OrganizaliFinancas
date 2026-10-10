# Regras financeiras e invariantes

Regras propostas para aceite do escopo. BRL, bruto opcional, previsão pelo líquido e recebido separado confirmados em D11/D12. Valores de exemplo são fictícios.

## 1. Vocabulário e saldos

- **Previsão de receita:** expectativa para uma competência, não dinheiro disponível.
- **Recebimento:** entrada efetiva em uma conta em uma data.
- **Obrigação:** compromisso de pagar, com valor, competência e vencimento.
- **Pagamento:** saída efetiva de uma conta e sua alocação em obrigação/fatura.
- **Competência:** período ao qual a receita/despesa pertence.
- **Saldo atual:** saldo inicial mais movimentos efetivos até a data de corte.
- **Saldo projetado:** saldo atual mais recebimentos previstos ainda abertos menos desembolsos futuros abertos, sem reaplicar movimentos já realizados.
- **Dívida total:** obrigações abertas; itens de cartão representados uma única vez, mesmo quando agrupados por fatura.

Valores monetários no domínio em decimal; persistência proposta `decimal(19,2)`, taxas informadas `decimal(9,6)`. Arredondamento monetário explícito a centavos; nunca `float`/`double` para soma oficial. API transporta dinheiro como string decimal sem formatação regional, por exemplo `"1250.50"`; UI formata `R$ 1.250,50`. Gráficos podem aproximar apresentação, mas não recalcular totais oficiais com ponto flutuante.

## 2. Renda prevista e realizada

Fonte de renda tem pessoa, categoria, valor esperado, recorrência e vigência. Previsão gerada mantém fotografia da regra/versão de origem. Ajuste salarial produz nova vigência e afeta ocorrências futuras; não reescreve recebimentos.

Uma previsão pode receber várias alocações. Recebimento pode ser dividido entre previsões mediante valores explícitos. Soma alocada nunca supera o recebimento. Excesso em relação à previsão é permitido e mostrado como diferença favorável; não gera previsão negativa.

Exemplo: previsão de R$ 4.000,00; recebimentos de R$ 2.000,00 e R$ 1.800,00. Realizado R$ 3.800,00, diferença R$ -200,00 e R$ 200,00 pendentes. Se o usuário encerrar a previsão como realizada a menor, registrar motivo e retirar o residual da projeção. Receber não equivale automaticamente a atingir o previsto.

## 3. Dívida e liquidação

Saldo aberto = principal + acréscimos confirmados − descontos confirmados − pagamentos líquidos de estornos. Não aceitar saldo negativo silencioso. Pagamento acima do saldo pede correção ou registro explícito de crédito/adiantamento (funcionalidade posterior).

Estados financeiros: aberta, parcialmente paga, quitada, cancelada ou substituída por renegociação. **Vencida é uma condição derivada** (saldo aberto > 0 e vencimento anterior à data local), podendo coexistir com pagamento parcial. Estorno de uma quitação reabre saldo. Alterações em dados liquidados precisam de operação corretiva, não simples edição do valor original.

Exemplo: conta de R$ 250,00, acréscimo R$ 10,00, desconto R$ 5,00 e pagamento R$ 100,00 deixa saldo R$ 155,00. Caixa reduz somente R$ 100,00. Estornar esse pagamento restaura R$ 100,00 e o saldo volta a R$ 255,00.

Lançamento e liquidação imediata podem ocorrer no mesmo formulário, mas continuam registros relacionados e transacionados juntos. Uma baixa pode vir de várias contas por registros distintos. Obrigações não representam execução real de pagamento bancário.

## 4. Contas e transferências

Toda conta tem data/valor inicial. Movimentos anteriores à data inicial não podem alterar saldo sem revisão explícita dessa base. Ajuste de conciliação é movimento identificado com motivo.

Transferência gera dois movimentos atômicos com mesmo vínculo. Transferir R$ 500,00 da corrente à poupança não é renda nem despesa; o saldo consolidado permanece igual. Uma tarifa de R$ 5,00 é despesa própria. Aportes em reserva são transferências/vinculações, não consumo.

## 5. Parcelamento

Informar total, número de parcelas, primeiro vencimento e intervalo; exibir prévia. R$ 100,00 em 3 parcelas resulta em R$ 33,33, R$ 33,33 e R$ 33,34. O resíduo vai para a última parcela e a soma deve ser exata. Calendário usa datas civis; dia 31 inexistente ajusta ao último dia do mês e mantém o dia-base nas próximas ocorrências.

Empréstimos/financiamentos no lançamento usam prestações informadas; sistemas de amortização e taxas contratuais não são deduzidos. Dívida contraída anteriormente pode ter saldo inicial de obrigação sem entrada fictícia de dinheiro; novo empréstimo recebido é entrada de financiamento, não renda de trabalho, e fica separado nos indicadores.

## 6. Recorrência

Frequências propostas: semanal, quinzenal por intervalo de 14 dias, mensal, trimestral, semestral e anual. Duas datas fixas no mês são configuração distinta de 14 dias. Confirmar demanda antes de expor todas no MVP.

Início, término/quantidade, dia-base, valor fixo/estimado e versão compõem regra. Proposta de horizonte materializado: mês corrente + 12 meses, configurável pela operação. Projeções fora disso precisam declarar horizonte. Chave única `(IdTenant, IdRecurringRule, OccurrenceDate)` impede repetição. Alterar regra não recria ocorrência liquidada; escolher “esta” ou “esta e próximas”, mostrar prévia e preservar ocorrências já pagas.

## 7. Cartões e dupla contagem

Cartão tem limite, fechamento e vencimento; compras geram parcelas vinculadas a ciclos/faturas. A regra do dia de corte precisa ser configurada e confirmada; uma prévia permite corrigir a fatura de destino antes de fechar. Alterar fechamento afeta ciclos futuros sem mover itens de faturas já fechadas.

Visões distintas:

| Evento | Despesa por categoria/competência | Fluxo de caixa bancário |
|---|---|---|
| Compra de R$ 300,00 no cartão | R$ 300,00 na competência definida | R$ 0,00 |
| Pagamento da fatura de R$ 300,00 | R$ 0,00 adicionais | Saída de R$ 300,00 |
| Estorno pelo estabelecimento | Crédito/redução da despesa vinculado à compra | Sem entrada bancária até reembolso efetivo |

Proposta para compras parceladas: orçamento reconhece parcelas na competência da respectiva fatura, e a tela da compra mostra compromisso total desde a contratação. Manter uma base de competência única no MVP; trocar regime em relatório não deve misturar valores.

Para projeção, somar valores abertos de faturas/parcelas agrupadas uma única vez. Não somar a fatura mais seus itens. Pagamento parcial de R$ 100,00 em fatura de R$ 300,00 deixa R$ 200,00 em aberto. Se houver transferência desse saldo para ciclo seguinte, registrar vínculo e substituir a representação anterior, sem extinguir ou duplicar principal. Encargos entram separadamente quando informados.

Fatura é agregadora, não uma segunda despesa. Limite disponível é estimativa baseada nas compras conhecidas; mostrar que pode diferir do banco. Proposta: comprometimento por principal ainda não liquidado, incluindo parcelas futuras. Antecipação deve identificar quais parcelas são liquidadas e seu desconto, sem reorganização silenciosa.

## 8. Orçamento e alertas

Orçamento define limite por mês/categoria. Comprometido considera despesas confirmadas da competência (incluindo parcelas de cartão da competência), excluindo canceladas e reduzindo estornos. Valor pago aparece como métrica separada; atraso de pagamento não transfere a despesa para outro orçamento.

Categoria pai agrega filhos. Orçamento específico no filho e teto no pai podem coexistir, mas o total da casa usa apenas os tetos de nível superior para não duplicar. Receita esperada não aumenta limite automaticamente. Sem limite definido, mostrar “Sem orçamento”; limite zero é válido e qualquer gasto positivo excede.

Alertas propostos de 80% e 100% são editáveis. R$ 900,00 comprometidos para limite R$ 1.000,00 gera aproximação; R$ 1.100,00 mostra excesso R$ 100,00 (10%). Só voltar a notificar se houver mudança relevante de faixa/versão; leitura de alerta não resolve sua causa.

Saldo projetado negativo identifica primeira data de déficit e lançamentos causadores. Atrasos de renda, faturas próximas, despesas variáveis sem confirmação e falta de orçamento alimentam central de ações. Alertas não fazem movimentações automáticas.

## 9. Rateio

Rateio por valor ou percentual confirmado em D10. Separar obrigação, pagador efetivo e responsabilidade de cada pessoa. Soma dos valores deve igualar a obrigação; percentuais totalizam 100%. Arredondar cotas a centavos e distribuir resíduo pelo método de maiores restos, desempate por Code da pessoa; mostrar a prévia antes de confirmar.

Exemplo: conta de R$ 100,00 dividida 50% para Ana e 50% para Bruno, paga integralmente por Ana. Despesa da casa R$ 100,00; cota R$ 50,00 por pessoa; acerto de Bruno para Ana R$ 50,00. Registrar acerto como transferência/contribuição interna, não como segunda despesa ou receita externa. Se o salário já entrou em conta da casa, sua destinação para pagar uma conta não constitui novo recebimento.

Alterar rateio após pagamento exige confirmação, histórico e recálculo dos acertos, sem modificar saída bancária. Percentual proporcional à renda fica para evolução; não foi escolhido para V1. O rateio é opcional por lançamento, mas a funcionalidade existe no piloto.

## 10. Integridade e histórico

Liquidação, alocação, movimento e auditoria pertencem à mesma transação. Bloqueio/versão de concorrência protege saldo. Requisições financeiras usam chave de idempotência por casa/operação com hash do payload; mesma chave com payload diferente retorna conflito. Chaves de operações financeiras persistem com seu histórico.

Cadastros usam exclusão lógica; movimentos confirmados usam estorno, não ocultação que apaga efeito do saldo. Ao inativar categoria/conta/pessoa, manter referências históricas e impedir novos usos. Catálogo de tipos/campos é versionado; alterações não reinterpretam registros antigos.

Renegociação encerra apenas o saldo substituído e cria obrigação vinculada em transação; mantém pagamentos anteriores. Juros não são aplicados por rotina sem taxa, base, período e política aprovados. Importações futuras devem detectar duplicidades e pedir decisão sobre correspondências ambíguas.
