using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
///     Entity Type Configuration for FeatureFlagUsage
/// </summary>
public class FeatureFlagUsageConfiguration : IEntityTypeConfiguration<FeatureFlagUsage>
{
    public void Configure(EntityTypeBuilder<FeatureFlagUsage> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("feature_flag_usage");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure foreign key
        builder.Property(x => x.FeatureFlagId)
            .IsRequired()
            .HasColumnName("feature_flag_id");

        // Configure GUID properties
        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        // Configure string properties
        builder.Property(x => x.Environment)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("environment");

        builder.Property(x => x.ReturnedValue)
            .HasMaxLength(1000)
            .HasColumnName("returned_value");

        builder.Property(x => x.ContextData)
            .HasMaxLength(2000)
            .HasColumnName("context_data");

        // Configure boolean properties
        builder.Property(x => x.WasEnabled)
            .HasColumnName("was_enabled");

        // Configure numeric properties
        builder.Property(x => x.AccessCount)
            .HasColumnName("access_count");

        // Configure datetime properties
        builder.Property(x => x.FirstAccessAt)
            .IsRequired()
            .HasColumnName("first_access_at");

        builder.Property(x => x.LastAccessAt)
            .IsRequired()
            .HasColumnName("last_access_at");

        // Configure relationship
        builder.HasOne(x => x.FeatureFlag)
            .WithMany(f => f.UsageAnalytics)
            .HasForeignKey(x => x.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for analytics queries
        builder.HasIndex(x => x.FeatureFlagId)
            .HasDatabaseName("idx_feature_flag_usage_feature_flag_id");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("idx_feature_flag_usage_tenant_id");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_feature_flag_usage_user_id");

        builder.HasIndex(x => x.Environment)
            .HasDatabaseName("idx_feature_flag_usage_environment");

        builder.HasIndex(x => x.LastAccessAt)
            .HasDatabaseName("idx_feature_flag_usage_last_access_at");

        builder.HasIndex(x => new { x.FeatureFlagId, x.TenantId, x.Environment })
            .HasDatabaseName("idx_feature_flag_usage_composite");

        // Data retention index for cleanup queries
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_feature_flag_usage_created_at");
    }
}
