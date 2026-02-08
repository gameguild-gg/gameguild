using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for AuthenticationAttempt
/// </summary>
public class AuthenticationAttemptConfiguration : IEntityTypeConfiguration<AuthenticationAttempt>
{
    public void Configure(EntityTypeBuilder<AuthenticationAttempt> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("authenticationattempt", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(1000);
        builder.Property(x => x.IsSuccessful).HasColumnName("is_successful").IsRequired();
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(50);
        builder.Property(x => x.AttemptedAt).HasColumnName("attempted_at").IsRequired();
        builder.Property(x => x.ProcessingTime).HasColumnName("processing_time").IsRequired();
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(x => x.DeviceFingerprint).HasColumnName("device_fingerprint").HasMaxLength(64);
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.IsSuspicious).HasColumnName("is_suspicious").IsRequired();
        builder.Property(x => x.RiskScore).HasColumnName("risk_score").IsRequired();
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasMaxLength(2000);
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_authenticationattempt_user_id");
        builder.HasIndex(x => x.Email).HasDatabaseName("ix_authenticationattempt_email");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_authenticationattempt_tenant_id");
        builder.HasIndex(x => x.AttemptedAt).HasDatabaseName("ix_authenticationattempt_attempted_at");
        builder.HasIndex(x => x.IpAddress).HasDatabaseName("ix_authenticationattempt_ip_address");
    }
}
