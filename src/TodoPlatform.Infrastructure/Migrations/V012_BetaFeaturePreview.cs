using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// B-12.2 — demo beta-tagged migration. Default <c>MigrateUp</c> skips it
/// (runner tags = stable + untagged). Logical pending item for beta-track tenants.
/// </summary>
[Tags("beta")]
[Migration(12, "V012_BetaFeaturePreview")]
public sealed class V012_BetaFeaturePreview : Migration
{
    public override void Up()
    {
        Create.Table("beta_preview_flags")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Name").AsString(64).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("beta_preview_flags");
    }
}
