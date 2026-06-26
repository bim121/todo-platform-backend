# Backend Phase B-23 — nginx Gateway & TLS

> **Теория:** [guides/b-23-nginx-gateway-theory.md](./guides/b-23-nginx-gateway-theory.md) — статус: placeholder

**Длительность:** 1–2 недели (15–25 ч)  
**Предусловия:** [B-17](./backend-phase-17-microservices-split.md), [B-13](./backend-phase-13-realtime-signalr.md), [B-22](./backend-phase-22-performance-load.md)  
**Цель:** nginx as edge reverse proxy, TLS termination, WebSocket upgrade, rate limit at edge, replace/dev alongside YARP.

---

## Результат фазы

- [ ] `infra/nginx/nginx.conf` — upstream gateway or direct services
- [ ] TLS with mkcert/dev certificates — HTTPS :443
- [ ] HTTP → HTTPS redirect
- [ ] WebSocket proxy for `/hubs/todos`
- [ ] `client_max_body_size` for file uploads (10m)
- [ ] Security headers: HSTS, X-Content-Type-Options, X-Frame-Options
- [ ] Optional: nginx rate limit zone per IP ( complement B-19)
- [ ] Docker service `nginx` in compose — public entry :443
- [ ] Document prod: Let's Encrypt + cert-manager (B-26)

---

## Неделя 1 — nginx config

### B-23.1 Basic reverse proxy

1. Upstream `gateway:8080` or split upstreams
2. Locations: `/api/`, `/admin/`, `/hubs/`, `/health`
3. Proxy headers: `X-Forwarded-For`, `X-Forwarded-Proto`, `Host`

**File:** `infra/nginx/conf.d/todo-platform.conf`

### B-23.2 TLS termination

1. Generate dev certs: `mkcert localhost todo.local`
2. Mount `./infra/nginx/certs` into container
3. `listen 443 ssl; ssl_certificate ...`

### B-23.3 WebSocket support

```nginx
location /hubs/ {
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_pass http://gateway;
}
```

---

## Неделя 2 — Hardening & integration

### B-23.4 Security headers & limits

1. `add_header Strict-Transport-Security`
2. `limit_req_zone` — 10r/s per IP burst
3. Gzip for JSON responses

### B-23.5 Docker compose integration

1. nginx depends on gateway healthy
2. Frontend `environment.apiUrl = https://localhost`
3. Keycloak redirect URIs updated for HTTPS

### B-23.6 Tests & docs

1. curl -k https://localhost/api/todos with auth
2. SignalR connect via wss through nginx
3. k6 re-run through nginx — compare latency overhead
4. ADR-036: nginx vs YARP vs cloud LB

---

## Команды

```bash
# mkcert (one-time)
mkcert -install
mkcert -cert-file infra/nginx/certs/local.pem -key-file infra/nginx/certs/local-key.pem localhost

docker compose up -d nginx gateway

curl -k https://localhost/health
curl -k https://localhost/api/todos -H "Authorization: Bearer <token>" -H "X-Tenant-Id: <t>"

# test websocket (integration test preferred)
dotnet test --filter "FullyQualifiedName~Nginx"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | HTTPS works | curl -k 200 |
| 2 | HTTP redirects | 301 to https |
| 3 | WebSocket via nginx | SignalR connected |
| 4 | Security headers | curl -I |
| 5 | Upload size limit | 413 on huge body |
| 6 | k6 overhead | <10ms p95 added |
| 7 | ADR-036 | edge proxy choice |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-23 | Angular environment `apiUrl` https |
| Phase 17 | Keycloak redirect https://localhost:4200 |
| B-26 | cert-manager replaces mkcert in AKS |

---

## Следующая фаза

→ [B-24 Observability (OpenTelemetry & Prometheus)](./backend-phase-24-observability.md)
