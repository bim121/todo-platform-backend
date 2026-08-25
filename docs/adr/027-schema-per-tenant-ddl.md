# ADR-027: Hybrid schema-per-tenant DDL

| | |
|---|---|
| **Статус** | Accepted (week 3 draft; physical DDL week 4) |
| **Дата** | 2026-08-26 |
| **Фаза** | B-12 |
| **План** | [backend-phase-12-tenant-schema-versioning.md](../../plans/backend-phase-12-tenant-schema-versioning.md) |
| **Связано** | [ADR-026](./026-shared-schema-rls.md) |

---

## Context

B-12 weeks 1–3 introduced **logical** per-tenant schema versioning (`tenant_schema_versions`, tracks `stable`/`beta`, admin apply). That allows rolling tenants onto beta independently in **accounting**, but on a **shared schema** a `CREATE TABLE` from FluentMigrator is visible to every tenant.

Logical tracks + feature flags are not enough when tenants need **different physical objects** (e.g. `beta_preview_flags` only for acme, not default).

Alternatives:

| Approach | Isolation | Independent DDL | Ops cost |
|----------|-----------|-----------------|----------|
| Shared schema + logical version only | RLS on rows | No — one catalog | Low |
| **Schema-per-tenant (`tenant_*`)** | RLS + `search_path` | Yes — per-tenant `VersionInfo` | Medium |
| Database-per-tenant | Strong | Yes | High |

---

## Decision

### 1. Hybrid model (not DB-per-tenant)

| Schema | Contents | Migrations |
|--------|----------|------------|
| `public` | Platform catalog: `tenants`, `tenant_schema_versions`, `migration_history`, global `VersionInfo` | Startup `MigrateUp` (V001–V011+) |
| `tenant_{slug}` | Tenant-owned: `todos`, `users`, beta tables, future ALTER | `ITenantMigrationRunner` on provision + admin apply |

Naming: `tenant_` + sanitised slug; `tenants.SchemaName` unique column (week 4).

### 2. ADR-026 remains for row isolation

RLS + `TenantId` stay as defense-in-depth even with `search_path`. A pool leak of `search_path` must not expose another tenant’s rows.

### 3. Week 3 deliverables (logical apply)

Until week 4 ships physical DDL:

- Admin apply bumps **logical** `CurrentVersion` and writes `migration_history`.
- Beta-tagged steps are **not** applied to `public` by startup migrate.
- Compatibility rules simulate breaking beta DDL (beta track + no existing todos).
- `TenantMigrationAppliedEvent` → outbox → MassTransit consumer log (B-12.8).

Week 4 replaces `LogicalTenantMigrationRunner` DDL stub with FluentMigrator inside `tenant_*`.

### 4. Rejected

- **DB-per-tenant** — connection routing and backup complexity for this platform size.
- **Logical-only forever** — cannot give two tenants different table sets.

---

## Consequences

**Positive**

- Clear path from logical tracks (week 3) to physical schema isolation (week 4).
- One API binary; admin controls rollout per tenant.
- Platform metadata stays queryable from `public`.

**Negative / tradeoffs**

- N× tenant `VersionInfo` tables; provision on create tenant.
- Cutover migration from `public.todos` → `tenant_*` (B-12.13).
- Application code must branch on track/version when touching beta-only objects until all tenants converge.
- `search_path` must be reset on connection return to pool.

---

## Links

- [docs/migrations/versioning.md](../migrations/versioning.md)
- `LogicalTenantMigrationRunner`, `TenantMigrationCompatibilityValidator`
- Planned: B-12.11 `search_path`, B-12.12 tenant FluentMigrator processor
