# Backend Phase B-17 — Microservices Split & YARP

> **Теория:** [guides/b-17-microservices-split-theory.md](./guides/b-17-microservices-split-theory.md) — статус: placeholder

**Длительность:** 4–5 недель (40–50 ч)  
**Предусловия:** [B-16](./backend-phase-16-kafka-streaming.md), [B-08](./backend-phase-08-docker-compose.md), [B-10 GraphQL](./backend-phase-10-complex-sql-readmodels.md)  
**Цель:** Разделить monolith на сервисы + **gRPC между ними** — учим protobuf в момент split, не в конце roadmap.

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
- [ ] `.proto` in `src/contracts/proto/` — `todos.proto`, `notifications.proto`
- [ ] gRPC server в Todos.Api + gRPC client в Notifications.Api (stub)
- [ ] Unary RPC: `NotifyTodoCreated`, `GetTodoSummary`
- [ ] JWT/metadata propagation: REST/GraphQL → gRPC `authorization` header
- [ ] YARP route `/graphql` → GraphQL host (from B-10)
- [ ] gRPC **internal only** — не expose публично без TLS
- [ ] ADR-017: gRPC vs REST for inter-service calls

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

## Неделя 4 — gRPC (service-to-service)

> **Момент обучения:** split monolith → синхронные вызовы между сервисами через gRPC, async — через RabbitMQ/Kafka (B-16).

### B-17.11 Proto definitions

**Файл:** `src/contracts/proto/todos/v1/todos.proto`

```protobuf
syntax = "proto3";
package todos.v1;
option csharp_namespace = "TodoPlatform.Contracts.Grpc.Todos";

service TodoNotifications {
  rpc NotifyTodoCreated (NotifyTodoCreatedRequest) returns (NotifyTodoCreatedResponse);
  rpc GetTodoSummary (GetTodoSummaryRequest) returns (TodoSummary);
}
```

```bash
dotnet add src/services/Todos/TodoPlatform.Todos.Api package Grpc.AspNetCore
dotnet add src/services/Notifications/TodoPlatform.Notifications.Api package Grpc.Net.Client
dotnet add src/contracts/TodoPlatform.Contracts.Grpc package Grpc.Tools
```

### B-17.12 gRPC server (Todos.Api)

**Файл:** `Grpc/TodoNotificationsGrpcService.cs` — implement `NotifyTodoCreated`.

### B-17.13 gRPC client (Notifications.Api)

Register `GrpcChannel` → `http://todos-api:8080`. Call when consumer needs todo details without HTTP hop.

### B-17.14 Inter-service auth

Pass `Metadata` with bearer token or internal service account JWT.

### B-17.15 Gateway routes (REST + GraphQL + gRPC)

| Path | Target |
|------|--------|
| `/api/*` | REST services |
| `/graphql` | GraphQL server (B-10) |
| gRPC | internal Docker network only |

---

## Неделя 5 — gRPC tests & perf notes

### B-17.16 Integration test

`WebApplicationFactory` + `GrpcChannel.ForAddress` — `NotifyTodoCreated` works.

### B-17.17 Load comparison doc

**Файл:** `docs/perf/rest-vs-grpc-internal.md` — same operation: REST hop vs gRPC binary.

### B-17.18 Interview prep

- «gRPC vs REST для public API?» → never public gRPC without good reason
- «When GraphQL h2c + gRPC internal?» → diagram in ADR-017

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
| 7 | gRPC NotifyTodoCreated | integration test |
| 8 | `/graphql` via gateway | Banana Cake Pop through :8080 |
| 9 | gRPC not public | network diagram |
| 10 | No breaking OpenAPI | contract unchanged externally |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-17 | Frontend keeps single `apiUrl` — gateway |
| Phase 13+ | No URL changes if gateway preserves paths |
| B-17 gRPC | Frontend **не** вызывает gRPC — [Phase 13-GraphQL](../../anular-ngrx-todo-auth/plans/phase-13-graphql-client.md) week 4 (architecture) |
| B-10 GraphQL | `/graphql` on gateway |
| B-23 | nginx replaces YARP at edge (optional) |

Parallel skills: Design notification system — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-18 Saga Patterns](./backend-phase-18-saga-patterns.md)
