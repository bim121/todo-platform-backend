using FluentMigrator;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-11.1–2 — tenants table, TenantId backfill, RLS on todos/users.
/// Plan named this V007; that number was already used → V009.
/// Columns stay quoted PascalCase to match existing FluentMigrator tables.
/// </summary>
[Migration(9, "V009_TenantsAndRowLevelSecurity")]
public sealed class V009_TenantsAndRowLevelSecurity : Migration
{
    public override void Up()
    {
        Create.Table("tenants")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Slug").AsString(64).NotNullable()
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Status").AsString(16).NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable();

        Create.Index("IX_tenants_Slug")
            .OnTable("tenants")
            .OnColumn("Slug")
            .Unique();

        Execute.Sql(
            $"""
            INSERT INTO tenants ("Id", "Slug", "Name", "Status", "CreatedAt")
            VALUES
              ('{WellKnownTenants.DefaultId}', '{WellKnownTenants.DefaultSlug}', '{WellKnownTenants.DefaultName}', 'Active', TIMESTAMPTZ '2026-01-01 00:00:00+00'),
              ('{WellKnownTenants.AcmeId}', '{WellKnownTenants.AcmeSlug}', '{WellKnownTenants.AcmeName}', 'Active', TIMESTAMPTZ '2026-01-01 00:00:00+00');
            """);

        Alter.Table("users")
            .AddColumn("TenantId").AsGuid().Nullable();
        Alter.Table("todos")
            .AddColumn("TenantId").AsGuid().Nullable();

        Execute.Sql(
            $"""
            UPDATE users SET "TenantId" = '{WellKnownTenants.DefaultId}' WHERE "TenantId" IS NULL;
            UPDATE todos SET "TenantId" = '{WellKnownTenants.DefaultId}' WHERE "TenantId" IS NULL;
            """);

        Alter.Table("users")
            .AlterColumn("TenantId").AsGuid().NotNullable();
        Alter.Table("todos")
            .AlterColumn("TenantId").AsGuid().NotNullable();

        Create.ForeignKey("FK_users_tenants_TenantId")
            .FromTable("users").ForeignColumn("TenantId")
            .ToTable("tenants").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        Create.ForeignKey("FK_todos_tenants_TenantId")
            .FromTable("todos").ForeignColumn("TenantId")
            .ToTable("tenants").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        Delete.Index("IX_users_Email").OnTable("users");
        Delete.Index("IX_users_KeycloakSub").OnTable("users");

        Create.Index("IX_users_TenantId_Email")
            .OnTable("users")
            .OnColumn("TenantId").Ascending()
            .OnColumn("Email").Ascending()
            .WithOptions()
            .Unique();

        Execute.Sql(
            """
            CREATE UNIQUE INDEX "IX_users_TenantId_KeycloakSub"
            ON users ("TenantId", "KeycloakSub")
            WHERE "KeycloakSub" IS NOT NULL;
            """);

        Create.Index("IX_todos_TenantId")
            .OnTable("todos")
            .OnColumn("TenantId");

        Execute.Sql(
            """
            CREATE OR REPLACE VIEW v_todo_stats_by_user AS
            SELECT
                "UserId",
                "TenantId",
                COUNT(*)::int AS "Total",
                COUNT(*) FILTER (WHERE NOT "Completed")::int AS "Active",
                COUNT(*) FILTER (WHERE "Completed")::int AS "Completed"
            FROM todos
            GROUP BY "UserId", "TenantId";
            """);

        // RLS: FORCE so the table owner cannot skip policies (non-superuser).
        // Docker POSTGRES_USER is typically superuser and still bypasses RLS — B-11.7.
        Execute.Sql(
            """
            ALTER TABLE todos ENABLE ROW LEVEL SECURITY;
            ALTER TABLE todos FORCE ROW LEVEL SECURITY;
            CREATE POLICY todos_tenant_isolation ON todos
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);

            ALTER TABLE users ENABLE ROW LEVEL SECURITY;
            ALTER TABLE users FORCE ROW LEVEL SECURITY;
            CREATE POLICY users_tenant_isolation ON users
                FOR ALL
                USING ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid)
                WITH CHECK ("TenantId" = NULLIF(current_setting('app.current_tenant', true), '')::uuid);
            """);
    }

    public override void Down()
    {
        Execute.Sql(
            """
            DROP POLICY IF EXISTS todos_tenant_isolation ON todos;
            ALTER TABLE todos NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE todos DISABLE ROW LEVEL SECURITY;

            DROP POLICY IF EXISTS users_tenant_isolation ON users;
            ALTER TABLE users NO FORCE ROW LEVEL SECURITY;
            ALTER TABLE users DISABLE ROW LEVEL SECURITY;
            """);

        Execute.Sql(
            """
            CREATE OR REPLACE VIEW v_todo_stats_by_user AS
            SELECT
                "UserId",
                COUNT(*)::int AS "Total",
                COUNT(*) FILTER (WHERE NOT "Completed")::int AS "Active",
                COUNT(*) FILTER (WHERE "Completed")::int AS "Completed"
            FROM todos
            GROUP BY "UserId";
            """);

        Execute.Sql("""DROP INDEX IF EXISTS "IX_todos_TenantId";""");
        Execute.Sql("""DROP INDEX IF EXISTS "IX_users_TenantId_KeycloakSub";""");
        Delete.Index("IX_users_TenantId_Email").OnTable("users");

        Execute.Sql(
            """
            CREATE UNIQUE INDEX "IX_users_Email" ON users ("Email");
            CREATE UNIQUE INDEX "IX_users_KeycloakSub" ON users ("KeycloakSub")
            WHERE "KeycloakSub" IS NOT NULL;
            """);

        Delete.ForeignKey("FK_todos_tenants_TenantId").OnTable("todos");
        Delete.ForeignKey("FK_users_tenants_TenantId").OnTable("users");
        Delete.Column("TenantId").FromTable("todos");
        Delete.Column("TenantId").FromTable("users");
        Delete.Table("tenants");
    }
}
