# Backend Phase B-22 — Performance & Load Testing

> **Теория:** [guides/b-22-performance-load-theory.md](./guides/b-22-performance-load-theory.md) — статус: placeholder

**Длительность:** 2 недели (20–30 ч)  
**Предусловия:** [B-21](./backend-phase-21-sharding-partitioning.md), [B-19](./backend-phase-19-redis-advanced.md), [B-08](./backend-phase-08-docker-compose.md)  
**Цель:** pgBench baseline, k6 load tests для API, SLO definitions, bottleneck report + fixes.

---

## Результат фазы

- [ ] pgBench scripts against primary and replica
- [ ] k6 scenarios: `load-todos.js`, `create-todo.js`, `search-todos.js`
- [ ] SLO doc: p95 < 200ms reads, p99 < 500ms, error rate < 0.1%
- [ ] Baseline report `docs/performance/baseline-YYYY-MM.md`
- [ ] At least 2 optimizations applied from findings (index, cache TTL, pool size)
- [ ] CI nightly smoke k6 (short run) optional
- [ ] Grafana dashboard stub for k6 results import (prep B-24)
- [ ] BenchmarkDotNet for hot handler path (optional)

---

## Неделя 1 — Database benchmarks

### B-22.1 pgBench setup

1. Scale factor for 10GB optional; small SF1 for CI
2. Custom script simulating todo lookups by user_id
3. Run against primary vs replica — compare TPS

**Files:**
- `perf/pgbench/todo-read.sql`
- `perf/pgbench/run.sh`

### B-22.2 EF vs Dapper numbers

1. Re-run B-10 benchmark under load
2. Document connection pool saturation point
3. Tune PgBouncer pool sizes

### B-22.3 Identify slow queries

1. pg_stat_statements top 10 during pgBench
2. EXPLAIN fixes if regressions found

---

## Неделя 2 — k6 API load tests

### B-22.4 k6 scripts

1. Auth setup — fetch Keycloak token in setup()
2. Scenario 1: 50 VUs GET /api/todos 5 min
3. Scenario 2: mixed read/write 70/30
4. Scenario 3: search endpoint stress
5. Thresholds: `http_req_duration{p(95)}<200`

**File:** `perf/k6/load-todos.js`

### B-22.5 Rate limit validation

1. Scenario exceeds B-19 limits — expect 429 rate ~expected
2. Verify no 500 under normal load

### B-22.6 Report & fixes

1. Capture CPU/memory docker stats
2. Apply fixes: Redis cache warming, index, Kestrel thread pool
3. Before/after comparison in baseline report
4. SLO error budget math — link to B-24

---

## Команды

```bash
# pgBench
docker exec todo-platform-postgres-primary pgbench -i -s 10 -U todo tododb
docker exec todo-platform-postgres-primary pgbench -U todo -d tododb -c 10 -j 2 -T 60 -f /scripts/todo-read.sql

# k6 (install k6 locally or docker)
k6 run perf/k6/load-todos.js -e API_URL=http://localhost:8080 -e TOKEN=<jwt>

k6 run perf/k6/mixed-workload.js --vus 50 --duration 5m

# dotnet microbench (optional)
dotnet run -c Release --project perf/TodoPlatform.Benchmarks
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | pgBench report | TPS numbers in doc |
| 2 | k6 thresholds pass | exit 0 |
| 3 | SLO documented | docs/performance/slo.md |
| 4 | Bottleneck identified | report section |
| 5 | 2+ fixes applied | git diff + re-run |
| 6 | Rate limit behavior | 429 chart |
| 7 | No error storm | <0.1% 5xx |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-22 | Shared SLO with frontend LCP targets |
| Phase 3 | Optimistic UI stress aligns with write load |
| B-24 | Metrics feed Grafana |

Parallel skills: SQL weeks 13+ load testing — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-23 nginx Gateway & TLS](./backend-phase-23-nginx-gateway.md)
