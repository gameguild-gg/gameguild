namespace GameGuild.Modules.Authentication;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(rt => rt.Id);

        // Index configurations for performance
        builder.HasIndex(rt => rt.UserId);
        builder.HasIndex(rt => rt.Token).IsUnique();
        builder.HasIndex(rt => rt.ExpiresAt);
        builder.HasIndex(rt => rt.IsRevoked);
        builder.HasIndex(rt => rt.CreatedAt);

        // Property configurations
        builder.Property(rt => rt.Token)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false);

        // Computed properties (read-only)
        builder.Ignore(rt => rt.IsExpired);
        builder.Ignore(rt => rt.IsActive);

        // Optimistic concurrency
        builder.Property(rt => rt.Version).IsConcurrencyToken();
    }
}