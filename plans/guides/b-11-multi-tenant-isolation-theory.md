# B-11 — Multi-tenant Isolation (теория)

> **Статус:** interview + ADR готовы; полный гайд можно расширить.  
> **Практика:** [../backend-phase-11-multi-tenant-isolation.md](../backend-phase-11-multi-tenant-isolation.md)  
> **ADR:** [ADR-026](../../docs/adr/026-shared-schema-rls.md) · **Debug:** [isolation.md](../../docs/multi-tenancy/isolation.md)

---

## 1. Зачем эта тема

<!-- Контекст FAANG / Microsoft L63+ -->

## 2. Базовые концепции

<!-- Определения -->

## 3. Глубокое погружение

<!-- Как работает под капотом -->

## 4. Примеры кода (C#)

\\\csharp
// TODO: примеры для Multi-tenant Isolation
\\\

## 5. Плюсы / минусы / когда НЕ использовать

| Плюсы | Минусы |
|-------|--------|
| | |

## 6. Сравнение с альтернативами

| Подход | Популярность | Когда выбрать |
|--------|--------------|---------------|
| | | |

## 7. Типичные ошибки

- 

## 8. Вопросы на интервью

**Story (defense in depth):**  
«Shared schema, не schema-per-tenant. Каждый ряд несёт `TenantId`. HTTP резолвит tenant (`X-Tenant-Id` / JWT), scoped `ITenantContext` прокидывает его в create. EF global query filter — seatbelt: обычный LINQ не увидит чужой tenant. Postgres RLS + `SET app.current_tenant` — airbag: даже сырой Dapper/`WHERE` без tenant не вернёт чужие строки на non-superuser. Redis ключи `todos:tenant:{tid}:user:{uid}` — иначе cache-aside сам станет дырой. Superuser (`POSTGRES_USER`) обходит RLS — в проде API ходит не под ним. Admin stats — отдельный `app.bypass_rls`, не дырка в CRUD.»

1. RLS vs schema-per-tenant vs DB-per-tenant — когда что?
2. Почему `FORCE ROW LEVEL SECURITY` не спасает `psql -U todo` в docker-compose?
3. Зачем query filter, если уже есть RLS? (defense in depth + InMemory tests)
4. Как не залить tenant A в кэш tenant B?
5. Как admin считает всех пользователей, не открывая CRUD?

Практика и debug: [docs/multi-tenancy/isolation.md](../../docs/multi-tenancy/isolation.md), [ADR-026](../../docs/adr/026-shared-schema-rls.md). 

## 9. Связь с другими фазами

- Предшествует: B-10
- Следует: B-12

## 10. Ресурсы

- [Microsoft Learn](https://learn.microsoft.com/dotnet/)
- 

