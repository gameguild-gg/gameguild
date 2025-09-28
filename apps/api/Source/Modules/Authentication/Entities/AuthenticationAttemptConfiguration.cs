namespace GameGuild.Modules.Authentication;

internal sealed class AuthenticationAttemptConfiguration : IEntityTypeConfiguration<AuthenticationAttempt>
{
    public void Configure(EntityTypeBuilder<AuthenticationAttempt> builder)
    {
        builder.HasKey(aa => aa.Id);

        // Index configurations for performance and queries
        builder.HasIndex(aa => aa.Email);
        builder.HasIndex(aa => aa.UserId);
        builder.HasIndex(aa => aa.IpAddress);
        builder.HasIndex(aa => aa.AttemptedAt);
        builder.HasIndex(aa => aa.IsSuccessful);
        builder.HasIndex(aa => aa.IsSuspicious);
        builder.HasIndex(aa => aa.RiskScore);
        builder.HasIndex(aa => aa.SessionId);
        builder.HasIndex(aa => aa.TenantId);
        builder.HasIndex(aa => aa.CorrelationId);

        // Composite indexes for common queries
        builder.HasIndex(aa => new { aa.Email, aa.AttemptedAt });
        builder.HasIndex(aa => new { aa.IpAddress, aa.AttemptedAt });
        builder.HasIndex(aa => new { aa.UserId, aa.IsSuccessful, aa.AttemptedAt });

        // Property configurations - these are already defined with data annotations
        // but we can add additional EF Core specific configurations here if needed

        builder.Property(aa => aa.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(aa => aa.IpAddress)
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(aa => aa.UserAgent)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(aa => aa.FailureReason)
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(aa => aa.Location)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(aa => aa.DeviceFingerprint)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(aa => aa.CorrelationId)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(aa => aa.RiskScore)
            .HasDefaultValue(0);

        builder.Property(aa => aa.IsSuspicious)
            .HasDefaultValue(false);

        // JSON column for metadata
        builder.Property(aa => aa.Metadata)
            .HasColumnType("jsonb") // PostgreSQL specific - use "json" for other databases
            .IsRequired(false);

        // Optimistic concurrency
        builder.Property(aa => aa.Version).IsConcurrencyToken();
    }
}