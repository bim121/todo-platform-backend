# SignalR client contract (B-13)

Live todo updates over WebSocket via ASP.NET Core SignalR.

## Endpoint

| | |
|---|---|
| Hub URL | `/hubs/todos` |
| Auth | JWT Bearer (same Keycloak realm as REST) |
| Tenant | Header `X-Tenant-Id` **or** query `?tenant=` **or** JWT claim `tenant_id` |

Browsers cannot set `Authorization` on the WebSocket upgrade. Pass the token as:

```text
/hubs/todos?access_token=<jwt>&tenant=default
```

Or use `@microsoft/signalr` `accessTokenFactory` (sends `access_token` on negotiate/WS).

## Groups

On connect the hub joins:

```text
tenant:{tenantId}:user:{userId}
```

Events are **never** broadcast globally — only to that group (B-13.4).

## Client events (`ITodoHubClient`)

| Method | When | Payload |
|--------|------|---------|
| `TodoCreated` | REST create → outbox → MassTransit | `{ id, title, completed, version }` |
| `TodoUpdated` | REST patch → outbox → MassTransit | same |
| `TodoDeleted` | REST delete → outbox → MassTransit | same |

`version` is Unix milliseconds (`OccurredOn`) — use for optional client-side ordering, not optimistic concurrency.

### Payload shape

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "title": "Buy milk",
  "completed": false,
  "version": 1735689600000
}
```

## Angular snippet

```bash
npm i @microsoft/signalr
```

```typescript
import * as signalR from '@microsoft/signalr';

export interface TodoRealtimeMessage {
  id: string;
  title: string;
  completed: boolean;
  version: number;
}

const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:8080/hubs/todos', {
    accessTokenFactory: () => keycloak.token!,
    headers: { 'X-Tenant-Id': tenantSlug },
  })
  .withAutomaticReconnect()
  .build();

connection.on('TodoCreated', (msg: TodoRealtimeMessage) => {
  // dispatch NgRx action
});
connection.on('TodoUpdated', (msg: TodoRealtimeMessage) => { /* ... */ });
connection.on('TodoDeleted', (msg: TodoRealtimeMessage) => { /* ... */ });

await connection.start();
```

## Scale-out (Redis backplane)

With `SignalR:UseRedisBackplane=true` (default when Redis is configured), hub messages cross API instances via Redis pub/sub.

Compose multi-instance check:

```bash
docker compose up -d --scale api=2
# Connect WS to one instance, POST create via the other (load balancer / alternate port)
# Client still receives TodoCreated
```

See `api` and optional `api-2` in [docker-compose.yml](../../docker-compose.yml).

## Pipeline (backend)

```text
REST command → UoW outbox → OutboxProcessor → MassTransit
  → Todo*SignalRConsumer → ITodoRealtimeNotifier → Hub group
```

## nginx notes (B-23)

WebSocket upgrade headers required:

```nginx
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection "upgrade";
proxy_set_header Host $host;
proxy_read_timeout 86400;
```
