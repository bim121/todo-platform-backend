# Backend Phase B-28 — Blue-Green & Canary per Tenant

> **Теория:** [guides/b-28-blue-green-canary-theory.md](./guides/b-28-blue-green-canary-theory.md) — статус: placeholder  
**Frontend spec:** [`../anular-ngrx-todo-auth/plans/admin-panel-spec.md`](../anular-ngrx-todo-auth/plans/admin-panel-spec.md)

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-26](./backend-phase-26-kubernetes-aks.md), [B-12](./backend-phase-12-tenant-schema-versioning.md)  
**Цель:** Per-tenant deployment tracks (blue/green), `SwitchTenantTrackCommand`, canary rollout for beta tenants, deployment status API.

---

## Результат фазы

- [ ] Helm releases `todo-platform-blue` and `todo-platform-green` (or traffic split via Argo Rollouts optional)
- [ ] Table `tenant_deployments` — TenantId, ActiveSlot (blue|green), TargetVersion
- [ ] `SwitchTenantTrackCommand` — move tenant to beta slot/version
- [ ] `GetDeploymentStatusQuery` — per tenant + global summary
- [ ] Gateway/YARP/nginx routes tenant to correct slot via header or config service
- [ ] `POST /admin/tenants/{id}/deployment/switch` OpenAPI
- [ ] Zero-downtime switch — drain connections, SignalR reconnect doc
- [ ] Automated smoke after switch — health + sample CRUD
- [ ] ADR-041: per-tenant canary vs global deploy

---

## Неделя 1 — Dual deployment slots

### B-28.1 Blue/green Helm installs

1. Two releases differing in image tag / feature flags
2. Shared Postgres/Redis — schema compatible both slots
3. Label selectors: `slot=blue|green`

**Files:**
- `charts/todo-platform/values-blue.yaml`
- `charts/todo-platform/values-green.yaml`

### B-28.2 tenant_deployments schema

1. Migration `V013__tenant_deployments.sql`
2. Seed: all tenants on `blue`, stable version
3. Beta tenants on `green` with newer image tag

### B-28.3 Routing layer

1. Gateway reads `ITenantDeploymentResolver`
2. Config from Redis cache — `tenant:{id}:slot`
3. Forward to k8s service `todos-api-blue` or `todos-api-green`

---

## Неделя 2 — Admin commands

### B-28.4 SwitchTenantTrackCommand

1. Input: TenantId, TargetSlot (green), TargetVersion optional
2. Validate migration plan compatible (B-12)
3. Update `tenant_deployments`, invalidate Redis route cache
4. Publish `TenantDeploymentSwitchedEvent` → audit Kafka

### B-28.5 GetDeploymentStatusQuery

1. Returns `{ slot, version, health, lastSwitchAt }`
2. Global: count tenants per slot
3. Admin UI polling endpoint

### B-28.6 Canary policy

1. Optional: auto-promote green → blue if error rate < threshold (feature flag off by default)
2. Document manual promote playbook

---

## Неделя 3 — Safety & tests

### B-28.7 Pre-switch validation

1. Run migration dry-run on tenant track
2. Block switch if pending saga (B-18)
3. Integration test: switch tenant, verify routing header

### B-28.8 SignalR & sticky sessions

1. On switch — broadcast reconnect hint event
2. Frontend doc: reconnect hub after admin switch

### B-28.9 Rollback

1. `SwitchTenantTrackCommand` back to blue
2. Playbook `docs/runbooks/rollback-tenant-slot.md`
3. E2E test full cycle

---

## Команды

```bash
helm upgrade --install todo-blue ./charts/todo-platform -f values-blue.yaml -n todo-platform-dev
helm upgrade --install todo-green ./charts/todo-platform -f values-green.yaml -n todo-platform-dev

curl -X POST http://localhost:8080/admin/tenants/<id>/deployment/switch \
  -H "Authorization: Bearer <admin_token>" \
  -H "Content-Type: application/json" \
  -d '{"targetSlot":"green","targetVersion":"1.2.0-beta"}'

curl http://localhost:8080/admin/deployment/status \
  -H "Authorization: Bearer <admin_token>"

dotnet test --filter "FullyQualifiedName~Deployment"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Two slots running | kubectl get deploy |
| 2 | Tenant routed correctly | version header test |
| 3 | Switch command works | DB + cache updated |
| 4 | Status API | admin UI ready |
| 5 | Rollback tested | switch back blue |
| 6 | Audit event | Kafka row |
| 7 | OpenAPI synced | admin-panel-spec |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-12 + B-28 | Frontend Phase 14–15 Admin panel |
| Phase 15 | Deployment switch UI + status polling |
| AdminFacade | `switchTrack()` → SwitchTenantTrackCommand |

См. [integration-map.md](./integration-map.md).

Parallel skills: Blue-green deploy — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-29 AI Vector Backend (pgvector)](./backend-phase-29-ai-vector-backend.md)
