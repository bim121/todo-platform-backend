# Backend ↔ Frontend Integration Sync

Backend разрабатывается **независимо**. Frontend подключается через feature flags когда backend-фаза готова.

**Frontend matrix:** [`../anular-ngrx-todo-auth/plans/integration-map.md`](../anular-ngrx-todo-auth/plans/integration-map.md)  
**Shared contract:** [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml)

---

## Календарь (независимые треки)

| Месяц | Frontend | Backend | Cutover |
|-------|----------|---------|---------|
| 1 | Phase 0–1 | — | OpenAPI draft |
| 2–3 | Phase 2–3 | B-00 → B-02 | — |
| 4–5 | Phase 4–5 | B-03 → B-08 | HttpTodoRepository skeleton |
| 6–7 | Phase 6–7 | B-09 → B-10 | SQL (backend only) |
| 8–9 | Phase 8–9 | B-11 → B-13 | Optional SignalR |
| 10–11 | Phase 10–11 | B-14 → B-16 | Pact tests |
| 12 | Phase 12 + 17 | B-05 + B-12 | Keycloak + Admin API |
| 13–14 | Phase 13 | B-17 | `useRealApi: true` |
| 15 | Phase 14 + Admin v1 | B-12, B-28 | Admin panel |
| 16 | Phase 15 + Admin v2 | B-28 | Blue/green UI |
| 17 | Phase 18 AI | B-29 | Semantic search |
| 18+ | Phase 16 CDN | B-20 → B-31 | Full stack deploy |

---

## Backend готовность → Frontend action

| Backend ready | Frontend enables |
|---------------|------------------|
| B-02 OpenAPI | Phase 1 contract sync, Phase 11 Pact |
| B-03 + B-05 | Phase 13 `useRealApi` |
| B-05 + B-08 | Phase 17 Keycloak |
| B-11 | Phase 14 tenant headers |
| B-12 + B-28 | Phase 14–15 Admin panel |
| B-13 | Phase 4–5 `useRealTime` |
| B-15 | Phase 13 `useBackendSearch` |
| B-14 | Attachments feature flag |
| B-29 | Phase 18 AI endpoints |

---

## Admin API (B-12, B-28)

```
GET    /admin/tenants
GET    /admin/tenants/{id}
GET    /admin/tenants/{id}/migrations
POST   /admin/tenants/{id}/switch-track
POST   /admin/tenants/{id}/migrate
POST   /admin/tenants/bulk-migrate
GET    /admin/deployment/status
```

Frontend spec: [`../anular-ngrx-todo-auth/plans/admin-panel-spec.md`](../anular-ngrx-todo-auth/plans/admin-panel-spec.md)
