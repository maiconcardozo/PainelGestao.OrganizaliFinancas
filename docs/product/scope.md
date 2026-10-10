# Escopo do Organizali Finanças

Versão 0.1 — 10/10/2026 — proposta para revisão antes do desenvolvimento.

## 1. Objetivo

Organizar as finanças de uma casa de forma compartilhada, permitindo comparar o previsto com o realizado e acompanhar compromissos futuros. O sistema será web responsivo e oferecido como SaaS. Cada assinatura atenderá uma única casa, conforme confirmação do usuário.

Perguntas que o produto deve responder: quem contribui, quanto cada pessoa ganha, quanto entrou, o que falta receber, o que vence, o que foi pago, qual o saldo e quais compromissos já ocupam os próximos meses.

## 2. Decisões confirmadas e propostas

Confirmado: projeto particular; uma casa por assinatura; RevendaPRO como padrão principal; Foundation.Base autorizado; dados compartilhados com permissões de edição; piloto manual; BRL; renda bruta opcional, líquida esperada e recebida; rateio por valor/percentual; dashboard moderno; documentação e escopo antes do código; perguntar quando houver dúvida; sem Wiki ou tarefas corporativas.

Proposto para revisão: pt-BR; moradores sem login; receitas/despesas manuais inicialmente; orçamento mensal; categorias e tipos livres por casa; lançamentos com competência e vencimento separados. Regras detalhadas de cartão, identidade visual e operação continuam no [registro de decisões](decisions.md).

## 3. Público e perfis

| Perfil proposto | Necessidade | Limite |
|---|---|---|
| Titular da assinatura | Administrar casa, integrantes, acesso e assinatura | Não remover o último titular ativo |
| Administrador da casa | Organizar cadastros, lançamentos e orçamento | Não administrar outras casas |
| Colaborador | Registrar e acompanhar informações autorizadas | Sem concessão de privilégio maior que o próprio |
| Leitor | Consultar dados e relatórios autorizados | Sem alteração financeira |
| Operador da plataforma | Administrar assinatura, disponibilidade e suporte | Sem acesso automático a detalhes financeiros |

Pessoa/morador e usuário são conceitos diferentes. Pode-se cadastrar quem contribui com a casa sem exigir e-mail ou login. Um usuário poderá ter vínculos com casas de assinaturas diferentes, se aprovado; a navegação sempre exige uma casa ativa e não consolida dados entre elas.

## 4. Módulos da primeira versão funcional

### Casa e acesso

Onboarding, convite e revogação de acesso, recuperação de senha, perfis, permissões e configuração da casa. Convite identifica a casa, expira e só pode ser aceito uma vez. A saída de um integrante preserva histórico financeiro.

### Pessoas e rendas

Cadastro de moradores; fontes de renda fixas, variáveis e avulsas; remuneração informada, valor líquido esperado, dia previsto e vigência. Salário, comissão, aluguel, benefício e trabalho extra são exemplos de categorias editáveis. Recebimentos reais podem acontecer em várias datas/contas e ser associados a uma previsão.

O sistema não calcula folha de pagamento ou encargos trabalhistas: registra valores informados. Valor bruto é informativo; a previsão de caixa utiliza o líquido esperado. Alterações salariais possuem vigência e preservam os meses anteriores.

### Contas, movimentos e saldos

Conta corrente, poupança e carteira como exemplos configuráveis. Saldo inicial com data de corte, extrato, ajustes justificados, transferências e conciliação manual. O cartão é meio de crédito separado de uma conta com dinheiro disponível.

### Parametrização

Tipos de dívida e despesa, categorias/subcategorias, credores, formas de pagamento, periodicidade, lembretes, responsável e campos adicionais tipados. Os usuários devem conseguir cadastrar água, luz, internet, aluguel, compra parcelada ou uma obrigação própria sem alterar código.

Tipos configuráveis selecionam comportamentos financeiros suportados e versionados: avulso, recorrente, parcelado ou cartão. Categorias classificam finalidade; formas de pagamento indicam como foi pago. Um novo nome não altera a matemática do sistema. Não haverá execução de SQL, scripts ou fórmulas arbitrárias cadastradas pelo usuário.

