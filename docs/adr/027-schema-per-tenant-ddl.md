# ADR-027: Hybrid schema-per-tenant DDL

| | |
|---|---|
| **Статус** | Accepted |
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
| `public` | Platform catalog: `tenants`, `tenant_schema_versions`, `migration_history`, global `VersionInfo` | Startup `MigrateUp` (V001–V013 platform) |
| `tenant_{slug}` | Tenant-owned: `todos`, `users`, beta tables, future ALTER | `PhysicalTenantMigrationRunner` on provision + admin apply |

Naming: `tenant_` + sanitised slug (`[a-z0-9_]`); `tenants.SchemaName` unique column.

### 2. ADR-026 remains for row isolation

RLS + `TenantId` stay as defense-in-depth even with `search_path`. A pool leak of `search_path` must not expose another tenant’s rows.

### 3. Two migration streams

| Stream | Versions | Tags | Runner |
|--------|----------|------|--------|
| Platform | V001–V013 | untagged / `platform` | Global `MigrateUp` at startup |
| Logical catalog | 1–12 | `beta` on V012 | `IMigrationPlanService` (admin UI) |
| Physical tenant | T1001 (baseline), T1012 (beta) | `tenant` | `TenantFluentMigrator` per schema |

Admin apply bumps **logical** `CurrentVersion`, writes `migration_history`, and runs physical tenant-stream DDL inside `tenant_*`.

V012 logical entry is catalog-only; physical beta table is **T1012** via `search_path` on the tenant connection.

### 4. Rejected

- **DB-per-tenant** — connection routing and backup complexity for this platform size.
- **Logical-only forever** — cannot give two tenants different table sets.

---

## Consequences

**Positive**

- Two tenants can have different physical catalogs (proof: `tenant_acme_corp.beta_preview_flags` vs absent in `tenant_default`).
- One API binary; admin controls rollout per tenant.
- Platform metadata stays queryable from `public`.

**Negative / tradeoffs**

- N× tenant `VersionInfo` tables; provision on create tenant (`TenantSchemaProvisioner`).
- Cutover migration V013 from `public.todos` → `tenant_*`; legacy `public.todos` retained for rollback window.
- Application code must branch on track/version when touching beta-only objects until all tenants converge.
- `search_path` must be reset on connection return to pool (`TenantSession.Reset`).

---

## Links

- [docs/migrations/versioning.md](../migrations/versioning.md)
- [docs/multi-tenancy/isolation.md](../multi-tenancy/isolation.md)
- `PhysicalTenantMigrationRunner`, `TenantFluentMigrator`, `V013_TenantSchemaCutover`
