# M4 — Contas e rendas

Validação em 10/10/2026 com dados sintéticos, MariaDB 11.8 descartável e serviços externos inexistentes. Nenhum banco persistente foi alterado e nenhum envio externo ocorreu.

## Testes .NET

`ORGANIZALI_DATABASE_TESTS=1 dotnet test tests/PainelGestao.OrganizaliFinancas.Tests` → **58 aprovados, 0 falhas** (`TestResults/m4.trx`).

Unitários novos (`Unit/IncomeRulesTests.cs`): ocorrências por periodicidade, dia 31, vigência nova afetando só datas posteriores, plano de geração idempotente sem reescrever previsões travadas, janela da média (outubro → 9 meses; janeiro → ano anterior ÷ 12; 135.000 → 15.000), formato monetário invariável, soma alocada acima do recebido, data anterior à abertura e trava da base da conta.

Integração (`Helpers/FinanceAssertions.cs`, via API real e banco descartável):

| Cenário | Resultado esperado e obtido |
|---|---|
| Previsão 4.000; recebimentos 2.000 + 1.800 | Recebido 3.800, diferença −200, falta 200 |
| Mesma `Idempotency-Key` e mesmo corpo | 201 com o mesmo `Code`; nenhum registro novo |
| Mesma chave com outro corpo / sem chave | 409 / 400 |
| Alocação maior que o recebido; data futura; data antes da abertura | 409 / 400 / 409 |
| Leitor registra recebimento; outra casa usa conta alheia | 403 / 404 |
| Transferir 500 | Total da casa inalterado (4.800); poupança 500 |
| Estornar transferência; repetir estorno; leitor estorna | Saldos restaurados; 409; 403 |
| Estornar recebimento de 1.800 | Original e compensação coexistem no extrato; recebido 2.000; total 3.000 |
| Alterar abertura com movimentos | 409 |
| PJ base 14.000 recebendo 16.000 (D25) | Recebido 16.000, diferença +2.000, falta 0 |
| Nova vigência 15.000 | Outubro (com recebimento) mantém 14.000; novembro passa a 15.000 |
| Gerar previsões novamente | 0 inseridas, 0 alteradas, 0 canceladas |
| Comissão variável com 9.000 em mês fechado (D24) | Sem previsto; estimativa e média = 9.000 ÷ meses fechados |
| Comparativo mensal | Previsto 18.000 e recebido 18.000 para o morador no mês |
| Duplicidade de previsões no banco | Nenhuma data repetida por fonte |

## Jornada web

`ORGANIZALI_DATABASE_TESTS=1 node --test tests/frontend/auth-flow.test.mjs tests/frontend/pilot-browser.test.mjs` → **2 aprovados**. A jornada (Chrome headless isolado, `next start`, API Release e MariaDB descartável) agora inclui: editor cria duas contas, transfere R$ 500 com o total mantido, cria fonte de R$ 4.000, registra R$ 2.000 e R$ 1.800 pela tela vinculando a previsão, vê “Falta receber R$ 200,00” e não vê “Estornar”; titular estorna com motivo e a previsão volta a faltar R$ 2.000. Sem rolagem horizontal em 360 px nas telas Contas e Rendas.

Capturas em `TestResults/browser-m3/`: `contas-desktop.png`, `contas-mobile.png`, `rendas-desktop.png`, `rendas-mobile.png`. Lint, `tsc --noEmit` e `next build` aprovados.

A revisão das capturas levou a dois ajustes antes da entrega: o comparativo mensal deixou de listar meses futuros (pareciam falta de renda) e a tabela de contas passou a empilhar no celular para não esconder as ações.
