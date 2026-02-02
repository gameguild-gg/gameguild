using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
///     Entity Type Configuration for FeatureFlagTarget
/// </summary>
public class FeatureFlagTargetConfiguration : IEntityTypeConfiguration<FeatureFlagTarget>
{
    public void Configure(EntityTypeBuilder<FeatureFlagTarget> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("feature_flag_targets");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure foreign key
        builder.Property(x => x.FeatureFlagId)
            .IsRequired()
            .HasColumnName("feature_flag_id");

        // Configure string properties
        builder.Property(x => x.TargetType)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("target_type");

        builder.Property(x => x.TargetIdentifier)
            .IsRequired()
            .HasMaxLength(255)
            .HasColumnName("target_identifier");

        builder.Property(x => x.CustomValue)
            .HasMaxLength(1000)
            .HasColumnName("custom_value");

        builder.Property(x => x.Metadata)
            .HasMaxLength(2000)
            .HasColumnName("metadata");

        builder.Property(x => x.DependsOn)
            .HasMaxLength(255)
            .HasColumnName("depends_on");

        // Configure boolean properties
        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled");

        // Configure numeric properties
        builder.Property(x => x.RolloutPercentage)
            .HasColumnName("rollout_percentage");

        builder.Property(x => x.Priority)
            .HasColumnName("priority");

        // Configure relationship
        builder.HasOne(x => x.FeatureFlag)
            .WithMany(f => f.Targets)
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(x => x.FeatureFlagId)
            .HasDatabaseName("idx_feature_flag_targets_feature_flag_id");

        builder.HasIndex(x => x.TargetType)
            .HasDatabaseName("idx_feature_flag_targets_target_type");

        builder.HasIndex(x => x.TargetIdentifier)
            .HasDatabaseName("idx_feature_flag_targets_target_identifier");

        builder.HasIndex(x => new { x.FeatureFlagId, x.TargetType, x.TargetIdentifier })
            .IsUnique()
            .HasDatabaseName("idx_feature_flag_targets_unique");

        builder.HasIndex(x => x.Priority)
            .HasDatabaseName("idx_feature_flag_targets_priority");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("idx_feature_flag_targets_tenant_id");
    }
}
