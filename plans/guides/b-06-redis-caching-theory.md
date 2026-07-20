# B-06 — Redis Caching (теория)

> **Статус:** full  
> **Практика:** [../backend-phase-06-redis-caching.md](../backend-phase-06-redis-caching.md)  
> **ADR:** [../../docs/adr/022-caching-strategy.md](../../docs/adr/022-caching-strategy.md)

Этот гайд — разбор **всего**, что сделано в B-06 (неделя 1 + 2): зачем Redis, cache-aside, ключи, TTL, инвалидация через domain events, метрики.

---

## 1. Зачем эта тема

На FAANG / Microsoft L63+ интервью спрашивают:

- cache-aside vs write-through vs write-behind;
- как не отдать **чужой** кэш (ключ с `userId`);
- как инвалидировать после write;
- stampede / thundering herd;
- TTL для пустых результатов.

В нашем API `GET /todos` идёт в Postgres на каждый запрос. Redis держит уже сериализованные **DTO**, чтобы:

1. снизить latency и нагрузку на БД;
2. одинаково работать на нескольких инстансах API (в отличие от in-memory словаря в процессе).

---

## 2. Базовые концепции

| Термин | Смысл |
|--------|--------|
| **Distributed cache** | Общий store (Redis), не локальная память одного pod |
| **Cache-aside** | Приложение само читает/пишет кэш; БД — source of truth |
| **HIT / MISS** | Ключ найден / не найден → factory (репозиторий) |
| **TTL** | Time-to-live — автоудаление ключа |
| **Invalidation** | Явное удаление ключей после мутации |
| **Prefix delete** | Удалить все ключи `todos:user:{id}:…` разом |

### Cache-aside (наш выбор)

```
Client → API → ICacheService.GetOrSet
                    │
          ┌─────────┴─────────┐
          │ HIT               │ MISS
          ▼                   ▼
     return DTO         repository → DB
                              │
                              ▼
                         set Redis + return
```

После **write** (create/update/delete) мы **не обновляем** кэш значением — мы **удаляем** ключи. Следующий read сделает MISS и подтянет свежие данные.

---

## 3. Глубокое погружение (как устроено в коде)

### 3.1 Слои

```
Application
  ICacheService, CacheKeys, CacheMetrics, CacheTtl
  GetTodosQueryHandler / GetTodoByIdQueryHandler  → GetOrSet
  *CacheInvalidator (MediatR INotificationHandler)

Infrastructure
  RedisCacheService  → IDistributedCache + IConnectionMultiplexer (SCAN)
  MemoryCacheService → тесты / Cache:UseMemory
```

`IDistributedCache` (Microsoft) умеет get/set/remove **одного** ключа.  
`RemoveByPrefixAsync` требует **StackExchange.Redis** `KEYS`/`SCAN` — поэтому в DI есть `IConnectionMultiplexer`.

Instance name `TodoPlatform:` добавляется Redis-провайдером к каждому ключу. SCAN ищет `TodoPlatform:todos:user:{guid}*`.

### 3.2 Ключи

```csharp
// Application/Caching/CacheKeys.cs
CacheKeys.TodosByUser(userId, activeOnly, skip, take);
// → "todos:user:{guid}:aFalse:s-:t-"

CacheKeys.TodosByUserPrefix(userId);
// → "todos:user:{guid}"   // для RemoveByPrefix

CacheKeys.TodoById(todoId);
// → "todo:{guid}"
```

Почему в list-ключе есть `active/skip/take`?  
Иначе `GET /todos?activeOnly=true` и полный список делили бы один ключ → **баг**.

Почему prefix без суффиксов?  
После create нужно сбросить **все** варианты фильтров этого пользователя одной командой.

### 3.3 TTL

| Случай | TTL |
|--------|-----|
| Список с элементами | 5 мин (`CacheTtl.TodosList`) |
| **Пустой** список | **30 сек** (`TodosListEmpty`) |
| Один todo | 10 мин (`TodoById`) |

Пустой список нельзя кэшировать надолго: пользователь только что зарегистрировался / удалил всё — иначе 5 минут «тишины».

Реализация: после `factory` смотрим `value is ICollection { Count: 0 }` и выбираем короткий TTL.

### 3.4 Invalidation flow

```
CreateTodo → SaveChanges → Publish TodoCreatedEvent
                              → TodoCreatedCacheInvalidator
                                 RemoveByPrefix(todos:user:{uid})

Complete via Update → Todo.Complete() → TodoCompletedEvent
                              → remove todo:{id} + list prefix

Delete → MarkDeleted() → TodoDeletedEvent → same

Update title only → нет domain event
                              → UpdateTodoHandler сам Remove*
```

**Важно:** domain event handlers работают **после** успешного commit (через UoW / TransactionBehavior из B-04). Кэш не чистится, если транзакция откатилась.

### 3.5 Memory vs Redis

| Режим | Когда |
|-------|--------|
| Redis | `ConnectionStrings:Redis` + `Cache:UseMemory=false` |
| Memory | тесты / `Database:UseInMemory` / `Cache:UseMemory=true` |

Так `WebApplicationFactory` не требует живой Redis.

### 3.6 Метрики (prep B-24)

```csharp
public sealed class CacheMetrics
{
    public void RecordHit();
    public void RecordMiss();
    public long Hits { get; }
    public long Misses { get; }
}
```

Пока process-local (`Interlocked`). В B-24 станут Prometheus counters `cache_hits_total` / `cache_misses_total`.

