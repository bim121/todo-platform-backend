# Todo Platform Backend — ASP.NET Core

[![Docker Smoke](https://github.com/bim121/todo-platform-backend/actions/workflows/docker-smoke.yml/badge.svg)](https://github.com/bim121/todo-platform-backend/actions/workflows/docker-smoke.yml)

Enterprise-grade todo platform backend built with Clean Architecture, CQRS (MediatR), PostgreSQL, Keycloak, and comprehensive test coverage.

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- Clean Architecture (Api → Application → Domain → Infrastructure)
- Entity Framework Core + PostgreSQL
- Redis, RabbitMQ (MassTransit), Keycloak (IAM)
- MediatR, Specification pattern, Outbox
- Serilog, Swagger, Health checks
- xUnit integration & unit tests
- Docker Compose full stack

## Quick Start (Docker — recommended)

```bash
git clone https://github.com/bim121/todo-platform-backend.git
cd todo-platform-backend
cp .env.example .env
./scripts/dev-up.sh          # or: make up
./scripts/smoke.sh           # health + Keycloak token + GET /api/todos
```

| URL | Description |
|-----|-------------|
| http://localhost:8080/swagger | API Swagger |
| http://localhost:8080/health/ready | Ready probe |
| http://localhost:8180/admin | Keycloak (`admin` / `admin`) |
| http://localhost:15672 | RabbitMQ management (`todo` / `todo`) |

Profiles: `make up-full` (Mailhog + Redis Commander), `make up-dev` (dotnet watch).  
Reset data: `make reset` (`docker compose down -v && up`).

Full ports / Keycloak issuer notes: [docs/docker/compose-dev.md](./docs/docker/compose-dev.md)

### API on host, infra in Docker

```bash
docker compose up -d postgres redis rabbitmq keycloak
dotnet run --project src/TodoPlatform.Api
# Swagger: http://localhost:5000 (launchSettings)
```

## Without Docker

```bash
dotnet build TodoPlatform.slnx
dotnet test TodoPlatform.slnx
dotnet run --project src/TodoPlatform.Api
```

## Project Structure

```
src/
├── TodoPlatform.Api/              # Endpoints, middleware, auth
├── TodoPlatform.Application/      # Commands, queries, validators (MediatR)
├── TodoPlatform.Domain/           # Entities, domain events
└── TodoPlatform.Infrastructure/   # EF Core, Redis, MassTransit, outbox
tests/
├── TodoPlatform.Api.Tests/
├── TodoPlatform.Application.Tests/
└── TodoPlatform.Infrastructure.Tests/
```

## Highlights

- **Clean Architecture** with clear layer boundaries and dependency inversion
- **Specification pattern** for composable EF Core queries
- **Domain events + transactional outbox** → RabbitMQ consumers
- **API versioning** and OpenAPI contract tests
- **Auth** with Keycloak (JWT + realm roles)
- **ADR documentation** in `docs/adr/`

## Documentation

- [Docker Compose (B-08)](./docs/docker/compose-dev.md)
- [GetTodos EXPLAIN / indexes (B-09)](./docs/db/explain-get-todos.md)
- [Connection pooling (B-09.6)](./docs/db/connection-pooling.md)
- [Keycloak dev](./docs/auth/keycloak-dev.md)
- [RabbitMQ / messaging](./docs/messaging/rabbitmq-dev.md)
- [Architecture Decision Records](./docs/adr/)
- [Development phases](./plans/README.md)

## Author

**Vitaliy Tyshyk** — Full-Stack Engineer | Angular, .NET, NestJS  
[LinkedIn](https://www.linkedin.com/in/vitaliy-t-2928313b9/) · [GitHub](https://github.com/bim121)
