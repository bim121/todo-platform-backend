# B-00 — Architecture & CQRS (полная теория)

> **Статус:** full  
> **Практика:** [../backend-phase-00-foundation.md](../backend-phase-00-foundation.md)  
> **Следующий глубокий CQRS:** [b-03-cqrs-mediatr-theory.md](./b-03-cqrs-mediatr-theory.md) (placeholder)

---

## 1. Зачем Commands/Queries как ось архитектуры

В типичном ASP.NET-проекте контроллер часто превращается в «божественный объект»: он валидирует вход, вызывает сервис, маппит DTO, ловит исключения, пишет в лог и возвращает HTTP-ответ. С ростом кодовой базы такой **imperative controller** становится трудно тестировать и невозможно масштабировать командой — каждый новый endpoint добавляет ещё одну ветку в уже перегруженный `TodoService`.

**Command/Query dispatch** (через MediatR или аналог) решает это на уровне **use-case**:

- Один класс = одно намерение пользователя (`CreateTodoCommand`, `GetTodosQuery`).
- **Single Responsibility** на уровне бизнес-операции, а не только на уровне «слоя».
- Handler тестируется изолированно: mock `ITodoRepository`, без HTTP и без `DbContext` в интеграционном стиле.
- Контроллер становится тонким: `return Ok(await _mediator.Send(command))`.

Для Microsoft L63+ и FAANG system design это базовый язык: «мы разделяем read и write paths», «каждая команда идемпотентна где возможно», «queries не имеют side effects».

---

## 2. Все актуальные архитектуры — сравнительная таблица

Оценка популярности для **.NET enterprise 2025–2026** (Microsoft docs, NuGet trends, job postings L63+):

| Архитектура | ★ | Microsoft / FAANG | Суть | Плюсы | Минусы |
|-------------|---|-------------------|------|-------|--------|
| **Layered (N-Tier)** | ★★★☆☆ | Legacy enterprise, старые банки | Controller → Service → Repository | Быстрый старт, понятно джуну | Anemic domain, Fat Service, сложно тестировать |
| **Clean Architecture** | ★★★★★ | Стандарт greenfield .NET | Зависимости внутрь, Domain в центре | Тестируемость, замена infra | Много boilerplate, дисциплина команды |
| **Onion Architecture** | ★★★★☆ | Часто = Clean в .NET | Слои-кольца вокруг domain | Чёткие границы | Путаница терминов с Hexagonal |
| **Hexagonal (Ports & Adapters)** | ★★★★☆ | DDD-проекты | Ports = interfaces, Adapters = EF/HTTP | Изоляция от БД и фреймворков | Нужно проектировать ports заранее |
| **Vertical Slice Architecture** | ★★★★★ | Trend 2024–2026, Jimmy Bogard | Feature folder: Request+Handler+Endpoint | Быстрая delivery, co-location | Риск дублирования между slices |
| **CQRS** | ★★★★★ | MediatR в ~70% новых .NET API | Command ≠ Query, разные модели | Масштабирование read/write отдельно | Сложность, eventual consistency |
| **CQRS + Event Sourcing** | ★★★☆☆ | Banking, audit-heavy (Goldman, некоторые Azure teams) | Events = source of truth | Полный audit, time travel | Дорого, сложные projections |
| **DDD (Tactical)** | ★★★★☆ | Enterprise .NET, Microsoft Dynamics | Aggregates, VO, Domain Events | Богатая модель | Кривая обучения, over-engineering риск |
| **Modular Monolith** | ★★★★★ | **Microsoft рекомендует** старт | Модули с границами, один deploy | Лучший путь до microservices | Нужны module boundaries |
| **Microservices** | ★★★★☆ | FAANG at scale, Netflix/Uber | Независимые сервисы + messaging | Независимый deploy, scale | Ops hell, distributed tracing |
| **Event-Driven Architecture** | ★★★★☆ | Kafka/RabbitMQ stacks | Async через domain/integration events | Loose coupling, scale consumers | Ordering, idempotency, debugging |
| **Serverless / Functions** | ★★★☆☆ | Azure Functions, AWS Lambda | Pay-per-invocation | Zero ops для spike | Cold start, stateless, vendor lock |

