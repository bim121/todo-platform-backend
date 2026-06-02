# Backend Phase B-05 — Keycloak JWT & RBAC

> **Теория:** [guides/b-05-keycloak-auth-theory.md](./guides/b-05-keycloak-auth-theory.md) — статус: placeholder  
> **Контракт:** [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml) — securitySchemes

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-04](./backend-phase-04-domain-events.md), [B-03](./backend-phase-03-cqrs-mediatr.md)  
**Цель:** Keycloak как IdP, JWT validation через JWKS, RBAC policies, замена mock auth из B-01.

---

## Результат фазы

- [ ] Keycloak realm `todo-platform` в docker-compose (dev)
- [ ] Clients: `todo-spa` (public), `todo-api` (bearer-only)
- [ ] Roles: `user`, `admin`; realm roles в JWT claim `realm_access.roles`
- [ ] ASP.NET Core JWT Bearer — validate issuer, audience, signature via JWKS
- [ ] `[Authorize(Roles = "admin")]` на `/admin/*` stubs
- [ ] `ICurrentUserService` — UserId, Email, Roles из claims
- [ ] Commands используют `ICurrentUserService` вместо `userId` в body (where applicable)
- [ ] Удалён temporary mock JWT из B-01
- [ ] Integration test с test JWT / Keycloak testcontainer

---

## Неделя 1 — Keycloak setup

### B-05.1 Docker Keycloak

1. Добавить сервис `keycloak` в `docker-compose.yml` (port 8080)
2. Import realm JSON: `infra/keycloak/todo-platform-realm.json`
3. Admin user: `admin` / `admin` (dev only)
4. Postgres для Keycloak или dev-file (dev mode)

**Файлы:**
- `docker-compose.yml` — keycloak service
- `infra/keycloak/todo-platform-realm.json`

### B-05.2 Realm configuration

1. Realm `todo-platform`, enabled email login
2. User `test@example.com` / `password123` — mirror json-server seed
3. Roles `user`, `admin`; test user → `user`; admin user → `admin`
4. Client `todo-spa`: public, redirect `http://localhost:4200/*`, PKCE

### B-05.3 Client `todo-api`

1. Bearer-only client for resource server
2. Audience mapper — `todo-api` in access token
3. Document token endpoint для Postman: `/realms/todo-platform/protocol/openid-connect/token`

---

## Неделя 2 — ASP.NET JWT integration

### B-05.4 Authentication middleware

1. NuGet: `Microsoft.AspNetCore.Authentication.JwtBearer`
2. `appsettings.Development.json`:

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/todo-platform",
  "Audience": "todo-api",
  "RequireHttpsMetadata": false
}
```

3. `AddAuthentication().AddJwtBearer()` — Authority, Audience, MetadataAddress
4. `AddAuthorization()` — policies `RequireAdmin`, `RequireUser`

**Файл:** `src/TodoPlatform.Api/Extensions/AuthExtensions.cs`

### B-05.5 ICurrentUserService

1. Interface: `UserId`, `Email`, `IsAuthenticated`, `IsInRole(role)`
2. `HttpContextCurrentUserService` — parse `sub`, `email`, `realm_access`
3. Register scoped in DI
4. `GetTodosQuery` — filter by `ICurrentUserService.UserId` if no explicit userId

### B-05.6 Secure controllers

1. `[Authorize]` на `TodosController`
2. `[Authorize(Roles = "admin")]` на `AdminController` stubs
3. Return 401/403 ProblemDetails (RFC 7807 from B-02)
4. Swagger — OAuth2 PKCE flow pointing to Keycloak

---

## Неделя 3 — Migration & tests

### B-05.7 Remove mock auth

1. Delete plain password check in `LoginCommand` (if exists)
2. OpenAPI: document that `/api/auth/login` deprecated → frontend uses Keycloak directly
3. Optional BFF endpoint `GET /api/auth/me` — returns user profile from token

### B-05.8 User sync (optional minimal)

1. On first authenticated request — upsert `User` row by `sub` claim
2. `UserRegisteredEvent` handler or middleware hook
3. Link todos to Keycloak `sub` as UserId (Guid parse or mapping table)

### B-05.9 Integration tests

1. Generate test JWT with `IdentityModel` helper or Keycloak token endpoint
2. Test: no token → 401; user token → GET todos OK; user → admin endpoint 403
3. Document curl recipe in `docs/auth/keycloak-dev.md`

---

## Команды

```bash
docker compose up -d keycloak postgres
# дождаться http://localhost:8080/health/ready

dotnet add src/TodoPlatform.Api package Microsoft.AspNetCore.Authentication.JwtBearer

# получить token (password grant — dev only)
curl -X POST "http://localhost:8080/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=test@example.com" \
  -d "password=password123"

# API call
curl http://localhost:5000/api/todos \
  -H "Authorization: Bearer <access_token>"

dotnet test src/TodoPlatform.Api.Tests --filter "FullyQualifiedName~Auth"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Keycloak up | `docker compose ps keycloak` |
| 2 | Token validates | API accepts Bearer token |
| 3 | RBAC enforced | admin route → 403 for user role |
| 4 | Swagger OAuth | Authorize button works |
| 5 | Mock auth removed | no hardcoded JWT secret in code |
| 6 | Tests green | `dotnet test` |
| 7 | OpenAPI securitySchemes | matches Keycloak URLs |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-03 + B-05 | Frontend Phase 13 `useRealApi: true` |
| B-05 + B-08 | Frontend Phase 17 Keycloak Angular adapter |
| Phase 17 | `keycloak-angular`, silent refresh, role guards |

См. [integration-map.md](./integration-map.md) и [integration-sync.md](./integration-sync.md).

---

## Следующая фаза

→ [B-06 Redis Caching](./backend-phase-06-redis-caching.md)
