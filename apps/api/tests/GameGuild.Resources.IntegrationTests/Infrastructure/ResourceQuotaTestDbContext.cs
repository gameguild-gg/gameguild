using GameGuild.Abstractions;
using GameGuild.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Resources.IntegrationTests.Infrastructure;

/// <summary>
/// Lightweight test-specific DbContext for Resource quota integration tests.
/// Contains only the entities needed for quota testing, avoiding complex dependencies.
/// </summary>
public class ResourceQuotaTestDbContext : DbContext, IApplicationDbContext
{
    public ResourceQuotaTestDbContext(DbContextOptions<ResourceQuotaTestDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Resource Quotas DbSet
    /// </summary>
    public DbSet<ResourceQuota> ResourceQuotas => Set<ResourceQuota>();

    /// <summary>
    /// Usage Records DbSet
    /// </summary>
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Apply Resources module configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ResourceQuota).Assembly,
            type => type.Namespace?.StartsWith("GameGuild.Resources") == true &&
                    type.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

        base.OnModelCreating(modelBuilder);
    }
}
