using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for ResourceThrottlingPolicy
/// </summary>
public class ResourceThrottlingPolicyConfiguration : IEntityTypeConfiguration<ResourceThrottlingPolicy>
{
    public void Configure(EntityTypeBuilder<ResourceThrottlingPolicy> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("ResourceThrottlingPolicies", "gameguild.resources");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.ResourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Strategy).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.ThrottlingThresholdPercent).IsRequired().HasDefaultValue(80);
        builder.Property(x => x.MaxRequestsPerWindow);
        builder.Property(x => x.WindowDurationSeconds);
        builder.Property(x => x.DegradationFactor).HasColumnType("decimal(5,2)").IsRequired().HasDefaultValue(0.5m);
        builder.Property(x => x.PriorityThreshold);
        builder.Property(x => x.Configuration).HasMaxLength(2000);
        builder.Ignore(x => x.Threshold);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_resourcethrottlingpolicy_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.ResourceType }).HasDatabaseName("ix_resourcethrottlingpolicy_tenant_type");
    }
}
