using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.2 — logical catalog entry for beta track. Physical DDL runs in tenant schema via T1012.
/// Global <c>MigrateUp</c> skips beta-tagged migrations.
/// </summary>
[Tags("beta")]
[Migration(12, "V012_BetaFeaturePreview")]
public sealed class V012_BetaFeaturePreview : Migration
{
    public override void Up()
    {
        // Physical CREATE TABLE runs in tenant_* via T1012 / ITenantMigrationRunner.
    }

    public override void Down()
    {
    }
}
