# M7 — Painel, fluxo de caixa, orçamento e alertas

Validação em 10/10/2026 com dados sintéticos, MariaDB 11.8 descartável e serviços externos inexistentes. Nenhum banco persistente foi alterado e nenhum envio externo ocorreu.

## Testes .NET

`ORGANIZALI_DATABASE_TESTS=1 dotnet test tests/PainelGestao.OrganizaliFinancas.Tests` → **85 aprovados, 0 falhas**.

Unitários novos (`Unit/PlanningTests.cs`): faixas 799,99 / 900 / 1.000 / 1.100 de 1.000; “sem orçamento” × limite zero; categoria pai somando filhas com total sem dupla contagem, cartão na competência e “Sem categoria”; projeção com vencida em hoje, primeiro déficit, menor saldo e evento fora do horizonte; alertas que só voltam com mudança de impressão ou fim do adiamento e leitura que não resolve.

Integração (`Helpers/PlanningAssertions.cs`):

| Cenário | Resultado esperado e obtido |
|---|---|
| Leitor grava orçamento | 403 |
| Mercado 1.000; despesa de 900 na subcategoria | Perto (90%); alerta novo de atenção visível também ao leitor |
| Leitor age no alerta; titular lê e resolve | 403; “lido”; some e não volta enquanto igual |
| Mais 200 na categoria pai | Comprometido 1.100, excesso 100; alerta volta como urgente com “Excesso de R$ 100,00 (10%)” |
| Remover limite; zero na subcategoria | Sem orçamento; zero excedido |
| Outra casa | Não vê a categoria |
| Preferências | Padrão 80/100/5; salvar → versão 1; mesma versão de novo 409; aproximação acima do limite 400 |
| Conta de 9.000.000 em 2 dias | Projeção parte do total das contas, déficit até o vencimento e causa listada; alerta de déficit urgente |
| Painel | Saldo igual ao total das contas; conta na agenda dos próximos 7 dias; orçamento e alerta presentes |
| Fluxo mensal | 12 meses; inicial + entradas + saídas + outros = final; final = inicial do seguinte; previsto a partir do mês atual; mês fora do intervalo 400 |
| Análise | Filtro pela categoria pai inclui a subcategoria (1.100 em 2 linhas); soma por categoria = total; “paga” = 900; conta que pagou retorna só a despesa paga; situação inválida 400 |

## Jornada web

`ORGANIZALI_DATABASE_TESTS=1 node --test tests/frontend/auth-flow.test.mjs tests/frontend/pilot-browser.test.mjs` → **2 aprovados**. Novos passos: editor salva limite de Moradia e vê “Dentro do limite”; leitor vê o orçamento sem campos de edição; titular abre o painel com o gráfico da projeção, fluxo de caixa, alertas e análise; nenhuma dessas telas tem rolagem horizontal em 360 px.

Capturas em `TestResults/browser-m3/`: `painel-desktop.png`, `painel-mobile.png`, `fluxo-desktop.png`, `fluxo-mobile.png`. Lint, `tsc --noEmit` e `next build` aprovados. Ajuste após a revisão das capturas: os rótulos do gráfico ficavam achatados no celular (texto dentro do SVG esticado); passaram a ser HTML posicionado sobre o gráfico, com o valor inicial, o final e a data do déficit.
