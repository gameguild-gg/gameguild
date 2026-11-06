using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     EF Core configuration for the durable fraud background re-scoring job queue (issue
///     #1218 G3). The status column doubles as the claim concurrency token (compare-and-swap
///     against competing workers), mirroring the academic outbox delivery discipline.
/// </summary>
public class FraudBackgroundScoringJobConfiguration : IEntityTypeConfiguration<FraudBackgroundScoringJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FraudBackgroundScoringJob> builder)
    {
        builder.HasKey(job => job.Id);

        builder.Property(job => job.TenantId).IsRequired();
        builder.Property(job => job.PaymentId).IsRequired();
        builder.Property(job => job.ContextJson).HasColumnType("text").IsRequired();
        builder.Property(job => job.DeferredProvidersJson).HasColumnType("text").IsRequired();
        builder.Property(job => job.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsConcurrencyToken();
        builder.Property(job => job.ClaimedBy).HasMaxLength(200);
        builder.Property(job => job.LastError).HasColumnType("text");

        // Due sweep: claim Pending rows past their backoff gate and reclaim stale leases.
        builder.HasIndex(job => new { job.Status, job.NextAttemptAt })
            .HasDatabaseName("ix_fraud_rescoring_jobs_due");
        builder.HasIndex(job => job.PaymentId)
            .HasDatabaseName("ix_fraud_rescoring_jobs_payment");
        builder.HasIndex(job => job.TenantId)
            .HasDatabaseName("ix_fraud_rescoring_jobs_tenant");
    }
}
