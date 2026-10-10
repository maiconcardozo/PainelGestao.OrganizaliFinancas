# Respostas HTTP planejadas

Adotar envelope do RevendaPRO `SuccessDetails<T>(status, title, detail, instance, data)`. Erros em ProblemDetails, mensagem em português, sem stack trace ou informação de outra casa. Corpo deve refletir o status HTTP real.

```json
{
  "status": 201,
  "title": "Created",
  "detail": "Pagamento registrado.",
  "instance": "/api/payments",
  "data": { "code": "<uuid-publico>", "amount": "100.00", "remainingAmount": "150.00" }
}
```

```json
{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "detail": "Este lançamento foi alterado. Atualize os dados antes de continuar.",
  "instance": "/api/payments",
  "code": "concurrency_conflict",
  "traceId": "<correlacao>"
}
```

| Status | Uso |
|---|---|
| 200 | Consulta/alteração com corpo |
| 201 | Criação com Location |
| 202 | Exportação enfileirada, quando implementada |
| 204 | Sucesso sem corpo, sem envelope |
| 400 | Formato/validação; errors por campo |
| 401 | Sessão ausente/expirada |
| 403 | Operação sem permissão |
| 404 | Recurso inexistente ou fora da casa |
| 409 | Versão divergente, idempotência conflitante ou duplicidade |
| 422 | Regra financeira não atendida |
| 429 | Limite de requisições, com Retry-After |
| 500 | Falha inesperada, mensagem genérica e correlação |

OpenAPI documenta payloads, permissões, erros e exemplos. Middleware centraliza exceções. Endpoints Minimal API recebem command/query e encaminham ao IMediator; metadados de autor e tenant são resolvidos do contexto seguro. DTOs expõem Code, nunca Id interno. Downloads têm Content-Type/Disposition e não usam envelope JSON.
