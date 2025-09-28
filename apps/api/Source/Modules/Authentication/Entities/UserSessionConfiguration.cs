namespace GameGuild.Modules.Authentication;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(us => us.Id);

        // Index configurations for performance
        builder.HasIndex(us => us.UserId);
        builder.HasIndex(us => us.RefreshToken).IsUnique();
        builder.HasIndex(us => us.IsActive);
        builder.HasIndex(us => us.ExpiresAt);
        builder.HasIndex(us => us.LastUsedAt);
        builder.HasIndex(us => us.CreatedAt);
        builder.HasIndex(us => us.DeviceFingerprint);
        builder.HasIndex(us => us.IsTrustedDevice);

        // Composite indexes for common queries
        builder.HasIndex(us => new { us.UserId, us.IsActive });
        builder.HasIndex(us => new { us.UserId, us.IsTrustedDevice });
        builder.HasIndex(us => new { us.IpAddress, us.CreatedAt });

        // Property configurations
        builder.Property(us => us.RefreshToken)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(us => us.AccessTokenHash)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(us => us.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(us => us.UserAgent)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(us => us.DeviceFingerprint)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(us => us.DeviceInfo)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired(false);

        builder.Property(us => us.Location)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired(false);

        builder.Property(us => us.TerminationReason)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(us => us.IsActive)
            .HasDefaultValue(true);

        builder.Property(us => us.IsTrustedDevice)
            .HasDefaultValue(false);

        // Computed properties (read-only)
        builder.Ignore(us => us.IsExpired);
        builder.Ignore(us => us.IsValid);

        // Optimistic concurrency
        builder.Property(us => us.Version).IsConcurrencyToken();
    }
}