# M6 — Cartões e faturas

Validação em 10/10/2026 com dados sintéticos, MariaDB 11.8 descartável e serviços externos inexistentes. Nenhum banco persistente foi alterado e nenhum envio externo ocorreu.

## Testes .NET

`ORGANIZALI_DATABASE_TESTS=1 dotnet test tests/PainelGestao.OrganizaliFinancas.Tests` → **77 aprovados, 0 falhas**.

Unitários novos (`Unit/CardScheduleTests.cs`): ciclo com vencimento no mesmo mês ou no seguinte e fechamento no último dia de fevereiro; regra do dia do fechamento (`next`/`current`) nos dias 24, 25 e 26; 300 em 3 faturas consecutivas, soma exata, compra à vista e deslocamento de um ciclo (dois ciclos recusado); apelido com número de cartão recusado; estorno acima do restante recusado.

Integração (`Helpers/CardAssertions.cs`, API real e banco descartável):

| Cenário | Resultado esperado e obtido |
|---|---|
| Apelido “4111 1111 1111 1111” | 400 |
| Leitor cadastra cartão; outra casa lista | 403 / lista vazia |
| Compra de 300 (mesma chave duas vezes) | 201 com o mesmo `Code`; conta continua 2.000 |
| Compra com data futura; leitor compra | 400 / 403 |
| Fatura | 300 no mês, fechamento e vencimento calculados pela regra; comprometido 300, disponível 700 |
| Editar a obrigação da fatura | 409 |
| Pagar 300 | Conta 1.700; fatura com 0 em aberto; disponível 1.000; soma das obrigações do cartão no banco = 300 |
| Fatura de 300 com pagamento de 100 | 200 em aberto |
| Prévia e compra de 300 em 3 | 100/100/100 em três faturas; com a compra anterior, as três somam 600 |
| Estorno total da compra parcelada | Restante 0; nenhum item ativo; segundo estorno 409 |
| Estorno parcial de 50 na fatura já paga | Fatura 250, em aberto −50 (crédito), item de crédito com o motivo; conta inalterada (1.600) |
| Outra casa consulta a fatura | 404 |
| Tipo “conta avulsa” no cartão; tipo “cartão” | 409 / 201 |

## Jornada web

`ORGANIZALI_DATABASE_TESTS=1 node --test tests/frontend/auth-flow.test.mjs tests/frontend/pilot-browser.test.mjs` → **2 aprovados**. Novos passos: editor cadastra “Visa da casa” com limite, vê a prévia de 1 parcela, lança compra de R$ 300,00 e o saldo da casa em Contas não muda; paga a fatura pela tela de Cartões, a fatura passa a “Paga” e o saldo da casa cai exatamente R$ 300,00. Sem rolagem horizontal em 360 px.

Capturas em `TestResults/browser-m3/`: `cartoes-desktop.png` e `cartoes-mobile.png`. Lint, `tsc --noEmit` e `next build` aprovados. Ajustes feitos durante a validação: a lista de faturas não exibia o cartão recém-criado (seleção calculada só na primeira renderização); fatura quitada antes do fechamento aparecia como “Aberta” (agora “Paga”); no celular a tabela de cartões rolava para o lado (valores agora quebram linha).
