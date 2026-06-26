# Backend Phase B-21 — Sharding & Partitioning

> **Теория:** [guides/b-21-sharding-partitioning-theory.md](./guides/b-21-sharding-partitioning-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-20](./backend-phase-20-db-replication-scaling.md), [B-11](./backend-phase-11-multi-tenant-isolation.md)  
**Цель:** Table partitioning by TenantId hash, shard routing layer, tenant-to-shard registry, migration path from single DB.

---

## Результат фазы

- [ ] `todos` table partitioned LIST/RANGE by `tenant_id` hash bucket (demo 4 partitions)
- [ ] Table `tenant_shards` — TenantId → ShardId mapping
- [ ] `IShardResolver` — resolve connection string per tenant
- [ ] EF Core `ShardDbContext` factory — multi-database ready (2 shards in compose)
- [ ] Prune old partitions script for `audit_logs` by month (RANGE)
- [ ] Admin API: `GET /admin/shards` — tenant distribution
- [ ] Rebalance plan document (no live rebalance — design only)
- [ ] ADR-035: shard key = TenantId rationale

---

## Неделя 1 — Partitioning in PostgreSQL

### B-21.1 Partition todos table

1. Migration strategy: new partitioned table `todos_p`, copy data, swap
2. `PARTITION BY HASH (tenant_id)` — 4 partitions for dev
3. Verify partition pruning: EXPLAIN shows only one partition scanned

**File:** `V012__todos_partitioning.sql`

### B-21.2 Audit log time partitioning

1. `audit_logs` PARTITION BY RANGE (occurred_at) — monthly
2. Script `scripts/create-audit-partition.sql`
3. Drop partition older than retention (B-16 policy)

### B-21.3 Index per partition

1. Local indexes on each partition
2. Unique constraints include partition key where needed

---

## Неделя 2 — Shard routing

### B-21.4 tenant_shards registry

1. Table + seed: tenants evenly on shard-0, shard-1
2. `ShardResolver` reads cache in Redis
3. New tenant assignment — round-robin algorithm

**Файл:** `Infrastructure/Sharding/ShardResolver.cs`

### B-21.5 Multi-database compose

1. `postgres-shard-0`, `postgres-shard-1` services
2. Each has subset of tenants (logical split)
3. `IDbContextFactory<AppDbContext>` with shard connection

### B-21.6 Cross-shard queries blocked

1. Admin global stats — map-reduce stub aggregating per shard
2. User queries always single-shard via tenant context
3. Integration test: tenant on shard-1 not visible on shard-0 connection

---

## Неделя 3 — Operations & design

### B-21.7 Rebalance design doc

1. `docs/sharding/rebalance-plan.md` — add shard, migrate tenants
2. Dual-write window concept
3. Interview talking points

### B-21.8 Admin shard API

1. `GetShardDistributionQuery`
2. Show tenant count per shard, disk usage note

### B-21.9 Tests & ADR

1. Partition prune verified in EXPLAIN
2. ShardResolver unit tests
3. ADR-035 published

---

## Команды

```bash
docker compose -f docker-compose.yml -f docker-compose.sharding.yml up -d

dotnet run --project src/TodoPlatform.Todos.Api -- --migrate

# partition explain
docker exec todo-platform-postgres-shard-0 psql -U todo -d tododb \
  -c "EXPLAIN SELECT * FROM todos WHERE tenant_id = '<uuid>';"

curl http://localhost:8080/admin/shards -H "Authorization: Bearer <admin_token>"

dotnet test --filter "FullyQualifiedName~Shard"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Partitions exist | \d+ todos |
| 2 | Partition pruning | EXPLAIN |
| 3 | Shard routing | tenant hits correct DB |
| 4 | Cross-shard blocked | test |
| 5 | Audit monthly partitions | table list |
| 6 | Admin shard API | GET /admin/shards |
| 7 | ADR-035 | shard key doc |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-21 | No frontend change — tenant header drives shard |
| Admin | Shard distribution dashboard |
| B-28 | Per-tenant deploy may target shard subsets |

Parallel skills: Design multi-tenant SaaS DB — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-22 Performance & Load Testing](./backend-phase-22-performance-load.md)
