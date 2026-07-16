# Backend Phase B-37 — AI Agents & MCP Servers

> **Теория:** [guides/b-37-ai-agents-mcp-theory.md](./guides/b-37-ai-agents-mcp-theory.md) — placeholder  
> **Предусловия:** [B-36](./backend-phase-36-rag-llm.md), [B-03](./backend-phase-03-cqrs-mediatr.md), [B-05](./backend-phase-05-keycloak-auth.md)  
> **Frontend:** [Phase 20](../../anular-ngrx-todo-auth/plans/phase-20-ai-agents-mcp-ui.md)

**Длительность:** 3–4 недели (30–40 ч)  
**Цель:** Tool-calling agent over todo domain + **MCP server** exposing safe tools; human-in-the-loop; prompt-injection defenses.

---

## Результат фазы

- [ ] Agent loop: plan → tool call → observe → answer (bounded steps)
- [ ] Tools: `search_todos`, `create_todo`, `complete_todo`, `rag_query` (reuse B-36)
- [ ] **MCP server** (`todo-platform-mcp`) — stdio and/or SSE; tools mirrored
- [ ] Auth: JWT / service principal; tenant scoped
- [ ] HITL: mutating tools → `requires_confirmation` for FE
- [ ] Audit log of tool calls
- [ ] Guardrails: allowlist, max steps, deny raw SQL
- [ ] ADR-046 agent patterns; ADR-047 MCP threat model

---

## Неделя 1 — Agent core

### B-37.1 Tool interface

```csharp
public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Task<ToolResult> ExecuteAsync(JsonElement args, AgentContext ctx, CancellationToken ct);
}
```

### B-37.2 AgentHost

1. LLM function calling
2. Max 8 steps; timeout
3. Persist transcript per session

---

## Неделя 2 — MCP server

### B-37.3 MCP project

1. Package `TodoPlatform.Mcp`
2. JSON schemas for tools
3. `docs/mcp/README.md` — Cursor / Claude Desktop config

### B-37.4 Security

1. Tenant + user required
2. Rate limit
3. Prompt injection tests (malicious titles)

---

## Неделя 3 — API & tests

### B-37.5 Endpoints

1. `POST /api/ai/agent/sessions`
2. `POST /api/ai/agent/sessions/{id}/messages`
3. Confirm flow for mutations

### B-37.6 Tests

1. Allowlist unit tests
2. E2E create todo with confirmation

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Agent + HITL | e2e |
| 2 | MCP lists tools | client |
| 3 | Injection resisted | adversarial set |
| 4 | ADRs | docs |

---

## Следующая фаза

→ [B-38 AWS ML Engineer Associate](./backend-phase-38-aws-ml-engineer.md)
