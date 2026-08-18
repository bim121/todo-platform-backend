using FluentMigrator;
using TodoPlatform.Domain.Tenancy;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.1 — per-tenant logical schema version + apply history.
/// Plan named this V008; that number was already used → V011.
/// </summary>
[Migration(11, "V011_TenantSchemaVersions")]
public sealed class V011_TenantSchemaVersions : Migration
{
    public override void Up()
    {
        Create.Table("tenant_schema_versions")
            .WithColumn("TenantId").AsGuid().PrimaryKey()
            .WithColumn("Track").AsString(16).NotNullable()
            .WithColumn("CurrentVersion").AsInt64().NotNullable()
            .WithColumn("UpdatedAt").AsDateTimeOffset().NotNullable();

        Create.ForeignKey("FK_tenant_schema_versions_tenants_TenantId")
            .FromTable("tenant_schema_versions").ForeignColumn("TenantId")
            .ToTable("tenants").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.Table("migration_history")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("TenantId").AsGuid().NotNullable()
            .WithColumn("Version").AsString(64).NotNullable()
            .WithColumn("AppliedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("AppliedBy").AsString(128).NotNullable();

        Create.Index("IX_migration_history_TenantId")
            .OnTable("migration_history")
            .OnColumn("TenantId");

        Create.ForeignKey("FK_migration_history_tenants_TenantId")
            .FromTable("migration_history").ForeignColumn("TenantId")
            .ToTable("tenants").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // All existing tenants start on stable at this migration (latest untagged / stable).
        Execute.Sql(
            $"""
            INSERT INTO tenant_schema_versions ("TenantId", "Track", "CurrentVersion", "UpdatedAt")
            SELECT t."Id", '{MigrationTracks.Stable}', 11, TIMESTAMPTZ '2026-01-01 00:00:00+00'
            FROM tenants t
            WHERE NOT EXISTS (
                SELECT 1 FROM tenant_schema_versions v WHERE v."TenantId" = t."Id"
            );
            """);
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_migration_history_tenants_TenantId").OnTable("migration_history");
        Delete.Index("IX_migration_history_TenantId").OnTable("migration_history");
        Delete.Table("migration_history");

        Delete.ForeignKey("FK_tenant_schema_versions_tenants_TenantId").OnTable("tenant_schema_versions");
        Delete.Table("tenant_schema_versions");
    }
}
