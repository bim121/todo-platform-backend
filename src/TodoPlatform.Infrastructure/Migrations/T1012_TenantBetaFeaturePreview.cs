using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.12 — physical beta DDL in tenant schema (logical V012 catalog entry stays separate).
/// </summary>
[Tags("tenant", "beta")]
[Migration(TenantPhysicalMigrationVersions.BetaFeaturePreview, "T1012_TenantBetaFeaturePreview")]
public sealed class T1012_TenantBetaFeaturePreview : Migration
{
    public override void Up()
    {
        Create.Table("beta_preview_flags")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(64).NotNullable();
    }

    public override void Down() => Delete.Table("beta_preview_flags");
}
