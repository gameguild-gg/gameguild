using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Entity Type Configuration for IdentityVerification
/// </summary>
public class IdentityVerificationConfiguration : IEntityTypeConfiguration<IdentityVerification>
{
    public void Configure(EntityTypeBuilder<IdentityVerification> builder)
    {
        // Configure table name (snake_case convention)
        builder.ToTable("identityverification", "gameguild.authentication");

        // Configure primary key
        builder.HasKey(x => x.Id);

        // Configure Id property
        builder.Property(x => x.Id).HasColumnName("id").IsRequired();

        // Property configurations
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.VerificationType).HasColumnName("verification_type").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(64).IsRequired();
        builder.Property(x => x.VerifiedValue).HasColumnName("verified_value").HasMaxLength(256).IsRequired();
        builder.Property(x => x.InitiatedAt).HasColumnName("initiated_at").IsRequired();
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.VerificationProvider).HasColumnName("verification_provider").HasMaxLength(256);
        builder.Property(x => x.ExternalVerificationId).HasColumnName("external_verification_id").HasMaxLength(256);
        builder.Property(x => x.ConfidenceScore).HasColumnName("confidence_score");
        builder.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
        builder.Property(x => x.DocumentIds).HasColumnName("document_ids").HasMaxLength(2000);
        builder.Property(x => x.Metadata).HasColumnName("metadata").HasMaxLength(2000);
        builder.Ignore(x => x.IsValid);
        builder.Ignore(x => x.IsPending);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_identityverification_user_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_identityverification_status");
        builder.HasIndex(x => new { x.UserId, x.VerificationType }).HasDatabaseName("ix_identityverification_user_type");
    }
}
