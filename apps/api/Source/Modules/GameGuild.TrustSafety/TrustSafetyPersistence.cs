using Microsoft.EntityFrameworkCore;

namespace GameGuild.TrustSafety;

internal sealed class TrustSafetyEventInboxRow
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public TrustSafetyEventKind Kind { get; set; }
    public long Version { get; set; }
    public TrustSafetyOutcome Outcome { get; set; }
    public long PolicyVersion { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public string RawObjectReference { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public bool SignatureVerified { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}

internal sealed class TrustSafetySubjectStateRow
{
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public long Version { get; set; }
    public TrustSafetyOutcome Outcome { get; set; }
    public string LastEventId { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public Guid? HoldId { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class TrustSafetyAppealRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public string RestrictionReferenceHash { get; set; } = string.Empty;
    public TrustSafetyAppealState State { get; set; }
    public Guid SubmittedBy { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid? DecidedBy { get; set; }
    public string SubmissionEvidenceHash { get; set; } = string.Empty;
    public string? DecisionEvidenceHash { get; set; }
    public string? ReasonCode { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public long Version { get; set; }
}

public sealed class TrustSafetyModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<TrustSafetyEventInboxRow>(builder =>
        {
            builder.ToTable("trust_safety_event_inbox", table =>
            {
                table.HasCheckConstraint("ck_trust_safety_event_inbox_version", "\"Version\" > 0 AND \"PolicyVersion\" > 0");
                table.HasCheckConstraint("ck_trust_safety_event_inbox_lifetime", "\"ExpiresAt\" > \"IssuedAt\" AND \"ReceivedAt\" >= \"IssuedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EventId).HasMaxLength(256);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(2048);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasMaxLength(2048);
            builder.Property(row => row.ProcessingError).HasMaxLength(100);
            builder.HasIndex(row => row.EventId).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.Version }).IsUnique();
        });
        modelBuilder.Entity<TrustSafetySubjectStateRow>(builder =>
        {
            builder.ToTable("trust_safety_subject_states", table =>
            {
                table.HasCheckConstraint("ck_trust_safety_subject_states_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_trust_safety_subject_states_lifetime", "\"ExpiresAt\" > \"IssuedAt\"");
            });
            builder.HasKey(row => new { row.TenantId, row.SubjectHash });
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.LastEventId).HasMaxLength(256);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => row.ExpiresAt);
        });
        modelBuilder.Entity<TrustSafetyAppealRow>(builder =>
        {
            builder.ToTable("trust_safety_appeals", table =>
            {
                table.HasCheckConstraint("ck_trust_safety_appeals_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_trust_safety_appeals_decision", "(\"State\" IN (1, 2) AND \"DecidedAt\" IS NULL AND \"DecidedBy\" IS NULL) OR (\"State\" IN (3, 4) AND \"DecidedAt\" IS NOT NULL AND \"DecidedBy\" IS NOT NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.RestrictionReferenceHash).HasMaxLength(128);
            builder.Property(row => row.SubmissionEvidenceHash).HasMaxLength(128);
            builder.Property(row => row.DecisionEvidenceHash).HasMaxLength(128);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.State });
        });
    }
}
