using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameGuild.Resources;

/// <summary>
///     DbContext for Resources module
/// </summary>
[ExcludeFromCodeCoverage]
public class ResourcesDbContext(DbContextOptions<ResourcesDbContext> options) : DbContext(options), IApplicationDbContext
{
    // Core entities
    public DbSet<ResourceQuota> ResourceQuotas { get => Set<ResourceQuota>(); }

    public DbSet<UsageRecord> UsageRecords { get => Set<UsageRecord>(); }

    // Extended entities
    public DbSet<CostAllocationReport> CostAllocationReports { get => Set<CostAllocationReport>(); }

    public DbSet<ResourceUsageTrend> ResourceUsageTrends { get => Set<ResourceUsageTrend>(); }

    public DbSet<ResourceThrottlingPolicy> ResourceThrottlingPolicies { get => Set<ResourceThrottlingPolicy>(); }

    public DbSet<UsageRetentionPolicy> UsageRetentionPolicies { get => Set<UsageRetentionPolicy>(); }

    public DbSet<SlaImpactAnalysis> SlaImpactAnalyses { get => Set<SlaImpactAnalysis>(); }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) { return await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false); }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourcesDbContext).Assembly, type => type.Namespace?.StartsWith("GameGuild.Resources.Entities") == true);
    }
}
