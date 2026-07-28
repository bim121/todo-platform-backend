# GetTodos — EXPLAIN baseline (B-09.2 / B-09.3)

## What GetTodos hits in SQL

`GetTodosQuery` → `TodoListSpecification` filters roughly:

```sql
SELECT *
FROM todos
WHERE "UserId" = $1
  -- and when activeOnly=true:
  AND "Completed" = false
ORDER BY ...   -- via Skip/Take when paging
LIMIT $n OFFSET $m;
```

(Exact SQL comes from EF Core; column names are PascalCase as created by FluentMigrator.)

---

## Indexes (audit)

| Index | Since | Columns / filter | Serves |
|-------|-------|------------------|--------|
| `IX_todos_UserId` | V002 | `"UserId"` | list by user |
| `IX_todos_UserId_Completed` | V003 | `"UserId", "Completed"` | user + completed filter |
| `IX_todos_UserId_Active` | **V007** | `"UserId" WHERE "Completed" = false` | `activeOnly=true` (partial) |
| `IX_users_Email` | V002 | unique email | login / sync |

Plan names `ix_todos_user_id` / `ix_todos_user_completed` map to the V002/V003 indexes above (already present).

**Prod note:** for large live tables prefer  
`CREATE INDEX CONCURRENTLY ...` outside a transaction (not inside FluentMigrator’s default TX). Dev migration V007 uses a normal `CREATE INDEX`.

---

## Seed 10k rows

```bash
docker compose exec -T postgres psql -U todo -d tododb < scripts/seed-load-test.sql
```

User: `loadtest@example.com` / id `aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee`.

---

## EXPLAIN — before vs after mental model

### Without a usable index (Seq Scan)

On a cold table with no `"UserId"` index, planner typically does:

```text
Seq Scan on todos  (cost=... rows=10000)
  Filter: ("UserId" = 'aaaaaaaa-...')
  Rows Removed by Filter: ~9900
Planning Time: ...
Execution Time: tens of ms+ (grows with table size)
```

Every row is read; filter applied afterward — fine for tiny seed, bad at 10k+.

### With `IX_todos_UserId` / composite (Index Scan / Bitmap Index Scan)

```bash
docker compose exec -T postgres psql -U todo -d tododb -c "
EXPLAIN (ANALYZE, BUFFERS)
SELECT * FROM todos
WHERE \"UserId\" = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'
  AND \"Completed\" = false;
"
```

Expected shape after V002/V003 (+ V007 partial preferred for active-only):

```text
Index Scan using "IX_todos_UserId_Active" on todos  (...)
  Index Cond: ("UserId" = 'aaaaaaaa-...')
  -- or Bitmap Index Scan on "IX_todos_UserId_Completed"
Planning Time: < 1 ms
Execution Time: low single-digit ms locally (order-of-magnitude better than Seq Scan on 10k)
```

| Plan node | Meaning |
|-----------|---------|
| **Seq Scan** | Full table read — no (or unused) index |
| **Index Scan** | Walk index, fetch matching heap rows |
| **Bitmap Index Scan** | Build bitmap of matches, then heap fetch (good for many matches) |
| **Index Only Scan** | Rare here unless covering index includes all selected columns |

---

## pg_stat_statements (B-09.1)

Enabled via `infra/postgres/postgresql.conf` (`shared_preload_libraries`). Extension created in migration V007.

**If Postgres volume was created before this conf:** recreate once:

```bash
docker compose down -v
docker compose up -d postgres
# wait healthy, then start api (AutoMigrate applies V007)
```

Top queries:

```bash
docker compose exec -T postgres psql -U todo -d tododb < scripts/pg-stat-top.sql
```

Or:

```sql
SELECT query, calls, mean_exec_time
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;
```

---

## Verify indexes

```bash
docker compose exec -T postgres psql -U todo -d tododb -c "\di *todos*"
```

Expect at least: `IX_todos_UserId`, `IX_todos_UserId_Completed`, `IX_todos_UserId_Active`.
