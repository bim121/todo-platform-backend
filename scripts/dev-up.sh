#!/usr/bin/env bash
# One-command local stack (B-08).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ ! -f .env ]]; then
  cp .env.example .env
  echo "Created .env from .env.example"
fi

PROFILE="${1:-}"
case "$PROFILE" in
  full)  docker compose --profile full up -d --build ;;
  dev)   docker compose --profile dev up -d --build --scale api=0 ;;
  *)     docker compose up -d --build ;;
esac

echo "Waiting for API /health/ready ..."
for i in $(seq 1 60); do
  if curl -sf "http://localhost:${API_PORT:-8080}/health/ready" >/dev/null; then
    echo "API is ready."
    docker compose ps
    exit 0
  fi
  sleep 3
done

echo "API did not become healthy in time." >&2
docker compose logs --tail=80 api
exit 1
