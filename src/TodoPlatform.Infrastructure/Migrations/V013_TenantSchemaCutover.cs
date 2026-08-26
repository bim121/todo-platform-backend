using FluentMigrator;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.13 — per-tenant PostgreSQL schemas, data cutover from public, SchemaName column.
/// </summary>
[Tags("platform")]
[Migration(13, "V013_TenantSchemaCutover")]
public sealed class V013_TenantSchemaCutover : Migration
{
    public override void Up()
    {
        Alter.Table("tenants")
            .AddColumn("SchemaName").AsString(128).Nullable();

        CutoverTenant(WellKnownTenants.DefaultId, WellKnownTenants.DefaultSlug);
        CutoverTenant(WellKnownTenants.AcmeId, WellKnownTenants.AcmeSlug);

        Alter.Table("tenants")
            .AlterColumn("SchemaName").AsString(128).NotNullable();

        Create.Index("IX_tenants_SchemaName")
            .OnTable("tenants")
            .OnColumn("SchemaName")
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("IX_tenants_SchemaName").OnTable("tenants");
        Delete.Column("SchemaName").FromTable("tenants");

        Execute.Sql(
            """
            DROP SCHEMA IF EXISTS tenant_default CASCADE;
            DROP SCHEMA IF EXISTS tenant_acme_corp CASCADE;
            """);
    }

    private void CutoverTenant(Guid tenantId, string slug)
    {
        var schemaName = TenantSchemaNaming.FromSlug(slug);

        Execute.Sql($"""CREATE SCHEMA IF NOT EXISTS "{schemaName}";""");
        Execute.Sql(TenantSchemaBaselineSql.ForSchema(schemaName));

        Execute.Sql(
            $"""
            INSERT INTO "{schemaName}".users ("Id", "Email", "PasswordHash", "Name", "KeycloakSub", "TenantId")
            SELECT "Id", "Email", "PasswordHash", "Name", "KeycloakSub", "TenantId"
            FROM public.users
            WHERE "TenantId" = '{tenantId}';

            INSERT INTO "{schemaName}".todos ("Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId")
            SELECT "Id", "Title", "Completed", "UserId", "Status", "Priority", "TenantId"
            FROM public.todos
            WHERE "TenantId" = '{tenantId}';

            UPDATE public.tenants SET "SchemaName" = '{schemaName}' WHERE "Id" = '{tenantId}';

            CREATE TABLE IF NOT EXISTS "{schemaName}"."VersionInfo" (
                "Version" bigint NOT NULL PRIMARY KEY,
                "AppliedOn" timestamptz NULL,
                "Description" varchar(1024) NULL
            );

            INSERT INTO "{schemaName}"."VersionInfo" ("Version", "AppliedOn", "Description")
            VALUES ({TenantPhysicalMigrationVersions.Baseline}, NOW(), 'T1001_TenantSchemaBaseline')
            ON CONFLICT ("Version") DO NOTHING;
            """);
    }
}
