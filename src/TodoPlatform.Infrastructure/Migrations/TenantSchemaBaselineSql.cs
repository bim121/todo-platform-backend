namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// DDL for tenant-owned tables (V001–V011 shape). Uses current <c>search_path</c> (B-12.12).
/// </summary>
internal static class TenantSchemaBaselineSql
{
    public static string Up() => Build(null);

    public static string ForSchema(string schemaName) => Build(schemaName);

    private static string Build(string? schemaName)
    {
        var q = string.IsNullOrWhiteSpace(schemaName) ? string.Empty : $"\"{schemaName}\".";
        return $"""
            CREATE TABLE IF NOT EXISTS {q}users (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Email" varchar(256) NOT NULL,
                "PasswordHash" varchar(512) NOT NULL,
                "Name" varchar(200) NOT NULL,
                "KeycloakSub" varchar(64) NULL,
                "TenantId" uuid NOT NULL
            );

            CREATE TABLE IF NOT EXISTS {q}todos (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Title" varchar(500) NOT NULL,
                "Completed" boolean NOT NULL DEFAULT false,
                "UserId" uuid NOT NULL,
                "Status" varchar(32) NOT NULL,
                "Priority" varchar(16) NOT NULL,
                "TenantId" uuid NOT NULL,
                CONSTRAINT "FK_todos_users_UserId" FOREIGN KEY ("UserId") REFERENCES {q}users ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_users_TenantId_Email" ON {q}users ("TenantId", "Email");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_TenantId_KeycloakSub"
                ON {q}users ("TenantId", "KeycloakSub")
                WHERE "KeycloakSub" IS NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_todos_Completed" ON {q}todos ("Completed");
            CREATE INDEX IF NOT EXISTS "IX_todos_Status" ON {q}todos ("Status");
            CREATE INDEX IF NOT EXISTS "IX_todos_UserId_Completed" ON {q}todos ("UserId", "Completed");
            CREATE INDEX IF NOT EXISTS "IX_todos_TenantId" ON {q}todos ("TenantId");
            CREATE INDEX IF NOT EXISTS "IX_todos_UserId" ON {q}todos ("UserId");

            CREATE OR REPLACE VIEW {q}v_todo_stats_by_user AS
            SELECT
                "UserId",
                "TenantId",
                COUNT(*)::int AS "Total",
                COUNT(*) FILTER (WHERE NOT "Completed")::int AS "Active",
                COUNT(*) FILTER (WHERE "Completed")::int AS "Completed"
            FROM {q}todos
            GROUP BY "UserId", "TenantId";

            ALTER TABLE {q}todos ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {q}todos FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS todos_tenant_isolation ON {q}todos;
            CREATE POLICY todos_tenant_isolation ON {q}todos
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);

            ALTER TABLE {q}users ENABLE ROW LEVEL SECURITY;
            ALTER TABLE {q}users FORCE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS users_tenant_isolation ON {q}users;
            CREATE POLICY users_tenant_isolation ON {q}users
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);
            """;
    }
}
