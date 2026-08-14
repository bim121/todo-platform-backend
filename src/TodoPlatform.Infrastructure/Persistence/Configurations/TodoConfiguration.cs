using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Persistence.Configurations;

public class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.ToTable("todos");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Completed);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => new { t.UserId, t.Completed });
        builder.HasIndex(t => t.TenantId);

        // B-09.3 — matches GetTodos ActiveOnly (Completed = false)
        builder.HasIndex(t => t.UserId)
            .HasFilter("\"Completed\" = false")
            .HasDatabaseName("IX_todos_UserId_Active");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
