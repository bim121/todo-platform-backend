# B-33 — Concurrency & Parallelism (Theory)

> **Практика:** [../backend-phase-33-concurrency-parallelism.md](../backend-phase-33-concurrency-parallelism.md)  
> **Java twin:** [../../../todo-platform-java/plans/guides/j-33-concurrency-virtual-threads-theory.md](../../../todo-platform-java/plans/guides/j-33-concurrency-virtual-threads-theory.md)

## 1. Зачем эта тема

ASP.NET Core async model + Channels — core для high-throughput APIs. Microsoft L63+: `IAsyncEnumerable`, bounded parallelism, DbContext thread safety.

## 2. Базовые концепции

- `async`/`await` — не создаёт поток на каждый request
- `Parallel.ForEachAsync` — bounded CPU/I/O parallelism
- `System.Threading.Channels` — producer/consumer backpressure
- EF Core: один `DbContext` на scope

## 3. Глубокое погружение

- Thread pool starvation symptoms
- `SemaphoreSlim` vs `lock` в async
- Graceful shutdown с `BackgroundService`

## 4. Примеры кода

См. практику B-33.

## 5. Плюсы / минусы

| Плюсы | Минусы |
|-------|--------|
| Channels — explicit backpressure | Сложнее чем fire-and-forget |
| IAsyncEnumerable streaming | Client must support streaming |
| Parallel.ForEachAsync в .NET 6+ | Легко исчерпать DB pool |

## 6. Сравнение с Java Virtual Threads

См. Java guide J-33.

## 7. Вопросы на интервью

1. `Task.Run` в ASP.NET — когда плохо?
2. Channel bounded vs unbounded?
3. Как тестировать race conditions в handlers?
