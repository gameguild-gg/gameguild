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
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DeviceInfo).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.TrustedAt).IsRequired();
        builder.Property(x => x.LastUsedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.AssociatedIpAddresses).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_trusteddevice_user_id");
        builder.HasIndex(x => new { x.UserId, x.DeviceFingerprint }).IsUnique().HasDatabaseName("ix_trusteddevice_user_fingerprint");
    }
}
