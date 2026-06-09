# Pact provider stub (B-02.6)

Backend URL for **frontend Phase 11** consumer-driven contract tests ([`phase-11-testing-quality.md`](../../anular-ngrx-todo-auth/plans/phase-11-testing-quality.md)).

## Provider base URL

| Environment | URL |
|-------------|-----|
| Local dev | `http://localhost:5000` |
| CI (same host) | `http://localhost:5000` |

API routes are under `/api` (e.g. `GET http://localhost:5000/api/todos?userId={uuid}`).

## Start provider locally

```bash
cd todo-platform-backend
docker compose up -d postgres   # optional; in-memory works for Pact smoke
dotnet run --project src/TodoPlatform.Api
```

Swagger: `http://localhost:5000/swagger`

## Contract source of truth

[`../../contracts/openapi.yaml`](../../contracts/openapi.yaml)

Exported backend OpenAPI snapshot: [`../artifacts/swagger-v1.json`](../artifacts/swagger-v1.json)

## Phase 11 consumer scope (initial)

| Consumer interaction | Provider endpoint | Notes |
|---------------------|-------------------|-------|
| List todos | `GET /api/todos?userId={uuid}` | Header `Accept-Version: v1` |
| Create todo | `POST /api/todos` | JSON body `CreateTodoRequest` |
| Login (mock) | `POST /api/auth/login` | Deprecated; replaced by Keycloak in B-05 |

Bearer auth is **not** enforced until B-05; Pact tests can omit `Authorization` for todo CRUD.

## Headers

| Header | Value | When |
|--------|-------|------|
| `Accept-Version` | `v1` | Recommended on all API calls |
| `Content-Type` | `application/json` | POST/PATCH bodies |

Deprecated endpoints (e.g. mock login) return `Deprecation: true` and `Sunset` (RFC 8594).

## CI note

Frontend CI may use a mock provider until backend is up. When verifying against this repo, start `TodoPlatform.Api` on port **5000** before `nx run todos-data-access:pact`.