### Obrigações e pagamentos

Contas fixas/variáveis, compras à vista ou parceladas, vencimentos, competência, responsável, rateio por valor/percentual, encargos informados e renegociação rastreada. Pagamentos totais/parciais vinculados a conta financeira. Estorno preserva histórico e recalcula saldo aberto. Rateio faz parte do piloto; renegociação completa entra em P1.

### Cartões e faturas

Titular, apelido, limite informado, fechamento, vencimento e compras à vista/parceladas. Mostrar fatura aberta, fechada, paga parcialmente e quitada; ajustar alocação de uma compra antes do fechamento com auditoria. Não guardar número completo do cartão, CVV ou credenciais bancárias.

Juros de financiamento do cartão são valores informados pelo usuário, não cálculos bancários presumidos. Saldo anterior e pagamento mínimo devem ser apresentados explicitamente; saldo não pago não pode desaparecer nem ser duplicado em duas faturas.

### Planejamento e análise

Painel mensal previsto/realizado, agenda, orçamento por categoria, vencidos, comparação por pessoa e categoria, projeção de saldo e dívida futura. Relatórios sempre informam regime (caixa ou competência), período e filtros. Relatório por competência e saldo bancário podem divergir legitimamente e devem ser rotulados.

### Operação do SaaS

Provisionamento isolado da casa, situação da assinatura, limites de plano e auditoria. Piloto com liberação manual confirmado; cobrança automática posterior. Exportação completa entra em P1 e deve estar disponível antes de abertura comercial. Piloto não significa ausência de isolamento, backup ou recuperação de acesso.

## 5. Ampliações planejadas

| Etapa | Recursos | Dependência |
|---|---|---|
| Complementos da V1 | Anexos de comprovantes, exportação Excel/PDF, metas e simulações | Núcleo financeiro estável; orçamento e alertas já são P0 |
| Comercial | Checkout, cobrança recorrente, eventos do provedor, portal da assinatura | Modelo comercial e provedor escolhidos |
| V2 | Metas de reserva, importação CSV/OFX com prévia e conciliação assistida | Formatos e critérios de duplicidade aprovados |
| V2 | Divisão proporcional à renda | Base de cálculo aprovada; acerto por valor/percentual já pertence ao piloto |
| Posterior | Open Finance, automação de documentos, notificações externas adicionais | Provedores, custos, contratos e consentimentos |

CSV/OFX é importação de extrato, não integração bancária em tempo real. Metas são planejamento, sem aplicação automática de recursos.

## 6. Fora do escopo inicial proposto

Aplicativos nativos, uso offline com sincronização financeira, múltiplas moedas/câmbio, múltiplas casas na mesma assinatura, execução real de pagamentos, recomendação de investimentos, cálculo tributário/folha, contabilidade fiscal, emissão de notas e conexão com sistemas da empresa. Qualquer inclusão requer revisão explícita de escopo.

## 7. Jornada de ponta a ponta

Titular cria casa, cadastra Ana e Bruno, informa saldo inicial e rendas esperadas. Cadastra água e luz como contas mensais variáveis e uma compra em três parcelas. Ao receber, registra data e conta; ao pagar, associa a obrigação. O painel exibe previsão, realização, saldo aberto e próximos vencimentos. No cartão, a compra ocupa a fatura; seu pagamento movimenta caixa sem gerar nova despesa por categoria.

## 8. Sucesso do lançamento

Uma família consegue concluir essa jornada pelo celular, sem planilha paralela para calcular saldos e sem acesso indevido entre assinaturas. Todos os cenários críticos do [plano de testes](../testing/test-patterns.md) devem passar. Meta de tempo do onboarding e indicadores de adoção serão medidos no piloto, não prometidos sem evidência.

## 9. Entrega e aprovação

Esta versão entrega documentos de requisitos, arquitetura, regras, modelo, contratos, UX, marcos, testes e operação. Aprovar o planejamento não executa deploy, cobrança ou envio de mensagens. O desenvolvimento será solicitado após resolução das decisões que alteram o modelo inicial.
