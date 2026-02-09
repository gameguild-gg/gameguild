using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for ResourceUsageTrend
/// </summary>
public class ResourceUsageTrendConfiguration : IEntityTypeConfiguration<ResourceUsageTrend>
{
    public void Configure(EntityTypeBuilder<ResourceUsageTrend> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("resource_usage_trends", "gameguild.resources");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.ResourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.AverageUsage).IsRequired();
        builder.Property(x => x.MinUsage).IsRequired();
        builder.Property(x => x.MaxUsage).IsRequired();
        builder.Property(x => x.StandardDeviation).IsRequired();
        builder.Property(x => x.GrowthRate).IsRequired();
        builder.Property(x => x.AnomalyCount).IsRequired();
        builder.Property(x => x.PeakUsageTime);
        builder.Property(x => x.Pattern).HasMaxLength(50).IsRequired().HasDefaultValue("Steady");
        builder.Property(x => x.PatternConfidence).IsRequired().HasDefaultValue(1.0);
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.Ignore(x => x.Type);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_resourceusagetrend_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.ResourceType }).HasDatabaseName("ix_resourceusagetrend_tenant_type");
        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).HasDatabaseName("ix_resourceusagetrend_period");
    }
}
