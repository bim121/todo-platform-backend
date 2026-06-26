# Backend Phase B-10 — Complex SQL & Dapper Read Models

> **Теория:** [guides/b-10-complex-sql-readmodels-theory.md](./guides/b-10-complex-sql-readmodels-theory.md) — статус: placeholder  
> **Паттерн:** CQRS read side — writes EF, reads Dapper

**Длительность:** 3–4 недели (35–45 ч)  
**Предусловия:** [B-09](./backend-phase-09-postgres-queries.md), [B-03](./backend-phase-03-cqrs-mediatr.md), [B-05 Auth](./backend-phase-05-keycloak-auth.md)  
**Цель:** Dapper для тяжёлых read queries + **GraphQL BFF** (Hot Chocolate) на том же MediatR — сразу после read models, не «в конце roadmap».

---

## Результат фазы

- [ ] Dapper + Npgsql registered in DI
- [ ] `IReadDbConnection` — separate read connection string (same DB for now)
- [ ] SQL view `v_todo_stats_by_user` — count total/active/completed
- [ ] `GetTodoStatsQuery` + handler via Dapper
- [ ] `GetTodosWithFiltersQuery` — dynamic SQL (status, priority, date range)
- [ ] Raw SQL file `Infrastructure/Persistence/Sql/todo-stats.sql`
- [ ] Endpoint `GET /api/todos/stats` in OpenAPI
- [ ] No EF for read handlers in this phase — clear folder `Application/ReadModels/`
- [ ] Benchmark: Dapper vs EF for stats query documented
- [ ] Hot Chocolate GraphQL `/graphql` — resolvers → **MediatR** (не дублировать логику)
- [ ] Schema: `Query` (todos, todoStats), `Mutation` (createTodo, updateTodo)
- [ ] `contracts/graphql/schema.graphql` exported для frontend [Phase 13-GraphQL](../../anular-ngrx-todo-auth/plans/phase-13-graphql-client.md)
- [ ] GraphQL auth: `@Authorize`, JWT + `X-Tenant-Id` в context
- [ ] DataLoader demo для N+1 (todo → user)
- [ ] ADR-010: REST vs GraphQL в этом проекте

---

## Неделя 1 — Dapper infrastructure

### B-10.1 Packages & connection

1. `Dapper`, `Npgsql` (already have)
2. `IReadDbConnection` with `IDbConnection CreateConnection()`
3. Read connection string key `ConnectionStrings:Read` — same as Default initially

**Файл:** `Infrastructure/Persistence/DapperReadDbConnection.cs`

### B-10.2 SQL view migration

1. `V006__todo_stats_view.sql`:

```sql
CREATE VIEW v_todo_stats_by_user AS
SELECT user_id,
       COUNT(*) AS total,
       COUNT(*) FILTER (WHERE NOT completed) AS active,
       COUNT(*) FILTER (WHERE completed) AS completed
FROM todos
GROUP BY user_id;
```

2. Grant SELECT to app role
3. Index support from B-09 ensures view performance

### B-10.3 First Dapper query

1. `GetTodoStatsQuery(Guid UserId)`
2. Handler queries view with parameter `@UserId`
3. Map to `TodoStatsDto`

---

## Неделя 2 — Dynamic filters & pagination

### B-10.4 GetTodosWithFiltersQuery

1. Filters: `status`, `priority`, `completed`, `search` (prep B-15)
2. `SqlBuilder` or manual StringBuilder with whitelisted columns only (SQL injection safe)
3. COUNT + page query pattern
4. Return `PagedResult<TodoListItemDto>`

**Файл:** `Infrastructure/Persistence/Sql/todos-filter.sql`

### B-10.5 OpenAPI update

1. Add query params to `GET /api/todos` or new `/api/todos/search`
2. Sync [`contracts/openapi.yaml`](../../contracts/openapi.yaml)
3. ProblemDetails on invalid filter combo

### B-10.6 Caching interaction

1. Stats query — cache 1 min in Redis (B-06) with key `stats:user:{id}`
2. Invalidate on todo mutations via domain events

---

## Неделя 3 — Admin read models prep

### B-10.7 Tenant-agnostic admin stats stub

