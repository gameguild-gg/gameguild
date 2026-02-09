using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for RefreshToken
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("refresh_tokens", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Token).HasColumnName("token").HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(x => x.IsRevoked).HasColumnName("is_revoked").IsRequired();
        builder.Property(x => x.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(45);
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.ReplacedByToken).HasColumnName("replaced_by_token").HasMaxLength(500);
        builder.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(45).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsActive);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_refreshtoken_user_id");
        builder.HasIndex(x => x.Token).IsUnique().HasDatabaseName("ix_refreshtoken_token");
        builder.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_refreshtoken_expires_at");
    }
}
