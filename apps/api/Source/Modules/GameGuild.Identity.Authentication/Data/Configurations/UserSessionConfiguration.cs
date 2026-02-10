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
        builder.ToTable("usersession", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RefreshToken).HasMaxLength(512).IsRequired();
        builder.Property(x => x.AccessTokenHash).HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasMaxLength(64);
        builder.Property(x => x.DeviceInfo).HasMaxLength(2000);
        builder.Property(x => x.Location).HasMaxLength(500);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.LastUsedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.TerminationReason).HasMaxLength(100);
        builder.Property(x => x.IsTrustedDevice).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsValid);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_usersession_user_id");
        builder.HasIndex(x => x.RefreshToken).IsUnique().HasDatabaseName("ix_usersession_refresh_token");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_usersession_expires_at");
    }
}
