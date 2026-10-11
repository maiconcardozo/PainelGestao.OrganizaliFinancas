# Registro de decisões e dúvidas

Estado inicial em 10/10/2026. O usuário pediu que dúvidas sejam perguntadas sempre. Uma proposta abaixo não equivale a decisão aceita.

## Confirmadas

| ID | Decisão | Origem |
|---|---|---|
| D01 | Projeto particular, sem envolvimento operacional da empresa | Mensagem do usuário |
| D02 | Não registrar na Wiki | Recusa explícita |
| D03 | RevendaPRO como referência principal de arquitetura e tecnologia | Confirmação do usuário |
| D04 | Uma casa por assinatura | Resposta à pergunta de escopo |
| D05 | Preparar escopo, requisitos, plano e README antes do desenvolvimento | Pedido inicial |
| D06 | Perguntar quando houver dúvida | Orientação explícita |
| D07 | Dados financeiros compartilhados; permissões controlam edição | Resposta do usuário |
| D08 | Piloto com famílias convidadas e liberação manual | Resposta do usuário |
| D18 | Dashboard moderno com saldo, fluxo de caixa, orçamento e alertas para coordenar a casa | Complemento explícito do usuário |
| D09 | Uso do Foundation.Base autorizado no projeto particular | Resposta específica do usuário; restore/feed serão validados em M1 |
| D10 | Rateio por valor ou percentual na primeira versão | Resposta específica do usuário |
| D11 | Moeda BRL no lançamento | Confirmação conjunta da proposta de renda |
| D12 | Bruto opcional, líquido esperado e recebimentos efetivos separados | Resposta específica do usuário |
| D19 | Interface em português e fuso inicial America/Sao_Paulo, configurável por casa | Resposta no detalhamento dos marcos |
| D20 | Um login pertence a uma única casa no piloto, seguindo RevendaPRO | Resposta no detalhamento dos marcos |
| D15 | Marca Organizali e textos em português, com layout e componentes seguindo RevendaPRO | Confirmação explícita do usuário sobre o frontend |
| D21 | Titular administra; editor cadastra rendas/despesas, liquida e mantém orçamento; leitor consulta. Estornos e integrantes exclusivos do titular | Usuário autorizou continuar após a apresentação da matriz |
| D22 | Fonte de renda aceita semanal, quinzenal (14 dias), mensal, trimestral, semestral e anual; valor fixo (ex.: PJ com base) ou variável (ex.: comissão). Entrada real pode ser registrada a qualquer momento, com ou sem previsão | Resposta no início do M4 |
| D23 | Previsões materializadas até o mês corrente + 12 meses | Resposta no início do M4 |
| D24 | Média de renda por ano civil e meses fechados: soma de janeiro até o último mês fechado ÷ quantidade desses meses; em janeiro usa o ano anterior inteiro ÷ 12. A média é indicador por pessoa, fonte e casa e vira a estimativa (rotulada) da renda variável | Resposta no início do M4 |
| D26 | Frontend igual ao RevendaPRO: casca, componentes e fluxo dos cadastros (lista em mosaico/linhas, “Novo …” e formulário em modal) | Pedido do usuário após o M8 (11/10/2026) |
| D25 | Recebimento acima do previsto é um único recebimento: realizado integral, residual zero e diferença favorável exibida apenas como comparativo | Resposta no início do M4 |

## Perguntas abertas

| ID | Questão | Alternativas/impacto | Momento limite |
|---|---|---|---|
| D13 | Fechamento do cartão usa lançamento no dia de corte em qual fatura? | Proposta de prévia editável; regra precisa ser escolhida pela casa/cartão. Implementado como opção por cartão com padrão da premissa A06 | Antes do módulo de cartões |
| D14 | Planos, preços, limites, trial, cancelamento e inadimplência? | Sem valores inventados; dependente da modalidade comercial | Antes do lançamento comercial |
| D16 | Hospedagem, domínio, e-mail, storage e metas de disponibilidade? | Mantém Docker; dimensionamento e custos dependem das escolhas | Antes de homologação externa |
| D17 | Prazo de retenção, exclusão definitiva e acesso excepcional de suporte? | Proposta técnica deve ser validada para a operação real | Antes de receber usuários externos |

