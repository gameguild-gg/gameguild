using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.KYC;

internal sealed class SumSubApplicantBindingRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string ExternalUserIdHash { get; set; } = string.Empty;
    public string IdempotencyKeyHash { get; set; } = string.Empty;
    public KycAmlState State { get; set; }
    public string? JurisdictionCode { get; set; }
    public long EvidenceVersion { get; set; }
    public DateTimeOffset? LastProviderIssuedAt { get; set; }
    public string? LastProviderEventId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class SumSubWebhookInboxRow
{
    public Guid Id { get; set; }
    public string ProviderEventId { get; set; } = string.Empty;
    public string ApplicantId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string RawObjectReference { get; set; } = string.Empty;
    public bool SignatureVerified { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}

public sealed class SumSubEvidenceModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<SumSubApplicantBindingRow>(builder =>
        {
            builder.ToTable("compliance_sumsub_applicant_bindings", table =>
            {
                table.HasCheckConstraint(
                    "ck_compliance_sumsub_applicant_bindings_state",
                    "\"State\" BETWEEN 1 AND 7");
                table.HasCheckConstraint(
                    "ck_compliance_sumsub_applicant_bindings_version",
                    "\"EvidenceVersion\" >= 0");
                table.HasCheckConstraint(
                    "ck_compliance_sumsub_applicant_bindings_jurisdiction",
                    "\"JurisdictionCode\" IS NULL OR \"JurisdictionCode\" ~ '^[A-Z]{3}$'");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.ApplicantId).HasMaxLength(256);
            builder.Property(row => row.ExternalUserIdHash).HasMaxLength(128);
            builder.Property(row => row.IdempotencyKeyHash).HasMaxLength(128);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(3).IsFixedLength();
            builder.Property(row => row.LastProviderEventId).HasMaxLength(256);
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash }).IsUnique();
            builder.HasIndex(row => row.ApplicantId).IsUnique();
            builder.HasIndex(row => row.IdempotencyKeyHash).IsUnique();
        });

        modelBuilder.Entity<SumSubWebhookInboxRow>(builder =>
        {
            builder.ToTable("compliance_sumsub_webhook_inbox", table =>
                table.HasCheckConstraint(
                    "ck_compliance_sumsub_webhook_inbox_time",
                    "\"ReceivedAt\" >= \"IssuedAt\""));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.ApplicantId).HasMaxLength(256);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(2048);
            builder.Property(row => row.ProcessingError).HasMaxLength(256);
            builder.HasIndex(row => row.ProviderEventId).IsUnique();
            builder.HasIndex(row => new { row.ApplicantId, row.IssuedAt });
        });
    }
}
