# Backend Phase B-13 — SignalR Realtime

> **Теория:** [guides/b-13-realtime-signalr-theory.md](./guides/b-13-realtime-signalr-theory.md) — статус: placeholder

**Длительность:** 2 недели (20–25 ч)  
**Предусловия:** [B-07](./backend-phase-07-rabbitmq-basics.md), [B-05](./backend-phase-05-keycloak-auth.md), [B-11](./backend-phase-11-multi-tenant-isolation.md)  
**Цель:** SignalR hub для live todo updates, Redis backplane, JWT auth на websocket, bridge из MassTransit consumers.

---

## Результат фазы

- [ ] `TodoHub` — groups per tenant+user: `tenant:{tid}:user:{uid}`
- [ ] JWT bearer auth on `/hubs/todos` negotiate
- [ ] Redis backplane для scale-out (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`)
- [ ] Events: `TodoCreated`, `TodoUpdated`, `TodoDeleted` — typed client interface
- [ ] `TodoUpdatedSignalRConsumer` — subscribes MassTransit, pushes to hub
- [ ] CORS + WebSocket proxy headers documented for nginx (B-23)
- [ ] Frontend contract doc: event payload shapes
- [ ] Integration test with `Microsoft.AspNetCore.SignalR.Client`

---

## Неделя 1 — Hub & auth

### B-13.1 SignalR setup

1. `services.AddSignalR().AddStackExchangeRedis(...)`
2. Map hub: `app.MapHub<TodoHub>("/hubs/todos")`
3. Enable detailed errors in Development

**Файл:** `src/TodoPlatform.Api/Hubs/TodoHub.cs`

### B-13.2 JWT for WebSockets

1. `OnMessageReceived` — read token from query `?access_token=` for WS
2. Same Keycloak validation as REST
3. `ITodoHubClient` interface with strongly typed methods

### B-13.3 Group management

1. On connect — resolve tenant from header/query, user from claims
2. `Groups.AddToGroupAsync(connectionId, groupName)`
3. On disconnect — cleanup logging

---

## Неделя 2 — Event bridge & scale

### B-13.4 MassTransit → SignalR

1. Consumer on `TodoCreatedIntegrationEvent`
2. Inject `IHubContext<TodoHub, ITodoHubClient>`
3. Broadcast to group `tenant:{tid}:user:{uid}` only — not global fanout

**Файл:** `Infrastructure/Realtime/TodoCreatedSignalRConsumer.cs`

### B-13.5 Update & delete events

1. Publish integration events from Update/Delete handlers (outbox)
2. Matching consumers for Updated/Deleted
3. Payload: minimal DTO `{ id, title, completed, version }`

### B-13.6 Tests & docs

1. Test client connects, receives event after REST create
2. Multi-instance test with Redis backplane (two API containers in compose)
3. `docs/realtime/signalr-client.md` — Angular `@microsoft/signalr` snippet

---

## Команды

```bash
dotnet add src/TodoPlatform.Api package Microsoft.AspNetCore.SignalR.StackExchangeRedis

docker compose up -d redis api

# test with signalr client (integration test)
dotnet test --filter "FullyQualifiedName~SignalR"

# manual: wscat not ideal — use Swagger + test harness or frontend Phase 4
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Hub connects | client negotiate 200 |
| 2 | JWT enforced | no token → 401 |
| 3 | Tenant isolation | events only to correct group |
| 4 | MassTransit bridge | create → WS event |
| 5 | Redis backplane | 2 instances receive |
| 6 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-13 | Frontend Phase 4–5 realtime NgRx effects |
| Phase 4 | `signalRService.connect()`, dispatch actions on events |
| B-23 | nginx WebSocket upgrade config |

Parallel skills: Design chat at scale — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-14 Azure Blob File Storage](./backend-phase-14-files-storage.md)
