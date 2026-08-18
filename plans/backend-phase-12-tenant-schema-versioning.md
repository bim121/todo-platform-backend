# Backend Phase B-12 — Tenant Schema Versioning & Admin API

> **Теория:** [guides/b-12-tenant-schema-versioning-theory.md](./guides/b-12-tenant-schema-versioning-theory.md) — статус: placeholder  
> **Frontend spec:** [`../anular-ngrx-todo-auth/plans/admin-panel-spec.md`](../anular-ngrx-todo-auth/plans/admin-panel-spec.md)

**Длительность:** 4 недели (45–60 ч)  
**Предусловия:** [B-11](./backend-phase-11-multi-tenant-isolation.md), [B-03](./backend-phase-03-cqrs-mediatr.md)  
**Цель:** Admin API для tenants, per-tenant migration tracks (stable/beta), planner/apply — и **отдельная PostgreSQL-схема на tenant**, чтобы DDL (новые таблицы/колонки) накатывался не на всю БД, а только на выбранный tenant.

Недели 1–3 — каталог, admin API, логический трек. Неделя 4 — физический schema-per-tenant (то, чего shared schema + «просто фичи» не умеют).

---

## Результат фазы

- [x] Tables `tenant_schema_versions`, `migration_history` (B-12.1; migration **V011**)
- [x] `GET /admin/tenants`, `GET /admin/tenants/{id}` — OpenAPI
- [ ] `GET /admin/tenants/{id}/migration-plan`
- [ ] `POST /admin/tenants/{id}/migrations/apply` — `ApplyTenantMigrationCommand` (**DDL в схеме tenant’а**, не только bump версии)
- [x] `GetTenantsQuery`, `GetTenantByIdQuery` handlers (B-12.3)
- [x] Track per tenant: `stable` | `beta` — determines pending migrations (`IMigrationPlanService`)
- [x] FluentMigrator tagged migrations `@Tags("beta")` demo (`V012`)
- [x] Admin-only `[Authorize(Roles = "admin")]`
- [ ] Audit log row on each apply
- [ ] **ADR-027** — hybrid: `public` = каталог платформы, `tenant_*` = данные tenant’а
- [ ] Postgres schema per tenant + `search_path` на запросе
- [ ] Apply V012-beta к одному tenant → таблица есть только в его схеме

---

## Неделя 1 — Schema & domain

### B-12.1 Migration tracking tables ✅

1. `tenant_schema_versions(tenant_id, track, current_version, updated_at)`
2. `migration_history(id, tenant_id, version, applied_at, applied_by)`
3. Seed: all tenants on `stable` at latest stable version (**V011**)

**File:** `Infrastructure/Migrations/V011_TenantSchemaVersions.cs` (plan named V008 — already used)

### B-12.2 Migration tagging strategy ✅

1. Document version numbering: `V011`, `V012-beta-feature` — [docs/migrations/versioning.md](../docs/migrations/versioning.md)
2. FluentMigrator tags: `[Tags("beta")]` on `V012_BetaFeaturePreview`
3. `IMigrationPlanService` — compute pending for tenant track

**Файл:** `Infrastructure/Migrations/MigrationPlanService.cs`

### B-12.3 Domain commands (stubs from B-03 filled) ✅

1. Implement `GetTenantsQuery` — Dapper list with stats join
2. `GetTenantByIdQuery` — detail + schema version
3. DTOs match admin-panel-spec field names (`id`, `name`, `schemaVersion`, `deploymentTrack`, `appVersion`, `status`)

---

## Неделя 2 — Admin API endpoints

### B-12.4 AdminController

1. Route prefix `/admin/tenants`
2. Pagination, filter by track/status
3. RFC 7807 errors — tenant not found, migration conflict

**OpenAPI sync:** [`contracts/openapi.yaml`](../../contracts/openapi.yaml)

### B-12.5 ApplyTenantMigrationCommand

Неделя 2 — контракт API + lock + history. Реальный DDL — после B-12.12 (неделя 4). Нельзя считать apply готовым, пока он только bump’ает `CurrentVersion`.

1. Input: TenantId, TargetVersion (optional — next pending on that tenant’s track)
2. Handler: `SELECT … FOR UPDATE` на `tenant_schema_versions`, затем `ITenantMigrationRunner.ApplyAsync(tenantId, target)`
3. Runner выполняет **следующий pending шаг в схеме этого tenant’а** (FluentMigrator `MigrateUp` с `search_path` / `VersionInfo` внутри `tenant_*`)
4. Успех: bump `CurrentVersion`, insert `migration_history`, domain event
5. Платформенный `MigrateUp` при старте API **не** накатывает tenant-tagged / tenant-stream шаги на `public`

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
4. ADR-027: schema-per-tenant vs shared schema (см. неделю 4)

---

## Неделя 4 — Per-tenant PostgreSQL schemas (реальный DDL)

Сейчас (неделя 1) `tenant_schema_versions` — **логический** номер на shared schema: `CREATE TABLE` в V012 появился бы у всех. Цель недели 4: **две схемы с разным набором объектов**.

Доказательство готовности:

