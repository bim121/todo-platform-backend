using System.Data;
using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

[Migration(1, "V001_CreateUsersAndTodosTables")]
public sealed class V001_CreateUsersAndTodosTables : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString(256).NotNullable()
            .WithColumn("PasswordHash").AsString(512).NotNullable()
            .WithColumn("Name").AsString(200).NotNullable();

        Create.Table("todos")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Title").AsString(500).NotNullable()
            .WithColumn("Completed").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("Status").AsString(32).NotNullable()
            .WithColumn("Priority").AsString(16).NotNullable();

        Create.ForeignKey("FK_todos_users_UserId")
            .FromTable("todos").ForeignColumn("UserId")
            .ToTable("users").PrimaryColumn("Id")
            .OnDelete(Rule.Cascade);
    }

    public override void Down()
    {
        Delete.ForeignKey("FK_todos_users_UserId").OnTable("todos");
        Delete.Table("todos");
        Delete.Table("users");
    }
}
