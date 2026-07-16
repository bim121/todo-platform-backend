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
| Observability | OpenTelemetry, **Prometheus, Grafana, Loki, Promtail**, Tempo | **B-24** |
| Load test | k6, BenchmarkDotNet | B-22 |
| Contract | OpenAPI 3.1 in `../../contracts/` | B-02 |
| **GraphQL** | Hot Chocolate | **B-10** |
| **gRPC** | Grpc.AspNetCore, protobuf | **B-17** |
| **Concurrency** | IAsyncEnumerable, Channels, Parallel.ForEachAsync | **B-33** |
| **AWS** | VPC, ECS/EKS, RDS, S3, IAM, CloudWatch | **B-34, B-35** |
| **RAG / Agents / MCP** | Hybrid retrieve, LLM tools, MCP server | **B-36, B-37** |
| **Bedrock / MLA** | Amazon Bedrock, Guardrails, batch embed | **B-38** |

## Azure-first (Microsoft interviews)

- AKS, Azure Database for PostgreSQL Flexible Server
- Azure Cache for Redis, Azure Blob, Key Vault
- Entra ID integration path (alternative to Keycloak in prod)

## AWS (Amazon interviews + certs)

- Practice track: [aws-cert-track.md](./aws-cert-track.md) — SAA, DVA, DOP, MLA
- EKS/ECS, RDS, ElastiCache, S3, Bedrock — B-34…B-38

## GCP equivalents (Google interviews)

Document in ADRs when comparing: Cloud SQL, GKE, Memorystore, GCS, Vertex AI.
