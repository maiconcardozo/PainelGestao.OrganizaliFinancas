# Contexto para continuidade

Desenvolvimento autorizado pelo usuário após os marcos. Iniciar por M1.1. Backend em Minimal API e frontend Next.js separado; Swagger conforme swagger-global, com identidade Organizali. A decisão substitui controllers sem alterar as demais camadas; ver ADR-0002.

Início organizado em M1.1–M3.4 em plans/development-milestones.md. D19/D20 confirmados: português, America/Sao_Paulo configurável por casa; cada login pertence a uma casa no piloto. Modelo de usuário segue User : TenantEntity do RevendaPRO, sem identidade global/múltiplos vínculos.

Organizali Finanças é projeto particular de SaaS doméstico. Usuário quer escopo completo antes do desenvolvimento e exige perguntas quando houver dúvidas. RevendaPRO é referência técnica principal; Arquitetura.Global orienta camadas; CP Comunica foi apenas comparação inicial.

Confirmado: uma casa por assinatura; dados compartilhados entre integrantes; permissões de edição; piloto manual; Foundation.Base autorizado; BRL; renda com bruto opcional, líquido previsto e recebido real; rateio por valor ou percentual; dashboard moderno com saldo, fluxo de caixa, orçamento e alertas. Wiki recusada. Não há chamado Azure/Redmine associado.

Estado: M1.1 concluído. Cinco camadas, restore, build Release sem avisos e 18 testes aprovados. Host preparado para Minimal API. Evidências em testing/m1-1-foundation.md. Próximo M1.2; banco, endpoints, Swagger e frontend não implementados. Nenhum deploy.

Skills usadas: clean-ddd-hexagonal, clean-code e frontend-design. Usar skills adicionais só quando correspondam à etapa real. Referências técnicas e versões em architecture/references.md e overview.md. Dúvidas bloqueiam apenas a implementação dependente, não a documentação independente.

Arquivos de entrega em C:/Codex/Upload; documentação de desenvolvimento também permanece neste repositório. A pasta .vs preexistia e não foi alterada.
