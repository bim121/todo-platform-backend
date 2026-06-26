using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

[Migration(2, "V002_CreateIndexes")]
public sealed class V002_CreateIndexes : Migration
{
    public override void Up()
    {
        Create.Index("IX_users_Email")
            .OnTable("users")
            .OnColumn("Email")
            .Ascending()
            .WithOptions()
            .Unique();

        Create.Index("IX_todos_UserId")
            .OnTable("todos")
            .OnColumn("UserId")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_todos_UserId").OnTable("todos");
        Delete.Index("IX_users_Email").OnTable("users");
    }
}
