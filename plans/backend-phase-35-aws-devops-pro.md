# Backend Phase B-35 — AWS DevOps Professional (DOP)

> **Теория:** [guides/b-35-aws-devops-pro-theory.md](./guides/b-35-aws-devops-pro-theory.md) — placeholder  
> **Cert:** [aws-cert-track.md](./aws-cert-track.md) — **DOP-C02**  
> **Предусловия:** [B-34](./backend-phase-34-aws-foundations.md), [B-28](./backend-phase-28-blue-green-canary.md), [B-24](./backend-phase-24-observability.md)

**Длительность:** 4–5 недель (40–55 ч)  
**Цель:** CI/CD, multi-env, automated rollback, observability on AWS, IaC at scale — практика под DevOps Engineer Professional.

---

## Результат фазы

- [ ] Multi-account or multi-env (dev/staging/prod) with Terraform/CDK workspaces or accounts
- [ ] Pipeline: GitHub Actions → build/test → ECR → deploy ECS/EKS (blue/green or canary)
- [ ] Automated rollback on failed health / error budget burn
- [ ] CloudTrail + Config rules (baseline compliance)
- [ ] Observability: Amazon Managed Prometheus/Grafana **или** LGTM on EKS (reuse B-24)
- [ ] Secrets rotation (Secrets Manager)
- [ ] Chaos/day-2: document runbook for deploy failure
- [ ] ADR-043: CodePipeline vs GitHub Actions on AWS

---

## Неделя 1 — Pipelines

### B-35.1 CI

1. Build matrix .NET; cache NuGet
2. Unit + integration (Testcontainers) in CI
3. Trivy/ECR image scan gate

### B-35.2 CD

1. Deploy to staging automatically
2. Prod requires approval + tagged release
3. Blue/green: swap ALB target group (parity B-28)

---

## Неделя 2 — Governance & ops

### B-35.3 Multi-env

1. Separate state backends; least-privilege deploy roles
2. Config rules: public S3 blocked, encryption required

### B-35.4 Observability on AWS

1. Ship container logs → CloudWatch → (optional) Promtail/Fluent Bit → Loki
2. Alarms → SNS → email/Slack
3. Dashboard: deploy success rate, MTTR stub metrics

---

## Неделя 3–4 — DOP exam domains practice

| DOP domain | Lab in this project |
|------------|---------------------|
| SDLC automation | Pipeline + gates |
| Config mgmt & IaC | Terraform modules + drift check |
| Resilient cloud solutions | Multi-AZ, rollback |
| Monitoring & logging | Alarms + LGTM/AMP |
| Incident & event response | Runbooks + alert routes |

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Staging auto-deploy | push to main |
| 2 | Prod gated | manual approve |
| 3 | Rollback tested | failed health → previous task def |
| 4 | Compliance baseline | Config rules compliant |
| 5 | Observability live | alarm fires on synthetic 5xx |

---

## Следующая фаза

→ [B-36 RAG & LLM Applications](./backend-phase-36-rag-llm.md)
