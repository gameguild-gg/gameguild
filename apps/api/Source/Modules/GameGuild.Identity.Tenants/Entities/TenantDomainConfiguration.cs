using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Tenants;

/// <summary>
///     Entity Framework configuration for TenantDomain entity
/// </summary>
public class TenantDomainConfiguration : IEntityTypeConfiguration<TenantDomain>
{
    public void Configure(EntityTypeBuilder<TenantDomain> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(td => td.Id);

        // Properties
        builder.Property(td => td.TenantId).IsRequired();
        builder.Property(td => td.TopLevelDomain).IsRequired().HasMaxLength(255);
        builder.Property(td => td.Subdomain).HasMaxLength(100).IsRequired(false);
        builder.Property(td => td.IsMainDomain).IsRequired();
        builder.Property(td => td.IsSecondaryDomain).IsRequired();
        builder.Property(td => td.UserGroupId).IsRequired(false);

        // Soft delete query filter
        builder.HasQueryFilter(td => td.DeletedAt == null);

        // Relationships
        builder.HasOne(td => td.Tenant).WithMany(t => t.TenantDomains).HasForeignKey(td => td.TenantId).OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(td => new { td.TopLevelDomain, td.Subdomain }).IsUnique();
        builder.HasIndex(td => new { td.TenantId, td.IsMainDomain });
    }
}
