using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for MfaAttempt
/// </summary>
public class MfaAttemptConfiguration : IEntityTypeConfiguration<MfaAttempt>
{
    public void Configure(EntityTypeBuilder<MfaAttempt> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("mfaattempt", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.Method).HasColumnName("method").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsSuccessful).HasColumnName("is_successful").IsRequired();
        builder.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500).IsRequired();
        builder.Property(x => x.AttemptedAt).HasColumnName("attempted_at").IsRequired();
        builder.Property(x => x.ProcessingTimeMs).HasColumnName("processing_time_ms").IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasColumnName("device_fingerprint").HasMaxLength(256);
        builder.Property(x => x.SessionId).HasColumnName("session_id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_mfaattempt_user_id");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_mfaattempt_tenant_id");
        builder.HasIndex(x => x.AttemptedAt).HasDatabaseName("ix_mfaattempt_attempted_at");
    }
}
