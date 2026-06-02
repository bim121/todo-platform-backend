# Backend Phase B-16 — Kafka Audit Streaming

> **Теория:** [guides/b-16-kafka-streaming-theory.md](./guides/b-16-kafka-streaming-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-07](./backend-phase-07-rabbitmq-basics.md), [B-04](./backend-phase-04-domain-events.md)  
**Цель:** Apache Kafka для immutable audit log, produce audit events from domain/outbox, consumer для audit store + analytics prep.

---

## Результат фазы

- [ ] Kafka + Zookeeper (or KRaft) in docker-compose profile `streaming`
- [ ] Topic `todo.audit.v1` — partitioned by TenantId
- [ ] `IAuditEventPublisher` — CloudEvents-style envelope
- [ ] Audit events: TodoCreated, TodoUpdated, TodoDeleted, TenantMigrationApplied
- [ ] `AuditLogConsumer` — writes to `audit_logs` table (append-only)
- [ ] RabbitMQ remains command/async; Kafka for event streaming (ADR)
- [ ] Schema: JSON with `schemaVersion` field (Avro optional note)
- [ ] Retention policy 30 days in dev; document prod compaction
- [ ] Dashboard query: `GET /admin/audit?tenantId=&from=&to=` (admin)

---

## Неделя 1 — Kafka infrastructure

### B-16.1 Docker Kafka

1. Use `bitnami/kafka` or Confluent single-node compose
2. Ports: 9092 (PLAINTEXT dev)
3. Create topic on startup script: `todo.audit.v1`, partitions 6

**Файл:** `docker-compose.streaming.yml` or profile

### B-16.2 Producer setup

1. `Confluent.Kafka` NuGet
2. `KafkaAuditEventPublisher` implements `IAuditEventPublisher`
3. Register singleton producer with retry config
4. Key = `{tenantId}:{aggregateId}` for ordering

### B-16.3 Wire domain events

1. MediatR pipeline behavior `AuditPublishingBehavior` for ICommand
2. Or dedicated handler on domain events → map to audit envelope
3. Include: UserId, TenantId, CorrelationId, Timestamp, Payload diff

---

## Неделя 2 — Consumer & storage

### B-16.4 audit_logs table

1. Migration `V011__audit_logs.sql` — jsonb payload, immutable (no UPDATE grant)
2. Indexes: `(tenant_id, occurred_at)`, `(aggregate_id)`

### B-16.5 AuditLogConsumer

1. Background hosted service or MassTransit Kafka rider (choose one — document)
2. Idempotent insert by `(event_id)` unique
3. Dead letter topic `todo.audit.dlq.v1` on poison messages

### B-16.6 Admin read API

1. `GetAuditLogsQuery` — Dapper paginated filter
2. `[Authorize(Roles = "admin")]`
3. OpenAPI under `/admin/audit`

---

## Неделя 3 — Operations & tests

### B-16.7 Correlation & tracing prep

1. Propagate `CorrelationId` from HTTP middleware to Kafka headers
2. Link to OpenTelemetry trace id (B-24)

### B-16.8 Tests

1. Testcontainers.Kafka — produce and consume one message
2. Verify audit row after CreateTodo
3. Load test: 1000 events/sec note (not full benchmark)

### B-16.9 ADR-030

1. RabbitMQ vs Kafka responsibilities
2. Event sourcing NOT adopted — audit only
3. Interview story: compliance audit trail design

---

## Команды

```bash
docker compose --profile streaming up -d kafka

dotnet add src/TodoPlatform.Infrastructure package Confluent.Kafka

# create topic (example)
docker exec todo-platform-kafka kafka-topics.sh --create \
  --topic todo.audit.v1 --partitions 6 --replication-factor 1 \
  --bootstrap-server localhost:9092

# consume debug
docker exec todo-platform-kafka kafka-console-consumer.sh \
  --bootstrap-server localhost:9092 --topic todo.audit.v1 --from-beginning --max-messages 5

dotnet test --filter "FullyQualifiedName~Audit"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Kafka up | topic exists |
| 2 | Events produced | consumer sees messages |
| 3 | audit_logs populated | SQL count increases |
| 4 | Idempotent consumer | duplicate skipped |
| 5 | Admin API | GET /admin/audit |
| 6 | ADR-030 | Rabbit vs Kafka |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-16 | Admin audit viewer (optional UI) |
| Phase 15 Admin | Timeline component consumes /admin/audit |
| B-24 | Metrics on lag consumer group |

Parallel skills: Design event sourcing audit — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-17 Microservices Split & YARP](./backend-phase-17-microservices-split.md)
