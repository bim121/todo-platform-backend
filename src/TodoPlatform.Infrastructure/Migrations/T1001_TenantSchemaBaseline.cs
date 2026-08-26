using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.12 — tenant-stream baseline (logical V001–V011). Runs inside <c>tenant_*</c> via search_path.
/// </summary>
[Tags("tenant")]
[Migration(TenantPhysicalMigrationVersions.Baseline, "T1001_TenantSchemaBaseline")]
public sealed class T1001_TenantSchemaBaseline : Migration
{
    public override void Up() => Execute.Sql(TenantSchemaBaselineSql.Up());

    public override void Down()
    {
        Execute.Sql(
            """
            DROP VIEW IF EXISTS v_todo_stats_by_user;
            DROP TABLE IF EXISTS todos;
            DROP TABLE IF EXISTS users;
            """);
    }
}
