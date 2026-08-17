# Backend Phase B-11 — Multi-Tenant Isolation (RLS)

> **Теория:** [guides/b-11-multi-tenant-isolation-theory.md](./guides/b-11-multi-tenant-isolation-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-10](./backend-phase-10-complex-sql-readmodels.md), [B-05](./backend-phase-05-keycloak-auth.md)  
**Цель:** TenantId на всех строках, PostgreSQL Row Level Security, header `X-Tenant-Id`, tenant resolution middleware.

---

## Результат фазы

- [x] Entity `Tenant` — Id, Slug, Name, Status, CreatedAt (B-11.1; migration **V009**)
- [x] `TenantId` column on `todos`, `users` + backfill to `default`
- [x] RLS policies + FORCE on `todos` / `users` (B-11.2)
- [x] `ITenantContext` + `TenantDbConnectionInterceptor` + Dapper SET (B-11.3)
- [x] Middleware `TenantResolutionMiddleware` — header / JWT claim (B-11.4)
- [x] `CreateTodo` assigns `TenantId` from `ITenantContext` (B-11.5)
- [x] OpenAPI `X-Tenant-Id` + Keycloak `tenant_id` mapper (B-11.6)
- [x] EF global query filter `.HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)`
- [x] Npgsql `SET app.current_tenant` per connection for RLS
- [x] Seed tenants: `default`, `acme-corp`
- [x] Integration tests — tenant A cannot read tenant B todos
- [x] Redis keys include tenant; invalidation uses `TenantId` from domain events
- [x] ADR-026 + `docs/multi-tenancy/isolation.md`

---

## Неделя 1 — Data model & migration

### B-11.1 Tenant entity

1. `Domain/Entities/Tenant.cs` — Id, Slug (unique), Name, CreatedAt
2. Table `tenants` + seed rows
3. Add `TenantId` FK to `todos`, backfill to `default` tenant

**Migration:** `Infrastructure/Migrations/V007__tenants_rls.sql`

### B-11.2 Row Level Security

1. `ALTER TABLE todos ENABLE ROW LEVEL SECURITY;`
2. Policy: `USING (tenant_id = current_setting('app.current_tenant')::uuid)`
3. Force RLS for table owner in dev to catch bugs
4. Same for `users` if tenant-scoped

### B-11.3 Connection interceptor

1. `TenantDbConnectionInterceptor` — on open, execute SET
2. Register with EF Core `AddInterceptors`
3. Dapper reads also SET tenant on connection open

---

## Неделя 2 — API & middleware

### B-11.4 TenantResolutionMiddleware ✅

1. Read `X-Tenant-Id` header (UUID or slug → resolve)
2. Fallback: JWT claim `tenant_id` if present
3. 400 if missing; 404 if tenant not found/inactive
4. Register **after** `UseAuthentication` (JWT claims available), **before** `UseCurrentUserSync` / `UseAuthorization` so RLS SET applies to user lookup

**Файл:** `Api/Middleware/TenantResolutionMiddleware.cs`

### B-11.5 ITenantContext ✅

1. Scoped service set by middleware
2. Handlers use `_tenantContext.TenantId` — remove trust of client-supplied tenant in body
3. Update CreateTodoCommand — assign TenantId server-side

### B-11.6 OpenAPI & Keycloak ✅

1. Document header `X-Tenant-Id` in OpenAPI (`TenantIdHeader` + Swagger operation filter)
2. Optional: custom mapper in Keycloak for tenant claim (`todo-spa` → `tenant_id`)
3. Update integration-map for frontend Phase 14

---

## Неделя 3 — Tests & hardening

### B-11.7 Cross-tenant tests ✅

1. Create todo in tenant A, GET with tenant B header → empty/404
2. Direct SQL bypass test — RLS blocks without SET (`todo_app` NOSUPERUSER)
3. Admin role bypass policy — `app.bypass_rls` for platform-wide stats only

### B-11.8 Cache key update ✅

1. Redis keys include tenant: `todos:tenant:{tid}:user:{uid}`
2. Update invalidation handlers from B-06

### B-11.9 ADR & docs ✅

1. ADR-026: RLS vs schema-per-tenant (why shared schema + RLS)
2. `docs/multi-tenancy/isolation.md` — debug guide
3. Interview story: defense in depth (app filter + RLS)

---

## Команды

```bash
dotnet run --project src/TodoPlatform.Api -- --migrate

# verify RLS
docker exec -it todo-platform-backend-postgres-1 psql -U todo -d tododb \
  -c "SELECT tablename, rowsecurity FROM pg_tables WHERE tablename = 'todos';"

# tenant A
curl http://localhost:8080/api/todos \
  -H "Authorization: Bearer <token>" \
  -H "X-Tenant-Id: <tenant-a-uuid>"

# tenant B — should not see A's data
curl http://localhost:8080/api/todos \
  -H "Authorization: Bearer <token>" \
  -H "X-Tenant-Id: <tenant-b-uuid>"

dotnet test --filter "FullyQualifiedName~Tenant"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | RLS enabled | pg_tables rowsecurity = true |
| 2 | Header required | missing X-Tenant-Id → 400 |
| 3 | Isolation proven | cross-tenant test fails |
| 4 | EF filter + RLS | both layers active |
| 5 | Cache tenant-scoped | redis KEYS pattern |
| 6 | OpenAPI header doc | swagger shows parameter |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-11 | Frontend Phase 14 tenant headers interceptor |
| Phase 14 | `X-Tenant-Id` on every HttpClient request |
| B-12 | Admin manages tenants — migration tracks |

См. [integration-map.md](./integration-map.md) — «B-11 → Phase 14 tenant headers».

---

## Следующая фаза

→ [B-12 Tenant Schema Versioning & Admin API](./backend-phase-12-tenant-schema-versioning.md)
