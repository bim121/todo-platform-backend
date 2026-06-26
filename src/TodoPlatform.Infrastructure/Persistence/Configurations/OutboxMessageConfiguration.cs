using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TodoPlatform.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.HasIndex(m => m.ProcessedAt);
    }
}
