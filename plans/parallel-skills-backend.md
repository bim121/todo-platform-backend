# Параллельный трек — Backend FAANG skills

**Время:** 2–3 ч/нед на протяжении backend roadmap.  
**Не блокирует фазы**, но обязателен для Microsoft/Google/Amazon L63+.

---

## 1. Backend System Design (2 кейса / месяц)

| # | Кейс | Связь с фазой |
|---|------|---------------|
| 1 | Design URL shortener | B-06 Redis |
| 2 | Design rate limiter | B-19 Redis advanced |
| 3 | Design notification system | B-07, B-17 |
| 4 | Design distributed cache | B-06, B-19 |
| 5 | Design multi-tenant SaaS DB | B-11, B-21 |
| 6 | Design event sourcing audit | B-16 Kafka |
| 7 | Design order saga (e-commerce) | B-18 Saga |
| 8 | Design search engine | B-15, B-29 |
| 9 | Design file upload + CDN | B-14 |
| 10 | Design chat at scale | B-13 SignalR |
| 11 | GraphQL BFF vs REST for admin dashboard | B-10 |
| 12 | gRPC vs REST between microservices | B-17 |
| 13 | Concurrency at scale (channels vs threads) | **B-33** |

**Формат:** `docs/system-design/backend/NN-<name>.md`

1. Requirements (functional, non-functional)
2. Estimations (QPS, storage, users)
3. High-level diagram
4. API design
5. Data model
6. Deep dives (2–3)
7. Tradeoffs
8. Failure modes

---

## 2. SQL & Database depth

| Неделя | Тема | Фаза |
|--------|------|------|
| 1–4 | Indexes, EXPLAIN | B-09 |
| 5–8 | Read replicas, pooling | B-20 |
| 9–12 | Partitioning, sharding | B-21 |
| 13+ | Load testing, locks | B-22 |
| 14+ | Channels, Parallel.ForEachAsync | **B-33** |

---

## 3. .NET depth (вплетать в фазы)

| Тема | Фаза |
|------|------|
| IAsyncEnumerable, ValueTask | B-01 |
| MediatR pipeline behaviors | B-03 |
| EF Core performance | B-09 |
| Source generators (optional) | B-03+ |
| Minimal APIs vs Controllers | B-02 |
| Hot Chocolate GraphQL | B-10 |
| gRPC + protobuf | B-17 |
| Concurrency | Channels, IAsyncEnumerable, BenchmarkDotNet | **B-33** |

---

## 4. Concurrency depth (B-33 / J-33)

| Тема | .NET | Java/Spring |
|------|------|-------------|
| I/O-bound parallelism | async/await, Channels | Virtual Threads |
| Bulk operations | `Parallel.ForEachAsync` | `newVirtualThreadPerTaskExecutor()` |
| Background work | `BackgroundService` + Channel | `@Async` + event listeners |
| Streaming | `IAsyncEnumerable` | `StreamingResponseBody` |
| Benchmark | BenchmarkDotNet | JMH |

Java track: [J-33](../../todo-platform-java/plans/java-phase-33-concurrency-virtual-threads.md)

---

## 5. DevOps & Cloud

| Тема | Фаза |
|------|------|
| Docker multi-stage | B-08 |
| Terraform modules | B-25 |
| Helm charts | B-26 |
| Blue-green deploy | B-28 |
| SLO/SLI, error budgets | B-24 |

---

## 6. Interview mock schedule

- 1 backend system design mock / 2 weeks (after B-10)
- 1 concurrency deep dive mock (after **B-33**)
- 1 coding (LeetCode medium) / week — shared with frontend track
- Microsoft-style: behavioral + architecture deep dive
