#!/usr/bin/env bash
# Smoke: health → Keycloak password grant → GET /api/todos
set -euo pipefail

API_URL="${API_URL:-http://localhost:8080}"
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8180}"
REALM="${KEYCLOAK_REALM:-todo-platform}"
CLIENT_ID="${KEYCLOAK_CLIENT_ID:-todo-spa}"
USERNAME="${KEYCLOAK_USERNAME:-test@example.com}"
PASSWORD="${KEYCLOAK_PASSWORD:-password123}"
TOKEN_URL="${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token"

echo "==> Waiting for API ready at ${API_URL}/health/ready"
for i in $(seq 1 90); do
  if curl -sf "${API_URL}/health/ready" >/dev/null; then
    echo "API healthy."
    break
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "API health check timed out." >&2
    exit 1
  fi
  sleep 2
done

echo "==> Waiting for Keycloak token (${TOKEN_URL})"
ACCESS_TOKEN=""
for i in $(seq 1 90); do
  RESP=$(curl -s -w "\n%{http_code}" -X POST "$TOKEN_URL" \
    -d "client_id=${CLIENT_ID}" \
    -d "grant_type=password" \
    -d "username=${USERNAME}" \
    -d "password=${PASSWORD}" || true)
  BODY=$(echo "$RESP" | sed '$d')
  CODE=$(echo "$RESP" | tail -n1)
  if [[ "$CODE" == "200" ]]; then
    ACCESS_TOKEN=$(echo "$BODY" | sed -n 's/.*"access_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -1)
    if [[ -n "$ACCESS_TOKEN" ]]; then
      echo "Keycloak issued token."
      break
    fi
  fi
  if [[ "$i" -eq 90 ]]; then
    echo "Keycloak token endpoint timed out (last HTTP ${CODE})." >&2
    echo "$BODY" >&2
    exit 1
  fi
  sleep 2
done

echo "==> GET ${API_URL}/api/todos"
HTTP=$(curl -s -o /tmp/todos.json -w "%{http_code}" \
  -H "Authorization: Bearer ${ACCESS_TOKEN}" \
  "${API_URL}/api/todos")

if [[ "$HTTP" != "200" ]]; then
  echo "GET /api/todos failed with HTTP ${HTTP}" >&2
  cat /tmp/todos.json >&2 || true
  exit 1
fi

echo "Smoke OK (GET /api/todos → 200)."
