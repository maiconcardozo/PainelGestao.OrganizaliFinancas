# M2.4 — Interface de acesso (parte independente entregue)

M2.4 ainda não está completo: menu por permissões, integrantes e jornada de cadastros dependem de M2.3. A matriz de operações foi enviada ao usuário e aguarda resposta; não houve seed de perfis.

## Implementado

- Login real via proxy Next, com mensagens de erro, estado de envio e cookies HttpOnly.
- Layout autenticado, entrada da casa e consulta de nome/e-mail em Minha conta. Cada página verifica a sessão no servidor.
- Sessão ausente encaminha para login; refresh disponível permite renovar a sessão por uma ação explícita, sem expor tokens ao cliente.
- Logout informa que encerra todas as sessões e só sai da página após resposta da API. Falha de rede preserva a tela e apresenta erro.
- Navegação desktop/mobile, estado ativo, link de salto para o conteúdo, labels e foco visível.
- Somente Minha casa e Minha conta são apresentadas: páginas comuns de acesso, sem operações financeiras ou administrativas. O menu financeiro por permissões será fornecido pelo servidor em M2.3.

## Referência e visual

RevendaPRO/frontend/app/login/page.tsx, app/(panel)/layout.tsx, components/layout/PanelShell.tsx e lib/session.ts foram lidos. Mantida organização App Router, componentes em components/layout, sessão no servidor e navegação lateral. Layout adaptado ao Organizali, sem copiar módulos de veículos.

Tokens preservados: azul #0b1e3f, destaque #0090c4, fundo #edf1f4, branco e borda #d3dbe3; Source Sans 3 e Barlow Condensed. Login em duas colunas; shell com lateral de 264px e cabeçalho; no celular, menu expansível no fluxo, sem sobreposição. Não há valores financeiros simulados nas telas.

## Validação executada

- Build Next.js de produção, TypeScript e ESLint aprovados.
- Teste HTTP com Next real e API simulada: rotas privadas retornam redirecionamento para login sem cookies; refresh isolado encaminha para renovação; Minha conta renderiza dados da sessão e não inclui os tokens no HTML.
- Mantidos os testes de cookies, Origin, proxy e logout de M2.2.

Inspeção visual em navegador e interação assistiva ainda pendentes: não havia navegador conectado nesta sessão. Os testes HTTP não representam aprovação visual nem uma jornada end-to-end com API/banco real. Backend foi validado separadamente com MariaDB descartável.

## Pendência concreta

Confirmar a matriz proposta: titular administra; editor registra rendas/despesas, liquida e mantém orçamento; leitor consulta; estornos e integrantes restritos ao titular. A proposta não foi aplicada como decisão. Conforme plans/development-milestones.md, a revisão precede o seed; M3 depende dessa autorização integrada. A autorização geral para continuar não resolve a escolha de poderes dos perfis.
