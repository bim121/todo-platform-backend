using Microsoft.EntityFrameworkCore;
using TodoPlatform.Application.Tenancy;
using TodoPlatform.Domain.Entities;

namespace TodoPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext? _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>True when middleware (or seeder) resolved a tenant — used by global query filters.</summary>
    public bool TenantFilterEnabled => _tenantContext is { IsResolved: true };

    /// <summary>Current tenant for EF query filters. Empty when the filter is off.</summary>
    public Guid CurrentTenantId =>
        _tenantContext is { IsResolved: true } ? _tenantContext.TenantId : Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantSchemaVersion> TenantSchemaVersions => Set<TenantSchemaVersion>();

    public DbSet<MigrationHistoryEntry> MigrationHistory => Set<MigrationHistoryEntry>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Todo> Todos => Set<Todo>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // B-11 defense in depth: app filter + Postgres RLS. Off when tenant is unresolved
        // so unit tests / seeder bootstrap that construct AppDbContext(options) still see all rows.
        modelBuilder.Entity<Todo>().HasQueryFilter(t => !TenantFilterEnabled || t.TenantId == CurrentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(u => !TenantFilterEnabled || u.TenantId == CurrentTenantId);

        base.OnModelCreating(modelBuilder);
    }
}