1. `GetSystemStatsQuery` — total users, todos, avg todos per user
2. SQL with JOIN users — practice complex query
3. `[Authorize(Roles = "admin")]` on controller

### B-10.8 Tests & benchmarks

1. Unit test SqlBuilder — rejects unknown column names
2. Integration test against Testcontainers Postgres
3. BenchmarkDotNet project: EF vs Dapper for stats (optional console app)
4. ADR-025: EF write / Dapper read split

---

## Неделя 3 — GraphQL BFF (Hot Chocolate)

> **Момент обучения:** сразу после Dapper read models — GraphQL отдаёт те же `GetTodosQuery`, `GetTodoStatsQuery` без новой бизнес-логики.  
> **Frontend подключится** в [Phase 13-GraphQL](../../anular-ngrx-todo-auth/plans/phase-13-graphql-client.md) сразу после REST cutover.

### B-10.3 Packages & setup

```bash
dotnet add src/TodoPlatform.Api package HotChocolate.AspNetCore
dotnet add src/TodoPlatform.Api package HotChocolate.Data
dotnet add src/TodoPlatform.Api package HotChocolate.AspNetCore.Authorization
```

**Program.cs:**
```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .AddProjections()
    .AddFiltering()
    .AddSorting();
app.MapGraphQL("/graphql");
```

### B-10.4 Schema & resolvers → MediatR

**Файлы:** `Api/GraphQL/Query.cs`, `Mutation.cs`, `Types/TodoType.cs`

```graphql
type Query {
  todos(userId: UUID!, status: TodoStatus): [Todo!]!
  todoStats(userId: UUID!): TodoStats!
}
```

```csharp
public async Task<IReadOnlyList<TodoDto>> GetTodos(
    Guid userId, [Service] IMediator mediator, CancellationToken ct)
    => await mediator.Send(new GetTodosQuery(userId, null), ct);
```

**Правило:** resolver = thin, как controller. EF/Dapper только в handlers.

### B-10.5 Dev UI & export schema

- Banana Cake Pop: `/graphql/ui` (dev only)
- `dotnet graphql export --output ../../contracts/graphql/schema.graphql`

### B-10.6 DataLoader (N+1)

`BatchDataLoader<Guid, UserDto>` для списка todos — interview topic.

---

## Неделя 4 — GraphQL tests & handoff

### B-10.7 Integration tests

```csharp
[Fact]
public async Task GraphQL_GetTodos_ReturnsData() { ... }
```

### B-10.8 Error handling

GraphQL `errors[]` format + mapping from MediatR validation exceptions.

### B-10.9 Frontend contract

Commit `contracts/graphql/schema.graphql` — frontend codegen в Phase 13-GraphQL.

---

## Команды

```bash
dotnet add src/TodoPlatform.Infrastructure package Dapper

dotnet run --project src/TodoPlatform.Api -- --migrate

curl "http://localhost:8080/api/todos/stats" -H "Authorization: Bearer <token>"

curl "http://localhost:8080/api/todos?status=active&page=1&pageSize=20" \
  -H "Authorization: Bearer <token>"

dotnet test --filter "FullyQualifiedName~Dapper"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Dapper handlers | folder ReadModels populated |
| 2 | View exists | `\dv` in psql |
| 3 | Filtered search works | curl with params |
| 4 | SQL injection safe | whitelist tests |
| 5 | OpenAPI synced | contract diff clean |
| 6 | Benchmark doc | EF vs Dapper numbers |
| 7 | GraphQL todos query | Banana Cake Pop / curl |
| 8 | Resolvers use MediatR only | no EF in GraphQL layer |
| 9 | Schema in contracts/graphql | file committed |
| 10 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-10 | Phase 14 filters UI — query params match |
| B-10 GraphQL | [Phase 13-GraphQL](../../anular-ngrx-todo-auth/plans/phase-13-graphql-client.md) — Kanban one-query |
| Admin panel | Stats widgets: REST `/api/todos/stats` **или** GraphQL `todoStats` |
| B-15 | Full-text replaces LIKE search |

После B-10 — backend system design mock recommended — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-11 Multi-Tenant Isolation (RLS)](./backend-phase-11-multi-tenant-isolation.md)
