# Backend Phase B-00 — Foundation

> **Теория:** [guides/b-00-architecture-and-cqrs-theory.md](./guides/b-00-architecture-and-cqrs-theory.md) — статус: **full**  
> **Frontend:** пока json-server, контракт — [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml)

**Длительность:** 1–2 недели (10–20 ч)  
**Предусловия:** .NET SDK 10+, IDE (Rider/VS/VS Code)  
**Цель:** Запускаемый ASP.NET Core API, Clean Architecture scaffold, Serilog, health checks, Swagger, тесты.

---

## Результат фазы

- [x] Solution `TodoPlatform.slnx` + 4 слоя + tests
- [x] `dotnet build` / `dotnet test` — green
- [x] `dotnet run --project src/TodoPlatform.Api` → Swagger на http://localhost:5000
- [x] `/health`, `/health/ready`, `GET /api/health`
- [x] Serilog в консоль
- [x] CORS для Angular `http://localhost:4200`
- [x] Extension methods `AddApplication()` / `AddInfrastructure()`
- [ ] ADR-020 в `docs/adr/` — выбор Clean Architecture + CQRS path
- [ ] `.editorconfig` + `Directory.Build.props` (nullable, warnings as errors — опционально)

---

## Неделя 1 — Solution & Clean Architecture

### B-00.1 Структура репозитория

```
todo-platform-backend/
├── TodoPlatform.slnx
├── src/
│   ├── TodoPlatform.Api/           # HTTP, Program.cs, Controllers
│   ├── TodoPlatform.Application/   # Use cases (B-03: MediatR)
│   ├── TodoPlatform.Domain/        # Entities, no dependencies
│   └── TodoPlatform.Infrastructure/ # EF, Redis, external (later)
├── tests/
│   └── TodoPlatform.Api.Tests/
├── plans/
└── docker-compose.yml              # Postgres — Phase B-08
```

**Шаги:**
1. `dotnet new sln -n TodoPlatform`
2. Создать 4 classlib/webapi проекта (net10.0)
3. Настроить references: Api → Application, Infrastructure; Application → Domain; Infrastructure → Application, Domain
4. Удалить template `WeatherForecast`

### B-00.2 Dependency Injection по слоям

**Файлы:**
- `src/TodoPlatform.Application/DependencyInjection.cs` → `AddApplication()`
- `src/TodoPlatform.Infrastructure/DependencyInjection.cs` → `AddInfrastructure()`

**Program.cs:**
```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
```

### B-00.3 Domain — базовый класс

**Файл:** `src/TodoPlatform.Domain/Common/Entity.cs`

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
```

---

## Неделя 2 — Observability & DX

### B-00.4 Serilog

```bash
dotnet add src/TodoPlatform.Api package Serilog.AspNetCore
```

**appsettings.json:**
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" }
  }
}
```

**Program.cs:** `builder.Host.UseSerilog(...)` + `app.UseSerilogRequestLogging()`

### B-00.5 Health checks

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
```

**Controller:** `GET /api/health` — JSON `{ status, service, timestamp }`

### B-00.6 Swagger (OpenAPI UI)

```bash
dotnet add src/TodoPlatform.Api package Swashbuckle.AspNetCore
```

Dev only: `/swagger` — документация API (расширим в B-02).

### B-00.7 CORS для Angular

```csharp
builder.Services.AddCors(o => o.AddPolicy("Frontend", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
app.UseCors("Frontend");
```

### B-00.8 Integration tests

```bash
dotnet add tests/TodoPlatform.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
```

Тесты: `/health` → 200, `/api/health` → body contains `Healthy`.

---

## Команды (ежедневно)

```bash
cd todo-platform-backend
dotnet build TodoPlatform.slnx
dotnet test TodoPlatform.slnx
dotnet run --project src/TodoPlatform.Api
# Swagger: http://localhost:5000/swagger
# Health:  http://localhost:5000/health
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | API стартует без ошибок | `dotnet run` |
| 2 | Swagger открывается | browser `/swagger` |
| 3 | Health 200 | `curl http://localhost:5000/health` |
| 4 | CORS headers для 4200 | browser devtools / OPTIONS |
| 5 | 2+ unit/integration tests | `dotnet test` |
| 6 | 4 проекта, правильные references | `dotnet list reference` |

---

## Связь с frontend

| Backend | Frontend |
|---------|----------|
| B-00 API scaffold | Phase 1 — OpenAPI draft в `contracts/` |
| CORS :4200 | Angular `ng serve` |
| Cutover | Phase 13 — `useRealApi: true` |

---

## Interview story (1 абзац)

«Стартовал backend как Modular Monolith на Clean Architecture: Api/Application/Domain/Infrastructure, Serilog, health probes для K8s, Swagger для contract-first с Angular. CQRS через MediatR — следующая фаза B-03.»

---

## Следующая фаза

→ [B-01 Clean API — CRUD + PostgreSQL](./backend-phase-01-clean-api.md)
