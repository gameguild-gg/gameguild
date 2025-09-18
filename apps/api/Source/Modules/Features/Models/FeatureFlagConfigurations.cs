using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Features.Models;

public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag> {
  public void Configure(EntityTypeBuilder<FeatureFlag> builder) {
    builder.ToTable("FeatureFlags");

    builder.HasKey(e => e.Id);

    builder.Property(e => e.Key).IsRequired().HasMaxLength(100);

    builder.Property(e => e.Name).IsRequired().HasMaxLength(200);

    builder.Property(e => e.Description).HasMaxLength(500);

    builder.Property(e => e.IsEnabled);

    builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(50);

    builder.Property(e => e.DefaultValue).HasMaxLength(1000);

    builder.Property(e => e.EnabledValue).HasMaxLength(1000);

    builder.Property(e => e.IsGlobal);

    builder.Property(e => e.RolloutPercentage).HasDefaultValue(100);

    builder.Property(e => e.Environment).HasMaxLength(50).HasDefaultValue("production");

    builder.Property(e => e.TenantId);

    // Relationships
    builder.HasMany(e => e.Targets).WithOne(t => t.FeatureFlag).HasForeignKey(t => t.FeatureFlagId).OnDelete(DeleteBehavior.Cascade);

    builder.HasMany(e => e.UsageAnalytics).WithOne(u => u.FeatureFlag).HasForeignKey(u => u.FeatureFlagId).OnDelete(DeleteBehavior.Cascade);

    // Indexes
    builder.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_FeatureFlags_Key");

    builder.HasIndex(e => e.IsEnabled).HasDatabaseName("IX_FeatureFlags_IsEnabled");

    builder.HasIndex(e => e.Environment).HasDatabaseName("IX_FeatureFlags_Environment");

    builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_FeatureFlags_TenantId");

    builder.HasIndex(e => e.IsGlobal).HasDatabaseName("IX_FeatureFlags_IsGlobal");
  }
}

public class FeatureFlagTargetConfiguration : IEntityTypeConfiguration<FeatureFlagTarget> {
  public void Configure(EntityTypeBuilder<FeatureFlagTarget> builder) {
    builder.ToTable("FeatureFlagTargets");

    builder.HasKey(e => e.Id);

    builder.Property(e => e.FeatureFlagId).IsRequired();

    builder.Property(e => e.TargetType).IsRequired().HasMaxLength(50);

    builder.Property(e => e.TargetIdentifier).IsRequired().HasMaxLength(255);

    builder.Property(e => e.IsIncluded).HasDefaultValue(true);

    builder.Property(e => e.Value).HasMaxLength(1000);

    // Indexes
    builder.HasIndex(e => new { e.FeatureFlagId, e.TargetType, e.TargetIdentifier }).HasDatabaseName("IX_FeatureFlagTargets_FeatureFlagId_TargetType_TargetIdentifier");

    builder.HasIndex(e => e.TargetType).HasDatabaseName("IX_FeatureFlagTargets_TargetType");
  }
}

public class FeatureFlagUsageConfiguration : IEntityTypeConfiguration<FeatureFlagUsage> {
  public void Configure(EntityTypeBuilder<FeatureFlagUsage> builder) {
    builder.ToTable("FeatureFlagUsage");

    builder.HasKey(e => e.Id);

    builder.Property(e => e.FeatureFlagId).IsRequired();

    builder.Property(e => e.UserId);

    builder.Property(e => e.TenantId);

    builder.Property(e => e.WasEnabled);

    builder.Property(e => e.ReturnedValue).HasMaxLength(1000);

    builder.Property(e => e.Environment).HasMaxLength(50);

    builder.Property(e => e.Reason).HasMaxLength(500);

    builder.Property(e => e.ContextData).HasColumnType("text");

    // Indexes for analytics queries
    builder.HasIndex(e => e.FeatureFlagId).HasDatabaseName("IX_FeatureFlagUsage_FeatureFlagId");

    builder.HasIndex(e => e.UserId).HasDatabaseName("IX_FeatureFlagUsage_UserId");

    builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_FeatureFlagUsage_TenantId");

    builder.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_FeatureFlagUsage_CreatedAt");

    builder.HasIndex(e => new { e.FeatureFlagId, e.CreatedAt }).HasDatabaseName("IX_FeatureFlagUsage_FeatureFlagId_CreatedAt");

    builder.HasIndex(e => e.WasEnabled).HasDatabaseName("IX_FeatureFlagUsage_WasEnabled");
  }
}
