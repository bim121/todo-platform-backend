# Backend Phase B-20 — DB Replication & Scaling

> **Теория:** [guides/b-20-db-replication-scaling-theory.md](./guides/b-20-db-replication-scaling-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–30 ч)  
**Предусловия:** [B-09](./backend-phase-09-postgres-queries.md), [B-10](./backend-phase-10-complex-sql-readmodels.md), [B-17](./backend-phase-17-microservices-split.md)  
**Цель:** PostgreSQL read replica, routing read queries to replica, PgBouncer connection pooling, lag monitoring.

---

## Результат фазы

- [ ] Postgres primary + replica in docker-compose (streaming replication)
- [ ] `ConnectionStrings:Read` points to replica; `Write` to primary
- [ ] EF Core writes primary only; Dapper reads replica via `IReadDbConnection`
- [ ] PgBouncer container — transaction pooling mode
- [ ] `IReplicationLagMonitor` — query `pg_stat_replication`, expose metric
- [ ] Fallback to primary if lag > 5s or replica down
- [ ] Read-your-writes workaround — sticky session flag optional
- [ ] Document Azure Flexible Server HA equivalent
- [ ] ADR-034: read replica routing rules

---

## Неделя 1 — Replication setup

### B-20.1 Docker primary/replica

1. `postgres-primary` and `postgres-replica` services
2. Init script: `infra/postgres/replication-setup.sh`
3. User replication slot, `hot_standby = on`
4. Verify: `SELECT * FROM pg_stat_replication;`

**Files:**
- `docker-compose.replication.yml`
- `infra/postgres/primary.conf`, `replica.conf`

### B-20.2 Connection strings

1. Write: port 5432 primary
2. Read: port 5433 replica
3. Update `DapperReadDbConnection` and EF DbContext registrations

### B-20.3 PgBouncer

1. Service `pgbouncer` — pools to primary for writes, replica for reads
2. `pool_mode = transaction`
3. App connects to pgbouncer ports not direct postgres

---

## Неделя 2 — Application routing

### B-20.4 Read/write split enforcement

1. Audit code — no accidental writes on read connection
2. `GetTodosQuery`, search, stats — replica only
3. Commands — primary + UoW

### B-20.5 Lag-aware routing

1. Background service checks replication lag every 10s
2. If lag > threshold → route reads to primary (feature flag)
3. Health check `/health/db` includes replica status

### B-20.6 Sticky read-after-write

1. Optional middleware sets cookie/header `read-primary=true` for 2s after mutation
2. Document tradeoff in ADR-034
3. Test: create todo → immediate GET sees new row

---

## Неделя 3 — Ops & tests

### B-20.7 Failover drill (dev)

1. Stop replica — app continues on primary reads
2. Document manual failover steps (not auto HA)
3. Azure HA comparison section in ADR

### B-20.8 Performance validation

1. Compare load on primary before/after — read QPS shifted
2. pg_stat_statements on replica
3. Integration tests with both connections

---

## Команды

```bash
docker compose -f docker-compose.yml -f docker-compose.replication.yml up -d

# check replication
docker exec todo-platform-postgres-primary psql -U todo -d tododb \
  -c "SELECT client_addr, state, sync_state FROM pg_stat_replication;"

# lag bytes
docker exec todo-platform-postgres-primary psql -U todo -d tododb \
  -c "SELECT pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) AS lag_bytes FROM pg_stat_replication;"

dotnet test --filter "FullyQualifiedName~Replication"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Replica streaming | pg_stat_replication row |
| 2 | Reads hit replica | log connection host |
| 3 | Writes on primary | EF SaveChanges path |
| 4 | PgBouncer pooling | show pools |
| 5 | Lag monitor | metric exposed |
| 6 | Failover dev test | replica stop OK |
| 7 | ADR-034 | routing rules |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-20 | Transparent — optional loading states if lag high |
| B-22 | Load test validates replica scaling |
| B-21 | Sharding replaces single replica at scale |

Parallel skills: SQL weeks 5–8 read replicas — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-21 Sharding & Partitioning](./backend-phase-21-sharding-partitioning.md)