**Легенда рейтинга:**

- ★★★★★ — industry default / Microsoft recommended для типичного enterprise API
- ★★★★☆ — widely adopted at scale, ожидают на L63+ interviews
- ★★★☆☆ — niche, legacy или domain-specific

---

## 3. CQRS глубоко

### 3.1 Command vs Query

| | Command | Query |
|---|---------|-------|
| **Intent** | Изменить состояние | Прочитать данные |
| **Side effects** | Да | **Нет** (идеально) |
| **Return** | Id, DTO, Unit, Result | DTO, collection, projection |
| **Idempotency** | Желательна (PUT, idempotency key) | Естественно идемпотентна |
| **Transaction** | Обычно в одной TX | Read-only, replica OK |
| **Пример** | `CreateTodoCommand` | `GetTodosQuery` |

**Важно:** CQRS ≠ «две базы данных». На старте (B-00…B-16) достаточно **одной PostgreSQL** с разными handlers: write через EF Core, read через Dapper или оптимизированные queries.

### 3.2 MediatR Pipeline

Порядок behaviors (типичный):

```
Request
  → LoggingBehavior
  → ValidationBehavior (FluentValidation)
  → TransactionBehavior (только для Commands)
  → Handler
  → Response
```

Каждый behavior — cross-cutting concern без загрязнения handler'а.

### 3.3 Thin Controllers

```csharp
[ApiController]
[Route("api/todos")]
public class TodosController : ControllerBase
{
    private readonly IMediator _mediator;

    public TodosController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create(
        CreateTodoCommand command,
        CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoDto>>> GetAll(
        [FromQuery] GetTodosQuery query,
        CancellationToken ct)
        => Ok(await _mediator.Send(query, ct));
}
```

Контроллер не знает про EF Core, Redis, RabbitMQ — только dispatch.

### 3.4 Read side vs Write side

**Write side (B-01, B-03, B-04):**

- Aggregates с инвариантами (`Todo.Complete()` проверяет статус).
- EF Core `DbContext`, Unit of Work.
- Domain events (`TodoCreated`) → outbox → Kafka (B-16).

**Read side (B-10):**

- Dapper, raw SQL, materialized views.
- Denormalized DTO для списков/Kanban/admin dashboard.
- Read replica routing (B-20).

### 3.5 Когда CQRS — overkill

- CRUD < 10 endpoints, команда из 1–2 человек.
- Нет разницы в нагрузке read vs write.
- Прототип / hackathon.

**TodoPlatform** быстро выходит за эти рамки: multi-tenant, admin migrations, search, realtime → CQRS оправдан с **B-03**.

---

## 4. Примеры C# — Layered vs CQRS

### 4.1 Layered (Fat Service) — anti-pattern для роста

```csharp
public class TodoService
{
    public async Task<TodoDto> CreateAsync(CreateTodoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            throw new ValidationException("Title required");
        var entity = new Todo { Title = req.Title, UserId = req.UserId };
        _db.Todos.Add(entity);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"todos:{req.UserId}");
        await _bus.PublishAsync(new TodoCreated(entity.Id));
        return _mapper.Map<TodoDto>(entity);
    }
    // + Update, Delete, GetAll, Search... → 800 строк
}
```

### 4.2 CQRS — один handler на use-case

```csharp
public record CreateTodoCommand(string Title, Guid UserId) : IRequest<TodoDto>;

public sealed class CreateTodoHandler : IRequestHandler<CreateTodoCommand, TodoDto>
{
    private readonly ITodoRepository _repo;
    private readonly IPublisher _publisher;

    public CreateTodoHandler(ITodoRepository repo, IPublisher publisher)
    {
        _repo = repo;
        _publisher = publisher;
    }

    public async Task<TodoDto> Handle(CreateTodoCommand cmd, CancellationToken ct)
    {
        var todo = Todo.Create(cmd.Title, cmd.UserId);
        await _repo.AddAsync(todo, ct);
        await _publisher.Publish(new TodoCreatedDomainEvent(todo.Id), ct);
        return TodoDto.FromEntity(todo);
    }
}
```

