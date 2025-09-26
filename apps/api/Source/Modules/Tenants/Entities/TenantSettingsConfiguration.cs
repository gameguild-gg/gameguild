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
        _ = builder.Property(ts => ts.DefaultLanguageId).IsRequired();

        _ = builder.Property(ts => ts.DefaultTimezone).IsRequired().HasMaxLength(50).HasDefaultValue("UTC");

        // Foreign key
        _ = builder.Property(ts => ts.TenantId).IsRequired(false);

        // Indexes
        _ = builder.HasIndex(ts => ts.TenantId).IsUnique().HasDatabaseName("ix_tenant_settings_tenant_id");

        // Relationships
        _ = builder.HasOne(ts => ts.Tenant).WithMany().HasForeignKey(ts => ts.TenantId).OnDelete(DeleteBehavior.Cascade);
        _ = builder.HasOne(ts => ts.DefaultLanguage).WithMany().HasForeignKey(ts => ts.DefaultLanguageId).OnDelete(DeleteBehavior.Restrict);
    }
}
