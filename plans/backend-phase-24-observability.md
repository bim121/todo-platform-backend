# Backend Phase B-24 — Observability (LGTM + Promtail + OTel)

> **Теория:** [guides/b-24-observability-theory.md](./guides/b-24-observability-theory.md) — статус: placeholder  
> **Стек:** **Grafana + Loki + Prometheus + Promtail** (+ OTel Collector, Tempo/Jaeger, Alertmanager)

**Длительность:** 2–3 недели (25–35 ч)  
**Предусловия:** [B-23](./backend-phase-23-nginx-gateway.md), [B-22](./backend-phase-22-performance-load.md)  
**Цель:** Полный local observability stack: metrics (Prometheus), logs (Loki+Promtail), dashboards/alerts (Grafana), traces (OTel→Tempo), SLO.

> **Почему здесь (не раньше):** нужны nginx, load tests, multi-service compose.  
> **Почему не позже:** Azure/AWS/K8s (B-25+) строятся поверх уже работающего LGTM.

---

## Результат фазы

- [ ] OpenTelemetry SDK — ASP.NET Core, HttpClient, EF Core, Npgsql
- [ ] OTLP → `otel-collector` → Tempo (traces) + Prometheus (metrics)
- [ ] **Prometheus** scrape `/metrics` + Alertmanager rules
- [ ] **Grafana** datasources: Prometheus, Loki, Tempo
- [ ] **Loki** log store
- [ ] **Promtail** — scrape Docker container logs → Loki (labels: `service`, `container`)
- [ ] Dashboards: API latency, error rate, DB pool, Redis, Kafka lag, saga count
- [ ] Structured Serilog JSON + `trace_id` / `span_id` correlation
- [ ] `traceparent` через YARP/nginx
- [ ] SLO/SLI doc linked to B-22; runbook stubs
- [ ] ADR-037: LGTM + Promtail vs Datadog/New Relic

---

## Архитектура стека

```
API / workers
  ├── /metrics ──────────────────► Prometheus ◄── Alertmanager
  ├── OTLP traces ──► OTel Collector ──► Tempo
  └── stdout JSON logs
         ▲
         │ scrape
      Promtail ──► Loki
         │
         ▼
      Grafana (Explore: metrics + logs + traces)
```

---

## Неделя 1 — OpenTelemetry + Prometheus

### B-24.1 OTel bootstrap

1. Packages: `OpenTelemetry.Extensions.Hosting`, AspNetCore/HttpClient/EF instrumentations
2. Resource: `service.name=todos-api`, `deployment.environment=dev`
3. Export OTLP `http://otel-collector:4317`

**File:** `src/TodoPlatform.Api/Extensions/TelemetryExtensions.cs`

### B-24.2 Custom spans

1. MediatR behavior — span per command/query
2. MassTransit/Kafka publish spans
3. Tags: TenantId (non-PII)

### B-24.3 Collector

1. `infra/otel/otel-collector-config.yaml`
2. Pipelines: traces → Tempo; metrics → Prometheus remote-write or scrape

### B-24.4 Prometheus

1. Histograms: request duration
2. Custom: `cache_hits_total`, `rate_limit_exceeded_total`, `outbox_pending_count`
3. `infra/prometheus/prometheus.yml` + scrape jobs

---

## Неделя 2 — Loki + Promtail + Grafana

### B-24.5 Loki

1. `infra/loki/loki-config.yaml` — single-binary / filesystem for local
2. Retention policy documented (dev: 7d)

### B-24.6 Promtail (обязательно)

1. `infra/promtail/promtail-config.yaml`
2. Docker service discovery — scrape all compose containers
3. Labels: `job`, `service` (from compose label `logging.service`), `level` from JSON
4. Pipeline stages: docker → json → labels → output

**Compose services (profile `observability`):**

```yaml
# docker-compose.observability.yml (excerpt)
services:
  prometheus:
    image: prom/prometheus:v2.54.0
  alertmanager:
    image: prom/alertmanager:v0.27.0
  loki:
    image: grafana/loki:3.1.0
  promtail:
    image: grafana/promtail:3.1.0
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./infra/promtail/promtail-config.yaml:/etc/promtail/config.yml
  grafana:
    image: grafana/grafana:11.2.0
  otel-collector:
    image: otel/opentelemetry-collector-contrib:0.111.0
  tempo:
    image: grafana/tempo:2.6.0
```

### B-24.7 Grafana

1. Provision datasources: Prometheus, Loki, Tempo (trace→log correlation)
2. Dashboards JSON in `infra/grafana/dashboards/`
3. Panels: todo CRUD rate, search latency, 5xx, p95

### B-24.8 App → Loki path (дополнительно к Promtail)

1. Serilog JSON to stdout (Promtail picks up) **или** `WriteTo.GrafanaLoki`
2. Prefer stdout + Promtail (K8s-idiomatic; same pattern on AKS/EKS later)

---

## Неделя 3 — Alerting & SLO

### B-24.9 Alertmanager

1. `HighErrorRate` — 5xx > 1% for 5m
2. `HighLatency` — p95 > SLO for 10m
3. `KafkaConsumerLag` — lag > 1000
4. `infra/prometheus/alerts.yml` + `promtool check rules`

### B-24.10 SLO

1. Error budget burn panel
2. `docs/performance/slo.md`
3. `docs/runbooks/` stubs

### B-24.11 Tests

1. Trace export test (in-memory exporter)
2. `/metrics` contains `todo_platform_*`
3. Promtail → Loki: after `curl` traffic, LogQL `{service="todos-api"}` returns lines
4. ADR-037

---

## Команды

```bash
docker compose --profile observability up -d \
  otel-collector prometheus alertmanager grafana loki promtail tempo

curl http://localhost:5000/metrics | head
# Grafana http://localhost:3000  admin/admin
# Prometheus http://localhost:9090
# Loki ready: curl http://localhost:3100/ready

k6 run perf/k6/load-todos.js
# Grafana Explore → Loki: {service="todos-api"} | json
# Jump to Tempo trace from log line
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Traces in Tempo/Grafana | UI |
| 2 | Prometheus targets UP | /targets |
| 3 | **Promtail ships logs** | Loki LogQL |
| 4 | Grafana has 3 datasources | Provisioning |
| 5 | Correlation log↔trace | click from log to trace |
| 6 | Alerts valid | `promtool check rules` |
| 7 | ADR-037 | committed |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-24 | FE Phase 12 — optional link to Grafana; Phase 21 — browser RUM |
| Shared SLO | E2E latency includes API spans |
| AWS later | Same LGTM pattern → AMP/AMG or self-hosted on EKS (B-35) |

---

## Следующая фаза

→ [B-25 Terraform Azure](./backend-phase-25-terraform-azure.md)  
Позже AWS observability reuse: [B-35](./backend-phase-35-aws-devops-pro.md)
