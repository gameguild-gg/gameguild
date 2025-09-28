using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Modules.Authentication;

internal sealed class TrustedDeviceConfiguration : IEntityTypeConfiguration<TrustedDevice>
{
    public void Configure(EntityTypeBuilder<TrustedDevice> builder)
    {
        builder.HasKey(td => td.Id);

        // Index configurations
        builder.HasIndex(td => td.UserId);
        builder.HasIndex(td => td.DeviceFingerprint);
        builder.HasIndex(td => td.IsActive);
        builder.HasIndex(td => td.TrustedAt);
        builder.HasIndex(td => td.LastUsedAt);
        builder.HasIndex(td => td.ExpiresAt);

        // Composite indexes for common queries
        builder.HasIndex(td => new { td.UserId, td.DeviceFingerprint }).IsUnique();
        builder.HasIndex(td => new { td.UserId, td.IsActive });

        // Property configurations
        builder.Property(td => td.DeviceFingerprint)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(td => td.DeviceName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(td => td.DeviceInfo)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired();

        builder.Property(td => td.AssociatedIpAddresses)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired(false);

        builder.Property(td => td.IsActive)
            .HasDefaultValue(true);

        // Computed properties (read-only)
        builder.Ignore(td => td.IsExpired);
        builder.Ignore(td => td.IsValid);

        // Optimistic concurrency
        builder.Property(td => td.Version).IsConcurrencyToken();
    }
}