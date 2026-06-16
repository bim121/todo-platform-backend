using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

/// <summary>
/// Indexes aligned with B-04 specifications: UserId filter, ActiveTodos (Completed), Status filters.
/// </summary>
[Migration(3, "V003_TodoSpecificationIndexes")]
public sealed class V003_TodoSpecificationIndexes : Migration
{
    public override void Up()
    {
        Create.Index("IX_todos_Completed")
            .OnTable("todos")
            .OnColumn("Completed")
            .Ascending();

        Create.Index("IX_todos_Status")
            .OnTable("todos")
            .OnColumn("Status")
            .Ascending();

        Create.Index("IX_todos_UserId_Completed")
            .OnTable("todos")
            .OnColumn("UserId").Ascending()
            .OnColumn("Completed").Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_todos_UserId_Completed").OnTable("todos");
        Delete.Index("IX_todos_Status").OnTable("todos");
        Delete.Index("IX_todos_Completed").OnTable("todos");
    }
}
