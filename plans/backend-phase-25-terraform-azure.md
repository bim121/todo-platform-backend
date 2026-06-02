# Backend Phase B-25 — Terraform Azure

> **Теория:** [guides/b-25-terraform-azure-theory.md](./guides/b-25-terraform-azure-theory.md) — статус: placeholder

**Длительность:** 2–3 недели (30–40 ч)  
**Предусловия:** [B-24](./backend-phase-24-observability.md), [B-08](./backend-phase-08-docker-compose.md)  
**Цель:** Terraform modules for Azure: RG, AKS stub, PostgreSQL Flexible, Redis, Blob, Key Vault, remote state.

---

## Результат фазы

- [ ] Terraform root `infra/terraform/environments/dev`
- [ ] Modules: `network`, `postgres`, `redis`, `storage`, `keyvault`, `aks` (minimal)
- [ ] Azure PostgreSQL Flexible Server — dev SKU
- [ ] Azure Cache for Redis — Basic tier dev
- [ ] Storage account + container for attachments
- [ ] Key Vault secrets — connection strings referenced by apps
- [ ] Remote state: Azure Storage backend
- [ ] `terraform plan` clean; `apply` documented (manual gate)
- [ ] Outputs for connection strings (sensitive)
- [ ] ADR-038: Azure vs AWS for this project

---

## Неделя 1 — Foundation

### B-25.1 Terraform layout

```
infra/terraform/
  modules/
    network/
    postgres/
    redis/
    storage/
    keyvault/
  environments/
    dev/
      main.tf
      variables.tf
      outputs.tf
      backend.tf
```

1. Provider `azurerm` ~> 3.x
2. Resource group `rg-todo-platform-dev`
3. Tags: project, environment, managed_by=terraform

### B-25.2 Remote state

1. Bootstrap script creates state storage account
2. `backend.tf` —azurerm backend config
3. State locking via blob lease

### B-25.3 Network module

1. VNet, subnets: `aks`, `data`, `private-endpoints`
2. NSG rules minimal for dev
3. Outputs subnet ids

---

## Неделя 2 — Data services

### B-25.4 PostgreSQL module

1. Flexible Server PostgreSQL 16
2. Database `tododb`, admin credentials in Key Vault
3. Firewall rule for dev IP optional
4. HA disabled for dev — document prod HA flag

### B-25.5 Redis module

1. Azure Cache for Redis — Basic C0
2. Connection string to Key Vault secret `Redis--ConnectionString`

### B-25.6 Storage module

1. Storage account `sttodoplatformdev`
2. Container `todo-attachments`
3. Managed identity prep for AKS (B-26)

---

## Неделя 3 — Key Vault & AKS prep

### B-25.7 Key Vault module

1. Secrets: Postgres, Redis, Storage, Kafka (placeholder)
2. Access policy for deployer SP
3. App references via `@Microsoft.KeyVault(SecretUri=...)`

### B-25.8 AKS minimal cluster

1. AKS module — 1 node pool, 2 nodes dev
2. Attach ACR optional
3. Not deploying app yet — B-26 Helm

### B-25.9 CI integration

1. `terraform fmt -check`, `validate`, `plan` in GitHub Actions
2. No auto-apply to prod
3. Cost estimate note in README

---

## Команды

```bash
cd infra/terraform/environments/dev

terraform init
terraform plan -out=tfplan
# terraform apply tfplan  # manual when ready

az postgres flexible-server show --resource-group rg-todo-platform-dev --name psql-todo-dev

terraform output -json | jq '.postgres_connection_string.value'

terraform fmt -recursive ../../
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | terraform validate | exit 0 |
| 2 | Modules composed | plan shows resources |
| 3 | Remote state works | state in azure storage |
| 4 | Postgres reachable | az cli test |
| 5 | Redis reachable | redis-cli PING via tunnel |
| 6 | Key Vault secrets | az keyvault secret list |
| 7 | ADR-038 | cloud choice doc |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-25 | Staging URLs for frontend environment.staging.ts |
| B-26 | Deploy backend — frontend points to ingress URL |
| Prod | Entra ID path documented as Keycloak alt |

Parallel skills: Terraform modules — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-26 Kubernetes AKS & Helm](./backend-phase-26-kubernetes-aks.md)
