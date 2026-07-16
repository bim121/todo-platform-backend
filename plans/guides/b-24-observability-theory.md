# B-24 — Observability Theory (LGTM + Promtail)

> **Практика:** [../backend-phase-24-observability.md](../backend-phase-24-observability.md)

## 1. Зачем

Три столпа: metrics, logs, traces. Без correlation on-call невозможен.

## 2. Стек

| Component | Role |
|-----------|------|
| Prometheus | metrics TSDB + alerts |
| Grafana | UI |
| Loki | log TSDB |
| Promtail | log shipper (Docker/K8s) |
| Tempo / Jaeger | traces |
| OTel Collector | vendor-neutral pipeline |

## 3. Почему Promtail

K8s/Docker-идиома: app пишет stdout JSON → agent собирает. Не хардкодить Loki sink в каждый сервис (хотя Serilog sink допустим для spike).

## 4. Interview

- RED vs USE metrics
- Cardinality pitfalls
- Log↔trace correlation via `trace_id`
