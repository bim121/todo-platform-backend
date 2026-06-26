# Backend Phase B-03 — CQRS MediatR

> **Теория:** [guides/b-03-cqrs-mediatr-theory.md](./guides/b-03-cqrs-mediatr-theory.md) — placeholder  
> **Обязательно прочитать:** [guides/b-00-architecture-and-cqrs-theory.md](./guides/b-00-architecture-and-cqrs-theory.md)

**Длительность:** 3–4 недели (30–40 ч)  
**Предусловия:** [B-01](./backend-phase-01-clean-api.md), [B-02](./backend-phase-02-openapi-contracts.md)  
**Цель:** Все use-cases через MediatR Commands/Queries + Pipeline Behaviors.

---

## Результат фазы

- [ ] `CreateTodoCommand`, `UpdateTodoCommand`, `DeleteTodoCommand`, `GetTodosQuery`, `GetTodoByIdQuery`
- [ ] Handlers в `Application/Todos/`
- [ ] `ValidationBehavior`, `LoggingBehavior`, `TransactionBehavior`
- [ ] Controllers только `_mediator.Send(...)`
- [ ] FluentValidation для каждой Command
- [ ] Unit test на каждый Handler
- [ ] ADR-020 опубликован
- [ ] Таблица mapping с frontend Facades (integration-map)

---

## Неделя 1 — MediatR setup

### B-03.1 Packages

```bash
dotnet add src/TodoPlatform.Application package MediatR
dotnet add src/TodoPlatform.Application package FluentValidation.DependencyInjectionExtensions
```

### B-03.2 Registration

```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
```

### B-03.3 First Command

**Файл:** `Application/Todos/Commands/CreateTodo/CreateTodoCommand.cs`

```csharp
public record CreateTodoCommand(string Title, Guid UserId) : IRequest<TodoDto>;
public sealed class CreateTodoHandler : IRequestHandler<CreateTodoCommand, TodoDto> { ... }
```

### B-03.4 First Query

**Файл:** `Application/Todos/Queries/GetTodos/GetTodosQuery.cs`

---

## Неделя 2 — Pipeline behaviors

### B-03.5 ValidationBehavior

Validate FluentValidation before handler runs.

### B-03.6 LoggingBehavior

Log request name + elapsed ms (Serilog).

### B-03.7 TransactionBehavior

Wrap `ICommand` (marker interface) in EF transaction.

---

## Неделя 3 — Migrate controllers

### B-03.8 TodosController refactor

**До:** `_todoService.CreateAsync(...)`  
**После:** `await _mediator.Send(new CreateTodoCommand(...), ct)`

### B-03.9 Remove Fat Service

Delete `TodoService` if exists — logic only in Handlers.

### B-03.10 Handler tests

Mock `ITodoRepository`, test handler in isolation.

---

## Неделя 4 — Admin command prep

### B-03.11 Stubs for Phase B-12/B-28

- `SwitchTenantTrackCommand` (empty handler OK)
- `GetTenantsQuery`

Связь с frontend AdminFacade — см. [integration-map.md](./integration-map.md).

---

## CQRS ↔ Frontend mapping

| Frontend Facade | Backend |
|-----------------|---------|
| `TodosFacade.add()` | `CreateTodoCommand` |
| `TodosFacade.load()` | `GetTodosQuery` |
| `AdminFacade.switchTrack()` | `SwitchTenantTrackCommand` |

> **GraphQL (B-10):** те же handlers — `Query.GetTodos` → `GetTodosQuery`, без дублирования логики.

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Zero business logic in controllers | code review |
| 2 | All todo endpoints via MediatR | grep IRequest |
| 3 | Handler tests pass | dotnet test |
| 4 | Validation returns ProblemDetails | POST invalid |

---

## Следующая фаза

→ [B-04 Domain Events](./backend-phase-04-domain-events.md)
