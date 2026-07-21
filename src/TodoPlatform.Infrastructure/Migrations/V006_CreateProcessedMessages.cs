using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

[Migration(6, "V006_CreateProcessedMessages")]
public sealed class V006_CreateProcessedMessages : Migration
{
    public override void Up()
    {
        Create.Table("processed_messages")
            .WithColumn("MessageId").AsGuid().PrimaryKey()
            .WithColumn("ProcessedAt").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("processed_messages");
    }
}
