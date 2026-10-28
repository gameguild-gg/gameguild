using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     EF Core configuration for the tenant-scoped fraud blacklist (issue #409).
/// </summary>
public class FraudBlacklistEntryConfiguration : IEntityTypeConfiguration<FraudBlacklistEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FraudBlacklistEntry> builder)
    {
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.TenantId).IsRequired();
        builder.Property(entry => entry.EntryType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(entry => entry.Value)
            .IsRequired()
            .HasMaxLength(255);
        builder.Property(entry => entry.Reason).HasMaxLength(1000);
        builder.Property(entry => entry.IsWhitelist).IsRequired();
        builder.Property(entry => entry.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        // One live entry per (tenant, type, value, kind): a blacklist row and a whitelist
        // override row for the same value may coexist; duplicates of the same kind cannot.
        builder.HasIndex(entry => new { entry.TenantId, entry.EntryType, entry.Value, entry.IsWhitelist })
            .IsUnique()
            .HasDatabaseName("ix_fraud_blacklist_tenant_type_value_unique")
            .HasFilter("\"DeletedAt\" IS NULL");

        // Look-back support for the blacklist evaluation queries per tenant and type.
        builder.HasIndex(entry => new { entry.TenantId, entry.EntryType })
            .HasDatabaseName("ix_fraud_blacklist_tenant_type");

        builder.HasIndex(entry => entry.ExpiresAt)
            .HasDatabaseName("ix_fraud_blacklist_expiry");
    }
}
