using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-11.7 — optional admin bypass GUC for platform-wide reads (system stats).
/// Superusers still bypass RLS regardless; this is for the non-superuser app role.
/// </summary>
[Migration(10, "V010_AdminRlsBypass")]
public sealed class V010_AdminRlsBypass : Migration
{
    public override void Up()
    {
        Execute.Sql(
            """
            DROP POLICY IF EXISTS todos_tenant_isolation ON todos;
            CREATE POLICY todos_tenant_isolation ON todos
                FOR ALL
                USING (
                    current_setting('app.bypass_rls', true) = 'true'
                    OR "TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid
                )
                WITH CHECK (
                    current_setting('app.bypass_rls', true) = 'true'
                    OR "TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid
                );

            DROP POLICY IF EXISTS users_tenant_isolation ON users;
            CREATE POLICY users_tenant_isolation ON users
                FOR ALL
                USING (
                    current_setting('app.bypass_rls', true) = 'true'
                    OR "TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid
                )
                WITH CHECK (
                    current_setting('app.bypass_rls', true) = 'true'
                    OR "TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid
                );
            """);
    }

    public override void Down()
    {
        Execute.Sql(
            """
            DROP POLICY IF EXISTS todos_tenant_isolation ON todos;
            CREATE POLICY todos_tenant_isolation ON todos
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);

            DROP POLICY IF EXISTS users_tenant_isolation ON users;
            CREATE POLICY users_tenant_isolation ON users
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);
            """);
    }
}
