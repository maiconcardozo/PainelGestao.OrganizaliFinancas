# Contexto para continuidade

Desenvolvimento autorizado pelo usuário após os marcos. Iniciar por M1.1. Backend em Minimal API e frontend Next.js separado; Swagger conforme swagger-global, com identidade Organizali. A decisão substitui controllers sem alterar as demais camadas; ver ADR-0002.

Início organizado em M1.1–M3.4 em plans/development-milestones.md. D19/D20 confirmados: português, America/Sao_Paulo configurável por casa; cada login pertence a uma casa no piloto. Modelo de usuário segue User : TenantEntity do RevendaPRO, sem identidade global/múltiplos vínculos.

Organizali Finanças é projeto particular de SaaS doméstico. Usuário quer escopo completo antes do desenvolvimento e exige perguntas quando houver dúvidas. RevendaPRO é referência técnica principal; Arquitetura.Global orienta camadas; CP Comunica foi apenas comparação inicial.

Confirmado: uma casa por assinatura; dados compartilhados entre integrantes; permissões de edição; piloto manual; Foundation.Base autorizado; BRL; renda com bruto opcional, líquido previsto e recebido real; rateio por valor ou percentual; dashboard moderno com saldo, fluxo de caixa, orçamento e alertas. Wiki recusada. Não há chamado Azure/Redmine associado.

Estado: M1.1 publicado e M1.2 concluído. Usuário autorizou escrita de teste somente em MariaDB descartável; 22 testes aprovados. Ver testing/m1-2-persistence.md. M1.3 implementado com 29 testes aprovados, publish e assets HTTP conferidos; inspeção visual pendente porque não há navegador conectado. M1.4 concluído tecnicamente: frontend compila, lint aprovado, imagens Docker e comunicação web/API verificadas sem banco persistente. Visual RevendaPRO confirmado. Próximo M2.1; ver testing/m1-4-web.md. Commit por marco concluído; push somente ao final.

Skills usadas: clean-ddd-hexagonal, clean-code e frontend-design. Usar skills adicionais só quando correspondam à etapa real. Referências técnicas e versões em architecture/references.md e overview.md. Dúvidas bloqueiam apenas a implementação dependente, não a documentação independente.

Arquivos de entrega em C:/Codex/Upload; documentação de desenvolvimento também permanece neste repositório. A pasta .vs preexistia e não foi alterada.


Continuação autorizada pelo usuário para todos os marcos, mantendo commits por marco e push ao final. M2.1 implementado: provisionamento administrativo, assinatura e histórico; 29 testes aprovados incluindo migration incremental e isolamento por FK. M2.2 é o próximo. Perguntas sobre matriz de permissões e corte de cartão enviadas; sem resposta ainda, não fazer seed nem presumir decisão. Visual RevendaPRO para Organizali confirmado (D15).
