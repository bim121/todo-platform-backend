# Tech Stack — Todo Platform Backend

| Слой | Технология | Фаза внедрения |
|------|------------|----------------|
| Runtime | .NET 10 | B-00 ✅ |
| API | ASP.NET Core Web API | B-00 |
| CQRS | MediatR | B-03 |
| Validation | FluentValidation | B-03 |
| ORM (write) | EF Core 9 | B-01 |
| SQL (read) | Dapper | B-10 |
| Database | PostgreSQL 16 | B-01 |
| Migrations | FluentMigrator | B-01 |
| Auth | Keycloak + JWT (JWKS) | B-05 |
| Cache | Redis / Azure Cache for Redis | B-06 |
| Messaging | RabbitMQ + MassTransit | B-07 |
| Streaming | Apache Kafka | B-16 |
| Realtime | SignalR + Redis backplane | B-13 |
| Search | PostgreSQL tsvector, Typesense | B-15 |
| Vector | pgvector / Qdrant | B-29 |
| Files | Azure Blob Storage | B-14 |
| Gateway | YARP + nginx | B-17, B-23 |
| Containers | Docker, docker-compose | B-08 |
| IaC | Terraform (Azure primary) | B-25 |
| Orchestration | Kubernetes (AKS), Helm | B-26 |
| Config mgmt | Ansible | B-27 |
| Observability | OpenTelemetry, Prometheus, Grafana, Loki | B-24 |
| Load test | k6, BenchmarkDotNet | B-22 |
| Contract | OpenAPI 3.1 in `../../contracts/` | B-02 |

## Azure-first (Microsoft interviews)

- AKS, Azure Database for PostgreSQL Flexible Server
- Azure Cache for Redis, Azure Blob, Key Vault
- Entra ID integration path (alternative to Keycloak in prod)

## AWS/GCP equivalents (Google/Amazon interviews)

Document in ADRs when comparing: RDS/Cloud SQL, EKS/GKE, ElastiCache/Memorystore, S3/GCS.
