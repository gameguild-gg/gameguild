namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for the TenantSettings entity
/// </summary>
public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        // Table configuration
        builder.ToTable("tenant_settings");

        // Primary key
        builder.HasKey(ts => ts.Id);

        // Properties
        builder.Property(ts => ts.DefaultLanguage).IsRequired().HasMaxLength(10).HasDefaultValue("en-US");

        builder.Property(ts => ts.DefaultTimezone).IsRequired().HasMaxLength(50).HasDefaultValue("UTC");

        // Foreign key
        builder.Property(ts => ts.TenantId).IsRequired(false);

        // Indexes
        builder.HasIndex(ts => ts.TenantId).IsUnique().HasDatabaseName("ix_tenant_settings_tenant_id");

        // Relationships
        builder.HasOne(ts => ts.Tenant).WithMany().HasForeignKey(ts => ts.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}
