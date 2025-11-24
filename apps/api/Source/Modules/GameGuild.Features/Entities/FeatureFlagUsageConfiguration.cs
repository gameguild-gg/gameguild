using GameGuild.Features.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features.Persistence.Configurations;

/// <summary>
///     Entity configuration for FeatureFlagUsage entity
/// </summary>
public class FeatureFlagUsageConfiguration : IEntityTypeConfiguration<FeatureFlagUsage>
{
    public void Configure(EntityTypeBuilder<FeatureFlagUsage> builder)
    {
        builder.ToTable("feature_flag_usage");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FeatureFlagId).IsRequired();

        builder.Property(e => e.UserId);

        builder.Property(e => e.TenantId);

        builder.Property(e => e.WasEnabled);

        builder.Property(e => e.ReturnedValue).HasMaxLength(1000);

        builder.Property(e => e.Environment).HasMaxLength(50);

        builder.Property(e => e.ContextData);

        // Indexes for analytics queries
        builder.HasIndex(e => e.FeatureFlagId);

        builder.HasIndex(e => e.UserId);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.CreatedAt);

        builder.HasIndex(e => new { e.FeatureFlagId, e.CreatedAt });

        builder.HasIndex(e => e.WasEnabled);
    }
}
