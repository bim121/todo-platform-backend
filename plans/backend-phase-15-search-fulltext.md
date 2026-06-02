# Backend Phase B-15 — Full-Text Search

> **Теория:** [guides/b-15-search-fulltext-theory.md](./guides/b-15-search-fulltext-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–30 ч)  
**Предусловия:** [B-10](./backend-phase-10-complex-sql-readmodels.md), [B-11](./backend-phase-11-multi-tenant-isolation.md)  
**Цель:** PostgreSQL full-text search (tsvector), search API с ranking, optional Typesense eval, sync on todo mutations.

---

## Результат фазы

- [ ] Column `search_vector tsvector` on `todos` + GIN index
- [ ] Trigger or domain handler updates vector on title/description change
- [ ] `SearchTodosQuery` — `plainto_tsquery`, `ts_rank`, highlight
- [ ] `GET /api/todos/search?q=&page=` endpoint
- [ ] Tenant + RLS respected in search SQL
- [ ] Replace LIKE filter from B-10 with FTS for text param
- [ ] Search integration event → optional Typesense indexer stub
- [ ] Performance test: 100k rows, search p95 < 100ms local
- [ ] ADR-029: Postgres FTS vs Elasticsearch/Typesense

---

## Неделя 1 — PostgreSQL FTS

### B-15.1 Schema migration

1. `V010__todo_fts.sql`:

```sql
ALTER TABLE todos ADD COLUMN search_vector tsvector;
CREATE INDEX ix_todos_search ON todos USING GIN(search_vector);
UPDATE todos SET search_vector = to_tsvector('english', coalesce(title,''));
```

2. Trigger `todos_search_vector_update` BEFORE INSERT OR UPDATE
3. Weight title higher than description if column added

### B-15.2 SearchTodosQuery

1. Dapper query with `WHERE search_vector @@ plainto_tsquery('english', @q)`
2. Order by `ts_rank(search_vector, query) DESC`
3. Return `SearchResultDto` with `Rank`, `Highlight` (ts_headline)

**Файл:** `Infrastructure/Persistence/Sql/search-todos.sql`

### B-15.3 API endpoint

1. `TodosController.Search` or dedicated `SearchController`
2. Min query length 2 chars — validation
3. OpenAPI update

---

## Неделя 2 — Sync & cache

### B-15.4 Keep search_vector fresh

1. `UpdateTodoCommand` — ensure trigger fires (or explicit update in handler)
2. Bulk reindex command for admin: `ReindexSearchCommand`
3. Domain event `TodoSearchIndexUpdatedEvent` (internal)

### B-15.5 Cache & Redis

1. Do NOT cache search results long — TTL 30s max
2. Cache popular queries optional — key includes tenant+q+page

### B-15.6 Typesense eval (optional)

1. Docker Typesense in profile `search`
2. `ISearchIndexer` abstraction — Postgres primary, Typesense optional
3. Document when to switch in ADR-029

---

## Неделя 3 — Load & frontend contract

### B-15.7 Seed 100k todos script

1. `scripts/seed-search-load.sql` — generate varied titles
2. EXPLAIN ANALYZE search query
3. Tune: `default_text_search_config`, consider `pg_trgm` for fuzzy (note in ADR)

### B-15.8 Tests

1. Search finds partial match, ranks exact higher
2. Tenant isolation — search doesn't leak
3. Empty query → 400

### B-15.9 Frontend payload doc

1. `docs/api/search.md` — query params, response shape for NgRx effects

---

## Команды

```bash
dotnet run --project src/TodoPlatform.Api -- --migrate

docker exec -i todo-platform-backend-postgres-1 psql -U todo -d tododb < scripts/seed-search-load.sql

curl "http://localhost:8080/api/todos/search?q=meeting&page=1" \
  -H "Authorization: Bearer <token>" \
  -H "X-Tenant-Id: <tenant>"

dotnet test --filter "FullyQualifiedName~Search"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | GIN index used | EXPLAIN shows Bitmap Index Scan |
| 2 | Search API works | curl returns ranked results |
| 3 | Tenant isolated | cross-tenant test |
| 4 | Trigger sync | update title → searchable |
| 5 | 100k perf | p95 documented |
| 6 | ADR-029 | tradeoffs written |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-15 | Backend search enabled in integration-map |
| Phase 14+ | Search box dispatches `searchTodos` action |
| B-29 | Vector search extends same API pattern |

---

## Следующая фаза

→ [B-16 Kafka Audit Streaming](./backend-phase-16-kafka-streaming.md)
