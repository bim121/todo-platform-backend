# Backend FAANG Roadmap (ASP.NET Core)

**Темп:** 24–30 месяцев, 5–10 ч/нед. **Независимо** от Angular-фронта.  
**Frontend:** [`../anular-ngrx-todo-auth`](../anular-ngrx-todo-auth) — подключается по готовности.  
**Java twin track:** [`../todo-platform-java`](../todo-platform-java) — те же 34 фазы (J-00…J-33).  
**Теория:** [`guides/README.md`](./guides/README.md)  
**Интеграция:** [`integration-sync.md`](./integration-sync.md)

---

## Навигация по фазам

| Фаза | Файл | Фокус |
|------|------|-------|
| B-00 | [backend-phase-00-foundation.md](./backend-phase-00-foundation.md) | Clean Architecture, scaffold |
| B-01 | [backend-phase-01-clean-api.md](./backend-phase-01-clean-api.md) | REST CRUD, EF Core, PostgreSQL |
| B-02 | [backend-phase-02-openapi-contracts.md](./backend-phase-02-openapi-contracts.md) | OpenAPI, RFC 7807, versioning |
| B-03 | [backend-phase-03-cqrs-mediatr.md](./backend-phase-03-cqrs-mediatr.md) | **MediatR CQRS** |
| B-04 | [backend-phase-04-domain-events.md](./backend-phase-04-domain-events.md) | Domain events, specifications |
| B-05 | [backend-phase-05-keycloak-auth.md](./backend-phase-05-keycloak-auth.md) | Keycloak JWT, RBAC |
| B-06 | [backend-phase-06-redis-caching.md](./backend-phase-06-redis-caching.md) | Redis cache |
| B-07 | [backend-phase-07-rabbitmq-basics.md](./backend-phase-07-rabbitmq-basics.md) | RabbitMQ + MassTransit |
| B-08 | [backend-phase-08-docker-compose.md](./backend-phase-08-docker-compose.md) | Docker full stack |
| B-09 | [backend-phase-09-postgres-queries.md](./backend-phase-09-postgres-queries.md) | **SQL optimization I** |
| B-10 | [backend-phase-10-complex-sql-readmodels.md](./backend-phase-10-complex-sql-readmodels.md) | **SQL II + GraphQL BFF** |
| B-11 | [backend-phase-11-multi-tenant-isolation.md](./backend-phase-11-multi-tenant-isolation.md) | Multi-tenant RLS |
| B-12 | [backend-phase-12-tenant-schema-versioning.md](./backend-phase-12-tenant-schema-versioning.md) | Migrations + **Admin API** |
| B-13 | [backend-phase-13-realtime-signalr.md](./backend-phase-13-realtime-signalr.md) | SignalR realtime |
| B-14 | [backend-phase-14-files-storage.md](./backend-phase-14-files-storage.md) | Azure Blob attachments |
| B-15 | [backend-phase-15-search-fulltext.md](./backend-phase-15-search-fulltext.md) | Full-text search |
| B-16 | [backend-phase-16-kafka-streaming.md](./backend-phase-16-kafka-streaming.md) | Kafka + audit log |
| B-17 | [backend-phase-17-microservices-split.md](./backend-phase-17-microservices-split.md) | Microservices + YARP + **gRPC** |
| B-18 | [backend-phase-18-saga-patterns.md](./backend-phase-18-saga-patterns.md) | **Saga patterns** |
| B-19 | [backend-phase-19-redis-advanced.md](./backend-phase-19-redis-advanced.md) | Rate limit, locks |
| B-20 | [backend-phase-20-db-replication-scaling.md](./backend-phase-20-db-replication-scaling.md) | **DB scaling I** |
| B-21 | [backend-phase-21-sharding-partitioning.md](./backend-phase-21-sharding-partitioning.md) | **DB scaling II** |
| B-22 | [backend-phase-22-performance-load.md](./backend-phase-22-performance-load.md) | **DB performance III** |
| B-23 | [backend-phase-23-nginx-gateway.md](./backend-phase-23-nginx-gateway.md) | nginx + TLS |
| B-24 | [backend-phase-24-observability.md](./backend-phase-24-observability.md) | Prometheus, Grafana, OTel |
| B-25 | [backend-phase-25-terraform-azure.md](./backend-phase-25-terraform-azure.md) | **Terraform Azure** |
| B-26 | [backend-phase-26-kubernetes-aks.md](./backend-phase-26-kubernetes-aks.md) | **Kubernetes AKS** |
| B-27 | [backend-phase-27-ansible-automation.md](./backend-phase-27-ansible-automation.md) | Ansible |
| B-28 | [backend-phase-28-blue-green-canary.md](./backend-phase-28-blue-green-canary.md) | Blue-green per tenant |
| B-29 | [backend-phase-29-ai-vector-backend.md](./backend-phase-29-ai-vector-backend.md) | AI / vector search |
| B-30 | [backend-phase-30-security-hardening.md](./backend-phase-30-security-hardening.md) | OWASP API Top 10 |
| B-31 | [backend-phase-31-system-design-capstone.md](./backend-phase-31-system-design-capstone.md) | System design capstone |
| ~~B-32~~ | [backend-phase-32-graphql-grpc.md](./backend-phase-32-graphql-grpc.md) | **→ перенесена** в B-10 + B-17 |
| **B-33** | [backend-phase-33-concurrency-parallelism.md](./backend-phase-33-concurrency-parallelism.md) | **Concurrency & Channels** |

> **GraphQL** — [B-10](./backend-phase-10-complex-sql-readmodels.md) (после read models). **gRPC** — [B-17](./backend-phase-17-microservices-split.md) (в момент split). **Concurrency** — [B-33](./backend-phase-33-concurrency-parallelism.md). B-32 — redirect для старых ссылок.

---

## Database Performance & Scaling (сквозной блок)

| Тема | Фазы |
|------|------|
| Indexes, EXPLAIN, N+1 | B-09 |
| Dapper, CTEs, materialized views | B-10 |
| Replication, PgBouncer | B-20 |
| Partitioning, sharding | B-21 |
| pgBench, k6, locks | B-22 |
| Async, Channels, Parallel.ForEachAsync | **B-33** |

---

## Архитектурный путь

```
Modular Monolith (B-00…B-16)
  └── Clean Architecture
        └── CQRS via MediatR (B-03)
                    └── DDD tactical (B-04)
                          └── GraphQL BFF on monolith (B-10)
                                └── Microservices + gRPC (B-17)
                                      └── Event-Driven + Saga (B-16, B-18)
                                            └── Concurrency & Channels (B-33)
```

---

## Как пользоваться

1. `phase-*.md` — **практика** (код, команды, чеклисты).
2. `guides/*-theory.md` — **теория** (глубоко, interview, tradeoffs).
3. Не переходи к следующей фазе без критериев готовности.
4. OpenAPI — всегда синхронизируй с [`../../contracts/openapi.yaml`](../../contracts/openapi.yaml).

---

## Параллельный трек

- [parallel-skills-backend.md](./parallel-skills-backend.md) — system design, algorithms for backend
- [tech-stack.md](./tech-stack.md) — версии пакетов и сервисов
