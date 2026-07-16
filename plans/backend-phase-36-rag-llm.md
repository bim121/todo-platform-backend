# Backend Phase B-36 — RAG & LLM Applications

> **Теория:** [guides/b-36-rag-llm-theory.md](./guides/b-36-rag-llm-theory.md) — placeholder  
> **Предусловия:** [B-29](./backend-phase-29-ai-vector-backend.md), [B-15](./backend-phase-15-search-fulltext.md), [B-05](./backend-phase-05-keycloak-auth.md)  
> **Frontend:** [Phase 19](../../anular-ngrx-todo-auth/plans/phase-19-rag-chat-ui.md)

**Длительность:** 3–4 недели (30–40 ч)  
**Цель:** Production-style RAG over todos/docs: chunk → embed → retrieve → generate with citations, eval, cost guards.

---

## Результат фазы

- [ ] Document/todo chunking pipeline (title+body; attachments text optional)
- [ ] Retrieval: hybrid (pgvector + FTS from B-15) + optional rerank
- [ ] `POST /api/ai/rag/query` — answer + `citations[]` (todo ids / chunk ids)
- [ ] Streaming response (SSE or chunked) for FE
- [ ] Prompt templates + system guardrails (no PII leak, refuse out-of-scope)
- [ ] Eval harness: faithfulness / relevance scores (offline set)
- [ ] Feature flag `ai.rag.enabled`; token budget per tenant
- [ ] Provider abstraction: OpenAI / Azure OpenAI / **AWS Bedrock** / Ollama
- [ ] ADR-044: RAG vs fine-tune; ADR-045: provider choice

---

## Неделя 1 — Indexing

### B-36.1 Chunker

1. Split long todos/notes; store `chunk_id`, `todo_id`, embedding
2. Re-index on update (domain event from B-04)

### B-36.2 Hybrid retrieve

1. Vector top-k + FTS top-k → merge/RRF
2. Tenant filter always applied

---

## Неделя 2 — Generation

### B-36.3 RagService

1. Build context window from chunks
2. Ask model for answer **only from context**
3. Return citations with scores

### B-36.4 Streaming

1. SSE endpoint for FE Phase 19
2. CancellationToken on client disconnect

---

## Неделя 3 — Quality & cost

### B-36.5 Eval

1. Golden Q/A set in `tests/ai/rag-eval/`
2. Fail CI if faithfulness below threshold (mock provider in CI)

### B-36.6 Cost

1. Max tokens / day per tenant
2. Cache identical queries in Redis (TTL)

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Citations present | API contract |
| 2 | Tenant isolation | security test |
| 3 | Stream works | curl -N |
| 4 | Eval harness | CI job |
| 5 | ADRs | docs |

---

## Следующая фаза

→ [B-37 AI Agents & MCP](./backend-phase-37-ai-agents-mcp.md)
