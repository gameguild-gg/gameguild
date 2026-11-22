using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Finance.Economy.AdRewards.Persistence;

// #1092 Tier 2.3: AdNetworkPolicyVersionRow merged into the shared
// EconomyPolicyVersionRow (PolicyDomain = AdNetwork, subject key = network
// name). The registration lives with the unified row in the Economy module;
// this configuration re-asserts it below.
// #1092 Tier 2.2: AdRewardCapConsumptionRow and AdRewardBudgetConsumptionRow
// merged into the shared EconomyUnitConsumptionRow (ConsumptionKind = 3 and
// 4). The registration lives with the unified row in the Economy module; this
// configuration re-asserts it below with the session foreign key.

internal sealed class AdRewardSessionRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string CreativeId { get; set; } = string.Empty;
    public string DeviceRiskHash { get; set; } = string.Empty;
    public string IpRiskHash { get; set; } = string.Empty;
    public string AsnRiskHash { get; set; } = string.Empty;
    public string NonceHash { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string TokenKeyId { get; set; } = string.Empty;
    public long RequiredDurationTicks { get; set; }
    public DurableAdRewardSessionState State { get; set; }
    public string StartIdempotencyKeyHash { get; set; } = string.Empty;
    public string StartRequestHash { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

/// <summary>
///     Discriminator for the unified ad-reward session timeline
///     (#1092 Tier 1.5). Each kind keeps its own sequence stream: the unique
///     (TimelineKind, SessionId, Sequence) index is what preserves the
///     continuity that <c>AdPlaybackVerification</c> and the session state
///     machines rely on.
/// </summary>
internal enum AdRewardTimelineKind
{
    /// <summary>Session lifecycle events
    /// (formerly <c>economy_ad_reward_session_events</c>).</summary>
    SessionEvent = 1,

    /// <summary>Ordered playback milestones
    /// (formerly <c>economy_ad_reward_playback_milestones</c>).</summary>
    PlaybackMilestone = 2,
}

internal sealed class AdRewardTimelineRow
{
    public Guid Id { get; set; }
    public AdRewardTimelineKind TimelineKind { get; set; }
    public Guid SessionId { get; set; }
    public long Sequence { get; set; }
    public DurableAdRewardSessionState? State { get; set; }
    public int? Percentage { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class AdRewardPendingClaimRow
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid SourceStampId { get; set; }
    public string CompletionIdempotencyKeyHash { get; set; } = string.Empty;
    public string CompletionRequestHash { get; set; } = string.Empty;
    public DateTimeOffset DeferredAt { get; set; }
    public Guid? ProviderReportId { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ConfirmationIdempotencyKeyHash { get; set; }
    public string? ConfirmationRequestHash { get; set; }
}

internal sealed class AdRewardCompletionRow
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public AdRewardCompletionState State { get; set; }
    public long RewardSoftUnits { get; set; }
    public Guid? SourceStampId { get; set; }
    public Guid? PostingId { get; set; }
    public Guid? OutputLotId { get; set; }
    public string? ProviderEventId { get; set; }
    public Guid? CapabilityReceiptId { get; set; }
    public string? CapabilityReceiptHash { get; set; }
    public long? ReserveVersion { get; set; }
    public Guid? RiskDecisionId { get; set; }
    public long? KillSwitchEpoch { get; set; }
    public string? JurisdictionCode { get; set; }
    public string? ProviderHash { get; set; }
    public string? DestinationHash { get; set; }
    public string EvidenceHashes { get; set; } = "[]";
    public DateTimeOffset CompletedAt { get; set; }
    public long Version { get; set; }
}

internal sealed class AdRewardAccumulatorRow
{
    public Guid TenantId { get; set; }
    public Guid WalletId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string RemainderNumerator { get; set; } = "0";
    public string CanonicalDenominator { get; set; } = "1";
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class AdRewardAttributionRow
{
    public Guid SessionId { get; set; }
    public Guid TenantId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string ProviderBatchId { get; set; } = string.Empty;
    public long EstimatedRevenueUsdNanos { get; set; }
    public long RewardSoftUnits { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

internal sealed class AdProviderReportRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Network { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public long ActualRevenueUsdNanos { get; set; }
    public string VerifiedSessionIds { get; set; } = "[]";
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset ImportedAt { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public bool SignatureVerified { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}

internal sealed class AdRewardReconciliationRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProviderReportId { get; set; }
    public string Network { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public long EstimatedRevenueUsdNanos { get; set; }
    public long PreviousActualRevenueUsdNanos { get; set; }
    public long ActualRevenueUsdNanos { get; set; }
    public long ActualDeltaUsdNanos { get; set; }
    public long VarianceUsdNanos { get; set; }
    public long HistoricalRewardSoftUnits { get; set; }
    public DateTimeOffset ReconciledAt { get; set; }
}

internal sealed class AdRewardProviderBatchClaimRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProviderReportId { get; set; }
    public Guid SessionId { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public DateTimeOffset ClaimedAt { get; set; }
}

public sealed class AdRewardsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // #1092 Tier 2.3: economy_ad_network_policy_versions merged into the
        // shared economy_policy_versions table (PolicyDomain = 2 = AdNetwork).
        // The module re-asserts the unified table so contexts composing only
        // this configuration address the merged table; the ad-network domain
        // keeps its (TenantId, network, Version) uniqueness and its
        // effective-window index through the partial indexes declared with
        // the unified registration in the Economy module.
        modelBuilder.Entity<EconomyPolicyVersionRow>(builder =>
        {
            builder.ToTable("economy_policy_versions");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectKey).HasMaxLength(100);
            builder.HasIndex(row => new { row.TenantId, row.SubjectKey, row.Version })
                .IsUnique()
                .HasFilter("\"PolicyDomain\" = 2")
                .HasDatabaseName("ux_economy_policy_versions_adnetwork_version");
            builder.HasIndex(row => new { row.TenantId, row.SubjectKey, row.EffectiveAt, row.ExpiresAt })
                .HasFilter("\"PolicyDomain\" = 2")
                .HasDatabaseName("ix_economy_policy_versions_adnetwork_window");
        });

        modelBuilder.Entity<AdRewardSessionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_sessions", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_sessions_window", "\"ExpiresAt\" > \"IssuedAt\" AND \"UpdatedAt\" >= \"IssuedAt\"");
                table.HasCheckConstraint("ck_economy_ad_reward_sessions_values", "\"PolicyVersion\" > 0 AND \"RequiredDurationTicks\" > 0 AND \"Version\" > 0");
                table.HasCheckConstraint("ck_economy_ad_reward_sessions_state", "\"State\" BETWEEN 1 AND 7");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.CreativeId).HasMaxLength(256);
            builder.Property(row => row.DeviceRiskHash).HasMaxLength(128);
            builder.Property(row => row.IpRiskHash).HasMaxLength(128);
            builder.Property(row => row.AsnRiskHash).HasMaxLength(128);
            builder.Property(row => row.NonceHash).HasMaxLength(128);
            builder.Property(row => row.TokenHash).HasMaxLength(128);
            builder.Property(row => row.TokenKeyId).HasMaxLength(256);
            builder.Property(row => row.StartIdempotencyKeyHash).HasMaxLength(128);
            builder.Property(row => row.StartRequestHash).HasMaxLength(128);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.TenantId, row.StartIdempotencyKeyHash }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.NonceHash }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.UserId, row.IssuedAt });
        });

        // #1092 Tier 1.5 — the session-event and playback-milestone tables
        // merged behind TimelineKind. Each kind keeps its own sequence
        // stream: the unique (TimelineKind, SessionId, Sequence) index
        // preserves the continuity AdPlaybackVerification relies on.
        modelBuilder.Entity<AdRewardTimelineRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_session_timeline", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_ad_reward_session_timeline_sequence",
                    "\"Sequence\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_ad_reward_session_timeline_kind",
                    "(\"TimelineKind\" = 1 AND \"State\" BETWEEN 1 AND 7 AND \"Percentage\" IS NULL) OR " +
                    "(\"TimelineKind\" = 2 AND \"Percentage\" BETWEEN 0 AND 100 AND \"State\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TimelineKind, row.SessionId, row.Sequence })
                .IsUnique()
                .HasDatabaseName("ux_economy_ad_reward_session_timeline_kind_session_sequence");
            builder.HasOne<AdRewardSessionRow>().WithMany().HasForeignKey(row => row.SessionId).OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 1.2: economy_ad_reward_provider_proof_inbox merged into
        // the shared FK-free economy_inbox table (InboxKind = 2 =
        // AdRewardProviderProof). The provider column carries the network; the
        // session link stays as plain data (no FK) so provider events keep
        // being accepted while sessions are rebuilt (#1092 §3.3). The module
        // contributes the AdRewardProviderProof binding (mirroring the
        // Marketplace/Treasury outbox contributions) so contexts composing
        // only this configuration can persist proof rows.
        modelBuilder.Entity<EconomyInboxRow>(builder =>
        {
            builder.ToTable("economy_inbox");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(1000);
            builder.Property(row => row.ProcessingError).HasMaxLength(2000);
            builder.HasIndex(row => new { row.Provider, row.Environment, row.ProviderEventId })
                .IsUnique()
                .HasFilter("\"InboxKind\" = 1")
                .HasDatabaseName("ux_economy_inbox_provider_event");
            builder.HasIndex(row => new { row.TenantId, row.Provider, row.ProviderEventId })
                .IsUnique()
                .HasFilter("\"InboxKind\" = 2")
                .HasDatabaseName("ux_economy_inbox_tenant_provider_event");
        });

        modelBuilder.Entity<AdRewardPendingClaimRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_pending_claims");
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.CompletionIdempotencyKeyHash).HasMaxLength(128);
            builder.Property(row => row.CompletionRequestHash).HasMaxLength(128);
            builder.Property(row => row.ConfirmationIdempotencyKeyHash).HasMaxLength(128);
            builder.Property(row => row.ConfirmationRequestHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.CompletionIdempotencyKeyHash }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.ConfirmationIdempotencyKeyHash })
                .IsUnique().HasFilter("\"ConfirmationIdempotencyKeyHash\" IS NOT NULL");
            builder.HasOne<AdRewardSessionRow>().WithOne().HasForeignKey<AdRewardPendingClaimRow>(row => row.SessionId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AdProviderReportRow>().WithMany().HasForeignKey(row => row.ProviderReportId).OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 2.2: economy_ad_reward_cap_consumptions and
        // economy_ad_reward_budget_consumptions merged into the shared
        // economy_unit_consumptions table (ConsumptionKind = 3 = AdRewardCap
        // and 4 = AdRewardBudget). The module re-asserts the unified table
        // and contributes the session foreign key; each kind keeps its
        // dedupe and reader indexes as partials (declared with the unified
        // registration in the Economy module and re-asserted here).
        modelBuilder.Entity<EconomyUnitConsumptionRow>(builder =>
        {
            builder.ToTable("economy_unit_consumptions");
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.SessionId, row.Scope })
                .IsUnique()
                .HasFilter("\"ConsumptionKind\" = 3")
                .HasDatabaseName("ux_economy_unit_consumptions_session_scope");
            builder.HasIndex(row => row.SessionId)
                .IsUnique()
                .HasFilter("\"ConsumptionKind\" = 4")
                .HasDatabaseName("ux_economy_unit_consumptions_budget_session");
            builder.HasIndex(row => new { row.TenantId, row.Scope, row.SubjectHash, row.ConsumedAt })
                .HasFilter("\"ConsumptionKind\" = 3")
                .HasDatabaseName("ix_economy_unit_consumptions_tenant_scope_subject");
            builder.HasIndex(row => new { row.UserId, row.ConsumedAt })
                .HasFilter("\"ConsumptionKind\" = 4")
                .HasDatabaseName("ix_economy_unit_consumptions_budget_user");
            builder.HasIndex(row => new { row.DeviceRiskHash, row.ConsumedAt })
                .HasFilter("\"ConsumptionKind\" = 4")
                .HasDatabaseName("ix_economy_unit_consumptions_budget_device");
            builder.HasIndex(row => new { row.Network, row.ConsumedAt })
                .HasFilter("\"ConsumptionKind\" = 4")
                .HasDatabaseName("ix_economy_unit_consumptions_budget_network");
            builder.HasOne<AdRewardSessionRow>()
                .WithMany()
                .HasForeignKey(row => row.SessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdRewardCompletionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_completions", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_completions_reward_nonnegative", "\"RewardSoftUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_ad_reward_completions_state", "\"State\" BETWEEN 1 AND 3");
                table.HasCheckConstraint("ck_economy_ad_reward_completions_issued_binding", "\"State\" <> 1 OR (\"RewardSoftUnits\" > 0 AND \"SourceStampId\" IS NOT NULL AND \"PostingId\" IS NOT NULL AND \"OutputLotId\" IS NOT NULL)");
            });
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.CapabilityReceiptHash).HasMaxLength(128);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.ProviderHash).HasMaxLength(128);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHashes).HasColumnType("jsonb");
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => row.IdempotencyKey).IsUnique();
            builder.HasIndex(row => row.ProviderEventId).IsUnique().HasFilter("\"ProviderEventId\" IS NOT NULL");
            builder.HasIndex(row => new { row.UserId, row.CompletedAt });
            builder.HasIndex(row => new { row.Network, row.PolicyVersion });
        });

        modelBuilder.Entity<AdRewardAccumulatorRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_accumulators", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_accumulators_numbers", "\"RemainderNumerator\" ~ '^[0-9]+$' AND \"CanonicalDenominator\" ~ '^[1-9][0-9]*$'");
                table.HasCheckConstraint("ck_economy_ad_reward_accumulators_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
            });
            builder.HasKey(row => new { row.TenantId, row.WalletId, row.Network });
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.RemainderNumerator).HasColumnType("text");
            builder.Property(row => row.CanonicalDenominator).HasColumnType("text");
        });

        // economy_ad_reward_budget_consumptions merged into
        // economy_unit_consumptions (#1092 Tier 2.2, ConsumptionKind = 4) —
        // see the unified EconomyUnitConsumptionRow registration above.

        modelBuilder.Entity<AdRewardAttributionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_attributions", table =>
                table.HasCheckConstraint("ck_economy_ad_reward_attributions_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"RewardSoftUnits\" >= 0"));
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ProviderBatchId).HasMaxLength(256);
            builder.HasIndex(row => new { row.Network, row.ProviderBatchId, row.CompletedAt });
        });

        modelBuilder.Entity<AdProviderReportRow>(builder =>
        {
            builder.ToTable("economy_ad_provider_reports", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_provider_reports_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_ad_provider_reports_window", "\"PeriodEnd\" > \"PeriodStart\" AND \"ImportedAt\" >= \"PeriodEnd\"");
                table.HasCheckConstraint("ck_economy_ad_provider_reports_revenue", "\"ActualRevenueUsdNanos\" >= 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ReportId).HasMaxLength(256);
            builder.Property(row => row.BatchId).HasMaxLength(256);
            builder.Property(row => row.VerifiedSessionIds).HasColumnType("jsonb");
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.Signature).HasColumnType("text");
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.ProcessingError).HasMaxLength(256);
            builder.HasIndex(row => new { row.TenantId, row.Network, row.ReportId, row.Version }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.Network, row.BatchId, row.Version }).IsUnique();
        });

        modelBuilder.Entity<AdRewardReconciliationRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_reconciliations", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"PreviousActualRevenueUsdNanos\" >= 0 AND \"ActualRevenueUsdNanos\" >= 0 AND \"HistoricalRewardSoftUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_conservation", "\"ActualDeltaUsdNanos\" = \"ActualRevenueUsdNanos\" - \"PreviousActualRevenueUsdNanos\" AND \"VarianceUsdNanos\" = \"ActualRevenueUsdNanos\" - \"EstimatedRevenueUsdNanos\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ReportId).HasMaxLength(256);
            builder.Property(row => row.BatchId).HasMaxLength(256);
            builder.HasIndex(row => new { row.TenantId, row.Network, row.ReportId, row.Version }).IsUnique();
            builder.HasOne<AdProviderReportRow>().WithOne().HasForeignKey<AdRewardReconciliationRow>(row => row.ProviderReportId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdRewardProviderBatchClaimRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_provider_batch_claims");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.BatchId).HasMaxLength(256);
            builder.HasIndex(row => new { row.ProviderReportId, row.SessionId }).IsUnique();
            builder.HasOne<AdProviderReportRow>().WithMany().HasForeignKey(row => row.ProviderReportId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<AdRewardSessionRow>().WithMany().HasForeignKey(row => row.SessionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