```text
acme    track=beta    apply V012  →  tenant_acme.beta_preview_flags существует
default track=stable  (без apply) →  tenant_default.beta_preview_flags нет
```

Это не feature-flag в коде. Это разный каталог Postgres.

### Модель (ADR-027)

Гибрид, не DB-per-tenant:

| Схема | Что лежит | Кто мигрирует |
|-------|-----------|----------------|
| `public` | `tenants`, `tenant_schema_versions`, `migration_history`, платформенный `VersionInfo` | Старт API, `MigrateUp` (как сейчас, V001–V011) |
| `tenant_{slug}` | Tenant-owned: `todos`, `users`, beta-таблицы, будущие ALTER | `ITenantMigrationRunner` на create tenant и на admin apply |

Именование: `tenant_` + slug, sanitise `[a-z0-9_]`, столбец `tenants.SchemaName` (уникальный). UUID в имени — fallback, если slug пустой.

**ADR-026 не отменяется целиком:** RLS + `TenantId` остаются как второй слой (ошибка `search_path` не должна показать чужие ряды). Меняется только §4: schema-per-tenant **принимаем** для tenant-owned DDL. Каталог и admin-статы остаются в `public`.

Один бинарь API по-прежнему. Код, который читает объекты только с beta-схемы, ветвится по `CurrentVersion` / track (иначе запрос к `beta_preview_flags` упадёт на stable tenant). Destructивные шаги (`DROP COLUMN`) допустимы **только** в tenant-stream и только для tenants, которые apply догнали; пока есть отстающие — либо не дропать, либо держать два пути в коде.

### B-12.10 ADR-027

Файл: `docs/adr/027-schema-per-tenant-ddl.md`

1. Context: logical tracks недостаточно для разного DDL
2. Decision: hybrid public + `tenant_*`; reject DB-per-tenant (ops) и «только фичи» (нет разных таблиц)
3. Consequences: N× `VersionInfo`, provision на create, cutover с `public.todos`

### B-12.11 `search_path` на запросе

1. После резолва tenant: `SET search_path TO tenant_acme, public` (тот же interceptor, что ставит `app.current_tenant`)
2. На возврат в пул: `RESET search_path` + сброс GUC (иначе соседний запрос другого tenant увидит чужую схему)
3. EF/Dapper: неквалифицированные `todos` / `users` резолвятся в tenant schema; каталог — `public.tenants`
4. Admin stats: `bypass_rls` **и** обход search_path (явный `public.` / union по схемам) — задокументировать в isolation.md

### B-12.12 `ITenantMigrationRunner`

1. Для схемы tenant’а свой FluentMigrator processor: `VersionInfo` **внутри** `tenant_*` (не общий `public.VersionInfo`)
2. Каталог шагов тот же (`[Migration(N)]`), но tenant-stream не выполняется глобальным `MigrateUp` (тег `tenant` и/или отдельный `Maintenance` runner)
3. `V012` переписать: `Create.Table(...).InSchema(currentTenantSchema)` **или** полагаться на `search_path` раннера — таблица не должна появиться в `public`
4. Apply одного шага за вызов (не скачок через breaking versions без проверки B-12.7)
5. Dry-run: `PreviewOnly` / SQL script в ответе плана, без commit

### B-12.13 Provision + cutover существующих данных

1. Create tenant (или seeder): `CREATE SCHEMA`, прогон tenant-stream до latest **stable** (не beta)
2. Платформенная миграция (V013): для каждого существующего tenant создать схему, скопировать его ряды из `public.todos` / `public.users`, проставить `SchemaName`
3. После cutover приложение **не** пишет tenant-данные в `public.todos` (оставить таблицы или view на время rollback — в ADR)
4. Новый tenant на beta: provision stable, затем admin apply V012 — не тащить beta в create по умолчанию

### B-12.14 Тесты недели 4

1. Testcontainers: apply beta только acme → `\dt tenant_acme.*` содержит `beta_preview_flags`, `tenant_default` — нет
2. Два параллельных HTTP-запроса разных tenants не протекают `search_path` через пул
3. Create tenant → схема существует, `CurrentVersion` = latest stable
4. Cutover: число todos в схеме = числу рядов с этим `TenantId` до миграции

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
| 4 | Apply works | history row **и** DDL в схеме tenant’а |
| 5 | OpenAPI synced | admin-panel-spec match |
| 6 | Audit trail | migration_history |
| 7 | Tests green | `dotnet test` |
| 8 | Schema isolation | V012-beta видна только у applied tenant (`\dt tenant_*`) |
| 9 | Pool safety | нет утечки `search_path` между tenants |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-12 + B-28 | Frontend Phase 14–15 Admin panel |
| Phase 14 | AdminFacade → GetTenantsQuery |
| B-12 GraphQL | Extend schema: `adminTenants`, `switchTenantTrack` mutation (from B-10 base) |
| Phase 13-GraphQL | Admin UI может `useGraphQL` для tenant list |
| B-18 | BulkApplyMigrationCommand via Saga |

См. [integration-map.md](./integration-map.md) — Admin API section.

---

## Следующая фаза

→ [B-13 SignalR Realtime](./backend-phase-13-realtime-signalr.md)
