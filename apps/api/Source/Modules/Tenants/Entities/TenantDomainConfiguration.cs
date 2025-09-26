namespace GameGuild.Modules.Tenants;

/// <summary>
/// Entity Framework configuration for the TenantDomain entity
/// </summary>
public class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        // Table configuration
        builder.ToTable("tenant_domains");

        // Primary key
        builder.HasKey(td => td.Id);

        // Properties
        builder.Property(td => td.TopLevelDomain)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(td => td.Subdomain)
            .HasMaxLength(100);

        builder.Property(td => td.IsMainDomain)
            .HasDefaultValue(false);

        builder.Property(td => td.IsSecondaryDomain)
            .HasDefaultValue(false);

        builder.Property(td => td.TenantId)
            .IsRequired();

        // Indexes
        builder.HasIndex(td => new { td.TopLevelDomain, td.Subdomain })
            .IsUnique()
            .HasDatabaseName("ix_tenant_domains_toplevel_subdomain");

        builder.HasIndex(td => new { td.TenantId, td.IsMainDomain })
            .HasDatabaseName("ix_tenant_domains_tenant_main");

        // Unique constraint for main domain per tenant
        builder.HasIndex(td => new { td.TenantId, td.IsMainDomain })
            .IsUnique()
            .HasFilter("is_main_domain = true")
            .HasDatabaseName("ix_tenant_domains_unique_main");

        // Relationships
        builder.HasOne(td => td.Tenant)
            .WithMany()
            .HasForeignKey(td => td.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}