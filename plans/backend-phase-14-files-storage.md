# Backend Phase B-14 — Azure Blob File Storage

> **Теория:** [guides/b-14-files-storage-theory.md](./guides/b-14-files-storage-theory.md) — статус: placeholder

**Длительность:** 2 недели (20–25 ч)  
**Предусловия:** [B-11](./backend-phase-11-multi-tenant-isolation.md), [B-05](./backend-phase-05-keycloak-auth.md)  
**Цель:** Attachments для todos через Azure Blob (Azurite locally), SAS upload/download, metadata в Postgres.

---

## Результат фазы

- [ ] Azurite container in docker-compose (blob port 10000)
- [ ] `IFileStorageService` — Upload, GetSasUrl, Delete
- [ ] Table `todo_attachments` — Id, TodoId, TenantId, BlobName, FileName, Size, ContentType
- [ ] `POST /api/todos/{id}/attachments` — returns upload SAS URL
- [ ] `GET /api/todos/{id}/attachments` — list metadata
- [ ] `DELETE /api/todos/{id}/attachments/{attachmentId}`
- [ ] Max size validation (10 MB), allowed MIME whitelist
- [ ] Tenant-scoped blob path: `{tenantId}/{todoId}/{guid}-{filename}`
- [ ] Optional virus scan stub hook

---

## Неделя 1 — Storage abstraction

### B-14.1 Azurite & packages

1. Docker service `azurite` — ports 10000-10002
2. `Azure.Storage.Blobs` NuGet
3. Connection string in appsettings for dev emulator

```yaml
azurite:
  image: mcr.microsoft.com/azure-storage/azurite
  ports:
    - "10000:10000"
```

### B-14.2 IFileStorageService

1. Interface in Application layer
2. `AzureBlobStorageService` implementation
3. Create container `todo-attachments` on startup if missing
4. Generate user-delegation SAS or account SAS (dev)

**Файл:** `Infrastructure/Storage/AzureBlobStorageService.cs`

### B-14.3 Attachment entity & migration

1. `TodoAttachment` entity
2. FK to Todo, cascade delete
3. Migration `V009__todo_attachments.sql`

---

## Неделя 2 — API & security

### B-14.4 Upload flow (direct-to-blob)

1. Client requests SAS: `POST .../attachments` with `{ fileName, contentType, size }`
2. Server validates todo ownership + tenant, returns `{ uploadUrl, attachmentId }`
3. Client PUT to blob directly
4. `POST .../attachments/{id}/confirm` — mark uploaded (optional callback)

### B-14.5 Download & delete

1. GET list — metadata only, no blob inline
2. GET `.../attachments/{id}/download-url` — short-lived read SAS (15 min)
3. DELETE — remove blob + DB row

### B-14.6 Tests & ADR

1. Integration test with Azurite Testcontainer
2. Reject `.exe` MIME type
3. ADR-028: direct upload vs API proxy
4. Document Azure Blob prod + CDN path for interviews

---

## Команды

```bash
docker compose up -d azurite

dotnet add src/TodoPlatform.Infrastructure package Azure.Storage.Blobs

# request upload URL
curl -X POST http://localhost:8080/api/todos/<todoId>/attachments \
  -H "Authorization: Bearer <token>" \
  -H "X-Tenant-Id: <tenant>" \
  -H "Content-Type: application/json" \
  -d '{"fileName":"note.pdf","contentType":"application/pdf","size":1024}'

dotnet test --filter "FullyQualifiedName~Attachment"
```

---

## Критерии готовности

| # | Критерий | Проверка |
|---|----------|----------|
| 1 | Azurite running | blob endpoint responds |
| 2 | SAS upload works | file in container |
| 3 | Tenant path isolation | blobs under tenant prefix |
| 4 | MIME validation | reject executable |
| 5 | OpenAPI paths | attachments documented |
| 6 | Tests green | `dotnet test` |

---

## Связь с frontend

| Когда | Действие |
|-------|----------|
| B-14 | Frontend attachments UI |
| Phase 15+ | Upload component uses SAS URL |
| Admin | Storage quota per tenant (future) |

Parallel skills: Design file upload + CDN — [parallel-skills-backend.md](./parallel-skills-backend.md).

---

## Следующая фаза

→ [B-15 Full-Text Search](./backend-phase-15-search-fulltext.md)
