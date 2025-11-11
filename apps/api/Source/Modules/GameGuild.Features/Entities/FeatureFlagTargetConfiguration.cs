using GameGuild.Features.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features.Persistence.Configurations;

/// <summary>
///     Entity configuration for FeatureFlagTarget entity
/// </summary>
public class FeatureFlagTargetConfiguration : IEntityTypeConfiguration<FeatureFlagTarget>
{
    public void Configure(EntityTypeBuilder<FeatureFlagTarget> builder)
    {
        builder.ToTable("feature_flag_targets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FeatureFlagId).IsRequired();

        builder.Property(e => e.TargetType).IsRequired().HasMaxLength(50);

        builder.Property(e => e.TargetIdentifier).IsRequired().HasMaxLength(255);

        builder.Property(e => e.IsEnabled);

        builder.Property(e => e.RolloutPercentage);

        builder.Property(e => e.CustomValue).HasMaxLength(1000);

        builder.Property(e => e.Metadata).HasMaxLength(2000);

        builder.Property(e => e.Priority);

        // Indexes
        builder.HasIndex(e => new { e.FeatureFlagId, e.TargetType, e.TargetIdentifier });

        builder.HasIndex(e => e.TargetType);

        builder.HasIndex(e => e.Priority);
    }
}
