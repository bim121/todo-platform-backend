# Connection pooling (B-09.6)

## Npgsql client pool (current)

ASP.NET uses **Npgsql connection pooling inside the process**. Each API instance keeps a pool of physical connections to Postgres.

Defaults applied when missing (see `DependencyInjection.EnsurePoolSettings`):

| Setting | Value | Meaning |
|---------|-------|---------|
| `Maximum Pool Size` | 100 | Max open connections per process |
| `Minimum Pool Size` | 0 | Do not keep idle connections warm |
| `Timeout` | 15 | Seconds to wait for a free pooled connection |

Example:

```
Host=localhost;Port=5432;Database=tododb;Username=todo;Password=todo;Maximum Pool Size=100;Minimum Pool Size=0;Timeout=15
```

Under load, if you see timeouts waiting for a connection, either raise pool size carefully or scale API replicas (each replica has its own pool — total DB connections ≈ replicas × max pool).

## Future: PgBouncer (B-20)

For many API pods, client pools multiply and can exhaust Postgres `max_connections`. **PgBouncer** (or similar) sits between apps and Postgres:

```
API → (small Npgsql pool) → PgBouncer → Postgres
```

Benefits: connection multiplexing, shorter-lived client connections, safer horizontal scale.

Not configured in this phase — track as B-20.

## Test

`GetTodos_100Parallel_AllSucceed` fires 100 concurrent `GET /api/todos` against the test host (InMemory DB). It does not stress a real Npgsql pool, but guards against lock / concurrency regressions in the handler path. Against real Postgres + pool settings, the same pattern is a smoke for pool exhaustion.
