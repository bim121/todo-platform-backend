# Backend Phase B-07 — RabbitMQ & MassTransit

> **Теория:** [guides/b-07-rabbitmq-basics-theory.md](./guides/b-07-rabbitmq-basics-theory.md) — статус: placeholder  
> **Связь:** Outbox из [B-04](./backend-phase-04-domain-events.md)

**Длительность:** 2–3 недели (25–30 ч)  
**Предусловия:** [B-06](./backend-phase-06-redis-caching.md), [B-04](./backend-phase-04-domain-events.md)  
**Цель:** Async messaging через RabbitMQ + MassTransit, outbox publisher, email notification consumer.

---

## Результат фазы

- [ ] RabbitMQ 3.x в docker-compose (management UI :15672)
- [ ] MassTransit + RabbitMQ transport configured
- [ ] Outbox publisher background service — poll `outbox_messages`, publish, mark ProcessedAt
- [ ] Integration event `TodoCreatedIntegrationEvent` (отделён от domain event)
- [ ] Consumer `SendTodoCreatedEmailConsumer` — logs/simulated SMTP
- [ ] Retry + error queue (MassTransit default)
- [ ] Idempotent consumer — check `ProcessedMessages` table
- [ ] MassTransit health check
- [ ] ADR-023: Outbox pattern

---

## Неделя 1 — RabbitMQ + MassTransit setup

### B-07.1 Docker RabbitMQ

1. Service `rabbitmq:3-management-alpine`
2. Ports: 5672 (AMQP), 15672 (UI)
3. Default user/pass in compose env — `todo` / `todo` (dev)

### B-07.2 MassTransit registration

1. Packages: `MassTransit`, `MassTransit.RabbitMQ`
2. `AddMassTransit` — configure endpoint `todo-created-email`
3. Consumer assembly scan from `Infrastructure`
4. `UsingRabbitMq` — host, credentials from config

**Файл:** `src/TodoPlatform.Api/Extensions/MessagingExtensions.cs`

### B-07.3 Integration events mapping

1. `DomainEventToIntegrationEventMapper` — TodoCreated → TodoCreatedIntegrationEvent
2. Payload in outbox: `{ "type": "...", "data": { ... } }`
3. Version field in envelope for evolution

---

## Неделя 2 — Outbox publisher

### B-07.4 OutboxProcessor hosted service

1. Poll every 5s: `SELECT * FROM outbox_messages WHERE processed_at IS NULL LIMIT 100`
2. Publish to MassTransit `IPublishEndpoint`
3. UPDATE `processed_at` in same DB transaction (or two-phase: publish then mark)
4. Handle failures — leave unprocessed for retry

**Файл:** `Infrastructure/Messaging/OutboxProcessor.cs`

### B-07.5 Wire CreateTodo flow

1. `EfUnitOfWork.CommitAsync` — domain events → outbox rows (B-04)
2. End-to-end: POST todo → outbox row → processor → queue → consumer
3. Disable synchronous email — all async

### B-07.6 Idempotency table

1. Migration `V004__processed_messages.sql`
2. Consumer stores `MessageId` before side effect
3. Duplicate delivery → skip

---

## Неделя 3 — Consumers & reliability

### B-07.7 SendTodoCreatedEmailConsumer

1. Log structured: `TodoCreatedEmailSent { TodoId, UserEmail }`
2. Optional: Mailhog container in compose for visual test
3. Configure retry intervals: 3 attempts exponential

### B-07.8 Monitoring & tests

1. MassTransit test harness — consumer receives message
2. Integration test with Testcontainers.RabbitMq (optional)
3. RabbitMQ UI — verify queue depth, publish rate
4. Document local debug in `docs/messaging/rabbitmq-dev.md`

### B-07.9 Future hooks

1. Stub consumer for `TodoCompletedIntegrationEvent` (B-13 SignalR bridge)
2. Comment in code: Kafka audit stream — B-16

---

## Команды

```bash
docker compose up -d rabbitmq

dotnet add src/TodoPlatform.Infrastructure package MassTransit
dotnet add src/TodoPlatform.Infrastructure package MassTransit.RabbitMQ

# management UI
start http://localhost:15672  # todo/todo

dotnet run --project src/TodoPlatform.Api

# trigger flow
curl -X POST http://localhost:5000/api/todos \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"title":"Async test"}'

# check outbox
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb \
  -c "SELECT id, type, processed_at FROM outbox_messages ORDER BY created_at DESC LIMIT 5;"

dotnet test --filter "FullyQualifiedName~MassTransit"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | RabbitMQ up | management UI login |
| 2 | Outbox → queue | message in queue after create |
| 3 | Consumer runs | log/email stub fired |
| 4 | Idempotent | duplicate message ignored |
| 5 | Retry on failure | poison message → error queue |
| 6 | Tests green | `dotnet test` |
| 7 | ADR-023 | outbox documented |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-07 | API остаётся sync REST — UX без изменений |
| B-13 | Consumer публикует в SignalR hub |
| Phase 5 | Toast «email sent» optional via websocket later |

Parallel skills: Design notification system — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-08 Docker Full Stack](./backend-phase-08-docker-compose.md)
