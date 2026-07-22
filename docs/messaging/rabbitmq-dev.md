# RabbitMQ & MassTransit — local debug (B-07)

## Prerequisites

```bash
docker compose up -d postgres redis rabbitmq
# optional Mailhog UI for email capture:
docker compose --profile dev-ui up -d mailhog
```

| Service | URL / port | Credentials |
|---------|------------|-------------|
| RabbitMQ Management | http://localhost:15672 | `todo` / `todo` |
| AMQP | `localhost:5672` | `todo` / `todo` |
| Mailhog UI | http://localhost:8025 | — |
| Mailhog SMTP | `localhost:1025` | — |

API `appsettings.Development.json` enables SMTP to Mailhog (`Smtp:Enabled: true`).

---

## End-to-end flow

1. Start API: `dotnet run --project src/TodoPlatform.Api`
2. Create a todo (`POST /api/todos` with auth).
3. Within ~5s `OutboxProcessor` publishes from `outbox_messages`.
4. Check logs for `TodoCreatedEmailSent {TodoId} {UserEmail}`.
5. If Mailhog is up — open http://localhost:8025 and confirm the message.

### Outbox SQL

```bash
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb \
  -c "SELECT id, type, processed_at FROM outbox_messages ORDER BY created_at DESC LIMIT 5;"
```

`processed_at` should become non-null after the processor runs.

### RabbitMQ UI checks

1. Login at http://localhost:15672
2. **Queues** — look for `todo-created-email` and `todo-completed-notification`
3. After create: message rates / Get Message (if not yet consumed)
4. On consumer failure after retries: `*_error` queue (MassTransit default)

---

## Queues & consumers

| Queue | Consumer | Message |
|-------|----------|---------|
| `todo-created-email` | `SendTodoCreatedEmailConsumer` | `TodoCreatedIntegrationEvent` |
| `todo-completed-notification` | `TodoCompletedNotificationConsumer` (stub → B-13 SignalR) | `TodoCompletedIntegrationEvent` |

Retry: **3 attempts**, exponential backoff (1s–30s). Then `_error` queue.

Idempotency: `processed_messages.MessageId` (same as outbox row id).

---

## Tests

```bash
# unit + MassTransit in-memory harness (no Docker broker required)
dotnet test --filter "FullyQualifiedName~Messaging"

# optional: Testcontainers.RabbitMq — not wired; harness covers consumer receive path
```

---

## Disable messaging

- Tests: `RabbitMq:Enabled: false` + environment `Testing`
- Local without broker: set `RabbitMq:Enabled: false` in appsettings
