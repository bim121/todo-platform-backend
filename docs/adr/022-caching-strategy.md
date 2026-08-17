# ADR-022: Redis cache-aside strategy

| | |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2026-07-20 |
| **Фаза** | B-06 |
| **Теория** | [plans/guides/b-06-redis-caching-theory.md](../../plans/guides/b-06-redis-caching-theory.md) |

---

## Context

Read-heavy endpoints (`GET /api/todos`, `GET /api/todos/{id}`) бьют PostgreSQL на каждый запрос. Нужен distributed cache, общий для нескольких инстансов API, с явной инвалидацией при мутациях.

---

## Decision

### 1. Pattern: **cache-aside** (lazy loading)

```
Read:  cache? → HIT return | MISS → DB → set cache → return
Write: DB commit → domain event / handler → delete keys
```

Не write-through: мутации уже идут через EF + UoW; дублировать запись в Redis усложняет consistency.

### 2. Abstraction: `ICacheService`

- `GetOrSetAsync`, `RemoveAsync`, `RemoveByPrefixAsync`
- JSON serialize DTO (не EF entities)
- Implementations: `RedisCacheService` (prod), `MemoryCacheService` (tests / `Cache:UseMemory`)

### 3. Keys

| Key | TTL |
|-----|-----|
| `todos:tenant:{tenantId}:user:{userId}:a{active}:s{skip}:t{take}` | 5 min (empty list → **30 s**) |
| `todo:tenant:{tenantId}:{id}` | 10 min |
| `stats:tenant:{tenantId}:user:{userId}` | 1 min |

Prefix invalidation: `todos:tenant:{tenantId}:user:{userId}` снимает все варианты фильтров.

B-11: tenant is part of every key so tenant B cannot read tenant A’s cached DTOs.

### 4. Invalidation

| Trigger | Action |
|---------|--------|
| `TodoCreatedEvent` | `RemoveByPrefix(todos:tenant:{tid}:user:{uid})` |
| `TodoCompletedEvent` | remove `todo:tenant:{tid}:{id}` + list prefix |
| `TodoDeletedEvent` | same |
| `UpdateTodoHandler` | same (title/status без domain event) |

### 5. Metrics (prep for B-24)

`CacheMetrics` — process-local hits/misses. OTel counters later.

---

## Consequences

**+** Faster reads; multi-instance safe with Redis  
**+** Empty lists не залипают на 5 мин  
**−** SCAN for prefix — OK for pet/dev; prod → Redis Sets of keys or hash tags  
**−** Stale window: между commit и event handler (обычно &lt; ms in-process)

---

## Links

- `Infrastructure/Caching/RedisCacheService.cs`
- `Application/Caching/CacheKeys.cs`
- `Application/Todos/EventHandlers/*CacheInvalidator.cs`
