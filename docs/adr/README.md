# Architecture Decision Records (ADR)

Короткие записи «почему мы так сделали». Удобно возвращаться после паузы в обучении.

| ID | Файл | Тема | Статус |
|----|------|------|--------|
| ADR-021 | [021-domain-events.md](./021-domain-events.md) | Domain Events, UoW, Outbox schema | **Accepted** (B-04.1–9) |
| ADR-020 | `020-clean-architecture-cqrs.md` | Clean Architecture + CQRS | planned (B-00) |
| ADR-022 | [022-caching-strategy.md](./022-caching-strategy.md) | Redis cache-aside | **Accepted** (B-06) |
| ADR-023 | `023-outbox-pattern.md` | Transactional outbox | planned (B-07) |
| ADR-025 | [025-ef-dapper-read-split.md](./025-ef-dapper-read-split.md) | EF write / Dapper read | **Accepted** (B-10) |
| ADR-026 | [026-shared-schema-rls.md](./026-shared-schema-rls.md) | Shared schema + RLS | **Accepted** (B-11) |

## Формат

Каждый ADR: **Context → Decision → Consequences → Ссылки на код**.

Теория по фазам: [`plans/guides/`](../plans/guides/README.md).
