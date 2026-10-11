# Entrega — Interface igual ao RevendaPRO

Data: 11/10/2026. Pedido do usuário depois do M8: “não gostei do front, ele não está baseado no RevendaPRO” e “os cadastros precisam ser tudo igual o estilo UX” (D26). Commit de origem: `UI Iguala visual e cadastros do frontend ao RevendaPRO`.

## O que mudou

- **Base visual do RevendaPRO** (`app/globals.css`): tokens de cor claro e escuro, controle único de 2,375 rem, fundo quadriculado, modal com véu até o menu, animações e redução de movimento. Fontes Source Sans 3, Barlow Condensed e IBM Plex Mono (valores).
- **Casca do painel** (`PanelShell`): menu lateral escuro recolhível com grupos (Visão geral, Dinheiro, Planejamento, Casa), barra superior translúcida com o nome da tela, troca de tema claro/escuro, menu do usuário e confirmação antes de sair. Perfil exibido como Titular, Editor ou Leitor.
- **Cabeçalho de página**: sobretítulo com o grupo do menu, título display e descrição; ação principal “Novo …” à direita.
- **Cadastros no fluxo do RevendaPRO**: Moradores, Integrantes, Cadastros financeiros, Tipos de despesa, Contas, Fontes de renda e Cartões com lista em **mosaico ou linhas** (escolha guardada no navegador), contagem, ícone de editar e formulário em **modal** com rótulos em caixa alta e botões Cancelar/Salvar no rodapé. Inativar continua sendo a forma de retirar um cadastro (não há exclusão física).
- **Lançamentos e ações** também em modal: nova despesa (avulsa, recorrente, parcelada), pagar despesa ou fatura, detalhes da despesa, cancelar, alterar recorrência, registrar recebimento, transferir, compra no cartão, estorno da loja, encerrar previsão, extrato da conta, convite e revogação de convite. Todo estorno pede motivo num modal próprio.
- **Telas públicas** (entrar, recuperar senha, nova senha, convite, sessão, sem permissão, erro) no layout de login do RevendaPRO: painel da marca à esquerda e formulário à direita.
- **Minha casa** passou a mostrar atalhos para as telas que o perfil pode abrir.

Regras de negócio, API e banco não mudaram.

## Validação

- `tsc`, `eslint` e `next build` aprovados.
- `auth-flow.test.mjs` aprovado.
- `pilot-browser.test.mjs` (Chrome real + Next + API + MariaDB descartável) aprovado com a jornada atualizada para os modais: cadastros, convites, contas, transferência, rendas e estorno, despesas e pagamento parcial, parcelamento, cartão e fatura, orçamento, painel e saída com confirmação. Sem rolagem lateral a 360 px.
- Capturas revisadas em desktop e celular: `TestResults/browser-m3/*.png` (cópia em `C:\Codex\Upload\Organizali-UI-RevendaPRO`).

## Pendências

- Tema escuro conferido pelos tokens do RevendaPRO; não houve revisão visual tela a tela no escuro.
- Exportação (botões do RevendaPRO) depende da matriz de exportação do M9.
