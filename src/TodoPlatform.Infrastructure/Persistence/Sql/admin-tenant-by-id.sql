-- B-12.3 — admin tenant detail + schema version + stats join.
SELECT
    t."Id"::text AS Id,
    t."Name" AS Name,
    COALESCE(v."CurrentVersion", 0) AS CurrentVersion,
    COALESCE(v."Track", 'stable') AS Track,
    LOWER(t."Status") AS Status
FROM tenants t
LEFT JOIN tenant_schema_versions v ON v."TenantId" = t."Id"
LEFT JOIN (
    SELECT "TenantId", COUNT(*)::int AS UserCount
    FROM users
    GROUP BY "TenantId"
) us ON us."TenantId" = t."Id"
LEFT JOIN (
    SELECT "TenantId", COUNT(*)::int AS TodoCount
    FROM todos
    GROUP BY "TenantId"
) ts ON ts."TenantId" = t."Id"
WHERE t."Id" = @TenantId;
