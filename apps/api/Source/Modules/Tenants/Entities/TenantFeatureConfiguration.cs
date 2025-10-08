namespace GameGuild.Modules.Tenants;

/// <summary>
///     Entity Framework configuration for the TenantFeature entity
/// </summary>
public class TenantFeatureConfiguration : IEntityTypeConfiguration<TenantFeature>
{
    public void Configure(EntityTypeBuilder<TenantFeature> builder)
    {
        // Table configuration
        builder.ToTable("tenant_features");

        // Primary key
        builder.HasKey(tf => tf.Id);

        // Properties
        builder.Property(tf => tf.TenantId)
            .IsRequired();

        builder.Property(tf => tf.FeatureKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tf => tf.FeatureName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(tf => tf.Description)
            .HasMaxLength(1000);

        builder.Property(tf => tf.IsEnabled)
            .HasDefaultValue(false);

        builder.Property(tf => tf.Category)
            .HasMaxLength(100);

        builder.Property(tf => tf.UsageCount)
            .HasDefaultValue(0);

        builder.Property(tf => tf.Configuration)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(tf => new { tf.TenantId, tf.FeatureKey })
            .IsUnique()
            .HasDatabaseName("ix_tenant_features_tenant_key");

        builder.HasIndex(tf => tf.FeatureKey)
            .HasDatabaseName("ix_tenant_features_key");

        builder.HasIndex(tf => tf.IsEnabled)
            .HasDatabaseName("ix_tenant_features_enabled");

        builder.HasIndex(tf => tf.ExpiresAt)
            .HasDatabaseName("ix_tenant_features_expires_at");

        builder.HasIndex(tf => tf.Category)
            .HasDatabaseName("ix_tenant_features_category");

        // Relationships
        builder.HasOne(tf => tf.Tenant)
            .WithMany()
            .HasForeignKey(tf => tf.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete query filter
        builder.HasQueryFilter(tf => !tf.IsDeleted);
    }
}
