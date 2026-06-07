using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Features;

/// <summary>
/// Entity Type Configuration for CapabilityAuditLog.
/// </summary>
public class CapabilityAuditLogConfiguration : IEntityTypeConfiguration<CapabilityAuditLog>
{
    public void Configure(EntityTypeBuilder<CapabilityAuditLog> builder)
    {
        // Table name
        builder.ToTable("capability_audit_logs");

        // Primary key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();

        // Required properties
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CapabilityKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.NewValue).IsRequired();
        builder.Property(x => x.ChangedAt).IsRequired();
        builder.Property(x => x.ChangeType).IsRequired();

        // Optional properties
        builder.Property(x => x.OldSource).HasMaxLength(100);
        builder.Property(x => x.NewSource).HasMaxLength(100);
        builder.Property(x => x.ChangeReason).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);

        // Index for querying by tenant and date
        builder.HasIndex(x => new { x.TenantId, x.ChangedAt })
            .HasDatabaseName("ix_capability_audit_logs_tenant_changed");

        // Index for querying by capability
        builder.HasIndex(x => x.CapabilityKey)
            .HasDatabaseName("ix_capability_audit_logs_capability");

        // Index for querying by user
        builder.HasIndex(x => x.ChangedByUserId)
            .HasDatabaseName("ix_capability_audit_logs_user")
            .HasFilter("\"ChangedByUserId\" IS NOT NULL");
    }
}
