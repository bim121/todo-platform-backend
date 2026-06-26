# Backend Phase B-04 — Domain Events, Specifications, Unit of Work

> **Теория:** [guides/b-04-domain-events-theory.md](./guides/b-04-domain-events-theory.md) — статус: **full** · **ADR:** [docs/adr/021-domain-events.md](../docs/adr/021-domain-events.md)
> **Обязательно прочитать:** [guides/b-00-architecture-and-cqrs-theory.md](./guides/b-00-architecture-and-cqrs-theory.md)

**Длительность:** 2–3 недели (20–30 ч)  
**Предусловия:** [B-03](./backend-phase-03-cqrs-mediatr.md) — MediatR handlers работают  
**Цель:** Domain events из агрегатов, Specification pattern, Unit of Work, подготовка transactional outbox для B-07.

---

## Результат фазы

- [ ] `IDomainEvent` + `Entity.RaiseDomainEvent()` в Domain
- [ ] События `TodoCreatedEvent`, `TodoCompletedEvent`, `TodoDeletedEvent`
- [ ] `DomainEventDispatcher` — публикует события через MediatR `INotification`
- [ ] `Specification<T>` + `TodoByUserSpec`, `ActiveTodosSpec` для репозитория
- [ ] `IUnitOfWork` + `EfUnitOfWork` — SaveChanges + dispatch events в одной транзакции
- [ ] `OutboxMessage` entity + таблица `outbox_messages` (без publisher — B-07)
- [ ] `TransactionBehavior` использует `IUnitOfWork` вместо прямого DbContext
- [ ] Unit tests: событие поднимается при `Todo.Complete()`, handler вызывается
- [ ] ADR-021: Domain Events vs Integration Events

---

## Неделя 1 — Domain events foundation

### B-04.1 Базовые типы событий

1. Создать `Domain/Common/IDomainEvent.cs` — marker + `OccurredOn: DateTimeOffset`
2. Добавить в `Entity` коллекцию `_domainEvents`, методы `RaiseDomainEvent`, `ClearDomainEvents`
3. В `Todo.Create()` — `RaiseDomainEvent(new TodoCreatedEvent(...))`
4. В `Todo.Complete()` — `TodoCompletedEvent`; в delete flow — `TodoDeletedEvent`
5. Запретить публичные setters — только factory/methods меняют state

**Файлы:**
- `src/TodoPlatform.Domain/Common/IDomainEvent.cs`
- `src/TodoPlatform.Domain/Events/TodoCreatedEvent.cs`
- `src/TodoPlatform.Domain/Entities/Entity.cs`

### B-04.2 MediatR notification handlers

1. `TodoCreatedEvent : IDomainEvent, INotification`
2. Handler `TodoCreatedAuditHandler` — логирует в Serilog (подготовка к B-16 audit)
3. Handler `TodoCreatedCacheInvalidator` — stub (реализация в B-06)
4. Регистрация: events dispatch после успешного SaveChanges

**Файл:** `Application/Common/DomainEventDispatcher.cs`

```csharp
public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> events, CancellationToken ct)
{
    foreach (var domainEvent in events)
        await _mediator.Publish(domainEvent, ct);
}
```

### B-04.3 Интеграция с CreateTodoHandler

1. Handler вызывает `todoRepository.Add(todo)` без SaveChanges
2. `IUnitOfWork.CommitAsync()` — SaveChanges + dispatch
3. Убедиться, что события не dispatch при rollback транзакции

---

## Неделя 2 — Specification pattern

### B-04.4 Specification base

1. `Specification<T>` с `Criteria`, `Includes`, `OrderBy`, `Paging`
2. `ISpecificationEvaluator` — переводит spec в `IQueryable`
3. `TodoRepository.ListAsync(ISpecification<Todo> spec)` вместо ad-hoc фильтров

**Файлы:**
- `Domain/Specifications/Specification.cs`
- `Infrastructure/Persistence/SpecificationEvaluator.cs`
- `Application/Todos/Specifications/TodoByUserSpecification.cs`

### B-04.5 Использование в queries

1. `GetTodosQueryHandler` — `new TodoByUserSpecification(userId)` + optional `ActiveTodosSpecification`
2. Комбинирование: `spec.And(otherSpec)` или `Specification<T>.operator &`
3. Pagination spec: `Skip/Take` из query params OpenAPI

### B-04.6 Repository refactor

1. Удалить дублирующие методы `GetByUserId`, `GetActive` — заменить specs
2. Index hints в FluentMigrator если spec использует `Status`, `Completed`
3. Unit test: evaluator генерирует корректный SQL (можно InMemory)

---

## Неделя 3 — Unit of Work + Outbox prep

### B-04.7 IUnitOfWork

1. Interface: `CommitAsync`, `RollbackAsync`, `Add<T>()`, `Repository<T>()`
2. `EfUnitOfWork` — оборачивает `AppDbContext`, собирает events из tracked entities
3. `TransactionBehavior` — `await uow.CommitAsync()` для `ICommand`

**Файл:** `Infrastructure/Persistence/EfUnitOfWork.cs`

### B-04.8 Outbox table (schema only)

1. Entity `OutboxMessage` — Id, Type, Payload (jsonb), CreatedAt, ProcessedAt
2. FluentMigrator `V003__outbox_messages.sql`
3. При Commit — INSERT outbox row для каждого domain event (serializer System.Text.Json)
4. `IOutboxStore` interface — реализация publisher в B-07 MassTransit

### B-04.9 Tests + ADR

1. Test: CreateTodo → 1 outbox row + TodoCreatedEvent handler invoked
2. Test: exception in handler до Commit — events cleared, no outbox rows
3. ADR-021 в `docs/adr/021-domain-events.md`

---

## Команды

```bash
# packages (если ещё нет)
dotnet add src/TodoPlatform.Application package MediatR.Contracts

# migration
dotnet run --project src/TodoPlatform.Api -- --migrate

# verify outbox table
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb -c "\d outbox_messages"

# tests
dotnet test src/TodoPlatform.Application.Tests --filter "FullyQualifiedName~DomainEvent"
dotnet test
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Events из domain methods | `Todo.Complete()` raises event |
| 2 | Dispatch после commit | handler не вызывается при failed SaveChanges |
| 3 | Specifications в GetTodos | grep `Specification` в handlers |
| 4 | Outbox rows persisted | INSERT при CreateTodo |
| 5 | UoW в pipeline | TransactionBehavior → IUnitOfWork |
| 6 | Tests green | `dotnet test` |
| 7 | ADR published | `docs/adr/021-domain-events.md` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-04 | Без изменений API — internal refactor |
| B-06 | `TodoCreatedCacheInvalidator` активирует cache bust |
| B-07 | Outbox → RabbitMQ для async notifications |
| Phase 4 | Realtime events приходят после B-13, не domain events напрямую |

См. [integration-map.md](./integration-map.md).

---

## Следующая фаза

→ [B-05 Keycloak JWT & RBAC](./backend-phase-05-keycloak-auth.md)
