using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

[Migration(5, "V005_AddUsersKeycloakSub")]
public sealed class V005_AddUsersKeycloakSub : Migration
{
    public override void Up()
    {
        Alter.Table("users")
            .AddColumn("KeycloakSub").AsString(64).Nullable();

        Create.Index("IX_users_KeycloakSub")
            .OnTable("users")
            .OnColumn("KeycloakSub")
            .Unique();
    }

    public override void Down()
    {
        Delete.Index("IX_users_KeycloakSub").OnTable("users");
        Delete.Column("KeycloakSub").FromTable("users");
    }
}
