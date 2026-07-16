# AWS Certification Practice Track

Практика привязана к фазам todo-platform. Экзамены **не** вместо roadmap — labs внутри **B-34 / B-35 / B-38**.

| Сертификация | Код | Фазы | Когда |
|-------------|-----|------|-------|
| Solutions Architect – Associate | SAA-C03 | [B-34](./backend-phase-34-aws-foundations.md) | после B-24 + B-17 |
| Developer – Associate | DVA-C02 | [B-34](./backend-phase-34-aws-foundations.md) | вместе с SAA labs |
| DevOps Engineer – Professional | DOP-C02 | [B-35](./backend-phase-35-aws-devops-pro.md) | после B-34 + B-28 |
| Machine Learning Engineer – Associate | MLA-C01 | [B-38](./backend-phase-38-aws-ml-engineer.md) | после B-36 RAG |

Azure path остаётся: B-25…B-26. AWS — **дополнение**, не замена.

---

## SAA-C03 → labs

| Exam theme | Lab |
|------------|-----|
| VPC / networking | B-34.1 |
| Compute (ECS/EKS/EC2) | B-34.3 |
| Database (RDS) | B-34.2 |
| Storage (S3) | B-34.2 / B-14 parity |
| Security (IAM, SG) | B-34.3–4 |
| Resilience / HA | Multi-AZ notes B-34 |
| Cost | Budgets B-34 |

## DVA-C02 → labs

| Exam theme | Lab |
|------------|-----|
| SDK & APIs | S3 presign, Secrets Manager |
| CI/CD basics | ECR push from Actions |
| Auth | Cognito or JWT validation |
| Debugging | CloudWatch Logs |
| Containers | Task defs |

## DOP-C02 → labs

| Exam theme | Lab |
|------------|-----|
| SDLC automation | B-35 pipelines |
| IaC | Terraform AWS modules |
| Monitoring | AMP/AMG or LGTM on EKS |
| Incident response | Runbooks + rollback |
| Configuration & compliance | Config rules |

## MLA-C01 → labs

| Exam theme | Lab |
|------------|-----|
| Data prep | B-36 chunks + B-38 S3 sets |
| Model selection | Bedrock ADR-048 |
| Deployment | ECS + feature flags |
| Monitoring | token/latency metrics |
| Responsible AI | Guardrails B-38 |

---

## Study rhythm

- 2–3 ч/нед exam questions **параллельно** с фазой
- После каждой фазы — practice exam section mapped above
- Frontend: Phase 21 for CloudFront/RUM story (SAA edge)
