using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Resources;

/// <summary>
///     Entity Type Configuration for UsageRetentionPolicy
/// </summary>
public class UsageRetentionPolicyConfiguration : IEntityTypeConfiguration<UsageRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<UsageRetentionPolicy> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("usage_retention_policies", "gameguild.resources");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.ResourceType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.RetentionDays).IsRequired().HasDefaultValue(90);
        builder.Property(x => x.ArchiveAfterDays).IsRequired().HasDefaultValue(30);
        builder.Property(x => x.EnableCompaction).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CompactionIntervalDays).IsRequired().HasDefaultValue(7);
        builder.Property(x => x.DownSamplingStrategy).HasMaxLength(50).IsRequired().HasDefaultValue("daily");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.LastExecutedAt);
        builder.Property(x => x.NextExecutionAt);
        builder.Property(x => x.Configuration).HasMaxLength(2000);

        // Indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_usageretentionpolicy_tenant_id");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("ix_usageretentionpolicy_is_active");
        builder.HasIndex(x => x.NextExecutionAt).HasDatabaseName("ix_usageretentionpolicy_next_execution");
    }
}
