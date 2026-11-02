using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for UserDeviceFingerprint (issue #1085).
/// </summary>
public class UserDeviceFingerprintConfiguration : IEntityTypeConfiguration<UserDeviceFingerprint>
{
    public void Configure(EntityTypeBuilder<UserDeviceFingerprint> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("userdevicefingerprint", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FirstSeenAt).IsRequired();
        builder.Property(x => x.LastSeenAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // One row per (user, fingerprint): first-seen detection relies on uniqueness.
        builder.HasIndex(x => new { x.UserId, x.DeviceFingerprint }).IsUnique()
            .HasDatabaseName("ix_userdevicefingerprint_user_id_device_fingerprint");
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_userdevicefingerprint_user_id");
    }
}
