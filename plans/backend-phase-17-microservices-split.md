# Backend Phase B-17 — Microservices Split & YARP

> **Теория:** [guides/b-17-microservices-split-theory.md](./guides/b-17-microservices-split-theory.md) — статус: placeholder

**Длительность:** 3–4 недели (35–45 ч)  
**Предусловия:** [B-16](./backend-phase-16-kafka-streaming.md), [B-08](./backend-phase-08-docker-compose.md)  
**Цель:** Разделить monolith на Todo.Service, Identity.Service (stub), Admin.Service, YARP API Gateway, shared contracts.

---

## Результат фазы

- [ ] Solution structure: `TodoPlatform.Todos.Api`, `.Admin.Api`, `TodoPlatform.Gateway`
- [ ] YARP reverse proxy routes `/api/todos/*`, `/admin/*`, `/hubs/*`
- [ ] Shared kernel: `TodoPlatform.Contracts` (DTOs, events)
- [ ] Each service own DbContext scope (shared DB still OK — logical split)
- [ ] Service-to-service auth: internal JWT or mTLS stub
- [ ] Docker compose — 3 API containers + gateway on :8080
- [ ] Health aggregation at gateway `/health`
- [ ] CorrelationId forwarded across services
- [ ] ADR-031: monolith split boundaries

---

## Неделя 1 — Solution split

### B-17.1 Project extraction

1. Move todo handlers to `TodoPlatform.Todos.Api`
2. Move admin handlers to `TodoPlatform.Admin.Api`
3. Keep shared Domain/Application split or duplicate minimally — document choice
4. Each service has own `Program.cs`, Dockerfile

**Paths:**
- `src/services/Todos/TodoPlatform.Todos.Api/`
- `src/services/Admin/TodoPlatform.Admin.Api/`
- `src/gateway/TodoPlatform.Gateway/`

### B-17.2 Shared contracts package

1. `TodoPlatform.Contracts` — NuGet project reference
2. DTOs, integration events, OpenAPI fragments
3. Versioning policy: semver on contracts

### B-17.3 Database ownership

1. Todos service owns `todos`, `todo_attachments`
2. Admin owns reads to `tenants`, `migration_*`, `audit_logs`
3. Shared Postgres — separate schemas optional: `todos`, `admin`

---

## Неделя 2 — YARP gateway

### B-17.4 YARP configuration

1. `Yarp.ReverseProxy` package in Gateway
2. `appsettings.json` routes:

```json
"ReverseProxy": {
  "Routes": {
    "todos": { "ClusterId": "todos", "Match": { "Path": "/api/todos/{**catch-all}" } },
    "admin": { "ClusterId": "admin", "Match": { "Path": "/admin/{**catch-all}" } }
  }
}
```

3. Transforms: forward Authorization, X-Tenant-Id, CorrelationId

### B-17.5 SignalR proxy

1. Route WebSocket to Todos service
2. `ActivityTimeout` tuned for long connections
3. Test through gateway not direct port

### B-17.6 Unified Swagger

1. Gateway aggregates OpenAPI (optional Swashbuckle multi-doc)
2. Or document per-service swagger ports for dev

---

## Неделя 3 — Docker & communication

### B-17.7 Compose multi-service

1. Services: `gateway`, `todos-api`, `admin-api`, infra unchanged
2. Internal network DNS: `http://todos-api:8080`
3. Only gateway exposes host port 8080

### B-17.8 Async boundaries

1. Todos publishes to RabbitMQ/Kafka — Admin consumes audit (already B-16)
2. No synchronous admin→todos HTTP except read replicas later
3. `GetSystemStatsQuery` in Admin — Dapper cross-schema read

---

## Неделя 4 — Hardening

### B-17.9 Resilience

1. Polly timeout on gateway clusters (if gateway calls health)
2. Circuit breaker notes for future external calls
3. Load test: gateway overhead < 5ms p95

### B-17.10 Tests & migration path

1. E2E test through gateway — full todo CRUD
2. Document strangler fig pattern — what stays monolithic until B-18
3. ADR-031 published

---

## Команды

```bash
dotnet new web -n TodoPlatform.Gateway -o src/gateway/TodoPlatform.Gateway
dotnet add src/gateway/TodoPlatform.Gateway package Yarp.ReverseProxy

docker compose -f docker-compose.yml -f docker-compose.microservices.yml build
docker compose -f docker-compose.yml -f docker-compose.microservices.yml up -d

curl http://localhost:8080/api/todos -H "Authorization: Bearer <token>" -H "X-Tenant-Id: <t>"

dotnet test tests/TodoPlatform.E2E.Tests
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | 3 services build | docker images |
| 2 | Gateway routes | todos + admin via :8080 |
| 3 | WebSocket via gateway | SignalR connect |
| 4 | Headers forwarded | tenant + auth work |
| 5 | E2E tests | CRUD through gateway |
| 6 | ADR-031 | boundaries doc |
| 7 | No breaking OpenAPI | contract unchanged externally |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-17 | Frontend keeps single `apiUrl` — gateway |
| Phase 13+ | No URL changes if gateway preserves paths |
| B-23 | nginx replaces YARP at edge (optional) |

Parallel skills: Design notification system — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-18 Saga Patterns](./backend-phase-18-saga-patterns.md)
