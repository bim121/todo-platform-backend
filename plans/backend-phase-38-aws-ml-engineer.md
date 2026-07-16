# Backend Phase B-38 — AWS ML Engineer Associate (MLA)

> **Теория:** [guides/b-38-aws-ml-engineer-theory.md](./guides/b-38-aws-ml-engineer-theory.md) — placeholder  
> **Cert:** [aws-cert-track.md](./aws-cert-track.md) — **MLA-C01**  
> **Предусловия:** [B-36](./backend-phase-36-rag-llm.md), [B-34](./backend-phase-34-aws-foundations.md), [B-29](./backend-phase-29-ai-vector-backend.md)

**Длительность:** 3–4 недели (30–40 ч)  
**Цель:** Практические lab'ы под AWS Certified Machine Learning Engineer – Associate: Bedrock, batch embeddings, pipelines, monitoring, responsible AI.

---

## Результат фазы

- [ ] **Amazon Bedrock** embeddings + chat as first-class `IEmbeddingService` / `IChatProvider`
- [ ] Batch embed job: Step Functions **или** SageMaker Processing job stub for backfill
- [ ] Model / prompt versioning documented (`docs/ai/model-card.md`)
- [ ] Data prep: train/eval split for RAG golden set in S3
- [ ] Monitoring: latency, token usage, error rate CloudWatch dashboards
- [ ] Responsible AI: PII redaction stub, content filters (Bedrock Guardrails)
- [ ] Cost attribution per tenant
- [ ] ADR-048: Bedrock vs OpenAI vs self-host
- [ ] Exam domain checklist mapped in [aws-cert-track.md](./aws-cert-track.md)

---

## Неделя 1 — Bedrock integration

### B-38.1 Providers

1. `BedrockEmbeddingService`, `BedrockChatService`
2. IAM role for ECS task: `bedrock:InvokeModel` on specific models only
3. Fallback to Ollama in local dev

### B-38.2 Guardrails

1. Bedrock Guardrails or app-level filters
2. Log blocked prompts (no raw PII)

---

## Неделя 2 — Pipelines

### B-38.3 Batch backfill

1. S3 input of todo ids → Step Functions → Lambda/ECS task → write embeddings to RDS
2. Idempotent; progress metric

### B-38.4 SageMaker (optional spike)

1. One Processing job notebook export for interview story
2. Or skip if Bedrock-only path chosen — document in ADR

---

## Неделя 3 — Ops & exam

### B-38.5 Monitoring

1. CloudWatch: `rag_latency_ms`, `tokens_total`, `agent_tool_errors`
2. Alarm on cost spike

### B-38.6 MLA domains practice

| Domain | Lab |
|--------|-----|
| Data prep | S3 golden set + chunking |
| Model development / selection | Bedrock model choice ADR |
| Deployment | ECS + IAM + feature flags |
| Monitoring & optimization | dashboards + budgets |
| Responsible AI | guardrails + audit |

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Bedrock path works in staging | smoke |
| 2 | Batch job completes | S3/RDS |
| 3 | Guardrails block unsafe | test |
| 4 | Cost visible | dashboard |
| 5 | Cert checklist ≥80% labs | aws-cert-track |

---

## Следующая фаза

Roadmap AI/AWS complete. Portfolio: refresh [B-31](./backend-phase-31-system-design-capstone.md) with RAG/agent/AWS docs.
