# Multi-tenant isolation — debug guide

How tenant isolation is enforced and how to see it fail (or appear to fail).

## Resolution order

Authenticated request:

1. `UseAuthentication` — JWT available
2. `TenantResolutionMiddleware`
   - Header `X-Tenant-Id` (UUID **or** slug: `default`, `acme-corp`)
   - else JWT claim `tenant_id`
   - missing → **400**; unknown/inactive → **404**
3. Scoped `ITenantContext.Set(id, slug, schemaName)`
4. EF interceptor / Dapper wrapper: `SET search_path TO tenant_{slug}, public` + `SET app.current_tenant`
5. `UseCurrentUserSync` / `[Authorize]`

Health and Swagger skip the header.

## Layers (defense in depth)

```
Client  --X-Tenant-Id / JWT-->  Middleware  --> ITenantContext
                                      |
                    +-----------------+------------------+
                    v                                    v
            EF query filter                    Postgres RLS + search_path
            (Todo, User)                       SET app.current_tenant
                                               SET search_path = tenant_*, public
                    |
                    v
            Redis keys  todos:tenant:{tid}:user:{uid}
```

Interview one-liner: *the app filter is the seatbelt; RLS is the airbag. Cache keys must include tenant or the seatbelt is pointless.*

## Why `psql -U todo` still sees every row

Compose `POSTGRES_USER=todo` is a **superuser**. Superusers bypass RLS even with `FORCE ROW LEVEL SECURITY`.

Check:

```sql
SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname IN ('todo', 'todo_app');
SELECT tablename, rowsecurity FROM pg_tables WHERE tablename IN ('todos', 'users');
```

To verify policies, connect as a `NOSUPERUSER NOBYPASSRLS` role (tests create `todo_app`).

```sql
SET app.current_tenant = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';  -- default
SELECT "Title", "TenantId" FROM todos;

SET app.current_tenant = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';  -- acme-corp
SELECT "Title", "TenantId" FROM todos;

RESET app.current_tenant;
SELECT COUNT(*) FROM todos;  -- 0 for todo_app; all rows for superuser
```

## Admin bypass

Platform-wide `GET /api/admin/stats` sets `app.bypass_rls=true` for that query, then RESET. Do not set this on ordinary todo CRUD.

Admin stats also **bypass `search_path`**: when `tenants.SchemaName` is populated, the query unions `users`/`todos` across all `tenant_*` schemas (see `DapperSystemStatsReadStore`). Catalog tables remain under explicit `public.`.

## Schema-per-tenant (`search_path`)

Since B-12 week 4, tenant-owned tables live in `tenant_{slug}` (column `tenants.SchemaName`). After tenant resolution:

```sql
SET search_path TO tenant_default, public;
SET app.current_tenant = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
SELECT * FROM todos;  -- resolves to tenant_default.todos
SELECT * FROM public.tenants;  -- platform catalog
```

On connection close / return to pool, `TenantSession.Reset` sets `search_path` back to `public` so the next request cannot inherit another tenant’s schema.

Verify:

```sql
SHOW search_path;
SELECT table_schema, table_name FROM information_schema.tables
WHERE table_name = 'beta_preview_flags';
-- only beta-applied tenant schemas
```

## Cache

| Key | Use |
|-----|-----|
| `todos:tenant:{tid}:user:{uid}:a…:s…:t…` | list |
| `todos:tenant:{tid}:user:{uid}` | prefix delete |
| `todo:tenant:{tid}:{todoId}` | by id |
| `stats:tenant:{tid}:user:{uid}` | aggregates |

If tenant B can `GET /todos/{id}` and receive tenant A’s JSON, look at **cache keys first**, then the query filter, then RLS.

## Common failures

| Symptom | Likely cause |
|---------|----------------|
| Authenticated 400 | Missing `X-Tenant-Id` and no `tenant_id` claim |
| 404 on a known slug | Tenant inactive, or typo |
| Cross-tenant list not empty (InMemory tests) | Query filter off (`ITenantContext` not resolved) |
| Cross-tenant list not empty (Postgres + `todo`) | Superuser bypass — use `todo_app` |
| Stale todo from another tenant | Cache key without tenant (pre-B-11.8) |
| Admin stats only one tenant | Bypass GUC not applied, or search_path still scoped to one tenant |
| Wrong tenant schema after idle | Pool leak — ensure `TenantSession.Reset` on connection close |

## Related

- [ADR-026](../adr/026-shared-schema-rls.md)
- [ADR-027](../adr/027-schema-per-tenant-ddl.md)
- `TenantResolutionMiddleware`, `TenantSession`, `AppDbContext` (`TenantFilterEnabled`)
