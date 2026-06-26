# OpenAPI diff — B-02.3 (2026-06-02)

**Exported:** `artifacts/swagger-v1.json` (Swashbuckle from running API)  
**Contract:** `../../contracts/openapi.yaml`

Paths in swagger are absolute (`/api/...`). Contract paths are relative to server `http://localhost:5000/api`.

## Implemented in backend (swagger) — synced in contract

| Contract path | Swagger path | Notes |
|---------------|--------------|-------|
| `POST /auth/login` | `/api/auth/login` | Added `400` ProblemDetails to contract |
| `POST /auth/register` | `/api/auth/register` | Backend returns `400` for duplicate email until B-05 (`409` kept as target) |
| `GET/POST /todos` | `/api/todos` | Added `400`; `userId` → `format: uuid` |
| `GET/PATCH/DELETE /todos/{id}` | `/api/todos/{id}` | Added `400`/`404` where swagger declares them |
| `GET /health` | `/api/Health` | New in contract (case-insensitive routing) |

## In contract only (not in swagger — expected)

| Path | Reason |
|------|--------|
| `GET/POST /users` | json-server mock (frontend dev) |
| `GET /tenants/{id}/config` | Phase 14+ |
| `/admin/*` | Phase 14–15 |
| `GET /search` | B-15 |

## Schema differences (intentional)

| Schema | Contract | Backend B-02 |
|--------|----------|--------------|
| `TodoDto` | `task`, `createdAt` for mock | `title`, `status`, `priority` only |
| `CreateTodoRequest` | optional mock fields | `title` + `userId` required |
| `AuthResponse` | `token` + `accessToken` | `token` only (mock uses `accessToken`) |
| `ProblemDetails.errors` | documented for validation | present at runtime via RFC 7807 extensions |

## Security

Contract keeps `bearerAuth` on `/todos` for mock/frontend. ASP.NET does not enforce JWT until **B-05** (swagger has no security block).

## Regenerate export

```powershell
dotnet build src/TodoPlatform.Api
.\scripts\export-openapi.ps1
```
