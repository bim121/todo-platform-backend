# Backend Phase B-18 — Saga Patterns

> **Теория:** [guides/b-18-saga-patterns-theory.md](./guides/b-18-saga-patterns-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-17](./backend-phase-17-microservices-split.md), [B-12](./backend-phase-12-tenant-schema-versioning.md), [B-07](./backend-phase-07-rabbitmq-basics.md)  
**Цель:** MassTransit Saga для `BulkApplyMigrationCommand`, compensating transactions, saga state in Postgres, timeout handling.

---

## Результат фазы

- [ ] `BulkApplyMigrationCommand` — apply migration to N tenants
- [ ] `BulkMigrationSaga` state machine — MassTransit
- [ ] Saga tables in Postgres (`saga_state`, MassTransit EF integration)
- [ ] Steps: Start → ApplyPerTenant → Notify → Complete | Compensate
- [ ] Compensation: rollback migration marker / alert ops (logical undo)
- [ ] Timeout: saga fault after 30 min, status `Failed`
- [ ] `GET /admin/migrations/bulk/{sagaId}` — status query
- [ ] Idempotent saga start — duplicate request returns existing sagaId
- [ ] ADR-032: Orchestration vs choreography

---

## Неделя 1 — Saga infrastructure

### B-18.1 MassTransit saga persistence

1. `MassTransit.EntityFrameworkCore` + saga DbContext
2. Migration for saga tables
3. Register saga in Admin service (or Todos if monolith still)

**Файл:** `Admin/Infrastructure/Sagas/BulkMigrationSaga.cs`

### B-18.2 State machine design

1. States: `Initial`, `Applying`, `Completed`, `Faulted`, `Compensating`
2. Events: `BulkMigrationStarted`, `TenantMigrationSucceeded`, `TenantMigrationFailed`, `BulkMigrationCompleted`
3. Correlate by `CorrelationId` / `SagaId`

### B-18.3 BulkApplyMigrationCommand handler

1. Validates admin role + migration plan exists
2. Publishes `BulkMigrationStarted` — does NOT apply synchronously
3. Returns `{ sagaId, tenantCount }` 202 Accepted

---

## Неделя 2 — Orchestration steps

### B-18.4 Per-tenant worker

1. Consumer `ApplyTenantMigrationActivity` — calls existing ApplyTenantMigration logic
2. Publish success/failure events per tenant
3. Saga increments counter, decides complete or fault

### B-18.5 Compensation path

1. On partial failure policy: `StopOnFirstError` vs `ContinueAll` (config)
2. Compensation: mark failed tenants, emit `TenantMigrationCompensated`
3. Admin notification via RabbitMQ email consumer

### B-18.6 Status API

1. `GetBulkMigrationStatusQuery(sagaId)`
2. Response: `{ state, completed, failed, tenants: [...] }`
3. OpenAPI update under `/admin/migrations/bulk`

---

## Неделя 3 — Reliability & tests

### B-18.7 Timeouts & retries

1. Saga timeout schedule — MassTransit `SchedulePublish`
2. Retry transient DB errors — Polly in activity
3. Poison tenant skipped with explicit failure reason

### B-18.8 Tests

1. Saga harness test — all tenants succeed → Completed
2. One tenant fails → policy verified
3. Duplicate start → same saga id returned

### B-18.9 Frontend integration notes

1. Admin UI polls status endpoint every 2s
2. Document in admin-panel-spec.md bulk migration section

---

## Команды

```bash
dotnet add src/TodoPlatform.Admin.Api package MassTransit.EntityFrameworkCore

dotnet run --project src/TodoPlatform.Admin.Api -- --migrate

curl -X POST http://localhost:8080/admin/migrations/bulk \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{"targetVersion": 10, "tenantIds": ["...","..."]}'

curl http://localhost:8080/admin/migrations/bulk/<sagaId> \
  -H "Authorization: Bearer <admin_token>"

dotnet test --filter "FullyQualifiedName~Saga"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Saga persists state | saga table rows |
| 2 | Bulk apply async | 202 + sagaId |
| 3 | Per-tenant tracking | status lists tenants |
| 4 | Compensation runs | failed scenario test |
| 5 | Timeout works | fault state |
| 6 | ADR-032 | orchestration doc |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-18 | Admin bulk migration UI |
| Phase 15 | Progress bar polls saga status |
| B-28 | SwitchTenantTrackCommand may trigger saga |

Parallel skills: Design order saga — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-19 Redis Advanced (Rate Limit & Locks)](./backend-phase-19-redis-advanced.md)
