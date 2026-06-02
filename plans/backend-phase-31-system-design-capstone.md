# Backend Phase B-31 — System Design Capstone

> **Теория:** [guides/b-31-system-design-capstone-theory.md](./guides/b-31-system-design-capstone-theory.md) — статус: placeholder  
> **Parallel track:** [parallel-skills-backend.md](./parallel-skills-backend.md)

**Длительность:** 3–4 недели (30–40 ч)  
**Предусловия:** [B-30](./backend-phase-30-security-hardening.md) — весь backend roadmap пройден  
**Цель:** Пять полноценных system design документов по реальной архитектуре todo-platform + portfolio polish.

---

## Результат фазы

- [ ] `docs/system-design/backend/01-multi-tenant-saas-db.md`
- [ ] `docs/system-design/backend/02-event-driven-notifications.md`
- [ ] `docs/system-design/backend/03-search-at-scale.md`
- [ ] `docs/system-design/backend/04-saga-migration-rollout.md`
- [ ] `docs/system-design/backend/05-global-db-sharding.md`
- [ ] Каждый doc: requirements, estimations, diagram, API, data model, deep dives, tradeoffs, failures
- [ ] Mermaid или Excalidraw диagrams в каждом doc
- [ ] Cross-links к ADR B-04…B-30
- [ ] 5-minute pitch deck outline `docs/system-design/backend/PITCH.md`
- [ ] README portfolio section — architecture overview link

---

## Неделя 1 — Multi-tenant & event-driven

### B-31.1 Doc 01: Multi-tenant SaaS DB

1. Functional: tenant isolation, admin migration tracks, RLS
2. NFR: 10k tenants, 1M todos, p95 read 100ms
3. Diagram: app → PgBouncer → primary/replica → RLS
4. Deep dive: RLS vs schema-per-tenant vs shard (B-11, B-21)
5. Failure: replica lag, tenant hot spot
6. Link ADR-026, ADR-035

**Файл:** `docs/system-design/backend/01-multi-tenant-saas-db.md`

### B-31.2 Doc 02: Event-driven notifications

1. Flow: domain event → outbox → RabbitMQ → email + SignalR
2. Estimation: 500 events/sec peak, queue sizing
3. Deep dive: at-least-once, idempotency, ordering per aggregate
4. Compare choreography vs orchestration (B-18)
5. Failure: broker down, poison messages, DLQ playbooks

---

## Неделя 2 — Search & saga

### B-31.3 Doc 03: Search at scale

1. FTS + vector hybrid (B-15, B-29)
2. Estimation: 100M todos, index size, rebuild strategy
3. Deep dive: Postgres FTS vs Typesense vs Elasticsearch
4. Deep dive: embedding pipeline cost, caching query vectors
5. Failure: index corruption, stale embeddings

### B-31.4 Doc 04: Saga migration rollout

1. Bulk tenant migration saga (B-18) + blue-green (B-28)
2. State machine diagram
3. Compensation policies StopOnFirstError vs ContinueAll
4. Admin UX polling, timeout handling
5. Failure: partial migration, rollback per tenant

---

## Неделя 3 — Global DB & diagrams

### B-31.5 Doc 05: Global DB sharding

1. TenantId shard key, geo regions note
2. Cross-shard admin queries map-reduce
3. Rebalance plan from B-21 expanded
4. Compare Cockroach/Yugabyte vs app-level sharding
5. Failure: shard outage, split-brain avoidance

### B-31.6 Architecture master diagram

1. Single diagram: nginx → gateway → services → data plane
2. Observability, Kafka audit side path
3. Include in README and PITCH.md

---

## Неделя 4 — Portfolio & mock interview

### B-31.7 PITCH.md

1. 5-minute spoken walkthrough script
2. Highlights: multi-tenant, event-driven, K8s deploy
3. Metrics from B-22 baseline (real numbers if available)

### B-31.8 Mock interview prep

1. Each doc → 2 likely interviewer follow-up questions + answers
2. «Why not X?» tradeoff tables consolidated
3. Record yourself or peer review once

### B-31.9 Final repo polish

1. Root README links all 5 docs
2. Ensure ADR index `docs/adr/README.md` complete
3. Changelog entry `docs/phase-b-changelog.md`

---

## Команды

```bash
# verify all docs exist
ls docs/system-design/backend/

# spellcheck optional
npx markdownlint docs/system-design/backend/*.md

# link check (if lychee installed)
lychee docs/system-design/backend/

# count words per doc (target 1500-2500 each)
wc -w docs/system-design/backend/*.md

# export pitch to PDF optional
# npx md-to-pdf docs/system-design/backend/PITCH.md
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | 5 design docs | files exist, complete template |
| 2 | Diagrams each doc | mermaid renders |
| 3 | Estimations | QPS/storage numbers |
| 4 | ADR cross-links | relative links work |
| 5 | PITCH.md | 5-min script |
| 6 | README updated | portfolio section |
| 7 | Interview ready | 2 Q&A per doc |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-31 | Full-stack portfolio — link frontend phases |
| Capstone | End-to-end demo video script (optional) |
| Interviews | Microsoft Azure path + Google scale path variants in doc 05 |

---

## Следующая фаза

→ **Capstone complete.** Portfolio polish, mock interviews, production hardening backlog.
