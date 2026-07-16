# Backend Phase B-33 — Concurrency & Parallelism

> **Теория:** [guides/b-33-concurrency-parallelism-theory.md](./guides/b-33-concurrency-parallelism-theory.md) — статус: placeholder  
> **Parallel track:** [parallel-skills-backend.md](./parallel-skills-backend.md) — секция «Concurrency depth»

**Длительность:** 3 недели (25–35 ч)  
**Предусловия:** [B-03](./backend-phase-03-cqrs-mediatr.md), [B-09](./backend-phase-09-postgres-queries.md), [B-19](./backend-phase-19-redis-advanced.md)  
**Цель:** Production-grade async/parallel patterns в ASP.NET Core: bounded concurrency, thread-safe aggregates, background channels, load-safe bulk operations.

---

## Результат фазы

- [ ] `IBulkTodoImportService` — `Parallel.ForEachAsync` с `MaxDegreeOfParallelism` + `SemaphoreSlim` на DB pool
- [ ] `Channel<TodoImportItem>` + `BackgroundService` consumer — backpressure для heavy writes
- [ ] `IAsyncEnumerable<TodoDto>` endpoint `GET /api/todos/stream` — chunked response
- [ ] Thread-safe in-memory cache wrapper `ConcurrentDictionary` + `GetOrAdd` pattern
- [ ] MediatR `ConcurrencyBehavior` — detect re-entrant command per aggregate (`Todo` id)
- [ ] Integration test: 50 parallel `POST /api/todos` — no duplicate titles under idempotency key
- [ ] BenchmarkDotNet: sequential vs `Parallel.ForEachAsync` vs Channel pipeline
- [ ] ADR-040: when to use `Task` vs `ValueTask` vs `IAsyncEnumerable` in handlers
- [ ] Docs: `docs/concurrency/dotnet-patterns.md`

---

## Неделя 1 — Async fundamentals & bounded parallelism

### B-33.1 IAsyncEnumerable streaming

1. `GetTodosStreamQuery` handler yields batches of 100
2. Controller: `return Ok(_mediator.CreateStream(...))` or minimal API `Results.Stream`
3. Client: Angular can consume via fetch reader (doc only)

```csharp
public async IAsyncEnumerable<TodoDto> Handle(
    GetTodosStreamQuery request,
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var batch in _repo.StreamByUserAsync(request.UserId, 100, ct))
        foreach (var item in batch)
            yield return _mapper.Map<TodoDto>(item);
}
```

### B-33.2 Parallel.ForEachAsync — bulk import

1. `BulkImportTodosCommand` — list up to 10k items
2. `Parallel.ForEachAsync` with `new ParallelOptions { MaxDegreeOfParallelism = 4 }`
3. **Never** parallelize single DbContext — scope per item via `IServiceScopeFactory`

```csharp
await Parallel.ForEachAsync(items, parallelOptions, async (item, ct) =>
{
    await using var scope = _scopeFactory.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
    await mediator.Send(new CreateTodoCommand(item.Title, item.UserId), ct);
});
```

### B-33.3 SemaphoreSlim — protect shared resource

1. Global `SemaphoreSlim(32)` around external API calls (if any)
2. Per-tenant semaphore in Redis advanced path (link B-19)
3. Unit test: semaphore limits concurrent calls

---

## Неделя 2 — Channels & BackgroundService

### B-33.4 Channel-based pipeline

1. `Channel.CreateBounded<TodoCreatedEvent>(capacity: 1000, FullMode.Wait)`
2. Producer: domain event handler writes to channel
3. Consumer: `TodoEventBackgroundService : BackgroundService` reads and pushes to SignalR (B-13)

```csharp
public class TodoEventBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _reader.ReadAllAsync(stoppingToken))
            await _hubContext.Clients.Group(evt.Group).TodoCreated(evt.Payload);
    }
}
```

### B-33.5 Graceful shutdown

1. `IHostApplicationLifetime` — drain channel on stop
2. Health check: channel depth < threshold
3. Log dropped events = 0 on shutdown test

### B-33.6 Concurrent collections

1. `ConcurrentDictionary<Guid, SemaphoreSlim>` per-aggregate lock (optimistic concurrency)
2. Avoid `lock` on async code — use `SemaphoreSlim.WaitAsync`
3. Document anti-pattern: `Task.Run` for CPU work on thread pool starvation

---

## Неделя 3 — Testing, benchmarks, production rules

### B-33.7 Integration tests

1. `WebApplicationFactory` + 50 `Task.WhenAll` POST requests
2. Assert connection pool not exhausted (Npgsql metrics or log)
3. Idempotency-Key header — duplicate returns 200 same body

### B-33.8 BenchmarkDotNet

```bash
dotnet run -c Release --project tests/TodoPlatform.Benchmarks
```

Compare: sequential import, Parallel.ForEachAsync (DOP 2/4/8), Channel pipeline.

### B-33.9 Production checklist

| Правило | Почему |
|---------|--------|
| One DbContext per scope | EF not thread-safe |
| Always pass `CancellationToken` | K8s pod termination |
| Bound parallelism to pool size | Avoid thread pool + DB pool exhaustion |
| Prefer Channel over unbounded `Task.Run` | Backpressure |
| `ValueTask` only when cached result common | Avoid allocation |

### B-33.10 Interview story

«На bulk import использовал `Parallel.ForEachAsync` с отдельным scope на item, SemaphoreSlim ограничивал DB pressure, тяжёлые side-effects — через bounded Channel + BackgroundService с graceful shutdown.»

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Stream endpoint works | curl chunked response |
| 2 | Bulk import 1k items < 30s local | script + timer |
| 3 | No DbContext threading exceptions | integration test green |
| 4 | Channel consumer survives restart | kill pod test |
| 5 | Benchmark doc committed | `docs/concurrency/` |
| 6 | ADR-040 published | `docs/adr/ADR-040-concurrency.md` |

---

## Связь с Java track

Эквивалент: [J-33 Concurrency & Virtual Threads](../../todo-platform-java/plans/java-phase-33-concurrency-virtual-threads.md) — Virtual Threads вместо thread pool, `@Async` вместо BackgroundService.

---

## Следующая фаза

→ [B-34 AWS Foundations (SAA + DVA)](./backend-phase-34-aws-foundations.md)  
Portfolio: [B-31 System Design Capstone](./backend-phase-31-system-design-capstone.md) — обновить после B-36…B-38.
