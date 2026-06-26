# Backend Phase B-24 — Observability (OpenTelemetry & Prometheus)

> **Теория:** [guides/b-24-observability-theory.md](./guides/b-24-observability-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-23](./backend-phase-23-nginx-gateway.md), [B-22](./backend-phase-22-performance-load.md)  
**Цель:** OpenTelemetry traces/metrics/logs, Prometheus scrape, Grafana dashboards, Loki log aggregation, SLO alerting.

---

## Результат фазы

- [ ] OpenTelemetry SDK — ASP.NET Core, HttpClient, EF Core, Npgsql instrumentation
- [ ] OTLP exporter to `otel-collector` container
- [ ] Prometheus metrics endpoint `/metrics` (prometheus-net or OTel exporter)
- [ ] Grafana + Prometheus + Loki in compose profile `observability`
- [ ] Dashboards: API latency, error rate, DB pool, Redis, Kafka lag, saga count
- [ ] Structured logging → Loki via Serilog sink or collector
- [ ] Trace correlation — `traceparent` header propagation through YARP/nginx
- [ ] Alert rules: p95 latency, 5xx rate, replication lag (from B-20)
- [ ] SLO/SLI doc linked to B-22 targets

---

## Неделя 1 — OpenTelemetry

### B-24.1 OTel bootstrap

1. Packages: `OpenTelemetry.Extensions.Hosting`, instrumentations
2. Resource attributes: `service.name=todos-api`, `deployment.environment=dev`
3. Export to `http://otel-collector:4317`

**File:** `src/TodoPlatform.Api/Extensions/TelemetryExtensions.cs`

### B-24.2 Custom spans

1. MediatR behavior — span per command/query
2. MassTransit/Kafka publish spans
3. Tag TenantId, UserId (non-PII)

### B-24.3 Collector config

1. `infra/otel/otel-collector-config.yaml`
2. Pipelines: traces → Tempo or Jaeger; metrics → Prometheus
3. Docker compose services

---

## Неделя 2 — Metrics & logs

### B-24.4 Prometheus metrics

1. `http_server_request_duration_seconds` histogram
2. Custom: `cache_hits_total`, `rate_limit_exceeded_total`, `outbox_pending_count`
3. Service discovery or static scrape configs

**File:** `infra/prometheus/prometheus.yml`

### B-24.5 Grafana dashboards

1. Import dotnet aspnet dashboard baseline
2. Custom panels: todo CRUD rate, search latency
3. Dashboard JSON in `infra/grafana/dashboards/`

### B-24.6 Loki logging

1. Serilog `WriteTo.GrafanaLoki` or OTel logs exporter
2. JSON format with trace_id correlation
3. Explore logs by trace id in Grafana

---

## Неделя 3 — Alerting & SLO

### B-24.7 Alertmanager rules

1. `HighErrorRate` — 5xx > 1% for 5m
2. `HighLatency` — p95 > SLO for 10m
3. `KafkaConsumerLag` — lag > 1000

**File:** `infra/prometheus/alerts.yml`

### B-24.8 SLO dashboards

1. Error budget burn rate panel
2. Link to `docs/performance/slo.md`
3. Runbook stubs in `docs/runbooks/`

### B-24.9 Tests

1. Integration test generates trace — verify exported (test exporter)
2. `/metrics` returns `todo_platform_*` metrics
3. ADR-037: observability stack choices

---

## Команды

```bash
docker compose --profile observability up -d otel-collector prometheus grafana loki

dotnet add src/TodoPlatform.Api package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/TodoPlatform.Api package OpenTelemetry.Instrumentation.AspNetCore

curl http://localhost:8080/metrics | head
start http://localhost:3000  # Grafana admin/admin

k6 run perf/k6/load-todos.js  # generate traffic, view dashboards

dotnet test --filter "FullyQualifiedName~Telemetry"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Traces visible | Grafana/Jaeger UI |
| 2 | Metrics scraped | Prometheus targets UP |
| 3 | Logs in Loki | query {service="todos-api"} |
| 4 | Correlation works | trace_id in logs |
| 5 | Dashboards load | JSON imported |
| 6 | Alerts defined | promtool check rules |
| 7 | ADR-037 | stack documented |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-24 | Frontend OTel browser SDK optional later |
| Shared SLO | End-to-end latency traces include API |
| Incidents | Runbooks for on-call practice |

Parallel skills: SLO/SLI error budgets — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-25 Terraform Azure](./backend-phase-25-terraform-azure.md)
