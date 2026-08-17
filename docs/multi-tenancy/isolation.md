# Multi-tenant isolation — debug guide

How tenant isolation is enforced and how to see it fail (or appear to fail).

## Resolution order

Authenticated request:

1. `UseAuthentication` — JWT available
2. `TenantResolutionMiddleware`
   - Header `X-Tenant-Id` (UUID **or** slug: `default`, `acme-corp`)
   - else JWT claim `tenant_id`
   - missing → **400**; unknown/inactive → **404**
3. Scoped `ITenantContext.Set(id, slug)`
4. EF interceptor / Dapper wrapper: `SET app.current_tenant`
5. `UseCurrentUserSync` / `[Authorize]`

Health and Swagger skip the header.

## Layers (defense in depth)

```
Client  --X-Tenant-Id / JWT-->  Middleware  --> ITenantContext
                                      |
                    +-----------------+------------------+
                    v                                    v
            EF query filter                    Postgres RLS
            (Todo, User)                       SET app.current_tenant
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
| Admin stats only one tenant | Bypass GUC not applied on the stats connection |

## Related

- [ADR-026](../adr/026-shared-schema-rls.md)
- `TenantResolutionMiddleware`, `TenantSession`, `AppDbContext` (`TenantFilterEnabled`)