D04, D07–D12, D15 e D19–D26 já foram respondidas. Não repetir perguntas já respondidas. D13, D14, D16 e D17 permanecem abertas. A matriz de exportação será definida quando o recurso M9 for detalhado.

Uma casa por assinatura e um login por casa são regras independentes, ambas confirmadas. A casa pode ter vários usuários; cada usuário pertence apenas a ela no piloto.

## Premissas adotadas na execução autônoma

Em 10/10/2026 o usuário autorizou concluir todos os marcos até a publicação Docker sem aguardar aprovação a cada etapa. Onde a regra não estava fechada, foi aplicada a proposta mais conservadora abaixo. Cada premissa pode ser revertida; nenhuma substitui as perguntas abertas.

| ID | Premissa aplicada | Marco | Como reverter |
|---|---|---|---|
| A01 | Recorrências e parcelamentos dividem só por percentual (o valor de cada ocorrência pode mudar); conta avulsa aceita valor ou percentual | M5 | Aceitar valor fixo por ocorrência no modelo da recorrência |
| A02 | Juros, multa e desconto não são apagados; erro se corrige com ajuste de sinal oposto e justificativa | M5 | Criar estorno de ajuste exclusivo do titular |
| A03 | O comportamento do tipo de despesa define o formulário (avulsa, recorrente, parcelada, cartão) | M5 | Permitir tipo neutro em qualquer formulário |
| A04 | O acerto do rateio é sugestão exibida; não gera lançamento. Se ocorrer entre contas da casa, registra-se transferência | M5 | Registrar acerto como movimento interno vinculado |
| A05 | Pagamento pode deixar a conta com saldo negativo (cheque especial); só o saldo aberto da despesa é limite | M5 | Bloquear saída acima do saldo da conta |
| A06 | D13 sem resposta: cada cartão escolhe a regra; o padrão é a compra do dia do fechamento entrar na **próxima** fatura. A prévia permite mover a primeira parcela um ciclo antes de salvar | M6 | Trocar o padrão para “fatura que fecha no dia” ou responder D13 |
| A07 | Compras no cartão não são rateadas entre moradores no M6 | M6 | Rateio por percentual nas parcelas, como nas recorrências |
| A08 | Estorno da loja cancela as últimas parcelas ainda abertas que cabem no valor e credita o restante na fatura da data; crédito de fatura já paga fica como saldo negativo dessa fatura | M6 | Transferir crédito automaticamente para a próxima fatura |
| A09 | Saldo projetado não inclui renda variável (estimativa pela média) nem renda já atrasada; contas vencidas contam no dia de hoje | M7 | Incluir estimativas rotuladas na projeção |
| A10 | Comprometido do orçamento inclui valores estimados de recorrências e encargos; o pago do cartão aparece só na fatura | M7 | Orçar só valores confirmados |
| A11 | Ler, adiar ou resolver alerta exige titular ou editor; o estado vale para toda a casa | M7 | Estado de alerta por usuário |
| A12 | Filtro por conta na análise considera despesas com pagamento ativo daquela conta; compras no cartão ficam fora desse filtro | M7 | Filtrar pela conta de pagamento da fatura |
| A13 | HTTPS pelo Caddy com certificado automático (ACME) informado por e-mail; `internal` só para teste local | M8 | Trocar o proxy ou usar certificado próprio |
| A14 | Recuperação de senha por SMTP genérico configurável; sem provedor o recurso responde indisponível | M8 | Integrar API de um provedor escolhido em D16 |
| A15 | Imagens construídas localmente com tag do commit; envio a registro e deploy só com registro/servidor definidos pelo usuário | M8 | Configurar `ORGANIZALI_REGISTRY` e o servidor |

## Regra de tratamento

Trabalhar nas partes independentes enquanto aguarda resposta. Não implementar a parte que depende de decisão aberta. Ao receber resposta, atualizar este registro e os documentos afetados, preservando histórico. Estimativas de prazo permanecem condicionais à capacidade disponível, compatibilidade da stack e regras detalhadas aprovadas.
