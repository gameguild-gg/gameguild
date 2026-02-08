using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for TrustedDevice
/// </summary>
public class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("trusteddevice", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasColumnName("device_fingerprint").HasMaxLength(64).IsRequired();
        builder.Property(x => x.DeviceName).HasColumnName("device_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.DeviceInfo).HasColumnName("device_info").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.TrustedAt).HasColumnName("trusted_at").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.AssociatedIpAddresses).HasColumnName("associated_ip_addresses").HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_trusteddevice_user_id");
        builder.HasIndex(x => new { x.UserId, x.DeviceFingerprint }).IsUnique().HasDatabaseName("ix_trusteddevice_user_fingerprint");
    }
}
