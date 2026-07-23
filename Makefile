.PHONY: up up-full up-dev down reset logs smoke ps

## Core stack (api + postgres + redis + rabbitmq + keycloak)
up:
	docker compose up -d --build

## + Mailhog + Redis Commander
up-full:
	docker compose --profile full up -d --build

## Hot-reload API via sdk + dotnet watch (published api scaled to 0)
up-dev:
	docker compose --profile dev up -d --build --scale api=0

down:
	docker compose down

## Wipe volumes (DB / Redis / RabbitMQ data) and bring stack back
reset:
	docker compose down -v
	docker compose up -d --build

logs:
	docker compose logs -f api

ps:
	docker compose ps

## Wait for health + Keycloak token + GET /api/todos
smoke:
	./scripts/smoke.sh
