using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.FinancialCrime;

internal sealed class FinancialCrimeScreeningRow
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ProviderEventId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public Guid? CaseId { get; set; }
    public long Version { get; set; }
    public FinancialCrimeOutcome Outcome { get; set; }
    public bool SanctionsMatch { get; set; }
    public bool PepMatch { get; set; }
    public bool AdverseMediaMatch { get; set; }
    public long PolicyVersion { get; set; }
    public string PayloadHash { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public string RawObjectReference { get; set; } = string.Empty;
    public bool SignatureVerified { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset NextScreenAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}

internal sealed class FinancialCrimeTransactionSignalRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public Guid CaseId { get; set; }
    public string OperationFingerprint { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public int Score { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; }
}

internal sealed class FinancialCrimeCaseRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public FinancialCrimeCaseState State { get; set; }
    public Guid? AssignedTo { get; set; }
    public Guid HoldId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

internal sealed class FinancialCrimeCaseEventRow
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public int Sequence { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class FinancialCrimeDecisionRow
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid TenantId { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public long Version { get; set; }
    public FinancialCrimeOutcome Outcome { get; set; }
    public long PolicyVersion { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public string RawObjectReference { get; set; } = string.Empty;
    public Guid DecidedBy { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

internal sealed class FinancialCrimeDecisionConsumptionRow
{
    public Guid Id { get; set; }
    public Guid DecisionId { get; set; }
    public Guid TenantId { get; set; }
    public string OperationFingerprint { get; set; } = string.Empty;
    public DateTimeOffset ConsumedAt { get; set; }
}

internal sealed class FinancialCrimeRegulatoryReferenceRow
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string JurisdictionCode { get; set; } = string.Empty;
    public string ReferenceHash { get; set; } = string.Empty;
    public Guid RecordedBy { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class FinancialCrimeModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<FinancialCrimeScreeningRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_screenings", table =>
            {
                table.HasCheckConstraint("ck_financial_crime_screenings_version", "\"Version\" > 0 AND \"PolicyVersion\" > 0");
                table.HasCheckConstraint("ck_financial_crime_screenings_lifetime", "\"ExpiresAt\" > \"IssuedAt\" AND \"NextScreenAt\" > \"IssuedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(2048);
            builder.HasIndex(row => new { row.Provider, row.Environment, row.ProviderEventId }).IsUnique()
                .HasDatabaseName("ux_financial_crime_screenings_provider_event");
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.Version }).IsUnique()
                .HasDatabaseName("ux_financial_crime_screenings_subject_version");
            builder.HasIndex(row => row.NextScreenAt);
            builder.HasOne<FinancialCrimeCaseRow>().WithMany().HasForeignKey(row => row.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancialCrimeTransactionSignalRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_transaction_signals", table =>
                table.HasCheckConstraint("ck_financial_crime_transaction_signals_score", "\"Score\" BETWEEN 0 AND 1000000"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.OperationFingerprint).HasMaxLength(256);
            builder.Property(row => row.SignalType).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.HasIndex(row => row.RequestHash).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.ObservedAt });
            builder.HasOne<FinancialCrimeCaseRow>().WithMany().HasForeignKey(row => row.CaseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancialCrimeCaseRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_cases", table =>
            {
                table.HasCheckConstraint("ck_financial_crime_cases_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_financial_crime_cases_closed", "(\"State\" = 4 AND \"ClosedAt\" IS NOT NULL) OR (\"State\" <> 4 AND \"ClosedAt\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.State });
        });
        modelBuilder.Entity<FinancialCrimeCaseEventRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_case_events", table =>
                table.HasCheckConstraint("ck_financial_crime_case_events_sequence", "\"Sequence\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Kind).HasMaxLength(50);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.CaseId, row.Sequence }).IsUnique();
            builder.HasOne<FinancialCrimeCaseRow>().WithMany().HasForeignKey(row => row.CaseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancialCrimeDecisionRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_decisions", table =>
            {
                table.HasCheckConstraint("ck_financial_crime_decisions_version", "\"Version\" > 0 AND \"PolicyVersion\" > 0");
                table.HasCheckConstraint("ck_financial_crime_decisions_lifetime", "\"ExpiresAt\" > \"IssuedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(2048);
            builder.HasIndex(row => new { row.CaseId, row.Version }).IsUnique();
            builder.HasOne<FinancialCrimeCaseRow>().WithMany().HasForeignKey(row => row.CaseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancialCrimeDecisionConsumptionRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_decision_consumptions");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.OperationFingerprint).HasMaxLength(256);
            builder.HasIndex(row => row.DecisionId).IsUnique();
            builder.HasOne<FinancialCrimeDecisionRow>().WithMany().HasForeignKey(row => row.DecisionId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FinancialCrimeRegulatoryReferenceRow>(builder =>
        {
            builder.ToTable("compliance_financial_crime_regulatory_references");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Kind).HasMaxLength(20);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.ReferenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.CaseId, row.Kind, row.ReferenceHash }).IsUnique();
            builder.HasOne<FinancialCrimeCaseRow>().WithMany().HasForeignKey(row => row.CaseId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
