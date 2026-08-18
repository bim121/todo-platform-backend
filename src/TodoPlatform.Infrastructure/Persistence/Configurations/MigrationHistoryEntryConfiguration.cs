using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Persistence.Configurations;

public sealed class MigrationHistoryEntryConfiguration : IEntityTypeConfiguration<MigrationHistoryEntry>
{
    public void Configure(EntityTypeBuilder<MigrationHistoryEntry> builder)
    {
        builder.ToTable("migration_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Version)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(h => h.AppliedAt)
            .IsRequired();

        builder.Property(h => h.AppliedBy)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(h => h.TenantId);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
