namespace GameGuild.Modules.Authentication;

internal sealed class MfaAttemptConfiguration : IEntityTypeConfiguration<MfaAttempt>
{
    public void Configure(EntityTypeBuilder<MfaAttempt> builder)
    {
        builder.HasKey(ma => ma.Id);

        // Index configurations for performance and security queries
        builder.HasIndex(ma => ma.UserId);
        builder.HasIndex(ma => ma.IsSuccessful);
        builder.HasIndex(ma => ma.CreatedAt);
        builder.HasIndex(ma => ma.IpAddress);
        builder.HasIndex(ma => ma.Method);
        builder.HasIndex(ma => ma.SessionId);

        // Composite indexes for common queries
        builder.HasIndex(ma => new { ma.UserId, ma.IsSuccessful, ma.CreatedAt });
        builder.HasIndex(ma => new { ma.IpAddress, ma.CreatedAt });

        // Property configurations
        builder.Property(ma => ma.Method)
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ma => ma.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(ma => ma.UserAgent)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(ma => ma.FailureReason)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(ma => ma.Location)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(ma => ma.IsSuccessful)
            .HasDefaultValue(false);

        // Optimistic concurrency
        builder.Property(ma => ma.Version).IsConcurrencyToken();
    }
}