# Backend Phase B-02 — OpenAPI & Contracts

> **Теория:** [guides/b-02-openapi-contracts-theory.md](./guides/b-02-openapi-contracts-theory.md) — статус: placeholder

**Длительность:** неделя 3 (15–20 ч)  
**Предусловия:** [B-01](./backend-phase-01-clean-api.md)  
**Цель:** RFC 7807 ProblemDetails, API versioning, синхронизация с `contracts/openapi.yaml`.

---

## Результат фазы

- [ ] Все ошибки API → `application/problem+json`
- [ ] `Accept-Version: v1` header поддержан
- [ ] Swagger export = `contracts/openapi.yaml`
- [ ] Global exception handler
- [ ] Validation errors → 400 с `errors` object
- [ ] CI step: validate OpenAPI diff

---

## Неделя 1 — ProblemDetails & Swagger

### B-02.1 ProblemDetails

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
```

**GlobalExceptionHandler:** map `NotFoundException` → 404, `ValidationException` → 400.

### B-02.2 Swagger polish

- XML comments на controllers
- `[ProducesResponseType(typeof(ProblemDetails), 400)]`
- Swagger UI title/version from assembly

### B-02.3 Sync contract

1. Export `swagger/v1/swagger.json`
2. Diff с `../../contracts/openapi.yaml`
3. Update shared contract if intentional breaking change

---

## Неделя 2 — Versioning & Pact prep

### B-02.4 Version middleware

```csharp
// Read Accept-Version header, store in HttpContext.Items
```

### B-02.5 Deprecation header

Response: `Deprecation: true` + `Sunset` header for old endpoints.

### B-02.6 Provider stub for Pact

Document provider URL `http://localhost:5000` for frontend Phase 11.

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | 404 returns ProblemDetails | curl invalid id |
| 2 | Validation 400 with field errors | POST empty title |
| 3 | OpenAPI matches contract | manual diff |
| 4 | Version header accepted | curl -H Accept-Version:v1 |

---

## Следующая фаза

→ [B-03 CQRS MediatR](./backend-phase-03-cqrs-mediatr.md)
