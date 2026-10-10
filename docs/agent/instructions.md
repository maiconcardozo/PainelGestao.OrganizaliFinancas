# Instruções locais de desenvolvimento

1. Ler README, contexto, decisões, arquitetura/camadas/dependências, requisitos e documento da área antes de trabalhar.
2. Usar RevendaPRO como padrão: cinco camadas, código em inglês, UI em português, Dapper em runtime e EF apenas schema, Foundation autorizado, Id interno e Code UUID v7 público.
3. Perguntar ao usuário quando houver dúvida de regra; registrar resposta em product/decisions.md e atualizar documentos afetados. Nunca tratar silêncio como aprovação.
4. Implementar por marcos. Início do desenvolvimento autorizado após planejamento; começar por M1.1. Backend Minimal API e frontend separado conforme ADR-0002; não reintroduzir controllers.
5. Preservar precisão, histórico e invariantes de financial-rules.md. Pagamento e movimentos precisam ser atômicos; transação não se divide para obter ID.
6. Validar isolamento da casa em SQL Dapper, joins, escrita, jobs, arquivos e exportações. Menu oculto não é segurança.
7. Não atualizar NuGet. Conferir versões/compatibilidade no M1 e perguntar sobre incompatibilidade, sem trocar stack silenciosamente.
8. Não copiar dados, endpoints, senhas ou dependências operacionais corporativas. Não criar Wiki, Redmine ou Azure para este projeto particular sem pedido novo.
9. Usar clean-ddd-hexagonal para limites; clean-code para implementação/revisão; frontend-design para interface. Ler a skill aplicável quando iniciar a etapa; guias genéricos não substituem convenções aprovadas.
10. Na etapa Next.js, ler documentação da versão instalada indicada pelo AGENTS da referência, sem confiar em APIs de versões diferentes.
11. Testes com dados sintéticos e serviços externos falsos; definir banco efêmero isolado antes de testes de integração com escrita. Nenhum e-mail, WhatsApp, cobrança ou pagamento real em testes.
12. Entregas devem incluir o que mudou, evidência de validação e pendências reais. Arquivos destinados ao usuário em C:/Codex/Upload.
13. Ao criar ou revisar README, aplicar a skill `readme-projetos` e o modelo Authentication fornecido pelo usuário: título com ícone, badges, funcionalidades destacadas, árvore comentada, seções temáticas e navegação por público. Adaptar fatos e links ao estado real do Organizali; manter a arquitetura RevendaPRO. A aparência do README não é definida apenas pelo checklist genérico do Arquitetura.Global.

Instrução mais recente: um commit por marco concluído e push somente ao final. Não fazer push a cada etapa. Deploy não solicitado. Não inventar par Azure/Redmine para satisfazer convenção corporativa.
