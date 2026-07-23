# Docker Compose — local full stack (B-08)

## Start

```bash
cp .env.example .env   # once
./scripts/dev-up.sh    # or: make up
```

| Command | What starts |
|---------|-------------|
| `make up` / `./scripts/dev-up.sh` | api, postgres, redis, rabbitmq, keycloak |
| `make up-full` / `./scripts/dev-up.sh full` | + Mailhog + Redis Commander |
| `make up-dev` / `./scripts/dev-up.sh dev` | infra + **api-watch** (`dotnet watch`), published `api` scaled to 0 |
| `make down` | stop containers (volumes kept) |
| `make reset` | `down -v` then `up` — **wipes DB / Redis / RabbitMQ data** |
| `make smoke` / `./scripts/smoke.sh` | health → Keycloak login → `GET /api/todos` |

Windows smoke: `pwsh ./scripts/smoke.ps1`

---

## Ports matrix

| Service | Host URL | Notes |
|---------|----------|--------|
| API / Swagger | http://localhost:8080 | `/swagger` in Development |
| API health | http://localhost:8080/health/ready | compose + CI probe |
| Keycloak admin | http://localhost:8180/admin | `admin` / `admin` |
| Keycloak realm | http://localhost:8180/realms/todo-platform | issuer in JWT |
| Postgres | localhost:5432 | `todo` / `todo` / `tododb` |
| Redis | localhost:6379 | |
| RabbitMQ AMQP | localhost:5672 | `todo` / `todo` |
| RabbitMQ UI | http://localhost:15672 | |
| Mailhog UI | http://localhost:8025 | profile `full` |
| Redis Commander | http://localhost:8081 | profile `full` |

---

## Keycloak: browser vs Docker network

Tokens from the browser have:

`iss = http://localhost:8180/realms/todo-platform`

The API container cannot call `localhost:8180` (that is itself). So compose sets:

| Setting | Value | Role |
|---------|-------|------|
| `Keycloak__Authority` | `http://localhost:8180/realms/...` | validate JWT `iss` |
| `Keycloak__MetadataAddress` | `http://keycloak:8080/realms/.../.well-known/...` | fetch JWKS / OIDC config via Docker DNS |

CORS origin defaults to `http://localhost:4200` (`CORS_ORIGIN` in `.env`).

---

## Migrations & seed on startup

When `Database__AutoMigrate=true` (default in compose), API runs FluentMigrator + `DbSeeder` on boot (test user / sample todos).

Keycloak imports realm JSON from `infra/keycloak` on every container start (`--import-realm`).

### Reset everything

```bash
make reset
# same as:
docker compose down -v && docker compose up -d --build
```

`-v` deletes named volumes → empty Postgres / Redis / RabbitMQ, then migrate+seed and realm import run again.

---

## Profiles

- **`full`** (alias **`dev-ui`**): Mailhog + Redis Commander  
- **`dev`**: `api-watch` with source mount + `dotnet watch`  

```bash
docker compose --profile full up -d
docker compose --profile dev up -d --scale api=0
```