---

## 4. Примеры кода (C#)

### GetOrSet в query handler

```csharp
public async Task<IReadOnlyList<TodoDto>> Handle(GetTodosQuery request, CancellationToken ct)
{
    var userId = request.UserId ?? currentUser.UserId;
    var key = CacheKeys.TodosByUser(userId, request.ActiveOnly, request.Skip, request.Take);

    return await cache.GetOrSetAsync(
        key,
        async token =>
        {
            var todos = await repository.ListAsync(
                TodoListSpecification.Create(userId, request.ActiveOnly, request.Skip, request.Take),
                token);
            return todos.Select(TodoDto.FromEntity).ToList();
        },
        CacheTtl.TodosList,
        ct,
        emptyCollectionTtl: CacheTtl.TodosListEmpty);
}
```

### Инвалидация

```csharp
public async Task Handle(TodoCreatedEvent e, CancellationToken ct)
{
    await cache.RemoveByPrefixAsync(CacheKeys.TodosByUserPrefix(e.UserId), ct);
}
```

### Docker

```yaml
redis:
  image: redis:7-alpine
  ports: ["6379:6379"]
  volumes: [redisdata:/data]
```

```bash
docker compose up -d redis
docker compose exec redis redis-cli PING   # PONG
docker compose exec redis redis-cli KEYS "TodoPlatform:*"
```

### HIT / MISS в логах (Debug)

```
Cache MISS for key todos:user:…:aFalse:s-:t-
Cache HIT for key todos:user:…:aFalse:s-:t-
```

---

## 5. Плюсы / минусы / когда НЕ использовать

| Плюсы | Минусы |
|-------|--------|
| Меньше load на Postgres | Stale до инвалидации / TTL |
| Масштабируется горизонтально | SCAN prefix — не идеал на огромных keyspace |
| Простая ментальная модель | Нужен Redis в compose/ops |
| DTO в кэше ≠ EF tracking bugs | Два источника правды (TTL + events) |

**Когда НЕ кэшировать:**

- редко читаемые / сильно персонализированные запросы;
- данные, где stale = security bug (permissions) — тогда короткий TTL + жёсткая инвалидация;
- write-heavy без выгоды на read.

---

## 6. Сравнение с альтернативами

| Подход | Популярность | Когда выбрать |
|--------|--------------|---------------|
| **Cache-aside** (мы) | ★★★★★ | Classic API reads |
| Write-through | ★★★☆☆ | Нужна свежесть в кэше сразу при write |
| Write-behind | ★★☆☆☆ | Высокий write throughput, сложный consistency |
| HTTP cache / CDN | ★★★★☆ | Публичные GET (у нас auth — осторожно) |
| EF 2nd level cache | ★★☆☆☆ | Редко; сложнее с multi-instance |
| HybridCache (.NET 9+) | ★★★★☆ | L1 memory + L2 Redis — рассмотреть позже |

---

## 7. Типичные ошибки

1. **Ключ без `userId`** → user A видит todos user B.  
2. **Кэш entity с navigation** → JSON циклы / lazy load. Кэшируем **DTO**.  
3. **Invalidate только `todo:{id}`**, забыв list → список устарел.  
4. **Бесконечный TTL пустого списка**.  
5. **Кэш до commit** → rollback, а кэш уже «есть».  
6. **KEYS в prod на миллионах ключей** → блокирует Redis; нужен SCAN + лимиты или индекс ключей в Set.  
7. **Stampede**: 100 MISS одновременно → 100 одинаковых SQL. Лечение: `SemaphoreSlim` per key (опционально в фазе) или singleflight.

---

## 8. Вопросы на интервью

1. Объясни cache-aside vs write-through на примере `GET /todos`.  
2. Как гарантировать, что кэш не утечёт между tenant/user?  
3. Что делать при cache stampede?  
4. Почему SCAN для prefix delete — компромисс? Какие альтернативы (Redis Set of keys, hash tag)?  
5. Как тестировать кэш без Redis? (`MemoryCacheService` / Testcontainers)  
6. Где в пайплайне CQRS вызывать invalidation — handler команды или domain event? (у нас: events + явный update)

**Короткий story:**  
«Read path — cache-aside через `ICacheService` и Redis. Ключи scoped по userId и фильтрам. Writes инвалидируют list prefix и todo key через domain events после UoW commit; update title — явно в command handler. Пустые списки — TTL 30s. Метрики hits/misses — prep к Prometheus.»

---

## 9. Связь с другими фазами

| Фаза | Связь |
|------|--------|
| B-03 CQRS | Query handlers вызывают cache |
| B-04 Domain events | Invalidators как `INotificationHandler` |
| B-05 Auth | `userId` из JWT / `ICurrentUserService` |
| B-08 Compose | Redis всегда в full stack |
| B-11 Multi-tenant | Ключи станут `todos:tenant:{tid}:user:{uid}` |
| B-19 Redis advanced | Rate limit / locks на том же Redis |
| B-24 OTel | Counters → Prometheus |

---

## 10. Ресурсы

- [IDistributedCache](https://learn.microsoft.com/aspnet/core/performance/caching/distributed)  
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)  
- [Cache-Aside pattern (Azure)](https://learn.microsoft.com/azure/architecture/patterns/cache-aside)  
- Код: `src/TodoPlatform.Infrastructure/Caching/`, `Application/Caching/`, `*CacheInvalidator.cs`
