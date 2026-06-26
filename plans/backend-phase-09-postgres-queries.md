# Backend Phase B-09 — PostgreSQL Query Optimization I

> **Теория:** [guides/b-09-postgres-queries-theory.md](./guides/b-09-postgres-queries-theory.md) — статус: placeholder

**Длительность:** 2 недели (20–25 ч)  
**Предусловия:** [B-08](./backend-phase-08-docker-compose.md), [B-01](./backend-phase-01-clean-api.md)  
**Цель:** Индексы, EXPLAIN ANALYZE, N+1 elimination, EF Core query tuning для todos/users.

---

## Результат фазы

- [ ] Index audit — `UserId`, `(UserId, Completed)`, `Email` unique
- [ ] Migration `V005__performance_indexes.sql` с CONCURRENTLY (prod note)
- [ ] N+1 устранён — `.Include()` или projection в GetTodos
- [ ] `AsNoTracking()` на все read queries
- [ ] Compiled queries или `IMemoryCache` для hot paths (optional)
- [ ] `pg_stat_statements` enabled в dev Postgres
- [ ] Slow query log > 200ms (Npgsql / EF logging)
- [ ] Benchmark doc: before/after EXPLAIN для GetTodos
- [ ] ADR-024: indexing strategy

---

## Неделя 1 — Analysis & indexes

### B-09.1 Enable pg_stat_statements

1. Postgres config: `shared_preload_libraries = 'pg_stat_statements'`
2. `docker-compose.yml` command or custom `postgresql.conf` mount
3. `CREATE EXTENSION pg_stat_statements;`
4. Query top 10: `SELECT * FROM pg_stat_statements ORDER BY total_exec_time DESC LIMIT 10;`

### B-09.2 EXPLAIN baseline

1. Capture EXPLAIN ANALYZE для `GetTodosQuery` SQL
2. Document Seq Scan vs Index Scan в `docs/db/explain-get-todos.md`
3. Seed 10k todos script: `scripts/seed-load-test.sql`

### B-09.3 Add indexes

1. `CREATE INDEX ix_todos_user_id ON todos(user_id);`
2. `CREATE INDEX ix_todos_user_completed ON todos(user_id, completed);`
3. Partial index optional: `WHERE completed = false`
4. Verify with EXPLAIN — Index Scan expected

---

## Неделя 2 — EF Core tuning

### B-09.4 Query handler optimization

1. `GetTodosQueryHandler` — project to DTO in SQL: `.Select(t => new TodoDto(...))`
2. Remove unnecessary `.Include(u => u.User)` if only UserId needed
3. Pagination: `.Skip/Take` with stable ORDER BY `Id`

### B-09.5 N+1 audit

1. Enable EF sensitive logging in dev
2. Fix any loop-loading patterns in handlers
3. `IQueryable` specs from B-04 — ensure Includes in specification when needed

### B-09.6 Connection pooling

1. Npgsql pool settings in connection string: `Maximum Pool Size=100`
2. Document PgBouncer as future (B-20)
3. Integration test: 100 parallel GetTodos — no pool exhaustion

### B-09.7 Monitoring hooks

1. Log queries > 200ms with tagged Serilog property `SlowQuery`
2. Export count metric for slow queries (prep B-24)
3. Final EXPLAIN doc with improvement metrics (e.g. 45ms → 3ms)

---

## Команды

```bash
# seed load data
docker exec -i todo-platform-backend-postgres-1 psql -U todo -d tododb < scripts/seed-load-test.sql

# explain from psql
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb \
  -c "EXPLAIN ANALYZE SELECT * FROM todos WHERE user_id = '...' AND completed = false;"

# pg_stat_statements
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb \
  -c "SELECT query, calls, mean_exec_time FROM pg_stat_statements ORDER BY mean_exec_time DESC LIMIT 5;"

dotnet test src/TodoPlatform.Infrastructure.Tests --filter "FullyQualifiedName~Query"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Indexes applied | `\di` in psql |
| 2 | Index Scan in EXPLAIN | doc screenshot/text |
| 3 | No N+1 | EF log shows 1 query per request |
| 4 | AsNoTracking reads | grep in query handlers |
| 5 | 10k seed performs | p95 < 50ms local |
| 6 | ADR-024 | indexing doc |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-09 | Faster list loads — Phase 3 entity adapter benefits |
| Phase 14 | Pagination params must match backend defaults |
| B-10 | Dapper read models for complex admin queries |

Parallel skills: SQL depth weeks 1–4 — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-10 Complex SQL & Dapper Read Models](./backend-phase-10-complex-sql-readmodels.md)
