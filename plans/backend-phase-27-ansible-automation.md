# Backend Phase B-27 — Ansible Automation

> **Теория:** [guides/b-27-ansible-automation-theory.md](./guides/b-27-ansible-automation-theory.md) — статус: placeholder

**Длительность:** 1–2 недели (15–20 ч)  
**Предусловия:** [B-26](./backend-phase-26-kubernetes-aks.md), [B-08](./backend-phase-08-docker-compose.md)  
**Цель:** Ansible playbooks for VM/bare-metal bootstrap, docker host provisioning, config drift prevention, complement to Terraform/K8s.

---

## Результат фазы

- [ ] Ansible layout `infra/ansible/` — inventories, roles, playbooks
- [ ] Role `docker` — install Docker CE, compose plugin
- [ ] Role `todo-stack` — deploy compose stack from templates
- [ ] Role `nginx` — copy TLS certs, reload config
- [ ] Role `postgres-backup` — cron pg_dump to blob mount
- [ ] Inventory `inventories/dev/hosts.yml` — local or VM
- [ ] Playbook `site.yml` — full stack on single VM (demo deploy)
- [ ] Vault encrypted secrets `group_vars/all/vault.yml`
- [ ] Idempotent run — second run 0 changes
- [ ] ADR-040: Ansible vs Terraform scope split

---

## Неделя 1 — Roles & inventory

### B-27.1 Project structure

```
infra/ansible/
  ansible.cfg
  inventories/dev/hosts.yml
  group_vars/all/vars.yml
  group_vars/all/vault.yml
  roles/docker/
  roles/todo-stack/
  roles/nginx/
  roles/postgres-backup/
  playbooks/site.yml
```

1. `ansible.cfg` — inventory path, roles path
2. Host group `todo_servers` — ansible_host, user

### B-27.2 docker role

1. Tasks: install prerequisites, add docker repo, install packages
2. Enable docker service, add user to docker group
3. Handlers: restart docker

### B-27.3 todo-stack role

1. Template `docker-compose.yml.j2` from vars
2. Copy `.env` from vault vars
3. `community.docker.docker_compose_v2` — up detached
4. Health check wait for API

---

## Неделя 2 — Operations playbooks

### B-27.4 nginx role

1. Deploy `infra/nginx` configs via template
2. Cert paths from variables
3. Handler nginx reload on config change

### B-27.5 postgres-backup role

1. Cron daily `pg_dump | gzip`
2. Upload to Azure blob via azcopy optional task
3. Retention 7 days local

### B-27.6 Vault & CI

1. `ansible-vault encrypt` secrets
2. CI job: `ansible-playbook --check` on PR
3. Document decrypt for local runs

---

## Команды

```bash
cd infra/ansible

ansible-galaxy collection install community.docker

ansible-vault create group_vars/all/vault.yml

ansible-playbook -i inventories/dev/hosts.yml playbooks/site.yml --check
ansible-playbook -i inventories/dev/hosts.yml playbooks/site.yml

# ad-hoc
ansible todo_servers -i inventories/dev/hosts.yml -m ping

ansible-playbook playbooks/backup-now.yml
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | ansible ping | all ok |
| 2 | Idempotent site.yml | 2nd run changed=0 |
| 3 | Stack running on VM | curl health |
| 4 | Vault secrets | no plaintext passwords in git |
| 5 | Backup cron | crontab -l |
| 6 | nginx deployed | https works |
| 7 | ADR-040 | Ansible scope doc |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-27 | Demo deploy on single VM for portfolio |
| Staging alt | VM-based staging if no AKS credits |
| B-28 | Ansible can flip nginx upstream weights |

---

## Следующая фаза

→ [B-28 Blue-Green & Canary per Tenant](./backend-phase-28-blue-green-canary.md)
