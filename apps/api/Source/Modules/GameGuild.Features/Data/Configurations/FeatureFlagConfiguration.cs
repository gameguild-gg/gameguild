using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
///     Entity Type Configuration for FeatureFlag
/// </summary>
public class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("feature_flags");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure string properties
        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("key");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("name");

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(x => x.DefaultValue)
            .HasMaxLength(1000)
            .HasColumnName("default_value");

        builder.Property(x => x.EnabledValue)
            .HasMaxLength(1000)
            .HasColumnName("enabled_value");

        builder.Property(x => x.Environment)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("environment");

        builder.Property(x => x.Owner)
            .HasMaxLength(200)
            .HasColumnName("owner");

        builder.Property(x => x.EscalationContact)
            .HasMaxLength(500)
            .HasColumnName("escalation_contact");

        builder.Property(x => x.GovernanceNotes)
            .HasMaxLength(2000)
            .HasColumnName("governance_notes");

        // Configure boolean properties
        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled");

        builder.Property(x => x.IsGlobal)
            .HasColumnName("is_global");

        builder.Property(x => x.IsKillSwitch)
            .HasColumnName("is_kill_switch");

        builder.Property(x => x.RequiresEncryption)
            .HasColumnName("requires_encryption");

        // Configure enum properties
        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion<string>();

        // Configure numeric properties
        builder.Property(x => x.RolloutPercentage)
            .HasColumnName("rollout_percentage");

        // Configure datetime properties
        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.ReviewDate)
            .HasColumnName("review_date");

        // Configure relationships
        builder.HasMany(x => x.Targets)
            .WithOne(t => t.FeatureFlag)
            .HasForeignKey(t => t.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.UsageAnalytics)
            .WithOne(u => u.FeatureFlag)
            .HasForeignKey(u => u.FeatureFlagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("idx_feature_flags_key");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("idx_feature_flags_tenant_id");

        builder.HasIndex(x => x.Environment)
            .HasDatabaseName("idx_feature_flags_environment");

        builder.HasIndex(x => x.IsEnabled)
            .HasDatabaseName("idx_feature_flags_is_enabled");

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("idx_feature_flags_type");

        builder.HasIndex(x => new { x.Key, x.Environment })
            .HasDatabaseName("idx_feature_flags_key_environment");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("idx_feature_flags_expires_at");

        builder.HasIndex(x => x.ReviewDate)
            .HasDatabaseName("idx_feature_flags_review_date");
    }
}
