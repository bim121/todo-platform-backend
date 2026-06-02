# Backend Phase B-29 — AI Vector Backend (pgvector)

> **Теория:** [guides/b-29-ai-vector-backend-theory.md](./guides/b-29-ai-vector-backend-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-15](./backend-phase-15-search-fulltext.md), [B-11](./backend-phase-11-multi-tenant-isolation.md)  
**Цель:** pgvector для semantic search todos, embedding pipeline, `POST /api/todos/semantic-search`, optional Azure OpenAI embeddings.

---

## Результат фазы

- [ ] PostgreSQL extension `vector` enabled
- [ ] Column `todos.embedding vector(1536)` — OpenAI ada-002 dimension
- [ ] `IEmbeddingService` — Azure OpenAI or local mock for dev
- [ ] Background job embeds todo on create/update (outbox consumer)
- [ ] `SemanticSearchTodosQuery` — cosine similarity `<=>` with tenant filter
- [ ] `POST /api/todos/semantic-search` — `{ query, topK }`
- [ ] IVFFlat or HNSW index on embedding column
- [ ] Hybrid search: combine FTS rank + vector score (optional)
- [ ] Rate limit embedding calls — cost control
- [ ] ADR-042: pgvector vs Qdrant

---

## Неделя 1 — pgvector setup

### B-29.1 Extension & schema

1. Docker image `pgvector/pgvector:pg16` or init script `CREATE EXTENSION vector`
2. Migration `V014__todo_embeddings.sql`
3. Backfill job stub for existing todos

```sql
ALTER TABLE todos ADD COLUMN embedding vector(1536);
CREATE INDEX ix_todos_embedding ON todos USING hnsw (embedding vector_cosine_ops);
```

### B-29.2 IEmbeddingService

1. Interface: `Task<float[]> EmbedAsync(string text, CancellationToken ct)`
2. `AzureOpenAIEmbeddingService` — `Azure.AI.OpenAI` SDK
3. `MockEmbeddingService` — deterministic hash-based vector for dev/tests

**File:** `Infrastructure/Ai/AzureOpenAIEmbeddingService.cs`

### B-29.3 Configuration

1. Key Vault secrets: `OpenAI--Endpoint`, `OpenAI--ApiKey`
2. Feature flag `Ai:EmbeddingsEnabled`
3. Max batch size for backfill

---

## Неделя 2 — Indexing pipeline

### B-29.4 Embed on mutation

1. Consumer `EmbedTodoConsumer` on TodoCreated/Updated integration events
2. Fetch todo title+description, call embedding service, UPDATE todos
3. Idempotent — skip if content hash unchanged

### B-29.5 Backfill command

1. Admin `ReindexEmbeddingsCommand` — batch process tenants
2. Progress in Redis or saga status table
3. Throttle 10 req/s to OpenAI

### B-29.6 SemanticSearchTodosQuery

1. Embed query text
2. SQL: `ORDER BY embedding <=> @queryVector LIMIT @topK`
3. Return todos with `similarityScore`

**File:** `Infrastructure/Persistence/Sql/semantic-search.sql`

---

## Неделя 3 — API & hybrid search

### B-29.7 API endpoint

1. `POST /api/todos/semantic-search` body `{ "query": "...", "topK": 10 }`
2. Validate min length, tenant scoped
3. OpenAPI + contract sync

### B-29.8 Hybrid search (optional)

1. `HybridSearchQuery` — weighted `0.6 * vector + 0.4 * fts_rank`
2. Compare quality manually — document in ADR-042

### B-29.9 Tests & cost guards

1. Mock embedding service in tests
2. Daily embedding quota counter in Redis
3. Integration test semantic finds synonym match

---

## Команды

```bash
# pgvector image
docker compose -f docker-compose.yml -f docker-compose.pgvector.yml up -d postgres

dotnet add src/TodoPlatform.Infrastructure package Azure.AI.OpenAI

dotnet run --project src/TodoPlatform.Todos.Api -- --migrate

curl -X POST http://localhost:8080/api/todos/semantic-search \
  -H "Authorization: Bearer <token>" \
  -H "X-Tenant-Id: <tenant>" \
  -H "Content-Type: application/json" \
  -d '{"query":"prepare quarterly report","topK":5}'

dotnet test --filter "FullyQualifiedName~SemanticSearch"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | vector extension | `\dx vector` |
| 2 | Embeddings stored | non-null after create |
| 3 | Semantic search works | synonym query hits |
| 4 | Tenant isolated | cross-tenant test |
| 5 | Index used | EXPLAIN on search |
| 6 | Mock dev mode | no API key required |
| 7 | ADR-042 | pgvector vs Qdrant |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-29 | Frontend Phase 18 AI features |
| Phase 18 | Semantic search UI, loading states for AI |
| B-15 | Keyword search coexists |

Parallel skills: Design search engine — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-30 Security Hardening (OWASP)](./backend-phase-30-security-hardening.md)
