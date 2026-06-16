# ADR-021: Domain Events, dispatch и Unit of Work

| | |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2026-06-02 |
| **Фаза** | B-04 (частично: B-04.1–B-04.3) |
| **Теория** | [plans/guides/b-04-domain-events-theory.md](../../plans/guides/b-04-domain-events-theory.md) |

---

## Context

После внедрения CQRS (B-03) side effects (audit log, будущий cache/search/messaging) нельзя размазывать по `CreateTodoHandler` и репозиторию. Нужно:

1. Фиксировать **факты** в domain (`TodoCreated`), а не дублировать бизнес-логику в Application.
2. Dispatch **после** успешного сохранения в БД, не до.
3. Не dispatch при failed transaction / failed `SaveChanges`.
4. Подготовить путь к **transactional outbox** (B-07) без смены domain model.

Рассматривались варианты:

- Dispatch в репозитории сразу после `SaveChanges` (отклонено в B-04.3).
- EF interceptor на `SaveChanges` (магия, сложнее unit-тесты).
- Только прямые вызовы сервисов из handler (нет единой точки для outbox).

---

## Decision

### 1. Три типа сообщений

| Тип | Где | Транспорт сейчас |
|-----|-----|------------------|
| **Command** | Application (`CreateTodoCommand`) | HTTP → `IMediator.Send` |
| **Domain Event** | Domain (`TodoCreatedEvent`) | In-process → `IMediator.Publish` |
| **Integration Event** | B-07+ (`TodoCreatedIntegrationEvent`) | Outbox → RabbitMQ (не реализовано) |

Domain events **не** экспортируются наружу как публичный API. Маппинг в integration event — в infrastructure/outbox (будущее).

### 2. Поднятие событий — только в агрегате

- `Entity.RaiseDomainEvent()` / `ClearDomainEvents()` в `TodoPlatform.Domain`.
- `Todo.Create()`, `Complete()`, `MarkDeleted()` поднимают `TodoCreatedEvent`, `TodoCompletedEvent`, `TodoDeletedEvent`.
- События реализуют `IDomainEvent` и `MediatR.INotification` (пакет `MediatR.Contracts` в Domain).

### 3. Repository не коммитит

`ITodoRepository` только стейджит изменения в `AppDbContext` (`Add` / `Update` / `Remove`). **Без** `SaveChangesAsync` и **без** dispatch.

### 4. Unit of Work — единая точка commit

`IUnitOfWork.CommitAsync()` (`EfUnitOfWork`):

1. Собрать domain events с tracked `Entity`.
2. `SaveChangesAsync`.
3. `ClearDomainEvents` на сущностях.
4. `IDomainEventDispatcher.DispatchEventsAsync` → `mediator.Publish`.

Command handlers вызывают: `repository.*` → `unitOfWork.CommitAsync()`.

### 5. In-process handlers

`DomainEventDispatcher` в Application. Примеры:

- `TodoCreatedAuditHandler` — structured log (задел под B-16 audit).
- `TodoCreatedCacheInvalidator` — stub до B-06.

Новые side effects добавляются как `INotificationHandler<TDomainEvent>`, не правкой command handler.

### 6. Транзакции

`TransactionBehavior` оборачивает **только** `ICommand` на relational DB:

- `BeginTransaction` → handler (включая `CommitAsync`) → `Commit` / `Rollback`.
- InMemory provider: behavior пропускает TX, `CommitAsync` всё равно выполняется.

### 7. Outbox (отложено)

Запись в `outbox_messages` при `CommitAsync` — **B-04.8 / B-07**, не в этом ADR revision.

---

## Consequences

### Положительные

- Чёткая граница: domain поднимает, UoW сохраняет и dispatch'ит, handlers реагируют.
- Репозиторий проще тестировать и переиспользовать в одной транзакции (несколько aggregate — позже).
- Outbox можно вставить в `EfUnitOfWork` без изменения `Todo.Create()`.
- Rollback: при падении `SaveChanges` dispatch не вызывается (покрыто тестом).

### Отрицательные / ограничения

- Два entry point MediatR (`Send` vs `Publish`) — нужна дисциплина в команде.
- In-process dispatch: падение handler откатывает внешнюю TX на PostgreSQL, но не даёт retry как очередь.
- `TodoCompletedEvent` / `TodoDeletedEvent` пока без handlers.
- `TransactionBehavior` ещё использует `AppDbContext` напрямую, не `IUnitOfWork` (рефактор в B-04.7).

### Риски до outbox

Если процесс упал **после** `SaveChanges`, но **до** dispatch — in-process событие потеряно. Mitigation: transactional outbox (B-07).

---

## Ссылки на код

| Компонент | Путь |
|-----------|------|
| Domain event marker | `src/TodoPlatform.Domain/Common/IDomainEvent.cs` |
| Entity events collection | `src/TodoPlatform.Domain/Common/Entity.cs` |
| Todo events | `src/TodoPlatform.Domain/Events/` |
| Dispatcher | `src/TodoPlatform.Application/Common/DomainEventDispatcher.cs` |
| UoW interface | `src/TodoPlatform.Application/Interfaces/IUnitOfWork.cs` |
| UoW impl | `src/TodoPlatform.Infrastructure/Persistence/EfUnitOfWork.cs` |
| Repository (stage only) | `src/TodoPlatform.Infrastructure/Repositories/TodoRepository.cs` |
| Create handler | `src/TodoPlatform.Application/Todos/Commands/CreateTodo/CreateTodoCommand.cs` |
| Transaction | `src/TodoPlatform.Infrastructure/Behaviors/TransactionBehavior.cs` |
| Tests | `tests/TodoPlatform.Infrastructure.Tests/Persistence/EfUnitOfWorkTests.cs` |

---

## Связанные ADR (planned)

- **ADR-023** — transactional outbox (B-07)
- **ADR-022** — cache invalidation on domain events (B-06)
