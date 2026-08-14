# ADR-025: EF write / Dapper read split

| | |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2026-08-11 |
| **Фаза** | B-10 |
| **План** | [backend-phase-10-complex-sql-readmodels.md](../../plans/backend-phase-10-complex-sql-readmodels.md) |

---

## Context

Write path already uses EF Core (aggregates, domain events, outbox, FluentMigrator schema). Heavy read scenarios (per-user stats via SQL view, dynamic filters + COUNT/page, admin JOIN aggregates) are awkward or slower as tracked LINQ. We need a clear rule for when to use which tool without duplicating business rules in SQL and C#.

---

## Decision

### 1. Split by responsibility (CQRS read models)

| Side | Tool | Owns |
|------|------|------|
| **Write** | EF Core + repositories + UoW | Commands, domain invariants, outbox rows |
| **Read (simple)** | EF projections / specs OK | `GetTodos`, `GetTodoById` (already cached) |
| **Read (complex)** | **Dapper** + `IReadDbConnection` | Stats view, filtered search, admin system stats |

Handlers depend on **interfaces** (`ITodoStatsReadStore`, `ITodoFilterReadStore`, `ISystemStatsReadStore`), not on Dapper/EF. Infrastructure picks the implementation (Dapper on Postgres, EF fallback for InMemory tests).

### 2. Separate connection string key

- Write: `ConnectionStrings:Default`
- Read: `ConnectionStrings:Read` (same DB today; replica later in B-20)

Dapper opens ADO.NET connections; Npgsql pools by connection string independently of EF's pool.

### 3. SQL lives next to Infrastructure

- Embedded / documented files under `Infrastructure/Persistence/Sql/`
- Dynamic predicates only via whitelist builders (`TodoFilterSqlBuilder`) — never concatenate user-supplied column names

### 4. Auth on admin aggregates

`GET /api/admin/stats` requires `[Authorize(Roles = "admin")]`. Stats are tenant-agnostic (platform-wide stub until B-11 RLS).

---

## Consequences

**Positive**

- Explicit, explainable SQL for hot reads; easy `EXPLAIN`
- Write model stays rich (events, outbox) without forcing every query through entities
- Tests can swap Dapper → EF without changing MediatR handlers

**Negative / tradeoffs**

- Two persistence styles to learn and review
- Schema drift risk: Dapper SQL must track FluentMigrator column names (PascalCase quoted)
- Duplicated mapping API ↔ DB enum strings (`TodoContractMapper`)

**Rejected alternatives**

- “Dapper everywhere” — loses EF change tracking / migrations story for writes
- “EF only + FromSqlRaw” — still couples read models to DbContext lifecycle; less clear CQRS boundary

---

## Links

- `DapperTodoStatsReadStore`, `DapperTodoFilterReadStore`, `DapperSystemStatsReadStore`
- `IReadDbConnection` / `DapperReadDbConnection`
- Benchmarks: `benchmarks/TodoPlatform.Benchmarks` (EF vs Dapper stats)
- Related: [ADR-022 caching](./022-caching-strategy.md) (stats key `stats:user:{id}`)
