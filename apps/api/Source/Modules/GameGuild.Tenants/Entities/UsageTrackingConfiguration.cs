using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Tenants.Entities;

/// <summary>
///     Entity Framework configuration for UsageTracking entity
/// </summary>
public class UsageTrackingConfiguration : IEntityTypeConfiguration<UsageTracking>
{
    public void Configure(EntityTypeBuilder<UsageTracking> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Primary Key
        builder.HasKey(ut => ut.Id);

        // Properties
        builder.Property(ut => ut.TenantId).IsRequired();
        builder.Property(ut => ut.Date).IsRequired();
        builder.Property(ut => ut.ResourceType).IsRequired().HasMaxLength(100);
        builder.Property(ut => ut.UsageAmount).IsRequired();
        builder.Property(ut => ut.Unit).HasMaxLength(50);
        builder.Property(ut => ut.Cost);
        builder.Property(ut => ut.Metadata).IsRequired(false);

        // Relationships
        builder.HasOne(ut => ut.Tenant).WithMany().HasForeignKey(ut => ut.TenantId).OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ut => new { ut.TenantId, ut.Date });
        builder.HasIndex(ut => ut.ResourceType);
    }
}
