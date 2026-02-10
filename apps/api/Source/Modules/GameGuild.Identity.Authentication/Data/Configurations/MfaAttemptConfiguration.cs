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
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.IsSuccessful).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AttemptedAt).IsRequired();
        builder.Property(x => x.ProcessingTimeMs).IsRequired();
        builder.Property(x => x.DeviceFingerprint).HasMaxLength(256);
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_mfaattempt_user_id");
        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_mfaattempt_tenant_id");
        builder.HasIndex(x => x.AttemptedAt).HasDatabaseName("ix_mfaattempt_attempted_at");
    }
}
