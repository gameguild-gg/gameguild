using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Entities;

/// <summary>
///     EntityBase Framework configuration for Tenant entity
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);

        builder.Property(t => t.Slug).IsRequired().HasMaxLength(255);

        builder.Property(t => t.AdminEmail).IsRequired().HasMaxLength(255);

        builder.Property(t => t.Description).HasMaxLength(500);

        builder.Property(t => t.IsActive).IsRequired();

        // Indexes
        builder.HasIndex(t => t.Name).IsUnique();

        builder.HasIndex(t => t.Slug).IsUnique();

        builder.HasIndex(t => t.AdminEmail);

        builder.HasIndex(t => t.IsActive);

        // Soft delete
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}
