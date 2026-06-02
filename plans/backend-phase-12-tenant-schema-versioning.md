# Backend Phase B-12 — Tenant Schema Versioning & Admin API

> **Теория:** [guides/b-12-tenant-schema-versioning-theory.md](./guides/b-12-tenant-schema-versioning-theory.md) — статус: placeholder  
> **Frontend spec:** [`../anular-ngrx-todo-auth/plans/admin-panel-spec.md`](../anular-ngrx-todo-auth/plans/admin-panel-spec.md)

**Длительность:** 3 недели (30–40 ч)  
**Предусловия:** [B-11](./backend-phase-11-multi-tenant-isolation.md), [B-03](./backend-phase-03-cqrs-mediatr.md)  
**Цель:** Admin API для tenants, per-tenant migration tracks (stable/beta), migration planner и apply commands.

---

## Результат фазы

- [ ] Tables `tenant_schema_versions`, `migration_plans`, `migration_history`
- [ ] `GET /admin/tenants`, `GET /admin/tenants/{id}` — OpenAPI
- [ ] `GET /admin/tenants/{id}/migration-plan`
- [ ] `POST /admin/tenants/{id}/migrations/apply` — `ApplyTenantMigrationCommand`
- [ ] `GetTenantsQuery`, `GetTenantByIdQuery`, `GetMigrationPlanQuery` handlers
- [ ] Track per tenant: `stable` | `beta` — determines pending migrations
- [ ] FluentMigrator tagged migrations `@Tags("beta")` demo
- [ ] Admin-only `[Authorize(Roles = "admin")]`
- [ ] Audit log row on each apply

---

## Неделя 1 — Schema & domain

### B-12.1 Migration tracking tables

1. `tenant_schema_versions(tenant_id, track, current_version, updated_at)`
2. `migration_history(id, tenant_id, version, applied_at, applied_by)`
3. Seed: all tenants on `stable` at latest stable version

**File:** `V008__tenant_migrations.sql`

### B-12.2 Migration tagging strategy

1. Document version numbering: `V009`, `V010-beta-feature`
2. FluentMigrator tags: `[Tags("beta")]`
3. `IMigrationPlanService` — compute pending for tenant track

**Файл:** `Infrastructure/Migrations/MigrationPlanService.cs`

### B-12.3 Domain commands (stubs from B-03 filled)

1. Implement `GetTenantsQuery` — Dapper list with stats join
2. `GetTenantByIdQuery` — detail + schema version
3. DTOs match admin-panel-spec field names

---

## Неделя 2 — Admin API endpoints

### B-12.4 AdminController

1. Route prefix `/admin/tenants`
2. Pagination, filter by track/status
3. RFC 7807 errors — tenant not found, migration conflict

**OpenAPI sync:** [`contracts/openapi.yaml`](../../contracts/openapi.yaml)

### B-12.5 ApplyTenantMigrationCommand

1. Input: TenantId, TargetVersion (optional — next pending)
2. Handler: run FluentMigrator for tenant context (shared schema — logical per-tenant version table)
3. For shared DB: migrations global but `migration_history` per tenant tracks logical upgrades
4. Transaction + lock per tenant row

### B-12.6 GetMigrationPlanQuery

1. Returns `{ currentVersion, track, pending: [{ version, description, tags }] }`
2. Used by frontend Admin migration UI

---

## Неделя 3 — Safety & integration

### B-12.7 Concurrency & validation

1. Optimistic concurrency on `tenant_schema_versions`
2. Reject apply if pending todos migration incompatible (simulate with beta tag)
3. Dry-run mode query param `?dryRun=true` — returns plan only

### B-12.8 Audit trail

1. Insert `migration_history` + domain event `TenantMigrationAppliedEvent`
2. Outbox → notification (B-07 consumer log)

### B-12.9 Tests

1. Admin GET tenants — 200; user role — 403
2. Apply migration bumps version
3. Beta tenant sees extra pending migration vs stable
4. ADR-027: per-tenant tracks vs global deploy

---

## Команды

```bash
dotnet run --project src/TodoPlatform.Api -- --migrate

# admin token required
curl http://localhost:8080/admin/tenants \
  -H "Authorization: Bearer <admin_token>"

curl http://localhost:8080/admin/tenants/<id>/migration-plan \
  -H "Authorization: Bearer <admin_token>"

curl -X POST http://localhost:8080/admin/tenants/<id>/migrations/apply \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{"targetVersion": 9}'

dotnet test --filter "FullyQualifiedName~Admin"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Admin endpoints | Swagger /admin/* |
| 2 | RBAC | non-admin 403 |
| 3 | Migration plan accurate | beta vs stable diff |
| 4 | Apply works | history row created |
| 5 | OpenAPI synced | admin-panel-spec match |
| 6 | Audit trail | migration_history |
| 7 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-12 + B-28 | Frontend Phase 14–15 Admin panel |
| Phase 14 | AdminFacade → GetTenantsQuery |
| B-18 | BulkApplyMigrationCommand via Saga |

См. [integration-map.md](./integration-map.md) — Admin API section.

---

## Следующая фаза

→ [B-13 SignalR Realtime](./backend-phase-13-realtime-signalr.md)
