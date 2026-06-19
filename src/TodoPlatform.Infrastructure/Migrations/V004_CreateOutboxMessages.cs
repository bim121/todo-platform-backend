using FluentMigrator;

namespace TodoPlatform.Infrastructure.Migrations;

[Migration(4, "V004_CreateOutboxMessages")]
public sealed class V004_CreateOutboxMessages : Migration
{
    public override void Up()
    {
        Create.Table("outbox_messages")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Type").AsString(500).NotNullable()
            .WithColumn("Payload").AsCustom("jsonb").NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ProcessedAt").AsDateTimeOffset().Nullable();

        Create.Index("IX_outbox_messages_ProcessedAt")
            .OnTable("outbox_messages")
            .OnColumn("ProcessedAt")
            .Ascending();
    }

    public override void Down()
    {
        Delete.Index("IX_outbox_messages_ProcessedAt").OnTable("outbox_messages");
        Delete.Table("outbox_messages");
    }
}
