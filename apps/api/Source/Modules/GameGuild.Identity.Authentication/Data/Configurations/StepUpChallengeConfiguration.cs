using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Identity.Authentication;

public sealed class StepUpChallengeConfiguration : IEntityTypeConfiguration<StepUpChallenge>
{
    public void Configure(EntityTypeBuilder<StepUpChallenge> builder)
    {
        builder.ToTable(
            "step_up_challenges",
            "gameguild.authentication",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_step_up_challenges_expiry",
                    "expires_at > created_at");
                table.HasCheckConstraint(
                    "ck_step_up_challenges_payload_hash",
                    "payload_hash ~ '^[0-9a-f]{64}$'");
                table.HasCheckConstraint(
                    "ck_step_up_challenges_verification",
                    "(verified_at IS NULL AND verification_method IS NULL AND receipt_hash IS NULL) OR " +
                    "(verified_at IS NOT NULL AND verification_method IS NOT NULL AND receipt_hash ~ '^[0-9a-f]{64}$')");
                table.HasCheckConstraint(
                    "ck_step_up_challenges_consumption",
                    "consumed_at IS NULL OR (verified_at IS NOT NULL AND consumed_at >= verified_at)");
            });
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).HasColumnName("id");
        builder.Property(challenge => challenge.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(challenge => challenge.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(challenge => challenge.SessionId).HasColumnName("session_id").IsRequired();
        builder.Property(challenge => challenge.OperationType).HasColumnName("operation_type").HasMaxLength(128).IsRequired();
        builder.Property(challenge => challenge.TargetReference).HasColumnName("target_reference").HasMaxLength(256).IsRequired();
        builder.Property(challenge => challenge.PayloadHash).HasColumnName("payload_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(challenge => challenge.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(challenge => challenge.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(challenge => challenge.VerifiedAt).HasColumnName("verified_at");
        builder.Property(challenge => challenge.VerificationMethod)
            .HasColumnName("verification_method")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(challenge => challenge.ReceiptHash).HasColumnName("receipt_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(challenge => challenge.ConsumedAt).HasColumnName("consumed_at");
        builder.HasIndex(challenge => challenge.ReceiptHash)
            .IsUnique()
            .HasFilter("receipt_hash IS NOT NULL")
            .HasDatabaseName("ux_step_up_challenges_receipt_hash");
        builder.HasIndex(challenge => new
            {
                challenge.TenantId,
                challenge.ActorId,
                challenge.SessionId,
                challenge.ExpiresAt
            })
            .HasDatabaseName("ix_step_up_challenges_subject_expiry");
    }
}
