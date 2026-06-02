# Integration Map (Backend view)

Frontend matrix: [`../anular-ngrx-todo-auth/plans/integration-map.md`](../anular-ngrx-todo-auth/plans/integration-map.md)

See [integration-sync.md](./integration-sync.md) for calendar and readiness gates.

**Contract:** [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml)

---

## When frontend connects

| Backend phase complete | Frontend enables |
|------------------------|------------------|
| B-02 | OpenAPI sync, Pact (Phase 11) |
| B-03 + B-05 | Phase 13 `useRealApi` |
| B-05 + B-08 | Phase 17 Keycloak |
| B-11 | Phase 14 tenant headers |
| B-12 + B-28 | Phase 14–15 Admin panel |
| B-13 | Phase 4–5 realtime |
| B-15 | Backend search |
| B-14 | Attachments |
| B-29 | Phase 18 AI |

---

## Admin API (implement in B-12, B-28)

Documented in OpenAPI under `/admin/*`.  
Frontend spec: [`../anular-ngrx-todo-auth/plans/admin-panel-spec.md`](../anular-ngrx-todo-auth/plans/admin-panel-spec.md)

---

## CQRS commands for admin

| Command | Phase |
|---------|-------|
| `GetTenantsQuery` | B-12 |
| `GetTenantByIdQuery` | B-12 |
| `GetMigrationPlanQuery` | B-12 |
| `SwitchTenantTrackCommand` | B-28 |
| `ApplyTenantMigrationCommand` | B-12 |
| `BulkApplyMigrationCommand` | B-18 Saga |
| `GetDeploymentStatusQuery` | B-28 |
