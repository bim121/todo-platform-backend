# Backend Phase B-30 — Security Hardening (OWASP)

> **Теория:** [guides/b-30-security-hardening-theory.md](./guides/b-30-security-hardening-theory.md) — статус: placeholder

**Длительность:** 2 недели (20–30 ч)  
**Предусловия:** [B-29](./backend-phase-29-ai-vector-backend.md), [B-05](./backend-phase-05-keycloak-auth.md), [B-23](./backend-phase-23-nginx-gateway.md)  
**Цель:** OWASP Top 10 mitigations, security headers, dependency scanning, secrets audit, penetration checklist.

---

## Результат фазы

- [ ] OWASP checklist doc `docs/security/owasp-checklist.md` — all items addressed or N/A
- [ ] `#nullable enable`, input validation audit — all Commands have FluentValidation
- [ ] SQL injection — parameterized queries only (Dapper audit grep)
- [ ] XSS — API returns JSON only; Content-Type enforced
- [ ] CSRF — not applicable SPA bearer; document SameSite cookies if BFF added
- [ ] Security headers via nginx + ASP.NET (`UseSecurityHeaders`)
- [ ] Rate limiting + account lockout notes (Keycloak brute force)
- [ ] Dependabot/`dotnet list package --vulnerable` — zero high/critical
- [ ] Trivy scan Docker images in CI
- [ ] Pen test script: OWASP ZAP baseline scan against staging
- [ ] ADR-043: threat model summary

---

## Неделя 1 — Application security

### B-30.1 Input validation audit

1. Grep all MediatR commands — validator exists
2. Max length on Title, FileName; reject HTML tags in title
3. Global exception handler — no stack trace in prod ProblemDetails

### B-30.2 AuthZ hardening

1. Policy tests — every endpoint has `[Authorize]` except health/swagger
2. Resource-based auth: user can only access own todos — integration tests
3. Admin endpoints double-check role + audit log

### B-30.3 Secrets & config

1. Grep repo for passwords, API keys — none in git
2. User secrets locally; Key Vault in K8s (B-26)
3. Rotate dev Keycloak admin password doc

---

## Неделя 2 — Infrastructure & scanning

### B-30.4 Security headers middleware

1. `Content-Security-Policy` for API minimal
2. `Referrer-Policy`, `Permissions-Policy`
3. Remove `Server` header leakage

**File:** `Api/Middleware/SecurityHeadersMiddleware.cs`

### B-30.5 Dependency & container scan

1. CI: `dotnet list package --vulnerable`
2. Trivy scan `todos-api` image — fail on CRITICAL
3. Pin base image digests in Dockerfiles

### B-30.6 OWASP ZAP baseline

1. Run against `https://localhost` or staging ingress
2. Save report `docs/security/zap-report.html`
3. Fix findings: cookie flags, missing headers, etc.

### B-30.7 Threat model & ADR

1. STRIDE diagram for todo platform
2. ADR-043 — accepted risks (dev Keycloak password grant)
3. Interview story: defense in depth (RLS + authZ + rate limit)

---

## Команды

```bash
# vulnerable packages
dotnet list src/TodoPlatform.Todos.Api package --vulnerable --include-transitive

# trivy
trivy image todo-platform/todos-api:latest --severity CRITICAL,HIGH

# zap baseline (docker)
docker run -t ghcr.io/zaproxy/zaproxy:stable zap-baseline.py \
  -t https://staging.todo-platform.local -r zap-report.html

# security headers check
curl -I -k https://localhost/api/todos

grep -r "password\s*=" src/ --include="*.json" --include="*.cs" | grep -v test

dotnet test --filter "FullyQualifiedName~Security"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | OWASP checklist complete | doc reviewed |
| 2 | No high vuln packages | dotnet list clean |
| 3 | Trivy CI gate | pipeline green |
| 4 | ZAP report | no high alerts unfixed |
| 5 | Security headers | curl -I |
| 6 | AuthZ tests | cross-user denied |
| 7 | ADR-043 | threat model |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-30 | Frontend security headers CSP for Angular |
| Phase 17 | Keycloak PKCE — no implicit flow |
| Shared | CORS policy tightened to known origins |

---

## Следующая фаза

→ [B-31 System Design Capstone](./backend-phase-31-system-design-capstone.md)
