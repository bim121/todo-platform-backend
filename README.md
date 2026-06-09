# Todo Platform Backend (ASP.NET Core)

Отдельный backend-проект для todo-платформы. Разрабатывается **независимо** от Angular-фронта в [`../anular-ngrx-todo-auth`](../anular-ngrx-todo-auth).

## Быстрый старт

```bash
cd todo-platform-backend
dotnet build TodoPlatform.slnx
dotnet test TodoPlatform.slnx
dotnet run --project src/TodoPlatform.Api
```

| URL | Описание |
|-----|----------|
| http://localhost:5000/swagger | Swagger UI |
| http://localhost:5000/health | Health check |
| http://localhost:5000/api/health | JSON health info |
| [`docs/pact-provider.md`](./docs/pact-provider.md) | Pact provider URL for frontend Phase 11 |

**Текущий этап:** B-00 выполнен (scaffold). Следующий: [B-01 CRUD + PostgreSQL](./plans/backend-phase-01-clean-api.md).

## Workspace

```
d:\programing\ngrx\
├── anular-ngrx-todo-auth\     # Angular + NgRx
├── todo-platform-backend\      # ← этот репозиторий
└── contracts\openapi.yaml
```

## Стек

- **.NET 10**, ASP.NET Core Web API
- Clean Architecture: Api / Application / Domain / Infrastructure
- Serilog, Swagger, Health checks
- (далее) MediatR, EF Core, PostgreSQL, Keycloak, Redis, Kafka…

## Roadmap

| Документ | Описание |
|----------|----------|
| [plans/README.md](./plans/README.md) | Все фазы B-00 … B-31 |
| [plans/backend-phase-00-foundation.md](./plans/backend-phase-00-foundation.md) | Текущая/завершённая фаза |
| [plans/guides/](./plans/guides/) | Теория (B-00 full) |
| [plans/integration-sync.md](./plans/integration-sync.md) | Когда подключать фронт |

## Структура кода

```
src/
├── TodoPlatform.Api/              # Program.cs, Controllers
├── TodoPlatform.Application/      # Use cases (MediatR в B-03)
├── TodoPlatform.Domain/           # Entities
└── TodoPlatform.Infrastructure/   # EF, Redis, external
tests/TodoPlatform.Api.Tests/
```

## Связь с фронтом

Фронт на json-server до Phase 13. Матрица: [`../anular-ngrx-todo-auth/plans/integration-map.md`](../anular-ngrx-todo-auth/plans/integration-map.md)
