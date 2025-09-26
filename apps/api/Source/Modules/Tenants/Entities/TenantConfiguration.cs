namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for the Tenant entity
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table configuration
        builder.ToTable("tenants");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Property(t => t.IsDefault)
            .HasDefaultValue(false);        // Indexes
        builder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("ix_tenants_slug");

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("ix_tenants_name");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("ix_tenants_is_active");

        builder.HasIndex(t => t.IsDefault)
            .IsUnique()
            .HasFilter("is_default = true")
            .HasDatabaseName("ix_tenant_unique_default");
    }
}