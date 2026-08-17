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
| B-10 GraphQL | **Phase 13-GraphQL** (`useGraphQL`); Kanban one-query |
| B-17 gRPC | Phase 13-GraphQL (architecture); internal only |

**GraphQL schema:** [`../../contracts/graphql/schema.graphql`](../../contracts/graphql/schema.graphql)

---

## Phase 14 — Tenant headers (B-11)

Backend `TenantResolutionMiddleware` is live. Angular must send `X-Tenant-Id` on every authenticated `HttpClient` request (Phase 14 interceptor). Tenant is **not** accepted in the JSON body.

| | |
|---|---|
| Header | `X-Tenant-Id` |
| Value | Tenant UUID **or** slug (`default`, `acme-corp`) |
| Fallback | JWT claim `tenant_id` (Keycloak user attribute mapper on `todo-spa`) |
| Missing (authenticated) | **400** ProblemDetails |
| Unknown / inactive | **404** ProblemDetails |
| Create todo | `TenantId` assigned server-side from `ITenantContext` |

Dev seed tenants:

| Slug | Id |
|------|----|
| `default` | `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa` |
| `acme-corp` | `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb` |

OpenAPI parameter: `components.parameters.TenantIdHeader`.

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
