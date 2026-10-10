# M2.3 — Perfis e integrantes

Matriz D21 confirmada pelo usuário ao autorizar a continuação após a proposta: titular administra; editor cadastra rendas/despesas, liquida e mantém orçamento; leitor consulta. Estornos e integrantes ficam com titulares. Permissões financeiras estão definidas, mas seus endpoints pertencem aos próximos marcos.

## Padrão e implementação

- Role e UserRole seguem os vínculos do RevendaPRO, com IdTenant e FKs compostas para impedir associação entre casas. No piloto são três perfis de sistema; a matriz de ações aprovada fica em HouseholdAccess. Não foi criada administração de perfis personalizados nem perfil de plataforma atribuível pela casa.
- SessionDto fornece roles, permissions e menu já filtrado pelo servidor. Guarda de rota Next e autorização no caso de uso são independentes. Minimal API permanece fina com MediatR.
- Titular lista integrantes, cria/revoga convites, altera perfil e bloqueia/desbloqueia acesso. Bloqueio preserva o registro e invalida sessões. A troca de perfil também revoga sessões.
- Convite vincula e-mail, nome, casa e perfil; token aleatório armazenado por hash, válido por 48 horas e uso único. Não há envio externo: o titular recebe o link para compartilhar manualmente. Token do link fica no fragmento, removido ao abrir a tela, sem ser enviado na URL HTTP.
- Criação de usuário, vínculo ao perfil e consumo do convite usam um commit. Mutações de acesso são serializadas pela casa e releem identidade/permissão. Duas mudanças concorrentes não removem o último titular ativo.
- Frontend entrega tela Integrantes, convites pendentes, aceite de convite e mensagens de acesso negado.

## Atualização de casas anteriores

Aplicar a migration HouseholdMembers via operação Migrate. Casas novas recebem perfis e titular no provisionamento atômico. Para cada casa provisionada em M2.1/M2.2, executar explicitamente `--Operation=InitializeAccess`, com `Pilot__HouseholdCode` e `Pilot__Actor`, além da conexão própria. A operação cria perfis ausentes e só atribui o titular inicial quando a casa ainda não possui qualquer UserRole; não restaura permissões retiradas posteriormente. IDs públicos novos são Guid v7.

Nenhum banco persistente foi migrado durante esta entrega.

## Evidência

- 29 testes .NET aprovados com MariaDB descartável, incluindo novas asserções de integração em MemberAssertions. Resultado TestResults/m2-3.trx.
- Seed repetido idempotente; titular vê menu; editor/leitor não administram; alteração de perfil e bloqueio invalidam JWT; referência a usuário de outra casa retorna 404; perfil de plataforma é recusado.
- Convite aceito uma vez, convite revogado recusado, aceitação concorrente com um sucesso e uma recusa, duas destituições concorrentes com um sucesso e um conflito.
- Build e lint Next aprovados. Teste HTTP do Next verifica rota Integrantes bloqueada para leitor antes de consultar dados, além de proteção de origem no aceite.

Revisão visual e jornada completa no navegador permanecem pendentes. O teste de frontend utiliza API simulada; a API foi testada separadamente contra MariaDB real descartável. M2.4 agora tem a integração de permissões, mas sua aprovação visual não é presumida.
