# Backend Phase B-06 — Redis Cache

> **Теория:** [guides/b-06-redis-caching-theory.md](./guides/b-06-redis-caching-theory.md) — статус: placeholder

**Длительность:** 1–2 недели (15–20 ч)  
**Предусловия:** [B-05](./backend-phase-05-keycloak-auth.md), [B-04](./backend-phase-04-domain-events.md)  
**Цель:** Distributed cache для read-heavy queries, cache-aside pattern, invalidation через domain events.

---

## Результат фазы

- [ ] Redis 7 в `docker-compose.yml` (port 6379)
- [ ] `IDistributedCache` + `StackExchange.Redis` configuration
- [ ] `ICacheService` wrapper — GetOrSet, RemoveByPrefix, JSON serialization
- [ ] `GetTodosQuery` — cache key `todos:user:{userId}` TTL 5 min
- [ ] `GetTodoByIdQuery` — cache key `todo:{id}` TTL 10 min
- [ ] `TodoCreatedCacheInvalidator` / `TodoDeletedCacheInvalidator` handlers (из B-04)
- [ ] Cache stampede protection — optional `SemaphoreSlim` per key
- [ ] Health check `/health` includes Redis
- [ ] Response header `X-Cache: HIT|MISS` (dev/debug)

---

## Неделя 1 — Infrastructure

### B-06.1 Redis docker service

1. Добавить `redis:7-alpine` в compose с volume `redisdata`
2. Connection string: `localhost:6379` in appsettings
3. Optional: redis-commander UI on :8081 (dev)

```yaml
redis:
  image: redis:7-alpine
  ports:
    - "6379:6379"
  volumes:
    - redisdata:/data
```

### B-06.2 Package & registration

1. `Microsoft.Extensions.Caching.StackExchangeRedis`
2. `services.AddStackExchangeRedisCache(...)`
3. `services.AddSingleton<ICacheService, RedisCacheService>()`
4. Configure `JsonSerializerOptions` for DTO caching

**Файл:** `Infrastructure/Caching/RedisCacheService.cs`

### B-06.3 ICacheService API

1. `GetOrSetAsync<T>(key, factory, ttl, ct)`
2. `RemoveAsync(key)` and `RemoveByPrefixAsync("todos:user:")`
3. Key builder: `CacheKeys.TodosByUser(Guid userId)`
4. Log cache hit/miss at Debug level

---

## Неделя 2 — Query caching + invalidation

### B-06.4 Cache-aside in handlers

1. `GetTodosQueryHandler` — wrap repository call in `GetOrSetAsync`
2. `GetTodoByIdQueryHandler` — single entity cache
3. Do not cache empty lists with infinite TTL — use short TTL
4. `[Authorize]` — cache key must include userId from `ICurrentUserService`

### B-06.5 Domain event invalidation

1. `TodoCreatedEvent` → remove `todos:user:{userId}` prefix
2. `TodoCompletedEvent`, `TodoDeletedEvent` → remove todo + list keys
3. `UpdateTodoCommand` — invalidate both old and new user if reassigned (future)

### B-06.6 Observability prep

1. Counter `cache_hits_total`, `cache_misses_total` (simple metrics class — full OTel B-24)
2. Integration test: first call MISS, second HIT
3. Document TTL strategy in `docs/adr/022-caching-strategy.md`

---

## Команды

```bash
docker compose up -d redis

dotnet add src/TodoPlatform.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add src/TodoPlatform.Infrastructure package StackExchange.Redis

# redis CLI smoke
docker exec -it todo-platform-backend-redis-1 redis-cli PING
docker exec -it todo-platform-backend-redis-1 redis-cli KEYS "todos:*"

dotnet test src/TodoPlatform.Application.Tests --filter "FullyQualifiedName~Cache"
dotnet run --project src/TodoPlatform.Api
curl -v http://localhost:5000/api/todos -H "Authorization: Bearer <token>"  # check X-Cache
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Redis running | `redis-cli PING` → PONG |
| 2 | Cache HIT on repeat | X-Cache header or logs |
| 3 | Invalidation works | create todo → list refreshed |
| 4 | Auth-scoped keys | user A cannot hit user B cache |
| 5 | Health check | `/health` reports Redis |
| 6 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-06 | Прозрачно для frontend — faster API |
| Phase 3 | Optimistic UI + cache — frontend NgRx entity cache отдельно |
| B-19 | Rate limiting тоже на Redis |

Parallel skills: Design URL shortener / distributed cache — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-07 RabbitMQ & MassTransit](./backend-phase-07-rabbitmq-basics.md)