```csharp
public record GetTodosQuery(Guid UserId, TodoStatus? Status) : IRequest<IReadOnlyList<TodoDto>>;

public sealed class GetTodosHandler : IRequestHandler<GetTodosQuery, IReadOnlyList<TodoDto>>
{
    private readonly ITodoReadDb _readDb;

    public GetTodosHandler(ITodoReadDb readDb) => _readDb = readDb;

    public Task<IReadOnlyList<TodoDto>> Handle(GetTodosQuery q, CancellationToken ct)
        => _readDb.GetByUserAsync(q.UserId, q.Status, ct);
}
```

### 4.3 Pipeline Behavior — Validation

```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### 4.4 Admin Command (связь с frontend Phase 14–15)

```csharp
public record SwitchTenantTrackCommand(
    string TenantId,
    DeploymentTrack TargetTrack) : IRequest<TenantDto>;

public enum DeploymentTrack { Blue, Green }
```

Handler обновляет tenant record, публикует event для gateway (B-28), frontend `AdminFacade.switchTrack()` dispatch'ит NgRx action → HTTP POST.

---

## 5. Как архитектуры сочетаются — наш выбор для TodoPlatform

```
Modular Monolith (B-00 … B-16)
  └── Clean Architecture (Api / Application / Domain / Infrastructure)
        └── CQRS via MediatR (B-03)
              └── DDD tactical: Aggregates, Events (B-04)
                    └── Multi-tenant + Admin Commands (B-11, B-12)
                          └── Microservices split (B-17)
                                └── Event-Driven + Kafka (B-16)
                                      └── Saga: BulkApplyMigration (B-18)
