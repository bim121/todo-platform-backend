# Backend Phase B-19 — Redis Advanced (Rate Limit & Locks)

> **Теория:** [guides/b-19-redis-advanced-theory.md](./guides/b-19-redis-advanced-theory.md) — статус: placeholder

**Длительность:** 1–2 недели (15–20 ч)  
**Предусловия:** [B-06](./backend-phase-06-redis-caching.md), [B-17](./backend-phase-17-microservices-split.md)  
**Цель:** Distributed rate limiting per user/tenant, RedLock for critical sections, idempotency keys on POST.

---

## Результат фазы

- [ ] AspNetCoreRateLimit or custom Redis sliding window middleware
- [ ] Limits: 100 req/min per user, 1000 req/min per tenant
- [ ] `429 Too Many Requests` + `Retry-After` header
- [ ] `IDistributedLock` — RedLock.net or Redis SET NX EX
- [ ] Lock on `ApplyTenantMigrationCommand` — one migration per tenant at a time
- [ ] Idempotency-Key header on `POST /api/todos` — 24h Redis store
- [ ] Admin override header `X-Bypass-RateLimit` (dev/admin only)
- [ ] Metrics: rate_limit_exceeded_total
- [ ] ADR-033: rate limit algorithm (sliding vs token bucket)

---

## Неделя 1 — Rate limiting

### B-19.1 Redis rate limit store

1. Implement `IRateLimitService` — sliding window with sorted sets
2. Key: `ratelimit:user:{userId}`, `ratelimit:tenant:{tenantId}`
3. Middleware early in pipeline — after auth + tenant resolution

**Файл:** `Api/Middleware/RateLimitMiddleware.cs`

### B-19.2 Configuration

1. `appsettings.json` sections `RateLimit:User`, `RateLimit:Tenant`
2. Whitelist health checks and swagger
3. Return ProblemDetails on 429

### B-19.3 Gateway integration

1. Rate limit at gateway OR per service — pick one (ADR)
2. Forward `Retry-After` to client
3. k6 test script prep (B-22) — verify 429 under load

---

## Неделя 2 — Locks & idempotency

### B-19.4 Distributed lock

1. `RedisDistributedLock` with auto-renewal for long migrations
2. Wrap `ApplyTenantMigrationHandler` in `await using (await lock.AcquireAsync(...))`
3. Timeout exception → 409 Conflict

### B-19.5 Idempotency keys

1. Middleware reads `Idempotency-Key` header
2. Store `{ key → response }` in Redis TTL 24h
3. Duplicate POST returns cached 201 response body
4. Only for POST todos and bulk migration start

### B-19.6 Tests

1. Unit test sliding window math
2. Integration: 101st request → 429
3. Parallel migration apply — second waits or conflicts

---

## Команды

```bash
dotnet add src/TodoPlatform.Infrastructure package RedLock.net

# quick rate limit test
for i in {1..105}; do curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:8080/api/todos -H "Authorization: Bearer <token>"; done

dotnet test --filter "FullyQualifiedName~RateLimit"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | 429 after limit | loop curl |
| 2 | Retry-After set | response headers |
| 3 | Tenant limit | separate bucket |
| 4 | Migration lock | concurrent apply blocked |
| 5 | Idempotency | duplicate POST same response |
| 6 | ADR-033 | algorithm documented |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-19 | Frontend handles 429 — retry with backoff |
| Phase 3 | Idempotency-Key on optimistic create |
| B-22 | k6 validates limits under load |

Parallel skills: Design rate limiter — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-20 DB Replication & Scaling](./backend-phase-20-db-replication-scaling.md)
