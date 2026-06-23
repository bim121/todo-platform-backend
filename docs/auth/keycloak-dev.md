# Keycloak — dev setup (B-05)

Realm **`todo-platform`** импортируется из [`infra/keycloak/todo-platform-realm.json`](../infra/keycloak/todo-platform-realm.json) при `docker compose up keycloak`.

## Запуск

```bash
docker compose up -d keycloak
# Admin Console: http://localhost:8080/admin  (admin / admin)
# Дождаться готовности: http://localhost:8080/health/ready
```

Если realm уже существует и нужно переимпортировать JSON — пересоздай контейнер:

```bash
docker compose rm -sf keycloak && docker compose up -d keycloak
```

Compose использует `--spi-import-export-import-strategy=OVERWRITE_EXISTING` для dev.

---

## Realm `todo-platform`

| Настройка | Значение |
|-----------|----------|
| Email login | включён (`loginWithEmailAllowed`) |
| Realm roles | `user`, `admin` (claim `realm_access.roles` в JWT) |

### Пользователи (dev only)

| Email | Password | Roles |
|-------|----------|-------|
| `test@example.com` | `password123` | `user` |
| `admin@example.com` | `password123` | `admin`, `user` |

`test@example.com` совпадает с seed в `DbSeeder` (B-01).

---

## Clients

### `todo-spa` (B-05.2)

| | |
|---|---|
| Тип | Public |
| Flow | Authorization Code + **PKCE** (S256) |
| Redirect | `http://localhost:4200/*` |
| Web origin | `http://localhost:4200` |
| Direct access grants | включён (только dev — password grant для Postman/curl) |

### `todo-api` (B-05.3)

| | |
|---|---|
| Тип | **Bearer-only** (resource server) |
| Назначение | Audience в access token для ASP.NET JWT validation |
| Audience mapper | на `todo-spa` → claim `aud` содержит `todo-api` |

---

## Token endpoint (Postman / curl)

**URL:**

```
POST http://localhost:8080/realms/todo-platform/protocol/openid-connect/token
```

**Content-Type:** `application/x-www-form-urlencoded`

### Password grant (dev only)

Параметры body:

| Key | Value |
|-----|-------|
| `grant_type` | `password` |
| `client_id` | `todo-spa` |
| `username` | `test@example.com` |
| `password` | `password123` |

```bash
curl -X POST "http://localhost:8080/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=test@example.com" \
  -d "password=password123"
```

Ответ: JSON с `access_token`, `expires_in`, `refresh_token`.

### Admin user token

```bash
curl -X POST "http://localhost:8080/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=admin@example.com" \
  -d "password=password123"
```

### Вызов API (после B-05.4)

```bash
curl http://localhost:5000/api/todos?userId=<guid> \
  -H "Authorization: Bearer <access_token>"
```

---

## Postman

1. **New request** → POST → `http://localhost:8080/realms/todo-platform/protocol/openid-connect/token`
2. **Body** → `x-www-form-urlencoded`:
   - `grant_type` = `password`
   - `client_id` = `todo-spa`
   - `username` = `test@example.com`
   - `password` = `password123`
3. **Send** → скопировать `access_token`
4. В запросах к API: **Authorization** → Bearer Token → вставить token

Для Angular (B-05 + Phase 17): Authorization Code + PKCE через `todo-spa`, не password grant.

---

## JWKS / metadata (для B-05.4)

| | URL |
|---|-----|
| OpenID Configuration | http://localhost:8080/realms/todo-platform/.well-known/openid-configuration |
| JWKS | http://localhost:8080/realms/todo-platform/protocol/openid-connect/certs |
| Issuer | `http://localhost:8080/realms/todo-platform` |
| Expected audience | `todo-api` |
