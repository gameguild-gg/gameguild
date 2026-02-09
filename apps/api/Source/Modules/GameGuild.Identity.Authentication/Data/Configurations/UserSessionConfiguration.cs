using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for UserSession
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("user_sessions", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.RefreshToken).HasColumnName("refresh_token").HasMaxLength(512).IsRequired();
        builder.Property(x => x.AccessTokenHash).HasColumnName("access_token_hash").HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasColumnName("device_fingerprint").HasMaxLength(64);
        builder.Property(x => x.DeviceInfo).HasColumnName("device_info").HasMaxLength(2000);
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(500);
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.LastUsedAt).HasColumnName("last_used_at").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(x => x.TerminationReason).HasColumnName("termination_reason").HasMaxLength(100);
        builder.Property(x => x.TerminatedAt).HasColumnName("terminated_at");
        builder.Property(x => x.IsTrustedDevice).HasColumnName("is_trusted_device").IsRequired();
        builder.Property(x => x.TrustedAt).HasColumnName("trusted_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_usersession_user_id");
        builder.HasIndex(x => x.RefreshToken).IsUnique().HasDatabaseName("ix_usersession_refresh_token");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_usersession_expires_at");
    }
}
