# Backend Phase B-08 — Docker Full Stack

> **Теория:** [guides/b-08-docker-compose-theory.md](./guides/b-08-docker-compose-theory.md) — статус: placeholder

**Длительность:** 1–2 недели (15–25 ч)  
**Предусловия:** [B-07](./backend-phase-07-rabbitmq-basics.md) — все сервисы работают локально по отдельности  
**Цель:** Единый `docker compose up` для API + Postgres + Redis + RabbitMQ + Keycloak, multi-stage Dockerfile, dev/prod profiles.

---

## Результат фазы

- [ ] Multi-stage `Dockerfile` для `TodoPlatform.Api` (build → runtime aspnet:9)
- [ ] `docker-compose.yml` — все сервисы из B-01–B-07
- [ ] `docker-compose.override.yml` — dev mounts, hot reload optional
- [ ] Healthchecks на postgres, redis, rabbitmq, api
- [ ] `depends_on` + condition `service_healthy`
- [ ] `.env.example` — все connection strings
- [ ] `Makefile` или `scripts/dev-up.sh` — one command startup
- [ ] CI job: `docker compose build && docker compose up -d && smoke curl`
- [ ] Document ports matrix in README

---

## Неделя 1 — Dockerfile & compose

### B-08.1 Multi-stage Dockerfile

1. Stage `build`: SDK 9, restore, publish Release
2. Stage `runtime`: aspnet:9-alpine, non-root user
3. EXPOSE 8080, `ENTRYPOINT ["dotnet", "TodoPlatform.Api.dll"]`
4. `.dockerignore` — exclude bin/obj, .git

**Файл:** `src/TodoPlatform.Api/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/TodoPlatform.Api/TodoPlatform.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoPlatform.Api.dll"]
```

### B-08.2 Unified compose

1. Services: `api`, `postgres`, `redis`, `rabbitmq`, `keycloak`
2. Network `todo-net` — internal DNS
3. API env: connection strings point to service names (`postgres`, `redis`, etc.)
4. Volumes for persistent data

### B-08.3 Healthchecks

1. Postgres: `pg_isready -U todo`
2. Redis: `redis-cli ping`
3. RabbitMQ: `rabbitmq-diagnostics ping`
4. API: `curl -f http://localhost:8080/health/ready`

---

## Неделя 2 — Profiles & CI

### B-08.4 Dev vs prod profiles

1. Profile `dev` — mount source, `dotnet watch` optional sidecar
2. Profile `full` — includes mailhog, redis-commander
3. `docker compose --profile full up -d`

### B-08.5 Environment files

1. `.env.example` with all vars documented
2. Keycloak hostname fix for docker network vs browser (`KC_HOSTNAME`, proxy headers)
3. CORS in API for `http://localhost:4200`

### B-08.6 CI smoke pipeline

1. GitHub Actions / Azure DevOps step: build images
2. Wait for healthy, run curl login + GET todos
3. `docker compose down -v` in teardown
4. Badge in README

### B-08.7 Seed on startup

1. API applies FluentMigrator + DbSeeder on first run
2. Keycloak realm import on container start
3. Document reset: `docker compose down -v && docker compose up -d`

---

## Команды

```bash
# build & run full stack
docker compose build api
docker compose up -d

# check health
docker compose ps
curl http://localhost:8080/health/ready

# logs
docker compose logs -f api

# reset everything
docker compose down -v
docker compose --profile full up -d

# smoke from host
curl http://localhost:8080/swagger/index.html
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Single command up | `docker compose up -d` all green |
| 2 | API reachable | Swagger on :8080 |
| 3 | Migrations auto | tables exist after fresh up |
| 4 | Keycloak + API auth | token + todos works |
| 5 | Healthchecks pass | `docker compose ps` healthy |
| 6 | CI smoke green | pipeline artifact |
| 7 | .env.example complete | no hardcoded secrets |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-05 + B-08 | Frontend Phase 17 — Keycloak + API URLs from compose |
| Phase 13 | `environment.apiUrl = http://localhost:8080` |
| Dev onboarding | One README section «start backend» |

См. [integration-map.md](./integration-map.md).

---

## Следующая фаза

→ [B-09 PostgreSQL Query Optimization I](./backend-phase-09-postgres-queries.md)
