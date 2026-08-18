using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Persistence.Configurations;

public sealed class TenantSchemaVersionConfiguration : IEntityTypeConfiguration<TenantSchemaVersion>
{
    public void Configure(EntityTypeBuilder<TenantSchemaVersion> builder)
    {
        builder.ToTable("tenant_schema_versions");

        builder.HasKey(v => v.TenantId);

        builder.Property(v => v.Track)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(v => v.CurrentVersion)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
