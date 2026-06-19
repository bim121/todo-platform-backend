# ADR-021: Domain Events, dispatch, Unit of Work и Outbox

| | |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2026-06-02 (обновлено: B-04.7–B-04.9) |
| **Фаза** | B-04 (B-04.1–B-04.9) |
| **Теория** | [plans/guides/b-04-domain-events-theory.md](../../plans/guides/b-04-domain-events-theory.md) |

---

## Context

После внедрения CQRS (B-03) side effects (audit log, будущий cache/search/messaging) нельзя размазывать по `CreateTodoHandler` и репозиторию. Нужно:

1. Фиксировать **факты** в domain (`TodoCreated`), а не дублировать бизнес-логику в Application.
2. Dispatch **после** успешного сохранения в БД, не до.
3. Не dispatch при failed transaction / failed `SaveChanges`.
4. **Transactional outbox** — domain events и данные в одной TX (B-04.8); publisher в B-07.

---

## Decision

### 1. Три типа сообщений

| Тип | Где | Транспорт сейчас |
|-----|-----|------------------|
| **Command** | Application (`CreateTodoCommand`) | HTTP → `IMediator.Send` |
| **Domain Event** | Domain (`TodoCreatedEvent`) | In-process → `IMediator.Publish` |
| **Integration Event** | B-07+ | Outbox row → MassTransit publisher |

### 2. Поднятие событий — только в агрегате

- `Entity.RaiseDomainEvent()` / `ClearDomainEvents()` в `TodoPlatform.Domain`.
- `Todo.Create()`, `Complete()`, `MarkDeleted()` поднимают события.
- События: `IDomainEvent` + `INotification`.

### 3. Repository не коммитит

`ITodoRepository` только стейджит в `AppDbContext`. Без `SaveChanges` и dispatch.

### 4. Unit of Work (B-04.7)

`IUnitOfWork`:

- `Repository<T>()` — generic `EfRepository<T>`
- `Add<T>(entity)` — стейдж в ChangeTracker
- `CommitAsync()` — outbox + SaveChanges + ClearEvents + dispatch
- `RollbackAsync()` — ClearDomainEvents + ChangeTracker.Clear()

**Command handlers не вызывают `CommitAsync`.** Commit выполняет `TransactionBehavior` после успешного handler.

### 5. TransactionBehavior (B-04.7)

Для `ICommand`:

```
BeginTransaction (PostgreSQL only)
  → handler (stage changes)
  → unitOfWork.CommitAsync()
  → CommitTransaction
```

InMemory: без TX, но `CommitAsync` / `RollbackAsync` на UoW всё равно вызываются.

### 6. Transactional Outbox (B-04.8)

Таблица `outbox_messages` (FluentMigrator **V004**):

| Column | Type |
|--------|------|
| Id | uuid PK |
| Type | varchar(500) — CLR type name |
| Payload | jsonb — `System.Text.Json` |
| CreatedAt | timestamptz |
| ProcessedAt | timestamptz nullable |

`IOutboxStore.Stage(events)` вызывается в `CommitAsync` **до** `SaveChanges` — outbox и агрегат в одной транзакции.

Publisher (MassTransit, B-07) читает `ProcessedAt IS NULL` и помечает обработанные.

### 7. In-process handlers

`DomainEventDispatcher` → `TodoCreatedAuditHandler`, `TodoCreatedCacheInvalidator` (stub).

Dispatch **после** SaveChanges (handlers видят persisted data). Outbox уже записан в той же TX.

---

## Consequences

### Положительные

- Единая точка commit: outbox + persistence + dispatch.
- Handlers тонкие — только бизнес-стейджинг.
- Outbox готов к B-07 без смены domain model.
- Rollback: handler exception → `RollbackAsync`, нет outbox rows.

### Ограничения

- In-process dispatch всё ещё синхронный; outbox mitigates потерю при crash после commit.
- `ProcessedAt` не обновляется до B-07.
- Publisher не реализован в B-04.8 (schema + stage only).

---

## Ссылки на код

| Компонент | Путь |
|-----------|------|
| IUnitOfWork | `src/TodoPlatform.Application/Interfaces/IUnitOfWork.cs` |
| IOutboxStore | `src/TodoPlatform.Application/Interfaces/IOutboxStore.cs` |
| EfUnitOfWork | `src/TodoPlatform.Infrastructure/Persistence/EfUnitOfWork.cs` |
| EfOutboxStore | `src/TodoPlatform.Infrastructure/Persistence/EfOutboxStore.cs` |
| OutboxMessage | `src/TodoPlatform.Infrastructure/Persistence/OutboxMessage.cs` |
| Migration V004 | `src/TodoPlatform.Infrastructure/Migrations/V004_CreateOutboxMessages.cs` |
| TransactionBehavior | `src/TodoPlatform.Infrastructure/Behaviors/TransactionBehavior.cs` |
| Tests | `tests/TodoPlatform.Infrastructure.Tests/Persistence/EfUnitOfWorkTests.cs` |
| Tests | `tests/TodoPlatform.Infrastructure.Tests/Behaviors/TransactionBehaviorTests.cs` |

---

## Связанные ADR

- **ADR-023** — outbox publisher + MassTransit (B-07)
- **ADR-022** — cache invalidation on domain events (B-06)
