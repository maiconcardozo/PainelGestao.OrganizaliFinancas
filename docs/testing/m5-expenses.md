# M5 — Despesas e liquidação

Validação em 10/10/2026 com dados sintéticos, MariaDB 11.8 descartável e serviços externos inexistentes. Nenhum banco persistente foi alterado e nenhum envio externo ocorreu.

## Testes .NET

`ORGANIZALI_DATABASE_TESTS=1 dotnet test tests/PainelGestao.OrganizaliFinancas.Tests` → **69 aprovados, 0 falhas**.

Unitários novos (`Unit/ExpenseRulesTests.cs`): 100 em 3 parcelas (33,33 + 33,33 + 33,34) com dia 31; parcelamentos inválidos; rateio por percentual com maiores restos e desempate pelo código; rateio por valor e percentual inválidos; saldo aberto do exemplo das regras (155 / 255 / paga / cancelada / vencida derivada); desconto negativo e saldo negativo recusado; despesa paga não cancela nem muda principal; plano de recorrência idempotente preservando ocorrência travada; acerto Bruno → Ana de 50.

Integração (`Helpers/ExpenseAssertions.cs`, API real e banco descartável):

| Cenário | Resultado esperado e obtido |
|---|---|
| Tipo Luz com campo obrigatório ausente | 400 |
| Mesma `Idempotency-Key` e corpo | 201 com o mesmo `Code` |
| Leitor lança; outra casa consulta | 403 / 404 |
| 250 + juros 10 − desconto 5; desconto 300 | Saldo 255; 409 |
| Pagamento 100 | Paga em parte, aberto 155, conta 4.900 |
| Pagar 155,01; data futura; leitor; sem chave | 409 / 400 / 403 / 400 |
| Cancelar despesa com pagamento | 409 |
| Estorno pelo leitor; pelo titular; repetido | 403; aberto volta a 255 e conta a 5.000; 409 |
| Quitação com juros de 2 no mesmo pagamento | Pagamento 257, situação paga |
| Dois pagamentos simultâneos de 60 em conta de 100 | Um 201 e um 409; aberto 40 |
| Cancelar sem pagamento; pagar cancelada | Cancelada com motivo; 409 |
| Prévia e criação de 100 em 3 | 33,33 / 33,33 / 33,34 em três competências; soma 100 no banco |
| Tipo avulso usado em parcelamento | 409 |
| Recorrência estimada de 120 | 13 ocorrências; gerar de novo 0/0/0 |
| Pagar a 1ª, editar a 3ª (131), alterar esta e próximas para 150 | 120 (paga), 150, 131 (editada), 150 |
| Esta e próximas antes da vigência | 409 |
| 100 dividido 50/50, pago na hora pela Ana | Uma despesa paga; cotas 50/50; Ana +50; acerto Bruno → Ana 50 |
| Rateio por valor somando 90 | 400 |
| Extrato | Pagamento e estorno ligados ao mesmo `PaymentCode` |
| Duplicidade de ocorrências no banco | Nenhuma data repetida por recorrência |

## Jornada web

`ORGANIZALI_DATABASE_TESTS=1 node --test tests/frontend/auth-flow.test.mjs tests/frontend/pilot-browser.test.mjs` → **2 aprovados**. Novos passos: editor lança “Conta de luz” de R$ 250,00, paga R$ 100,00 (fica “Paga em parte”, R$ 150,00 em aberto), vê a prévia de 3 parcelas com R$ 33,34 na última e lança a compra; editor não vê “Estornar”; titular estorna o pagamento e a conta volta a “Em aberto”. Sem rolagem horizontal em 360 px.

Capturas em `TestResults/browser-m3/`: `despesas-desktop.png` e `despesas-mobile.png`. Lint, `tsc --noEmit` e `next build` aprovados. A revisão das capturas levou a dois ajustes: o mês aparecia como “Outubro De 2026” (capitalização de cada palavra) e a situação “Em aberto” aparecia duas vezes por linha; no celular o seletor de mês virou setas e o resumo passou a duas colunas.
