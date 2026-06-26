# Backend Phase B-26 — Kubernetes AKS & Helm

> **Теория:** [guides/b-26-kubernetes-aks-theory.md](./guides/b-26-kubernetes-aks-theory.md) — статус: placeholder

**Длительность:** 3 недели (35–45 ч)  
**Предусловия:** [B-25](./backend-phase-25-terraform-azure.md), [B-17](./backend-phase-17-microservices-split.md)  
**Цель:** Helm charts for todos/admin/gateway, deploy to AKS, ingress + cert-manager TLS, secrets from Key Vault CSI.

---

## Результат фазы

- [ ] Helm chart `charts/todo-platform/` — umbrella or per-service subcharts
- [ ] Deployments: gateway, todos-api, admin-api with probes
- [ ] Services ClusterIP, HPA on CPU for todos-api
- [ ] Ingress nginx + cert-manager ClusterIssuer Let's Encrypt staging
- [ ] Azure Key Vault CSI driver — mount secrets as env
- [ ] ConfigMaps for non-secret appsettings
- [ ] `helm upgrade --install` documented for dev/staging
- [ ] PodDisruptionBudget minAvailable 1
- [ ] GitHub Actions deploy workflow (helm dry-run + staging apply)

---

## Неделя 1 — Helm charts

### B-26.1 Chart structure

```
charts/todo-platform/
  Chart.yaml
  values.yaml
  values-dev.yaml
  templates/
    deployment-gateway.yaml
    deployment-todos.yaml
    deployment-admin.yaml
    service-*.yaml
    ingress.yaml
    hpa.yaml
    pdb.yaml
```

1. Image tags from CI build pipeline
2. Resource requests/limits defined
3. Liveness `/health/live`, readiness `/health/ready`

### B-26.2 Values per environment

1. `values-dev.yaml` — 1 replica, small resources
2. `values-staging.yaml` — 2 replicas, HPA min 2
3. Document override pattern

### B-26.3 Local validation

1. `helm template` output reviewed
2. kubeconform or helm lint pass
3. Optional: kind cluster local test

---

## Неделя 2 — AKS deployment

### B-26.4 Connect to AKS from B-25

1. `az aks get-credentials`
2. Namespace `todo-platform-dev`
3. Install ingress-nginx via helm
4. Install cert-manager

### B-26.5 Key Vault CSI

1. SecretProviderClass for connection strings
2. Workload identity or managed identity binding
3. Pods mount secrets — no plain text in values.yaml

### B-26.6 Deploy stack

1. `helm upgrade --install todo-platform ./charts/todo-platform -f values-dev.yaml`
2. Verify pods running, ingress external IP
3. Smoke curl through ingress URL

---

## Неделя 3 — Production patterns

### B-26.7 HPA & PDB

1. HPA targets 70% CPU, min 2 max 10 todos-api
2. PDB ensures rolling updates safe
3. Rolling update strategy `maxUnavailable: 0`

### B-26.8 Observability in K8s

1. OTel collector DaemonSet or sidecar
2. ServiceMonitor for Prometheus operator (if installed)
3. Logs to Azure Monitor optional note

### B-26.9 CI/CD workflow

1. Build push to ACR
2. Helm dry-run on PR
3. Staging deploy on main merge (manual approval)
4. ADR-039: Helm vs Kustomize

---

## Команды

```bash
az aks get-credentials -g rg-todo-platform-dev -n aks-todo-dev

kubectl create namespace todo-platform-dev

helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx -n ingress-nginx --create-namespace

cd charts/todo-platform
helm lint .
helm template todo-platform . -f values-dev.yaml | kubectl apply --dry-run=client -f -

helm upgrade --install todo-platform . -n todo-platform-dev -f values-dev.yaml

kubectl get pods -n todo-platform-dev
kubectl logs -n todo-platform-dev deploy/todo-platform-todos-api

curl https://<ingress-host>/health/ready
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | helm lint pass | exit 0 |
| 2 | Pods healthy | all Running |
| 3 | Ingress TLS | https valid (staging LE) |
| 4 | Secrets from KV | no secrets in git |
| 5 | HPA active | kubectl get hpa |
| 6 | Smoke test | CRUD via ingress |
| 7 | CI workflow | dry-run green |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-26 | Deploy Angular to Static Web Apps — API URL = ingress |
| Staging | Full E2E against AKS |
| B-28 | Blue-green uses Helm weights |

Parallel skills: Helm charts — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-27 Ansible Automation](./backend-phase-27-ansible-automation.md)
