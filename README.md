# Todo Platform Backend — ASP.NET Core

Enterprise-grade todo platform backend built with Clean Architecture, CQRS (MediatR), PostgreSQL, Keycloak, and comprehensive test coverage.

## Tech Stack

- .NET / ASP.NET Core Web API
- Clean Architecture (Api → Application → Domain → Infrastructure)
- Entity Framework Core + PostgreSQL
- Keycloak (IAM)
- MediatR, Specification pattern, Outbox
- Serilog, Swagger, Health checks
- xUnit integration & unit tests

## Quick Start

### With Docker (PostgreSQL + Keycloak)

```bash
git clone https://github.com/bim121/todo-platform-backend.git
cd todo-platform-backend
docker compose up -d
dotnet build TodoPlatform.slnx
dotnet test TodoPlatform.slnx
dotnet run --project src/TodoPlatform.Api
```

| URL | Description |
|-----|-------------|
| http://localhost:5000/swagger | Swagger UI |
| http://localhost:5000/health | Health check |
| http://localhost:8080 | Keycloak admin (admin/admin) |

### Without Docker

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
└── TodoPlatform.Infrastructure/   # EF Core, repositories, Keycloak, outbox
tests/
├── TodoPlatform.Api.Tests/        # Integration tests (WebApplicationFactory)
├── TodoPlatform.Application.Tests/
└── TodoPlatform.Infrastructure.Tests/
```

## Highlights

- **Clean Architecture** with clear layer boundaries and dependency inversion
- **Specification pattern** for composable EF Core queries
- **Domain events + outbox** for reliable async processing
- **API versioning** and OpenAPI contract tests
- **Auth integration** with Keycloak
- **ADR documentation** in `docs/adr/`

## Documentation

- [Architecture Decision Records](./docs/adr/)
- [Development phases](./plans/README.md)

## Author

**Vitaliy Tyshyk** — Full-Stack Engineer | Angular, .NET, NestJS  
[LinkedIn](https://www.linkedin.com/in/vitaliy-t-2928313b9/) · [GitHub](https://github.com/bim121)
