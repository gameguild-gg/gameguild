using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Authentication;

/// <summary>
/// Durable storage mapping for one-time email-verification and password-reset
/// tokens (issue #1151). Mirrors <see cref="MagicLinkTokenModelConfiguration"/>.
/// </summary>
public sealed class VerificationTokenModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VerificationToken>();
        entity.ToTable("verification_tokens", "gameguild.authentication");
        entity.HasKey(token => token.TokenHash);
        entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsFixedLength();
        entity.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        entity.Property(token => token.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        entity.Property(token => token.TokenType).HasColumnName("token_type").HasMaxLength(50).IsRequired();
        entity.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        entity.Property(token => token.ConsumedAt).HasColumnName("consumed_at");
        entity.HasIndex(token => token.ExpiresAt).HasDatabaseName("ix_verification_tokens_expires_at");
        entity.HasIndex(token => token.TokenType).HasDatabaseName("ix_verification_tokens_token_type");
    }
}
