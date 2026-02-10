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
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.VerificationType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(64).IsRequired();
        builder.Property(x => x.VerifiedValue).HasMaxLength(256).IsRequired();
        builder.Property(x => x.InitiatedAt).IsRequired();
        builder.Property(x => x.VerificationProvider).HasMaxLength(256);
        builder.Property(x => x.ExternalVerificationId).HasMaxLength(256);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DocumentIds).HasMaxLength(2000);
        builder.Property(x => x.Metadata).HasMaxLength(2000);
        builder.Ignore(x => x.IsValid);
        builder.Ignore(x => x.IsPending);

        // Indexes
        builder.HasIndex(x => x.UserId).HasDatabaseName("ix_identityverification_user_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_identityverification_status");
        builder.HasIndex(x => new { x.UserId, x.VerificationType }).HasDatabaseName("ix_identityverification_user_type");
    }
}
