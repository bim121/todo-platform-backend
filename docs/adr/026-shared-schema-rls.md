# ADR-026: Shared schema + Row Level Security

| | |
|---|---|
| **Статус** | Accepted |
| **Дата** | 2026-08-17 |
| **Фаза** | B-11 |
| **План** | [backend-phase-11-multi-tenant-isolation.md](../../plans/backend-phase-11-multi-tenant-isolation.md) |
| **Теория** | [plans/guides/b-11-multi-tenant-isolation-theory.md](../../plans/guides/b-11-multi-tenant-isolation-theory.md) |

---

## Context

Todo Platform is multi-tenant. Isolation must hold even if an application bug forgets a `WHERE "TenantId" = …` clause. Alternatives: database-per-tenant, schema-per-tenant, or a shared schema with PostgreSQL Row Level Security (RLS).

We already have one Postgres, FluentMigrator, EF writes, Dapper reads, and a small number of tenants in dev (`default`, `acme-corp`). Ops complexity should stay low until B-12 (tenant schema versioning).

---

## Decision

### 1. Shared schema + RLS (not schema-per-tenant)

All tenants share `todos` / `users`. Every row has `TenantId`. Policies:

```sql
USING (
    current_setting('app.bypass_rls', true) = 'true'
    OR "TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid
)
```

`FORCE ROW LEVEL SECURITY` so the table owner cannot skip policies. **Superusers still bypass** (docker `POSTGRES_USER=todo` is a superuser — verify with a `NOSUPERUSER` role).

### 2. Defense in depth

| Layer | What it does |
|-------|----------------|
| **HTTP** | `TenantResolutionMiddleware` — `X-Tenant-Id` or JWT `tenant_id` |
| **App** | Scoped `ITenantContext`; commands assign `TenantId` server-side |
| **EF** | Global query filter on `Todo` / `User` when tenant is resolved |
| **Postgres** | RLS + `SET app.current_tenant` on EF/Dapper connection open |
| **Cache** | Keys include tenant: `todos:tenant:{tid}:user:{uid}` |

A missed `WHERE` in Dapper still hits RLS. A cache key without tenant would leak across tenants — hence B-11.8.

### 3. Admin platform-wide reads

`GET /api/admin/stats` is not tenant-scoped. Session GUC `app.bypass_rls=true` for that query only (Dapper). EF test store uses `IgnoreQueryFilters()`. Regular todo CRUD stays tenant-scoped even for `admin` role.

### 4. Rejected (B-11): schema-per-tenant as the *isolation* strategy

Separate `tenant_acme.todos` schemas: strong isolation and independent migrations, but N× schema drift, connection routing, and FluentMigrator complexity. For B-11 the default is shared tables + RLS.

**Follow-up (B-12 week 4):** independent **DDL** per tenant is now in scope — [backend-phase-12](../../plans/backend-phase-12-tenant-schema-versioning.md) + planned **ADR-027**. Isolation of *rows* stays RLS; isolation of *schema objects* (different tables/columns per tenant) moves to `tenant_*` PostgreSQL schemas. This section is not a veto of that work.

---

## Consequences

**Positive**

- One migration stream; isolation proven in tests without a query-filter-only lie
- App bugs cannot read another tenant’s rows on a non-superuser role
- Same connection pool; tenant is a session GUC, RESET on close (no pool leak)

**Negative / tradeoffs**

- Superuser and `BYPASSRLS` roles ignore policies — never run the API as `postgres`/`todo` superuser in prod
- Custom GUCs (`app.current_tenant`) are easy to forget on a raw SQL script
- `FORCE RLS` does not apply to superusers; tests must use `todo_app` (NOSUPERUSER)

---

## Links

- Migration `V009_TenantsAndRowLevelSecurity`, `V010_AdminRlsBypass`
- `TenantResolutionMiddleware`, `TenantDbConnectionInterceptor`, `AppDbContext` query filters
- Debug: [docs/multi-tenancy/isolation.md](../multi-tenancy/isolation.md)
- Cache keys: [ADR-022](./022-caching-strategy.md) (updated in B-11)
