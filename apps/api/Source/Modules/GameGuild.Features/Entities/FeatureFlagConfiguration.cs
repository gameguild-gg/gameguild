using GameGuild.Features.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features.Persistence.Configurations;

/// <summary>
///     Entity configuration for FeatureFlag entity
/// </summary>
public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

        builder.Property(e => e.Description).HasMaxLength(500);

        builder.Property(e => e.IsEnabled);

        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

        builder.Property(e => e.DefaultValue).HasMaxLength(1000);

        builder.Property(e => e.EnabledValue).HasMaxLength(1000);

        builder.Property(e => e.IsGlobal);

        builder.Property(e => e.RolloutPercentage);

        builder.Property(e => e.Environment).HasMaxLength(50);

        builder.Property(e => e.TenantId);

        builder.Property(e => e.ExpiresAt);

        builder.Property(e => e.ReviewDate);

        // Relationships
        builder.HasMany(e => e.Targets).WithOne(t => t.FeatureFlag).HasForeignKey(t => t.FeatureFlagId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.UsageAnalytics).WithOne(u => u.FeatureFlag).HasForeignKey(u => u.FeatureFlagId).OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.Key).IsUnique();

        builder.HasIndex(e => e.IsEnabled);

        builder.HasIndex(e => e.Environment);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.IsGlobal);
    }
}
