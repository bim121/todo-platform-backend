# Backend Phase B-01 — Clean API (CRUD + PostgreSQL)

> **Теория:** [guides/b-01-clean-api-theory.md](./guides/b-01-clean-api-theory.md) — статус: placeholder  
> **Контракт:** [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml)  
> **Frontend cutover:** Phase 13 (пока json-server)

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-00](./backend-phase-00-foundation.md) — API запускается  
**Цель:** REST endpoints todos/users как в OpenAPI, EF Core + PostgreSQL, миграции.

---

## Результат фазы

- [ ] `User`, `Todo` entities в Domain
- [ ] `AppDbContext` + configurations в Infrastructure
- [ ] FluentMigrator или EF migrations — версионированные SQL
- [ ] `GET/POST /api/todos`, `PATCH/DELETE /api/todos/{id}`
- [ ] `POST /api/auth/login`, `POST /api/auth/register` (mock JWT или plain — до B-05)
- [ ] Seed: user `test@example.com` / `password123` (как json-server)
- [ ] Docker Postgres в `docker-compose.yml`
- [ ] Swagger отражает новые endpoints
- [ ] Integration tests для todos CRUD

---

## Неделя 1 — PostgreSQL + Domain model

### B-01.1 Docker Postgres

**docker-compose.yml:**
```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: todo
      POSTGRES_PASSWORD: todo
      POSTGRES_DB: tododb
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
volumes:
  pgdata:
```

```bash
docker compose up -d postgres
```

### B-01.2 Connection string

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=tododb;Username=todo;Password=todo"
  }
}
```

### B-01.3 NuGet packages

```bash
dotnet add src/TodoPlatform.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/TodoPlatform.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/TodoPlatform.Infrastructure package FluentMigrator.Runner
dotnet add src/TodoPlatform.Infrastructure package FluentMigrator.Runner.Postgres
```

### B-01.4 Domain entities

**Файлы:**
- `Domain/Entities/User.cs` — Id, Email, PasswordHash, Name
- `Domain/Entities/Todo.cs` — Id, Title, Completed, UserId, Status, Priority

```csharp
public class Todo : Entity
{
    public string Title { get; private set; } = string.Empty;
    public bool Completed { get; private set; }
    public Guid UserId { get; private set; }
    // factory methods Create(), Complete(), UpdateTitle()
}
```

### B-01.5 DbContext

**Файл:** `Infrastructure/Persistence/AppDbContext.cs`

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Todo> Todos => Set<Todo>();
}
```

**Configurations:** `TodoConfiguration`, `UserConfiguration` — indexes на `UserId`, unique `Email`.

---

## Неделя 2 — REST Controllers (Layered, до MediatR)

> CQRS/MediatR — **B-03**. Здесь — thin controllers + service/repository для скорости.

### B-01.6 TodosController

| Method | Route | Action |
|--------|-------|--------|
| GET | `/api/todos?userId=` | List by user |
| GET | `/api/todos/{id}` | Get one |
| POST | `/api/todos` | Create |
| PATCH | `/api/todos/{id}` | Partial update |
| DELETE | `/api/todos/{id}` | Delete |

Сверять с [`contracts/openapi.yaml`](../../contracts/openapi.yaml).

### B-01.7 AuthController (temporary)

| Method | Route | Body |
|--------|-------|------|
| POST | `/api/auth/login` | email, password |
| POST | `/api/auth/register` | email, password, name |

До Phase B-05: простой JWT или mock token (как json-server). **Не production.**

### B-01.8 DTOs + mapping

**Папка:** `Application/Dtos/` — `TodoDto`, `CreateTodoRequest`, `UserDto`  
Mapster или manual static `TodoDto.FromEntity(todo)`.

### B-01.9 Repository (temporary)

**Interface:** `Application/Interfaces/ITodoRepository`  
**Impl:** `Infrastructure/Repositories/TodoRepository.cs` — EF Core

Подготовка к B-03: методы совпадают с будущими Commands/Queries.

---

## Неделя 3 — Migrations, seed, tests

### B-01.10 FluentMigrator

**Migration V001:** `users`, `todos` tables  
**Migration V002:** indexes

```bash
# после настройки runner в Program.cs / hosted service
dotnet run --project src/TodoPlatform.Api  # applies migrations on start (dev)
```

### B-01.11 Seed data

**Файл:** `Infrastructure/Persistence/DbSeeder.cs`

- User: `test@example.com`, password hash for `password123`
- 2–3 sample todos

### B-01.12 Integration tests

```csharp
// WebApplicationFactory + Testcontainers.PostgreSql (optional)
// или in-memory SQLite для CI
[Fact] public async Task CreateTodo_Returns201() { ... }
```

### B-01.13 Manual smoke

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'

curl http://localhost:5000/api/todos?userId=<guid>
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Postgres в docker | `docker compose ps` |
| 2 | Migrations applied | таблицы в psql `\dt` |
| 3 | CRUD todos работает | curl / Swagger |
| 4 | Login/register | same as json-server test user |
| 5 | OpenAPI sync | paths match `contracts/openapi.yaml` |
| 6 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-01 done | Можно тестировать Postman против :5000 |
| Phase 4 | `HttpTodoRepository` skeleton указывает на :5000 |
| Phase 13 | `useRealApi: true` |

---

## Следующая фаза

→ [B-02 OpenAPI & RFC 7807](./backend-phase-02-openapi-contracts.md)
