# Requisitos funcionais

Proposta para implementação. Prioridades: **P0** obrigatório para piloto financeiro; **P1** completa a V1; **P2** evolução. P0 inclui o dashboard, orçamento e alertas pedidos pelo usuário. Os marcos não representam recursos já implementados.

| ID | Prioridade | Requisito | Critério de aceite |
|---|---|---|---|
| RF01 | P0 | Provisionar uma casa por assinatura, com liberação manual | Reexecutar provisionamento não duplica casa; ativação e suspensão auditadas |
| RF02 | P0 | Autenticar, sair, recuperar senha e revogar sessões | Recuperação expira e funciona uma vez; saída revoga sessão; erro não revela existência de e-mail |
| RF03 | P0 | Convidar usuários e atribuir permissões | Convite vincula somente a casa de origem; remoção revoga acesso; último titular protegido |
| RF04 | P0 | Cadastrar pessoas sem obrigatoriedade de login | Morador sem e-mail pode receber renda e ser responsável por dívida |
| RF05 | P0 | Gerenciar configurações da casa | Moeda, fuso, início de uso e preferências aplicados de forma consistente |
| RF06 | P0 | Cadastrar múltiplas fontes de renda por pessoa | Fonte tem tipo, vigência, recorrência e valor esperado; bruto opcional e líquido propostos |
| RF07 | P0 | Gerar receitas previstas por competência | Alteração futura não reescreve meses anteriores; repetição do job não duplica previsão |
| RF08 | P0 | Registrar recebimentos reais e associação à previsão | Permite vários recebimentos, datas e contas; soma atualiza diferença sem duplicar renda |
| RF09 | P0 | Registrar renda avulsa ou extra | Recebimento sem previsão aparece no realizado e é identificado como não previsto |
| RF10 | P0 | Gerenciar contas financeiras e saldos iniciais | Extrato reconstrói saldo a partir da data inicial e movimentos efetivos |
| RF11 | P0 | Transferir entre contas da casa | Débito/crédito atômicos; consolidado e resultado mensal não mudam |
| RF12 | P0 | Cadastrar categorias/subcategorias | Categoria nova aparece em lançamento, orçamento e filtro sem deploy |
| RF13 | P0 | Cadastrar tipos de dívida e despesa | Usuário cria tipo com comportamento suportado, defaults e campos próprios |
| RF14 | P0 | Cadastrar credores, formas de pagamento e tipos de renda | Cadastros isolados por casa, reutilizáveis e inativáveis |
| RF15 | P0 | Configurar campos adicionais por tipo | Texto, decimal, data, booleano e lista; obrigatoriedade, limites e versão validados no servidor |
| RF16 | P0 | Lançar despesa/obrigação avulsa | Competência, vencimento, valor, categoria, responsável e credor distinguíveis |
| RF17 | P0 | Gerar despesas recorrentes fixas/variáveis | Sem duplicidade; valor estimado pode ser confirmado; edição desta/próximas explicita alcance |
| RF18 | P0 | Cadastrar compra parcelada | Prévia de parcelas, soma exata, datas válidas e compromisso futuro visível |
| RF19 | P0 | Registrar pagamento total/parcial | Saldo aberto e extrato atualizados juntos; não aceitar excesso sem destino explícito |
| RF20 | P0 | Informar desconto, juros, multa e ajuste | Componentes separados, justificativa e efeito visível antes de confirmar |
| RF21 | P0 | Estornar recebimento ou pagamento | Movimento compensatório e vínculo original; histórico permanece íntegro |
| RF22 | P0 | Cancelar obrigação ainda não liquidada | Cancela saldo futuro e preserva histórico; registro pago exige estorno do pagamento |
| RF23 | P0 | Cadastrar cartão | Apelido, titular, fechamento, vencimento e limite informado, sem PAN/CVV |
| RF24 | P0 | Lançar compra no cartão e suas parcelas | Total/parcelas/faturas conciliados; compra não reduz saldo bancário |
| RF25 | P0 | Fechar, consultar e pagar fatura | Pagamento parcial preserva dívida; quitação reduz caixa uma única vez |
| RF26 | P0 | Exibir painel de gestão da casa | Saldo, previsto/realizado, projeção, vencimentos, faturas e alertas com detalhamento |
| RF27 | P0 | Exibir fluxo de caixa diário e mensal | Mostra saldo inicial, entradas, saídas, saldo final e trecho futuro diferenciado |
| RF28 | P0 | Criar orçamento mensal por categoria | Limites independentes do gasto; cópia do mês anterior pede revisão; sem dupla soma de subcategorias |
| RF29 | P0 | Alertar aproximação e estouro de orçamento | Limiares configuráveis; exemplo 80%/100%; alerta explica categoria, valor e excesso |
| RF30 | P0 | Alertar saldo projetado negativo e vencimentos | Data do déficit, origem e obrigações acessíveis; projeção não altera saldo real |
| RF31 | P0 | Gerenciar central de alertas | Ler, adiar ou resolver; ausência de evento repetido para mesma condição/versão |
| RF32 | P0 | Filtrar e detalhar indicadores | Filtros por mês, conta, pessoa, categoria e situação consistentes entre gráfico e lista |
| RF33 | P0 | Consultar agenda financeira | Vencidos, hoje, próximos 7/30 dias e ocorrências futuras, com valor estimado sinalizado |
| RF34 | P1 | Anexar comprovantes | Upload e download autorizados pela casa; validação de tipo/tamanho; arquivo privado |
| RF35 | P1 | Exportar relatórios e dados da casa | Mesmo filtro e total da tela; arquivo acessível só a usuários autorizados; geração auditada |
| RF36 | P0 | Auditar mudanças relevantes | Autor, casa, recurso, ação, data e justificativa disponíveis a perfil autorizado |
| RF37 | P0 | Administrar piloto e situação das assinaturas | Operador ativa/suspende sem visualizar salários ou movimentações por padrão |
| RF38 | P1 | Cadastrar metas de reserva e acompanhar evolução | Valor alvo, prazo, conta vinculada e progresso; reservar não cria renda fictícia |
| RF39 | P1 | Conciliar saldo manualmente | Diferença, justificativa e ajuste explícito; sem sobrescrever extrato histórico |
| RF40 | P1 | Renegociar dívida | Vincula contrato anterior ao novo, encerra saldo substituído e evita duplicidade |
| RF41 | P0 | Ratear despesas por valor ou percentual e acompanhar contribuição | D10 confirmado; soma dos rateios igual ao total e distinção entre pagador e responsável |
| RF42 | P1 | Encerrar casa e solicitar exportação/exclusão | Fluxo de titular, prazo informado, revogação e retenção conforme política aprovada |
| RF43 | P2 | Cobrar assinatura automaticamente | Webhook autenticado/idempotente, eventos fora de ordem tratados e acesso por estado validado |
| RF44 | P2 | Importar CSV/OFX | Prévia, mapeamento de contas, duplicidade e confirmação antes de gravar |
| RF45 | P1 | Apresentar resumo de ações do mês | Recebimentos faltantes, contas sem confirmação, estouros, compromissos e categorias sem orçamento |
| RF46 | P1 | Simular uma nova despesa ou parcela | Simulação identificada, projeção recalculada, sem criar obrigação até confirmação |

## Acesso inicial

Todos os integrantes autorizados veem as informações financeiras da casa. Não existem lançamentos privados na V1. Permissões separam consulta, cadastro, lançamento, liquidação, estorno, orçamento, exportação e administração. A matriz final de perfis deve ser validada antes do seed.

O responsável pelo pagamento e a pessoa que cadastrou são campos distintos. Identidade do autor é obtida da sessão, nunca de um campo livre enviado pelo navegador.

## Parametrização e limite funcional

O usuário cria novos nomes e campos nos comportamentos financeiros suportados. Um novo mecanismo de cálculo (por exemplo amortização Price/SAC) exige uma nova regra de domínio, revisão e testes; não pode ser criado como texto livre e executado pelo servidor. O MVP registra prestações informadas e parcelas simples, sem presumir contrato bancário.