```

**Почему Modular Monolith первым:** Microsoft Architecture Center и .NET teams рекомендуют не начинать с microservices. Один deploy, чёткие module boundaries (`Todos`, `Tenants`, `Admin`, `Search`), потом extract в сервисы когда есть реальная потребность в independent scale.

---

## 6. Маппинг на фронт (NgRx Facades) — CQRS-lite

Frontend Phase 4 вводит Facades — **осознанное упрощение** backend CQRS:

| Frontend (Phase 4) | Backend (B-03) |
|--------------------|----------------|
| `TodosFacade.add(dto)` | `CreateTodoCommand` |
| `TodosFacade.load()` | `GetTodosQuery` |
| `TodosActions.addTodo` | Command dispatch |
| `selectAllTodos` | Query result |
| `todosEffects.addTodo$` | Handler + side effects (HTTP) |
| HTTP interceptors chain | MediatR Pipeline Behaviors |
| Entity adapter (read model) | Dapper / projections |
| Reducer (write model) | Aggregate + EF Core |

**Admin panel (Phase 14–15):**

| `AdminFacade.switchTrack(id, track)` | `SwitchTenantTrackCommand` |
| `AdminFacade.migrateTenant(id, ver)` | `ApplyTenantMigrationCommand` |
| `selectTenants` | `GetTenantsQuery` |

Полный flow для интервью: «User clicks Move to Green → Facade dispatches action → Effect POST /admin/tenants/{id}/switch-track → MediatR Command → DB update → Outbox event → Gateway routes tenant to green cluster».

---

## 7. Clean Architecture — слои TodoPlatform

```
TodoPlatform.Api          ← Controllers, Middleware, DI composition
TodoPlatform.Application  ← Commands, Queries, Handlers, Behaviors, Interfaces
TodoPlatform.Domain       ← Entities, Value Objects, Domain Events (no deps)
TodoPlatform.Infrastructure ← EF, Dapper, Redis, MassTransit, Keycloak client
```

**Правило зависимостей:** Domain не ссылается ни на что. Application зависит только от Domain. Infrastructure реализует interfaces из Application.

---

## 8. Типичные ошибки и anti-patterns

1. **CQRS everywhere on day 1** — начни с B-01 layered CRUD, введи MediatR в B-03.
2. **Query с side effects** — `GetTodosQuery` не должен писать в БД или слать email.
3. **Один handler на 10 операций** — нарушает SRP, вернись к Fat Service.
4. **Две БД без причины** — premature optimization; одна Postgres достаточна до B-20+.
5. **Controller вызывает DbContext напрямую** — обход Application layer.
6. **Игнорирование idempotency** — duplicate POST создаёт два todo; используй idempotency key (B-19).
7. **Microservices до product-market fit** — extract после modular monolith boundaries доказаны метриками.

---

## 9. Interview bank (15 вопросов)

1. **Когда CQRS не нужен?** — Малый CRUD, нет split read/write нагрузки, команда 1–2 человека.
2. **Command vs Domain Event?** — Command = intent пользователя; Event = факт что произошло (past tense).
3. **Как обеспечить idempotency?** — Idempotency-Key header + Redis dedup (B-19); или natural keys в БД.
4. **Modular monolith vs microservices?** — Monolith пока нет independent scaling/deploy requirements; extract по модулю с highest churn.
5. **Где транзакция в CQRS?** — TransactionBehavior на Command pipeline; Queries — read-only без TX.
6. **Eventual consistency — как объяснить пользователю?** — «Обновление появится через секунду» + optimistic UI на фронте.
7. **Clean vs Vertical Slice?** — Clean = horizontal layers; VSA = vertical features; можно комбинировать (modules + slices внутри).
8. **Anemic domain model — проблема?** — Вся логика в Service → дублирование, сложные инварианты; богатые Aggregates предпочтительнее.
9. **Как тестировать Handler?** — Unit test с mock IRepository, без WebApplicationFactory.
10. **Read replica lag?** — Query после Command может не видеть write; read-your-writes через primary или session stickiness.
11. **MediatR vs custom dispatcher?** — MediatR = pipeline, community, Microsoft ecosystem; custom — только если extreme constraints.
12. **Gateway (YARP) vs nginx?** — YARP = .NET native, dynamic config; nginx = battle-tested TLS/rate limit; часто оба (nginx edge, YARP internal).
13. **Saga orchestration vs choreography?** — Orchestrator = central coordinator; choreography = events only; hybrid в B-18.
14. **Multi-tenant: shared DB vs schema per tenant?** — Shared + RLS для старта; schema per tenant для stronger isolation (B-11).
15. **Как показать архитектуру на собеседовании Microsoft?** — Modular monolith → CQRS → Azure AKS path; упомяни Entra ID, App Insights, Well-Architected Framework.

---

## 10. Связь с другими фазами

| Фаза | Связь |
|------|-------|
| B-01 | Первый CRUD до MediatR |
| B-03 | MediatR CQRS в коде |
| B-04 | Domain events из handlers |
| B-10 | Read side Dapper |
| B-11–B-12 | Tenant Commands для admin |
| B-17 | Extract modules → microservices |
| B-18 | Saga для bulk migrate |
| Frontend Phase 4 | Facades = CQRS-lite |
| Frontend Phase 14–15 | Admin panel Commands |

---

## 11. Дополнительные ресурсы

- [Microsoft — Architect modern applications](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [Jimmy Bogard — Vertical Slice Architecture](https://www.jimmybogard.com/blogs/vertical-slice-architecture/)
- [Martin Fowler — CQRS](https://martinfowler.com/bliki/CQRS.html)
- [Microsoft Learn — Cloud Design Patterns](https://learn.microsoft.com/azure/architecture/patterns/)
- Книга: *Architecture Patterns with Python* (концепции переносимы на C#)
- Книга: *Domain-Driven Design* — Eric Evans (tactical patterns)
