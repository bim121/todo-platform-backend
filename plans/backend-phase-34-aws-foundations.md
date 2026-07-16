# Backend Phase B-34 — AWS Foundations (SAA + DVA)

> **Теория:** [guides/b-34-aws-foundations-theory.md](./guides/b-34-aws-foundations-theory.md) — placeholder  
> **Cert track:** [aws-cert-track.md](./aws-cert-track.md) — **SAA-C03** + **DVA-C02**  
> **Azure twin:** [B-25](./backend-phase-25-terraform-azure.md) / [B-26](./backend-phase-26-kubernetes-aks.md)

**Длительность:** 4–5 недель (40–55 ч)  
**Предусловия:** [B-24](./backend-phase-24-observability.md), [B-17](./backend-phase-17-microservices-split.md), [B-31](./backend-phase-31-system-design-capstone.md) recommended  
**Цель:** Развернуть todo-platform на AWS идиоматично — покрыть Solutions Architect Associate + Developer Associate на практике.

---

## Результат фазы

- [ ] Terraform (или CDK) AWS modules: VPC, subnets, NAT, security groups
- [ ] RDS PostgreSQL (Multi-AZ optional in staging) + ElastiCache Redis
- [ ] S3 bucket for attachments (parity B-14) + IAM least privilege
- [ ] Secrets Manager / SSM Parameter Store for connection strings
- [ ] Compute path A: **ECS Fargate** *или* EKS (минимум) *или* App Runner — ADR
- [ ] ALB + HTTPS (ACM) + target groups
- [ ] Cognito User Pool **или** keep Keycloak behind ALB — ADR-041
- [ ] CloudWatch Logs + metrics; export optional to Grafana
- [ ] IAM roles for tasks (no long-lived keys in containers)
- [ ] Developer: SDK usage (AWSSDK), signed URLs for S3, CI build→ECR
- [ ] Cost dashboard / budget alarm
- [ ] ADR-042: Azure vs AWS deploy for portfolio

---

## Неделя 1 — Network & data (SAA)

### B-34.1 VPC design

1. Public/private subnets, 2 AZs
2. ALB public; ECS/RDS private
3. Security groups: ALB→app→RDS/Redis only

### B-34.2 RDS + Redis + S3

1. RDS Postgres 16, parameter group, backups
2. ElastiCache Redis
3. S3 private + bucket policy; app uses IAM role

---

## Неделя 2 — Compute & identity (SAA + DVA)

### B-34.3 Container deploy

1. ECR repository; GitHub Actions push image
2. ECS Fargate service + task definition (or EKS Helm reuse from B-26)
3. Health checks → ALB

### B-34.4 Auth on AWS

1. Option A: Cognito JWT → same ASP.NET JWT validation
2. Option B: Keycloak on ECS (existing realm)
3. Document token claims mapping

### B-34.5 Developer practices

1. AWS SDK for S3 presigned upload (B-14 parity)
2. Localstack optional for unit tests
3. Structured logging → CloudWatch Logs

---

## Неделя 3–4 — App wiring & cert practice

### B-34.6 Wire todo-platform

1. Connection strings from Secrets Manager
2. Feature flags env: `Cloud=AWS`
3. Smoke: CRUD + attachments + Redis cache

### B-34.7 SAA/DVA exam labs

1. Well-Architected review checklist (5 pillars) for this design
2. Practice: IAM policy that allows only `s3:PutObject` on attachments prefix
3. Practice: troubleshooting 502 via ALB target health + CloudWatch

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | HTTPS API on ALB | curl |
| 2 | RDS + Redis from private tasks | health ready |
| 3 | S3 upload works | integration test |
| 4 | No static AWS keys in images | ECR scan / code review |
| 5 | Budget alarm | AWS Budgets |
| 6 | ADRs 041–042 | docs |

---

## Frontend

→ Phase 16 CDN notes + [Phase 21](../../anular-ngrx-todo-auth/plans/phase-21-frontend-aws-observability.md) CloudFront.

---

## Следующая фаза

→ [B-35 AWS DevOps Professional](./backend-phase-35-aws-devops-pro.md)
