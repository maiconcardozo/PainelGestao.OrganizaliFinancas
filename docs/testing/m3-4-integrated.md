# M3.4 — Revisão integrada de acesso e cadastros

Data: 10/10/2026. [Entrega e regras](../deliveries/M3.4.md) · [Estado geral](../status/PROJECT_STATUS.md).

## Jornada validada

O teste `tests/frontend/pilot-browser.test.mjs` executa Chrome headless com perfil novo e contextos separados, Next em modo produção, API real e MariaDB 11.8 descartável. Credenciais sintéticas são geradas por execução. Nenhum banco existente ou transporte externo é usado.

1. Migrar, provisionar e ativar uma casa por operações administrativas.
2. Entrar como titular; verificar que os cookies de sessão não estão disponíveis para JavaScript.
3. Criar morador sem login pela tela; conferir largura de 360px sem transbordamento horizontal.
4. Criar convite manual de editor, aceitar em sessão separada e entrar.
5. Criar categoria e tipo Água recorrente com campo obrigatório Instalação.
6. Confirmar rejeição de campo vazio no formulário e aprovação do valor preenchido pela API.
7. Aceitar convite de leitor; acesso à tela de integrantes negado, formulário de edição ausente e escrita direta na API com HTTP 403.
8. Abrir Swagger e verificar montagem do tema Organizali e operações.
9. Sair de todas as sessões e confirmar que a rota de moradores retorna ao login.

Isolamento entre casas, autoria, histórico, conflitos de versão, concorrência do último titular e consumo único de convite também são cobertos pelas asserções de integração .NET dos marcos anteriores.

## Correções encontradas

- `HeadContent` estava atribuído depois de `InjectStylesheet/InjectJavascript`, removendo suas inclusões no HTML. Ordem corrigida.
- Inserção do cabeçalho usa o pai efetivo da seção de informação do Swagger, compatível com seu DOM.
- Identidade e textos atualizados; removidas afirmações antigas de que login não existia. Link de operações aponta para a documentação abaixo da introdução.
- README, contexto, plano e documentos de entrega agora distinguem o estado vigente dos registros históricos.

Não houve nova dependência ou atualização de NuGet. A estrutura de código permanece a do RevendaPRO com a exceção Minimal API já aprovada.

## Reprodução

Pré-requisitos: versões do repositório, Docker ativo, Chrome em `C:/Program Files/Google/Chrome/Application/chrome.exe`, dependências restauradas. A escrita de testes é opt-in e exclusivamente no banco efêmero criado pelo teste.

```powershell
dotnet build PainelGestao.OrganizaliFinancas.slnx -c Release --no-restore
npm run build --prefix frontend
npm run lint --prefix frontend
node --test tests/frontend/auth-flow.test.mjs
$env:ORGANIZALI_DATABASE_TESTS = '1'
try {
    dotnet test PainelGestao.OrganizaliFinancas.slnx -c Release --no-build --logger 'trx;LogFileName=m3-4.trx' --results-directory TestResults
    node --test tests/frontend/pilot-browser.test.mjs
} finally {
    Remove-Item Env:ORGANIZALI_DATABASE_TESTS
}
```

Resultados: 38 testes .NET aprovados na validação do M3.3; build, lint e teste HTTP Next aprovados nessa etapa. No M3.4, build e teste integrado de navegador aprovados após a correção do Swagger. Não houve alteração de domínio ou banco no M3.4.

Evidências locais regeneráveis em `TestResults/browser-m3/`: `result.json`, `moradores-mobile.png`, `tipos-desktop.png`, `swagger-desktop.png`. As capturas foram inspecionadas. O banco e os processos criados pelo teste são encerrados no bloco de limpeza. Perfil de navegador de teste fica em TestResults e não é incluído na entrega.

## Limites

Revisão em Chrome desktop e viewport estreito; não equivale a auditoria completa de acessibilidade, leitores de tela ou compatibilidade em dispositivos físicos. Não houve deploy. Recuperação por e-mail real, operação externa e todas as capacidades financeiras de M4 em diante permanecem pendentes. M2.4 está integrado no escopo de acesso/cadastros, sem dashboard financeiro fictício.
