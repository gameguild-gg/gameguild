using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
/// Entity Type Configuration for TenantCapability.
/// </summary>
public class TenantCapabilityConfiguration : IEntityTypeConfiguration<TenantCapability>
{
    public void Configure(EntityTypeBuilder<TenantCapability> builder)
    {
        // Table name
        builder.ToTable("tenant_capabilities");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        // Required properties
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CapabilityKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional properties
        builder.Property(x => x.Source)
            .HasMaxLength(100);

        builder.Property(x => x.Priority)
            .HasDefaultValue(0);

        builder.Property(x => x.Metadata)
            .HasMaxLength(4000);

        builder.Property(x => x.ModificationReason)
            .HasMaxLength(500);

        // Unique index on TenantId + CapabilityKey
        builder.HasIndex(x => new { x.TenantId, x.CapabilityKey })
            .IsUnique()
            .HasDatabaseName("ix_tenant_capabilities_tenant_capability");

        // Index for querying by tenant
        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_tenant_capabilities_tenant_id");

        // Index for expiration queries
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_tenant_capabilities_expires_at")
            .HasFilter("\"ExpiresAt\" IS NOT NULL");
    }
}
