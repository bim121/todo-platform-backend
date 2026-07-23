# Keycloak вЂ” dev setup (B-05)

Realm **`todo-platform`** РёРјРїРѕСЂС‚РёСЂСѓРµС‚СЃСЏ РёР· [`infra/keycloak/todo-platform-realm.json`](../infra/keycloak/todo-platform-realm.json) РїСЂРё `docker compose up keycloak`.

## Р—Р°РїСѓСЃРє

```bash
docker compose up -d keycloak
# Admin Console: http://localhost:8180/admin  (admin / admin)
# Р”РѕР¶РґР°С‚СЊСЃСЏ РіРѕС‚РѕРІРЅРѕСЃС‚Рё: http://localhost:8180/health/ready
```

Р•СЃР»Рё realm СѓР¶Рµ СЃСѓС‰РµСЃС‚РІСѓРµС‚ Рё РЅСѓР¶РЅРѕ РїРµСЂРµРёРјРїРѕСЂС‚РёСЂРѕРІР°С‚СЊ JSON вЂ” РїРµСЂРµСЃРѕР·РґР°Р№ РєРѕРЅС‚РµР№РЅРµСЂ:

```bash
docker compose rm -sf keycloak && docker compose up -d keycloak
```

Compose РёСЃРїРѕР»СЊР·СѓРµС‚ `--spi-import-export-import-strategy=OVERWRITE_EXISTING` РґР»СЏ dev.

---

## Realm `todo-platform`

| РќР°СЃС‚СЂРѕР№РєР° | Р—РЅР°С‡РµРЅРёРµ |
|-----------|----------|
| Email login | РІРєР»СЋС‡С‘РЅ (`loginWithEmailAllowed`) |
| Realm roles | `user`, `admin` (claim `realm_access.roles` РІ JWT) |

### РџРѕР»СЊР·РѕРІР°С‚РµР»Рё (dev only)

| Email | Password | Roles |
|-------|----------|-------|
| `test@example.com` | `password123` | `user` |
| `admin@example.com` | `password123` | `admin`, `user` |

`test@example.com` СЃРѕРІРїР°РґР°РµС‚ СЃ seed РІ `DbSeeder` (B-01).

---

## Clients

### `todo-spa` (B-05.2)

| | |
|---|---|
| РўРёРї | Public |
| Flow | Authorization Code + **PKCE** (S256) |
| Redirect | `http://localhost:4200/*` |
| Web origin | `http://localhost:4200` |
| Direct access grants | РІРєР»СЋС‡С‘РЅ (С‚РѕР»СЊРєРѕ dev вЂ” password grant РґР»СЏ Postman/curl) |

### `todo-api` (B-05.3)

| | |
|---|---|
| РўРёРї | **Bearer-only** (resource server) |
| РќР°Р·РЅР°С‡РµРЅРёРµ | Audience РІ access token РґР»СЏ ASP.NET JWT validation |
| Audience mapper | РЅР° `todo-spa` в†’ claim `aud` СЃРѕРґРµСЂР¶РёС‚ `todo-api` |

---

## Token endpoint (Postman / curl)

**URL:**

```
POST http://localhost:8180/realms/todo-platform/protocol/openid-connect/token
```

**Content-Type:** `application/x-www-form-urlencoded`

### Password grant (dev only)

РџР°СЂР°РјРµС‚СЂС‹ body:

| Key | Value |
|-----|-------|
| `grant_type` | `password` |
| `client_id` | `todo-spa` |
| `username` | `test@example.com` |
| `password` | `password123` |

```bash
curl -X POST "http://localhost:8180/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=test@example.com" \
  -d "password=password123"
```

РћС‚РІРµС‚: JSON СЃ `access_token`, `expires_in`, `refresh_token`.

### Admin user token

```bash
curl -X POST "http://localhost:8180/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=admin@example.com" \
  -d "password=password123"
```

### Р’С‹Р·РѕРІ API (B-05.4+)

```bash
# РЎРїРёСЃРѕРє todos С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ (userId РѕРїС†РёРѕРЅР°Р»РµРЅ вЂ” Р±РµСЂС‘С‚СЃСЏ РёР· С‚РѕРєРµРЅР°)
curl http://localhost:5000/api/todos \
  -H "Authorization: Bearer <access_token>"

# РџСЂРѕС„РёР»СЊ С‚РµРєСѓС‰РµРіРѕ РїРѕР»СЊР·РѕРІР°С‚РµР»СЏ (BFF)
curl http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer <access_token>"

# Admin endpoint вЂ” С‚РѕР»СЊРєРѕ СЂРѕР»СЊ admin
curl http://localhost:5000/api/admin/tenants \
  -H "Authorization: Bearer <admin_access_token>"
```

### РџРѕР»РЅС‹Р№ dev-СЃС†РµРЅР°СЂРёР№ (copy-paste)

```bash
# 1. Keycloak access token (test user)
TOKEN=$(curl -s -X POST "http://localhost:8180/realms/todo-platform/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=todo-spa" \
  -d "grant_type=password" \
  -d "username=test@example.com" \
  -d "password=password123" | jq -r .access_token)

# 2. РџСЂРѕС„РёР»СЊ (РїРµСЂРІС‹Р№ Р·Р°РїСЂРѕСЃ Р»РёРЅРєСѓРµС‚ Keycloak sub Рє seed-РїРѕР»СЊР·РѕРІР°С‚РµР»СЋ РІ Р‘Р”)
curl -s http://localhost:5000/api/auth/me -H "Authorization: Bearer $TOKEN" | jq

# 3. Todos
curl -s http://localhost:5000/api/todos -H "Authorization: Bearer $TOKEN" | jq

# 4. Р‘РµР· С‚РѕРєРµРЅР° в†’ 401 ProblemDetails
curl -i http://localhost:5000/api/todos

# 5. User token РЅР° admin в†’ 403
curl -i http://localhost:5000/api/admin/tenants -H "Authorization: Bearer $TOKEN"
```

### Mock login СѓРґР°Р»С‘РЅ (B-05.7)

`POST /api/auth/login` РІРѕР·РІСЂР°С‰Р°РµС‚ **410 Gone** СЃ `Deprecation`/`Sunset` headers.
РСЃРїРѕР»СЊР·СѓР№ Keycloak token endpoint РІС‹С€Рµ.

---

## Postman

1. **New request** в†’ POST в†’ `http://localhost:8180/realms/todo-platform/protocol/openid-connect/token`
2. **Body** в†’ `x-www-form-urlencoded`:
   - `grant_type` = `password`
   - `client_id` = `todo-spa`
   - `username` = `test@example.com`
   - `password` = `password123`
3. **Send** в†’ СЃРєРѕРїРёСЂРѕРІР°С‚СЊ `access_token`
4. Р’ Р·Р°РїСЂРѕСЃР°С… Рє API: **Authorization** в†’ Bearer Token в†’ РІСЃС‚Р°РІРёС‚СЊ token

Р”Р»СЏ Angular (B-05 + Phase 17): Authorization Code + PKCE С‡РµСЂРµР· `todo-spa`, РЅРµ password grant.

---

## JWKS / metadata (РґР»СЏ B-05.4)

| | URL |
|---|-----|
| OpenID Configuration | http://localhost:8180/realms/todo-platform/.well-known/openid-configuration |
| JWKS | http://localhost:8180/realms/todo-platform/protocol/openid-connect/certs |
| Issuer | `http://localhost:8180/realms/todo-platform` |
| Expected audience | `todo-api` |
