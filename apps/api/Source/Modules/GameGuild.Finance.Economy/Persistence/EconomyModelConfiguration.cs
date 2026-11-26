using GameGuild.Finance.Economy.Integrations.AI;
using GameGuild.Finance.Economy.Ledger;
using GameGuild.Finance.Economy.Transfers;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Finance.Economy.Persistence;

public sealed class EconomyModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        ConfigureWallets(modelBuilder);
        ConfigureSources(modelBuilder);
        ConfigureFunding(modelBuilder);
        ConfigureDisputes(modelBuilder);
        ConfigurePostings(modelBuilder);
        ConfigureLedger(modelBuilder);
        ConfigureLineage(modelBuilder);
        ConfigureOperations(modelBuilder);
        ConfigureChain(modelBuilder);
        ConfigureReserves(modelBuilder);
        ConfigureRisk(modelBuilder);
        ConfigureSelfServiceTransfers(modelBuilder);
        ConfigureControlPlane(modelBuilder);
        ConfigureLegacyShadowMigration(modelBuilder);
        ConfigureRegisteredPostingReceipt(modelBuilder);
        ConfigureHardToSoftConversionRiskDecisionReceipt(modelBuilder);
        ConfigureFifoFragmentReservationReceipt(modelBuilder);
        ConfigureMarketplaceFifoReservationReceipt(modelBuilder);
        ConfigureProviderReversalReceipt(modelBuilder);
        ConfigureEconomyWalletProvisioningReceipt(modelBuilder);
        modelBuilder.ApplyConfiguration(new AiProviderCostFactEntityConfiguration());
    }

    private static void ConfigureWallets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyWalletRow>(builder =>
        {
            builder.ToTable("economy_wallets");
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.TenantId, row.OwnerId }).IsUnique();
        });

        modelBuilder.Entity<EconomyWalletBalanceProjectionRow>(builder =>
        {
            builder.ToTable("economy_wallet_balance_projections", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_wallet_balance_projections_amounts_nonnegative",
                    "\"PendingHard\" >= 0 AND \"PendingSoft\" >= 0 AND \"PurchasedHard\" >= 0 AND " +
                    "\"EarnedHard\" >= 0 AND \"RestrictedHard\" >= 0 AND \"Soft\" >= 0 AND " +
                    "\"ImmatureEarnedHard\" >= 0 AND \"HeldHard\" >= 0 AND \"HeldSoft\" >= 0 AND " +
                    "\"AvailableHardToSpend\" >= 0 AND \"AvailableSoftToSpend\" >= 0 AND \"WithdrawableHard\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_wallet_balance_projections_sequence_nonnegative",
                    "\"SourceJournalSequence\" >= 0");
            });
            builder.HasKey(row => row.WalletId);
            builder.Property(row => row.ProjectionHash).HasMaxLength(128);
            builder.HasIndex(row => row.ReviewState)
                .HasDatabaseName("ix_economy_wallet_balance_projections_review_state");
            builder.HasOne<EconomyWalletRow>()
                .WithOne()
                .HasForeignKey<EconomyWalletBalanceProjectionRow>(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyProjectionReconciliationEventRow>(builder =>
        {
            builder.ToTable("economy_projection_reconciliation_events", table =>
                table.HasCheckConstraint(
                    "ck_economy_projection_events_sequence_nonnegative",
                    "\"SourceJournalSequence\" >= 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.PreviousHash).HasMaxLength(128);
            builder.Property(row => row.RebuiltHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.WalletId, row.DetectedAt })
                .HasDatabaseName("ix_economy_projection_reconciliation_events_wallet_detected");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyAccountRow>(builder =>
        {
            builder.ToTable("economy_accounts", table =>
                table.HasCheckConstraint(
                    "ck_economy_accounts_wallet_partition",
                    "(\"WalletId\" IS NULL AND \"Code\" NOT IN (2, 3, 4)) OR (\"WalletId\" IS NOT NULL AND \"Code\" IN (2, 3, 4))"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.WalletId, row.Code, row.Currency, row.Provenance }).IsUnique();
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSources(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomySourceStampRow>(builder =>
        {
            builder.ToTable("economy_source_stamps", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_source_stamps_units_nonnegative",
                    "\"AuthoritativeUnits\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_source_stamps_confirmation",
                    "(\"State\" IN (2, 5, 6) AND \"ConfirmedAt\" IS NOT NULL AND \"ConfirmedAt\" >= \"ObservedAt\") OR " +
                    "(\"State\" IN (1, 3, 4) AND \"ConfirmedAt\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SourceKind).HasMaxLength(100);
            builder.Property(row => row.InternalSourceId).HasMaxLength(256);
            builder.Property(row => row.SourceLegId).HasMaxLength(256);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.ProviderReference).HasMaxLength(256);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.SourceKind, row.InternalSourceId, row.SourceLegId })
                .IsUnique()
                .HasDatabaseName("ux_economy_source_stamps_internal_leg");
            builder.HasIndex(row => new { row.Provider, row.ProviderReference })
                .IsUnique()
                .HasFilter("\"Provider\" IS NOT NULL AND \"ProviderReference\" IS NOT NULL")
                .HasDatabaseName("ux_economy_source_stamps_provider_reference");
        });

        // #1092 Tier 2.1 — the per-aggregate event and audit streams merged
        // behind StreamKind. Every stream keeps its former ordering and
        // replay guarantees as a kind-filtered partial unique index, and the
        // kind-shape check binds each kind to its own subject and payload
        // columns. The SECURITY DEFINER funding, reversal and marketplace
        // writers append to this table; the kind predicate must ride along
        // on every read.
        modelBuilder.Entity<EconomyEventRow>(builder =>
        {
            builder.ToTable("economy_events", table =>
            {
                table.HasCheckConstraint("ck_economy_events_sequence", "\"Sequence\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_events_values",
                    "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND " +
                    "(\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND " +
                    "(\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND " +
                    "(\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND " +
                    "(\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND " +
                    "(\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\"))");
                table.HasCheckConstraint(
                    "ck_economy_events_stream_shape",
                    // 1 — source stamp: state in Kind, single evidence hash.
                    "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 2 — provider dispute: replay key is the provider event id.
                    "(\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 3 — wallet debt: signed movement and running balance.
                    "(\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 4 — funding hold: integer kind, acting user, evidence hash.
                    "(\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 5 — risk review: evidence hashes are a JSON array; the
                    // resolution and decision code are optional.
                    "(\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 6 — compliance hold: text kind; (hold, kind, actor) is
                    // unique so an actor records each step once.
                    "(\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 7 — legacy cutover audit: the cutover is addressed by its
                    // tenant key.
                    "(\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 8 — marketplace settlement: text kind, tenant scoped.
                    "(\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 9 — ad-reward session lifecycle event (former TimelineKind 1).
                    "(\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 10 — ad-reward playback milestone (former TimelineKind 2).
                    "(\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 11 — admin withdrawal audit: a tenant-scoped hash chain;
                    // the acting user is optional.
                    "(\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND " +
                    "\"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR " +
                    // 12 — bounty expiration: one record per bounty.
                    "(\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND " +
                    "\"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.ProviderDisputeReference).HasMaxLength(256);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHashes).HasColumnType("jsonb");
            builder.Property(row => row.KindText).HasMaxLength(100);
            builder.Property(row => row.Reason).HasMaxLength(500);
            builder.Property(row => row.ReauthenticationHash).HasMaxLength(128);
            builder.Property(row => row.Evidence).HasColumnType("text");
            builder.Property(row => row.PreviousHash).HasMaxLength(128);
            builder.Property(row => row.Hash).HasMaxLength(128);

            builder.HasIndex(row => new { row.SourceStampId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 1")
                .HasDatabaseName("ux_economy_events_source_stamp_sequence");
            builder.HasIndex(row => row.ProviderEventId)
                .IsUnique()
                .HasFilter("\"StreamKind\" = 2")
                .HasDatabaseName("ux_economy_events_provider_event");
            builder.HasIndex(row => new { row.ProviderDisputeReference, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 2")
                .HasDatabaseName("ux_economy_events_dispute_sequence");
            builder.HasIndex(row => new { row.DebtWalletId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 3")
                .HasDatabaseName("ux_economy_events_debt_sequence");
            builder.HasIndex(row => new { row.HoldId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 4")
                .HasDatabaseName("ux_economy_events_hold_sequence");
            builder.HasIndex(row => new { row.RiskReviewCaseId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 5")
                .HasDatabaseName("ux_economy_events_review_case_sequence");
            builder.HasIndex(row => new { row.ComplianceHoldId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 6")
                .HasDatabaseName("ux_economy_events_compliance_hold_sequence");
            builder.HasIndex(row => new { row.ComplianceHoldId, row.KindText, row.ActorId })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 6")
                .HasDatabaseName("ux_economy_events_compliance_hold_kind_actor");
            builder.HasIndex(row => new { row.LegacyCutoverTenantId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 7")
                .HasDatabaseName("ux_economy_events_legacy_cutover_sequence");
            builder.HasIndex(row => new { row.SettlementId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 8")
                .HasDatabaseName("ux_economy_events_settlement_sequence");
            // Session events (9) and playback milestones (10) number their
            // sequences independently per session — a milestone and the
            // "Issued" session event are both Sequence = 1 — so each kind
            // keeps its own unique stream, as the legacy (TimelineKind,
            // SessionId, Sequence) index did. Both index the same columns, so
            // they carry distinct model names: an unnamed HasIndex on the same
            // properties reconfigures the first index instead of adding one.
            builder.HasIndex(row => new { row.SessionId, row.Sequence }, "EconomyEventAdRewardSessionSequence")
                .IsUnique()
                .HasFilter("\"StreamKind\" = 9")
                .HasDatabaseName("ux_economy_events_ad_reward_session_sequence");
            builder.HasIndex(row => new { row.SessionId, row.Sequence }, "EconomyEventAdRewardMilestoneSequence")
                .IsUnique()
                .HasFilter("\"StreamKind\" = 10")
                .HasDatabaseName("ux_economy_events_ad_reward_milestone_sequence");
            builder.HasIndex(row => new { row.RunId, row.Sequence })
                .IsUnique()
                .HasFilter("\"StreamKind\" = 11")
                .HasDatabaseName("ux_economy_events_admin_withdrawal_sequence");
            builder.HasIndex(row => row.Hash)
                .IsUnique()
                .HasFilter("\"StreamKind\" = 11")
                .HasDatabaseName("ux_economy_events_admin_withdrawal_hash");
            builder.HasIndex(row => row.BountyId)
                .IsUnique()
                .HasFilter("\"StreamKind\" = 12")
                .HasDatabaseName("ux_economy_events_bounty_expiration");
            builder.HasIndex(row => row.SourceStampId)
                .HasDatabaseName("ix_economy_events_source_stamp");
            builder.HasIndex(row => row.SessionId)
                .HasDatabaseName("ix_economy_events_session");
            builder.HasIndex(row => new { row.OccurredAt, row.Id })
                .HasFilter("\"StreamKind\" = 12")
                .HasDatabaseName("ix_economy_events_bounty_expiration_recorded");

            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyProviderDisputeRow>()
                .WithMany()
                .HasForeignKey(row => row.ProviderDisputeReference)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletDebtRow>()
                .WithMany()
                .HasForeignKey(row => row.DebtWalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyHoldRow>()
                .WithMany()
                .HasForeignKey(row => row.HoldId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyRiskReviewCaseRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskReviewCaseId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyComplianceHoldRow>()
                .WithMany()
                .HasForeignKey(row => row.ComplianceHoldId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyLegacyCutoverRow>()
                .WithMany()
                .HasForeignKey(row => row.LegacyCutoverTenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureFunding(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyFundingClaimRow>(builder =>
        {
            builder.ToTable("economy_funding_claims", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_funding_claims_amount_positive",
                    "\"AuthoritativeUsdMinorUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_funding_claims_version_positive",
                    "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_funding_claims_provider_reversal_bounds",
                    "\"CumulativeProviderReversalUnits\" >= 0 AND \"CumulativeProviderReversalUnits\" <= \"AuthoritativeUsdMinorUnits\"");
                table.HasCheckConstraint(
                    "ck_economy_funding_claims_lifecycle",
                    "(\"State\" = 1 AND \"ConfirmedAt\" IS NULL AND \"StateChangedAt\" = \"ObservedAt\" AND \"PostingGroupId\" IS NULL AND \"RootCreditLotId\" IS NULL AND \"CumulativeProviderReversalUnits\" = 0) OR " +
                    "(\"State\" = 2 AND \"ConfirmedAt\" >= \"ObservedAt\" AND \"StateChangedAt\" >= \"ConfirmedAt\" AND \"PostingGroupId\" IS NOT NULL AND \"RootCreditLotId\" IS NOT NULL) OR " +
                    "(\"State\" IN (3, 4) AND \"ConfirmedAt\" IS NULL AND \"StateChangedAt\" >= \"ObservedAt\" AND \"PostingGroupId\" IS NULL AND \"RootCreditLotId\" IS NULL AND \"CumulativeProviderReversalUnits\" = 0) OR " +
                    "(\"State\" IN (5, 6) AND \"ConfirmedAt\" >= \"ObservedAt\" AND \"StateChangedAt\" >= \"ConfirmedAt\" AND \"PostingGroupId\" IS NOT NULL AND \"RootCreditLotId\" IS NOT NULL AND \"CumulativeProviderReversalUnits\" > 0)");
            });
            builder.HasKey(row => row.SourceStampId);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ConnectedAccount).HasMaxLength(256);
            builder.Property(row => row.ProviderObject).HasMaxLength(256);
            builder.Property(row => row.ProviderMonetaryLeg).HasMaxLength(256);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new
            {
                row.Provider,
                row.Environment,
                row.ConnectedAccount,
                row.ProviderObject,
                row.ProviderMonetaryLeg
            })
                .IsUnique()
                .HasDatabaseName("ux_economy_funding_claims_provider_leg");
            builder.HasIndex(row => row.PostingGroupId)
                .IsUnique()
                .HasFilter("\"PostingGroupId\" IS NOT NULL")
                .HasDatabaseName("ux_economy_funding_claims_posting_group");
            builder.HasIndex(row => row.RootCreditLotId)
                .IsUnique()
                .HasFilter("\"RootCreditLotId\" IS NOT NULL")
                .HasDatabaseName("ux_economy_funding_claims_root_lot");
            builder.HasOne<EconomySourceStampRow>()
                .WithOne()
                .HasForeignKey<EconomyFundingClaimRow>(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyPostingGroupRow>()
                .WithOne()
                .HasForeignKey<EconomyFundingClaimRow>(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithOne()
                .HasForeignKey<EconomyFundingClaimRow>(row => row.RootCreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDisputes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyProviderDisputeRow>(builder =>
        {
            builder.ToTable("economy_provider_disputes", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_provider_disputes_sequence_positive",
                    "\"LatestProviderSequence\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_provider_disputes_version_positive",
                    "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_provider_disputes_amount_partition",
                    "\"CumulativeDisputedHardUnits\" > 0 AND \"BaselineReversedHardUnits\" >= 0 AND " +
                    "\"BaselineReversedHardUnits\" <= \"CumulativeDisputedHardUnits\" AND " +
                    "\"FrozenHardEquivalentUnits\" >= 0 AND " +
                    "\"FrozenHardEquivalentUnits\" <= (\"CumulativeDisputedHardUnits\" - \"BaselineReversedHardUnits\")");
                table.HasCheckConstraint(
                    "ck_economy_provider_disputes_lifecycle",
                    "(\"Status\" = 1 AND \"ReversalIdempotencyKey\" IS NULL) OR " +
                    "(\"Status\" = 2 AND \"FrozenHardEquivalentUnits\" = 0 AND \"ReversalIdempotencyKey\" IS NULL) OR " +
                    "(\"Status\" = 3 AND \"FrozenHardEquivalentUnits\" = 0 AND \"ReversalIdempotencyKey\" IS NOT NULL)");
            });
            builder.HasKey(row => row.ProviderDisputeReference);
            builder.Property(row => row.ProviderDisputeReference).HasMaxLength(256);
            builder.Property(row => row.ReversalIdempotencyKey).HasMaxLength(128);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => row.SourceStampId)
                .IsUnique()
                .HasFilter("\"Status\" = 1")
                .HasDatabaseName("ux_economy_provider_disputes_active_source");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.ResponsibleWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyDisputeFragmentFreezeRow>(builder =>
        {
            builder.ToTable("economy_dispute_fragment_freezes", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_dispute_fragment_freezes_amount_positive",
                    "\"AmountUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_dispute_fragment_freezes_state_timestamp",
                    "(\"Status\" = 1 AND \"TerminalAt\" IS NULL) OR " +
                    "(\"Status\" IN (2, 3) AND \"TerminalAt\" >= \"PlacedAt\")");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ProviderDisputeReference).HasMaxLength(256);
            builder.HasIndex(row => new { row.RootSourceStampId, row.Status })
                .HasDatabaseName("ix_economy_dispute_fragment_freezes_root_status");
            builder.HasOne<EconomyProviderDisputeRow>()
                .WithMany()
                .HasForeignKey(row => row.ProviderDisputeReference)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.CreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyDisputeFragmentRangeRow>(builder =>
        {
            builder.ToTable("economy_dispute_fragment_ranges", table =>
                table.HasCheckConstraint(
                    "ck_economy_dispute_fragment_ranges_half_open",
                    "\"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\" AND \"ReversalEpoch\" >= 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new
                {
                    row.DisputeFragmentFreezeId,
                    row.StartInclusive,
                    row.EndExclusive
                })
                .IsUnique()
                .HasDatabaseName("ux_economy_dispute_fragment_ranges_freeze_interval");
            builder.HasOne<EconomyDisputeFragmentFreezeRow>()
                .WithMany()
                .HasForeignKey(row => row.DisputeFragmentFreezeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyWalletDebtRow>(builder =>
        {
            builder.ToTable("economy_wallet_debts", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_wallet_debts_nonnegative",
                    "\"OutstandingHardUnits\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_wallet_debts_version_positive",
                    "\"Version\" > 0");
            });
            builder.HasKey(row => row.WalletId);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasOne<EconomyWalletRow>()
                .WithOne()
                .HasForeignKey<EconomyWalletDebtRow>(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // economy_wallet_debt_events merged into economy_events (#1092 Tier 2.1,
        // StreamKind = 3) — see the EconomyEventRow configuration above.
    }

    private static void ConfigurePostings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyPostingGroupRow>(builder =>
        {
            builder.ToTable("economy_posting_groups", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_posting_groups_reserve_authorization",
                    "\"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"RiskDecisionId\" IS NOT NULL");
                table.HasCheckConstraint(
                    "ck_economy_posting_groups_template_state",
                    "\"TemplateKind\" BETWEEN 1 AND 26 AND \"TemplateVersion\" = 1 AND \"Status\" = 1");
                table.HasCheckConstraint(
                    "ck_economy_posting_groups_authority_template",
                    "(\"TemplateKind\" IN (1, 2, 3, 18, 19, 20) AND \"Authority\" = 1) OR " +
                    "(\"TemplateKind\" IN (4, 5, 7, 8, 17, 22) AND \"Authority\" = 2) OR " +
                    "(\"TemplateKind\" IN (6, 21) AND \"Authority\" = 3) OR " +
                    "(\"TemplateKind\" IN (9, 10, 23, 24) AND \"Authority\" = 4) OR " +
                    "(\"TemplateKind\" IN (11, 12, 13) AND \"Authority\" = 5) OR " +
                    "(\"TemplateKind\" IN (14, 15, 16) AND \"Authority\" = 6) OR " +
                    "(\"TemplateKind\" IN (25, 26) AND \"Authority\" = 7)");
                table.HasCheckConstraint(
                    "ck_economy_posting_groups_source_requirement",
                    "\"TemplateKind\" NOT IN (1, 2, 3, 18, 19, 20) OR \"SourceStampId\" IS NOT NULL");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(128);
            builder.HasIndex(row => row.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("ux_economy_posting_groups_idempotency_key");
            builder.HasIndex(row => row.SourceStampId)
                .IsUnique()
                .HasFilter("\"SourceStampId\" IS NOT NULL AND \"TemplateKind\" = 1")
                .HasDatabaseName("ux_economy_posting_groups_source_stamp");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyJournalEntryRow>(builder =>
        {
            builder.ToTable("economy_journal_entries", table =>
                table.HasCheckConstraint(
                    "ck_economy_journal_entries_hash_algorithm",
                    "(\"HashAlgorithmVersion\" = 0 AND \"CanonicalPayloadHash\" IS NULL) OR " +
                    "(\"HashAlgorithmVersion\" IN (1, 2) AND length(btrim(\"CanonicalPayloadHash\")) > 0)"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.PreviousHash).HasMaxLength(128);
            builder.Property(row => row.CanonicalPayloadHash).HasMaxLength(128);
            builder.Property(row => row.Hash).HasMaxLength(128);
            builder.HasIndex(row => row.PostingGroupId)
                .IsUnique()
                .HasDatabaseName("ux_economy_journal_entries_posting_group_id");
            builder.HasIndex(row => row.Sequence)
                .IsUnique()
                .HasDatabaseName("ux_economy_journal_entries_sequence");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyJournalLineRow>(builder =>
        {
            builder.ToTable("economy_journal_lines", table =>
                table.HasCheckConstraint("ck_economy_journal_lines_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.JournalEntryId, row.Sequence }).IsUnique();
            builder.HasOne<EconomyJournalEntryRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyAccountRow>()
                .WithMany()
                .HasForeignKey(row => row.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.CreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLedger(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyCreditLotRow>(builder =>
        {
            builder.ToTable("economy_credit_lots", table =>
            {
                table.HasCheckConstraint("ck_economy_credit_lots_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint("ck_economy_credit_lots_maturity_order", "\"OriginalMaturesAt\" >= \"ConfirmedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_credit_lots_maturity_policy",
                    "(\"Provenance\" = 2 AND \"Currency\" = 1 AND \"CashOutEligible\" AND \"OriginalMaturesAt\" = \"ConfirmedAt\" + INTERVAL '120 days') OR (\"Provenance\" <> 2 AND NOT \"CashOutEligible\")");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.RootSourceStampId)
                .HasDatabaseName("ix_economy_credit_lots_root_source");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyEntryAllocationRow>(builder =>
        {
            builder.ToTable("economy_entry_allocations", table =>
                table.HasCheckConstraint("ck_economy_entry_allocations_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.ParentLotId).HasDatabaseName("ix_economy_entry_allocations_parent_lot");
            builder.HasIndex(row => new { row.JournalLineId, row.ParentLotId })
                .IsUnique()
                .HasDatabaseName("ux_economy_entry_allocations_line_parent");
            builder.HasOne<EconomyJournalLineRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalLineId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ParentLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyProviderFactAllocationRow>(builder =>
        {
            builder.ToTable("economy_provider_fact_allocations", table =>
                table.HasCheckConstraint(
                    "ck_economy_provider_fact_allocations_cumulative_bounds",
                    "\"AllocatedUnits\" > 0 AND \"CumulativeCreditedUnits\" >= \"AllocatedUnits\" AND \"CumulativeCreditedUnits\" <= \"AuthoritativeUnits\""));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ConnectedAccount).HasMaxLength(256);
            builder.Property(row => row.ProviderObject).HasMaxLength(256);
            builder.Property(row => row.ProviderMonetaryLeg).HasMaxLength(256);
            builder.HasIndex(row => new
            {
                row.Provider,
                row.Environment,
                row.ConnectedAccount,
                row.ProviderObject,
                row.ProviderMonetaryLeg
            })
                .IsUnique()
                .HasDatabaseName("ux_economy_provider_fact_allocations_provider_leg");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyJournalLineRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalLineId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLineage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyLotLineageEdgeRow>(builder =>
        {
            builder.ToTable("economy_lot_lineage_edges", table =>
                table.HasCheckConstraint("ck_economy_lot_lineage_edges_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.ParentLotId).HasDatabaseName("ix_economy_lot_lineage_edges_parent_lot");
            builder.HasIndex(row => new { row.ParentLotId, row.ChildLotId })
                .IsUnique()
                .HasDatabaseName("ux_economy_lot_lineage_edges_parent_child");
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ParentLotId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ChildLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyFragmentRootRangeRow>(builder =>
        {
            builder.ToTable("economy_fragment_root_ranges", table =>
            {
                table.HasCheckConstraint("ck_economy_fragment_root_ranges_half_open", "\"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\"");
                table.HasCheckConstraint("ck_economy_fragment_root_ranges_single_owner", "(\"CreditLotId\" IS NULL) <> (\"EntryAllocationId\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.RootSourceStampId, row.ReversalEpoch })
                .HasDatabaseName("ix_economy_fragment_root_ranges_root_epoch");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.CreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyEntryAllocationRow>()
                .WithMany()
                .HasForeignKey(row => row.EntryAllocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyRootReversalStateRow>(builder =>
        {
            builder.ToTable("economy_root_reversal_states", table =>
            {
                table.HasCheckConstraint("ck_economy_root_reversal_states_epoch_nonnegative", "\"Epoch\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_root_reversal_states_cumulative_bounds",
                    "\"CumulativeProviderUnits\" >= 0 AND \"ReversedUnits\" >= 0 AND \"ReversedUnits\" <= \"CumulativeProviderUnits\"");
            });
            builder.HasKey(row => row.RootSourceStampId);
            builder.HasIndex(row => new { row.RootSourceStampId, row.Epoch })
                .IsUnique()
                .HasDatabaseName("ux_economy_root_reversal_states_root_epoch");
            builder.Property(row => row.State).HasMaxLength(50);
            builder.Property(row => row.TargetedRanges).HasColumnType("jsonb");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyDispatchSnapshotRow>(builder =>
        {
            builder.ToTable("economy_dispatch_snapshots", table =>
            {
                table.HasCheckConstraint("ck_economy_dispatch_snapshots_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_dispatch_snapshots_reserve_authorization",
                    "\"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SnapshotHash).HasMaxLength(128);
            builder.Property(row => row.Destination).HasMaxLength(512);
            builder.Property(row => row.EligibilityPayload).HasColumnType("jsonb");
            builder.Property(row => row.ChainHash).HasMaxLength(128);
            builder.HasIndex(row => row.SnapshotHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_dispatch_snapshots_hash");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 1.4 — the payout and admin-withdrawal provider-event
        // replay-dedupe anchors merged behind ProviderEventKind. Every lookup
        // must carry the kind; per-kind unique indexes preserve the old
        // primary keys. Cross-module foreign keys (OperationId, RunId) are
        // contributed by the Payouts and Treasury module configurations.
        modelBuilder.Entity<EconomyProviderEventRow>(builder =>
        {
            builder.ToTable("economy_provider_events");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).HasDefaultValueSql("gen_random_uuid()");
            builder.Property(row => row.EventId).HasMaxLength(256);
            builder.Property(row => row.EventHash).HasMaxLength(128);
            builder.HasIndex(row => row.EventId)
                .IsUnique()
                .HasFilter("\"ProviderEventKind\" = 1")
                .HasDatabaseName("ux_economy_provider_events_payout_event");
            builder.HasIndex(row => new { row.TenantId, row.EventId })
                .IsUnique()
                .HasFilter("\"ProviderEventKind\" = 2")
                .HasDatabaseName("ux_economy_provider_events_admin_tenant_event");
            builder.HasIndex(row => new { row.OperationId, row.RecordedAt })
                .HasFilter("\"ProviderEventKind\" = 1")
                .HasDatabaseName("ix_economy_provider_events_operation_recorded");
            builder.HasIndex(row => new { row.RunId, row.RecordedAt })
                .HasFilter("\"ProviderEventKind\" = 2")
                .HasDatabaseName("ix_economy_provider_events_run_recorded");
        });

        // #1092 Tier 1.1 — the five former outbox tables merged behind
        // OutboxKind. Queue-specific unique indexes are partial (per kind);
        // cross-module foreign keys (SettlementId, OperationId, RunId) are
        // contributed by the owning module configurations.
        modelBuilder.Entity<EconomyOutboxRow>(builder =>
        {
            builder.ToTable("economy_outbox", table =>
                table.HasCheckConstraint("ck_economy_outbox_attempts", "\"AttemptCount\" >= 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.MessageType).HasMaxLength(200);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.LeaseOwner).HasMaxLength(256);
            builder.Property(row => row.LastError).HasMaxLength(1_000);
            // The SECURITY DEFINER posting writers enqueue without listing the
            // discriminator or the lease columns; the column defaults keep
            // those inserts valid (they are posting-accepted rows by
            // construction — OutboxKind = 1).
            builder.Property(row => row.OutboxKind).HasDefaultValue(EconomyOutboxKind.Posting);
            builder.Property(row => row.AttemptCount).HasDefaultValue(0);
            builder.HasIndex(row => row.PayloadHash)
                .IsUnique()
                .HasFilter("\"OutboxKind\" = 1")
                .HasDatabaseName("ux_economy_outbox_payload_hash");
            builder.HasIndex(row => new { row.PublishedAt, row.OccurredAt })
                .HasFilter("\"OutboxKind\" = 2")
                .HasDatabaseName("ix_economy_outbox_marketplace_dispatch");
            builder.HasIndex(row => new { row.PublishedAt, row.LeaseExpiresAt, row.OccurredAt })
                .HasFilter("\"OutboxKind\" = 2")
                .HasDatabaseName("ix_economy_outbox_marketplace_lease");
            builder.HasIndex(row => row.OperationId)
                .IsUnique()
                .HasFilter("\"OutboxKind\" = 3")
                .HasDatabaseName("ux_economy_outbox_payout_operation");
            builder.HasIndex(row => new { row.CompletedAt, row.AvailableAt, row.LeaseExpiresAt })
                .HasFilter("\"OutboxKind\" IN (3, 4)")
                .HasDatabaseName("ix_economy_outbox_dispatch_queue");
            builder.HasIndex(row => row.RunId)
                .IsUnique()
                .HasFilter("\"OutboxKind\" = 4")
                .HasDatabaseName("ux_economy_outbox_admin_run");
            builder.HasIndex(row => new { row.TenantId, row.RunId })
                .IsUnique()
                .HasFilter("\"OutboxKind\" = 4")
                .HasDatabaseName("ux_economy_outbox_admin_tenant_run");
            builder.HasIndex(row => row.EvidenceId)
                .IsUnique()
                .HasFilter("\"OutboxKind\" = 5")
                .HasDatabaseName("ux_economy_outbox_evidence");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyComplianceEvidenceRow>()
                .WithMany()
                .HasForeignKey(row => row.EvidenceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 2.3 — the two signed dual-control policy-version tables
        // merged behind PolicyDomain. Each domain keeps its (TenantId,
        // subject, Version) uniqueness through partial unique indexes, and
        // the effective-window indexes stay per domain so the effective and
        // historical readers (settlement uses effective, refund uses
        // historical) keep their index paths.
        modelBuilder.Entity<EconomyPolicyVersionRow>(builder =>
        {
            builder.ToTable("economy_policy_versions", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_policy_versions_version", "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\"");
                table.HasCheckConstraint(
                    "ck_economy_policy_versions_dual_control", "\"ProposedBy\" <> \"ApprovedBy\"");
                table.HasCheckConstraint(
                    "ck_economy_policy_versions_marketplace_shape",
                    "(\"PolicyDomain\" = 2) OR (\"SubjectId\" IS NOT NULL AND \"SellerId\" IS NOT NULL AND \"Mode\" IS NOT NULL AND \"PlatformFeeWalletId\" IS NOT NULL AND \"RefundHoldTicks\" IS NOT NULL AND \"PlatformFeePpm\" BETWEEN 0 AND 999999 AND \"RefundHoldTicks\" > 0 AND ((\"Mode\" = 1 AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" = 0) OR (\"Mode\" = 2 AND \"HardPriceUnits\" = 0 AND \"SoftPriceUnits\" > 0) OR (\"Mode\" IN (3, 4) AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" > 0)))");
                table.HasCheckConstraint(
                    "ck_economy_policy_versions_adnetwork_shape",
                    "(\"PolicyDomain\" = 1) OR (\"SubjectKey\" IS NOT NULL AND \"IssuanceMode\" IS NOT NULL AND \"YieldState\" IS NOT NULL AND \"ReportsCurrentThrough\" IS NOT NULL AND \"ProviderHash\" IS NOT NULL AND \"ProviderCertified\" IS NOT NULL AND \"ContractedRevenueSharePpm\" BETWEEN 0 AND 1000000 AND \"SafetyBufferPpm\" BETWEEN 0 AND 999999 AND \"MinimumVisiblePpm\" BETWEEN 0 AND 1000000 AND \"EstimatedNetEcpmUsdNanos\" > 0 AND \"MaximumRewardSoftUnits\" > 0 AND \"MaximumFocusLossTicks\" >= 0 AND \"ReportStaleAfterTicks\" > 0 AND \"Ranking\" >= 0 AND \"BudgetWindowTicks\" > 0 AND \"MaximumUserSoftUnits\" > 0 AND \"MaximumDeviceSoftUnits\" > 0 AND \"MaximumIpSoftUnits\" > 0 AND \"MaximumAsnSoftUnits\" > 0 AND \"MaximumNetworkSoftUnits\" > 0 AND \"MaximumGlobalSoftUnits\" > 0 AND \"FundedLossBudgetUsdNanos\" > 0)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).HasDefaultValueSql("gen_random_uuid()");
            builder.Property(row => row.SubjectKey).HasMaxLength(100);
            builder.Property(row => row.CanonicalPayload).HasColumnType("text");
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasColumnType("text");
            builder.HasIndex(row => new { row.TenantId, row.SubjectId, row.Version })
                .IsUnique()
                .HasFilter("\"PolicyDomain\" = 1")
                .HasDatabaseName("ux_economy_policy_versions_marketplace_version");
            builder.HasIndex(row => new { row.TenantId, row.SubjectKey, row.Version })
                .IsUnique()
                .HasFilter("\"PolicyDomain\" = 2")
                .HasDatabaseName("ux_economy_policy_versions_adnetwork_version");
            builder.HasIndex(row => new { row.TenantId, row.SubjectId, row.EffectiveAt, row.ExpiresAt })
                .HasFilter("\"PolicyDomain\" = 1")
                .HasDatabaseName("ix_economy_policy_versions_marketplace_window");
            builder.HasIndex(row => new { row.TenantId, row.SubjectKey, row.EffectiveAt, row.ExpiresAt })
                .HasFilter("\"PolicyDomain\" = 2")
                .HasDatabaseName("ix_economy_policy_versions_adnetwork_window");
        });

        // #1092 Tier 2.4 — payout operations and admin-withdrawal runs merged
        // behind OperationKind. The fencing discipline is preserved: each kind
        // keeps its own monotonic token allocator sequence, dispatch verifies
        // exact version + token + epoch, and Treasury's ExecutionEpoch (no
        // payout twin) stays as a kind-2-only column. Cross-module foreign
        // keys (outbox/provider-events OperationId and RunId, payout
        // authorization evidence, admin-withdrawal audit events) are
        // contributed by the Payouts and Treasury module configurations.
        modelBuilder.Entity<EconomyOperationRow>(builder =>
        {
            builder.ToTable("economy_operations", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_operations_state",
                    "(\"OperationKind\" = 1 AND \"State\" BETWEEN 1 AND 6) OR (\"OperationKind\" = 2 AND \"State\" BETWEEN 1 AND 7)");
                table.HasCheckConstraint(
                    "ck_economy_operations_positive_values",
                    "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0 AND ((\"OperationKind\" = 1 AND \"KillSwitchEpoch\" >= 0) OR (\"OperationKind\" = 2 AND \"KillSwitchEpoch\" IS NULL AND \"ExecutionEpoch\" > 0))");
                table.HasCheckConstraint(
                    "ck_economy_operations_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_operations_dispatch",
                    "(\"OperationKind\" = 1 AND ((\"State\" = 1 AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 2 AND 6 AND \"DispatchSnapshotHash\" IS NOT NULL))) OR " +
                    "(\"OperationKind\" = 2 AND ((\"State\" IN (1, 2) AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 3 AND 7 AND \"DispatchSnapshotHash\" IS NOT NULL)))");
                table.HasCheckConstraint(
                    "ck_economy_operations_approval",
                    "(\"OperationKind\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"OperationKind\" = 2 AND ((\"State\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"State\" BETWEEN 2 AND 7 AND \"ApprovedBy\" IS NOT NULL)))");
                table.HasCheckConstraint(
                    "ck_economy_operations_subject_shape",
                    "(\"OperationKind\" = 1 AND \"PayeeId\" IS NOT NULL AND \"WalletId\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"ProviderBindingHash\" IS NOT NULL AND \"EligibilityHash\" IS NOT NULL AND \"RiskDecisionId\" IS NOT NULL AND \"PeriodStart\" IS NULL AND \"PlatformFeeWalletId\" IS NULL AND \"SourceAssetKey\" IS NULL AND \"ExecutionEpoch\" IS NULL) OR " +
                    "(\"OperationKind\" = 2 AND \"PayeeId\" IS NULL AND \"WalletId\" IS NULL AND \"ProviderAccountId\" IS NULL AND \"ProviderBindingHash\" IS NULL AND \"EligibilityHash\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"KillSwitchEpoch\" IS NULL AND \"PeriodStart\" IS NOT NULL AND \"PlatformFeeWalletId\" IS NOT NULL AND \"SourceAssetKey\" IS NOT NULL AND \"ExecutionEpoch\" IS NOT NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ProviderAccountId).HasMaxLength(256);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.Property(row => row.ProviderBindingHash).HasMaxLength(128);
            builder.Property(row => row.EligibilityHash).HasMaxLength(128);
            builder.Property(row => row.DispatchSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.ProviderPayoutId).HasMaxLength(256);
            builder.Property(row => row.SourceAssetKey).HasMaxLength(256);
            builder.Property(row => row.ProviderTransferId).HasMaxLength(256);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.OperationKind, row.TenantId, row.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_economy_operations_kind_tenant_idempotency");
            builder.HasIndex(row => new { row.TenantId, row.PeriodStart })
                .IsUnique()
                .HasFilter("\"OperationKind\" = 2 AND \"State\" NOT IN (6, 7)")
                .HasDatabaseName("ux_economy_operations_active_period");
            builder.HasIndex(row => new { row.OperationKind, row.State, row.UpdatedAt })
                .HasDatabaseName("ix_economy_operations_kind_state_updated");
            builder.HasIndex(row => new { row.TenantId, row.OperationKind, row.State, row.UpdatedAt })
                .HasDatabaseName("ix_economy_operations_tenant_kind_state_updated");
        });

        modelBuilder.Entity<EconomyIdempotencyRecordRow>(builder =>
        {
            builder.ToTable("economy_idempotency_records");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Key).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.HasIndex(row => row.Key)
                .IsUnique()
                .HasDatabaseName("ux_economy_idempotency_records_key");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyChainHeadRow>(builder =>
        {
            builder.ToTable("economy_chain_head", table =>
                table.HasCheckConstraint("ck_economy_chain_head_singleton", "\"Id\" = 1"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Hash).HasMaxLength(128);
        });

        modelBuilder.Entity<EconomyExternalAnchorRow>(builder =>
        {
            builder.ToTable("economy_external_anchors");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.Signature).HasMaxLength(1024);
            builder.Property(row => row.WormReference).HasMaxLength(1024);
            builder.Property(row => row.DispatchSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.ProviderReference).HasMaxLength(256);
            builder.HasIndex(row => row.JournalSequence)
                .HasDatabaseName("ix_economy_external_anchors_chain_sequence");
        });
    }

    private static void ConfigureReserves(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyReserveHeadRow>(builder =>
        {
            builder.ToTable("economy_reserve_heads", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_reserve_heads_versions_positive",
                    "\"Version\" > 0 AND \"PolicyVersion\" > 0 AND \"AuthorizationEpoch\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_reserve_heads_window",
                    "\"ExpiresAt\" > \"ObservedAt\" AND \"ActivatedAt\" >= \"ObservedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_reserve_heads_amounts_nonnegative",
                    "\"HardFaceValueUsdMinor\" >= 0 AND \"RequiredHardReserveUsdMinor\" >= 0 AND " +
                    "\"SoftFaceValueUsdNanos\" >= 0 AND \"StressedExpectedRedemptionCostUsdNanos\" >= 0 AND " +
                    "\"RequiredSoftReserveUsdNanos\" >= 0 AND \"HardBackingUsdNanos\" >= 0 AND \"SoftBackingUsdNanos\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_reserve_heads_values_valid",
                    "\"Coverage\" IN (1, 2) AND length(btrim(\"EvidenceHash\")) > 0");
            });
            builder.HasKey(row => row.Version);
            builder.Property(row => row.Version).ValueGeneratedNever();
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => row.IsActive)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE")
                .HasDatabaseName("ux_economy_reserve_heads_active");
            builder.HasIndex(row => row.AuthorizationEpoch)
                .IsUnique()
                .HasDatabaseName("ux_economy_reserve_heads_authorization_epoch");
        });

        modelBuilder.Entity<EconomyReserveAssetAllocationRow>(builder =>
        {
            builder.ToTable("economy_reserve_asset_allocations", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_reserve_asset_allocations_value_positive",
                    "\"EligibleUsdNanos\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_reserve_asset_allocations_values_valid",
                    "\"Purpose\" IN (1, 2) AND length(btrim(\"AssetKey\")) > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.AssetKey).HasMaxLength(256);
            builder.HasIndex(row => new { row.ReserveVersion, row.AssetKey })
                .IsUnique()
                .HasDatabaseName("ux_economy_reserve_asset_allocations_version_asset");
            builder.HasOne<EconomyReserveHeadRow>()
                .WithMany()
                .HasForeignKey(row => row.ReserveVersion)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRisk(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyRegisteredCapabilityRow>(builder =>
        {
            builder.ToTable("economy_registered_capabilities", table =>
                table.HasCheckConstraint(
                    "ck_economy_registered_capabilities_state",
                    "(\"IsEnabled\" AND \"RevokedAt\" IS NULL) OR (NOT \"IsEnabled\" AND \"RevokedAt\" IS NOT NULL)"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Name).HasMaxLength(100);
            builder.Property(row => row.AllowedTemplateKinds).HasColumnType("jsonb");
            builder.HasIndex(row => row.Name)
                .IsUnique()
                .HasDatabaseName("ux_economy_registered_capabilities_name");
        });

        modelBuilder.Entity<EconomyRiskDecisionRow>(builder =>
        {
            builder.ToTable("economy_risk_decisions", table =>
            {
                table.HasCheckConstraint("ck_economy_risk_decisions_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint("ck_economy_risk_decisions_lifetime", "\"ExpiresAt\" > \"IssuedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_risk_decisions_versions_positive",
                    "\"PolicyVersion\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND " +
                    "\"FeatureVersion\" > 0 AND \"CounterVersion\" > 0 AND \"EntityGraphVersion\" >= 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.OperationFingerprint).HasMaxLength(128);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(128);
            builder.Property(row => row.ActorHash).HasMaxLength(128);
            builder.Property(row => row.ProviderReferenceHash).HasMaxLength(128);
            builder.Property(row => row.EntityGraphEvidenceHash).HasMaxLength(128);
            builder.Property(row => row.CurrencyLegs).HasColumnType("jsonb");
            builder.Property(row => row.SourceRoots).HasColumnType("jsonb");
            builder.Property(row => row.ReasonCodes).HasColumnType("jsonb");
            builder.HasIndex(row => row.OperationFingerprint)
                .IsUnique()
                .HasDatabaseName("ix_economy_risk_decisions_operation_fingerprint");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceWalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.DestinationWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 2.2 — the four consumption tables (capability receipts,
        // risk decisions, ad-reward caps, ad-reward budgets) merged behind
        // ConsumptionKind. The risk-decision kind keeps its decision and
        // posting-group partial uniques and its insert-validation trigger;
        // the capability-receipt kind keeps the receipt partial unique the
        // SECURITY DEFINER posting writers join against. The session foreign
        // key is contributed by the AdRewards module configuration.
        modelBuilder.Entity<EconomyUnitConsumptionRow>(builder =>
        {
            builder.ToTable("economy_unit_consumptions", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_unit_consumptions_kind_shape",
                    "(\"ConsumptionKind\" = 1 AND \"ReceiptId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"OperationFingerprint\" IS NOT NULL AND \"KillSwitchEpoch\" IS NOT NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"SessionId\" IS NULL AND \"Scope\" IS NULL AND \"SubjectHash\" IS NULL) OR " +
                    "(\"ConsumptionKind\" = 2 AND \"RiskDecisionId\" IS NOT NULL AND \"PostingGroupId\" IS NOT NULL AND \"OperationFingerprint\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"ActorId\" IS NULL AND \"KillSwitchEpoch\" IS NULL AND \"SessionId\" IS NULL AND \"Scope\" IS NULL AND \"SubjectHash\" IS NULL) OR " +
                    "(\"ConsumptionKind\" = 3 AND \"SessionId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"Scope\" IS NOT NULL AND \"SubjectHash\" IS NOT NULL AND \"WindowStartedAt\" IS NOT NULL AND \"WindowEndsAt\" IS NOT NULL AND \"SoftUnits\" IS NOT NULL AND \"LossBudgetUsdNanos\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"WindowEndsAt\" > \"WindowStartedAt\" AND \"ConsumedAt\" >= \"WindowStartedAt\" AND \"ConsumedAt\" < \"WindowEndsAt\" AND \"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" >= 0 AND \"Scope\" BETWEEN 1 AND 6) OR " +
                    "(\"ConsumptionKind\" = 4 AND \"SessionId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"UserId\" IS NOT NULL AND \"Network\" IS NOT NULL AND \"DeviceRiskHash\" IS NOT NULL AND \"SoftUnits\" IS NOT NULL AND \"LossBudgetUsdNanos\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" > 0)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.OperationFingerprint).HasMaxLength(128);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.DeviceRiskHash).HasMaxLength(128);
            builder.HasIndex(row => row.ReceiptId)
                .IsUnique()
                .HasFilter("\"ConsumptionKind\" = 1")
                .HasDatabaseName("ux_economy_unit_consumptions_receipt");
            builder.HasIndex(row => row.RiskDecisionId)
                .IsUnique()
                .HasFilter("\"ConsumptionKind\" = 2")
                .HasDatabaseName("ux_economy_unit_consumptions_risk_decision");
            builder.HasIndex(row => row.PostingGroupId)
                .IsUnique()
                .HasFilter("\"ConsumptionKind\" = 2")
                .HasDatabaseName("ux_economy_unit_consumptions_posting");
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
            builder.HasOne<EconomyCapabilityReceiptRow>()
                .WithMany()
                .HasForeignKey(row => row.ReceiptId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyRiskDecisionRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyRiskCounterRow>(builder =>
        {
            builder.ToTable("economy_risk_counters", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_risk_counters_bounds",
                    "\"CounterVersion\" > 0 AND \"MaxUnits\" > 0 AND \"UsedUnits\" >= 0 AND \"UsedUnits\" <= \"MaxUnits\"");
                table.HasCheckConstraint(
                    "ck_economy_risk_counters_window",
                    "\"WindowEndsAt\" > \"WindowStartedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.HasIndex(row => new
            {
                row.TenantId,
                row.Dimension,
                row.SubjectHash,
                row.Operation,
                row.Currency,
                row.WindowStartedAt
            })
                .IsUnique()
                .HasDatabaseName("ux_economy_risk_counters_scope_window");
        });

        modelBuilder.Entity<EconomyRiskCounterReservationRow>(builder =>
        {
            builder.ToTable("economy_risk_counter_reservations", table =>
            {
                table.HasCheckConstraint("ck_economy_risk_counter_reservations_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_risk_counter_reservations_lifetime",
                    "\"ExpiresAt\" > \"ReservedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_risk_counter_reservations_state",
                    "(\"Status\" = 1 AND \"ConsumedAt\" IS NULL AND \"ReleasedAt\" IS NULL) OR " +
                    "(\"Status\" = 2 AND \"ConsumedAt\" >= \"ReservedAt\" AND \"ReleasedAt\" IS NULL) OR " +
                    "(\"Status\" = 3 AND \"ReleasedAt\" >= \"ReservedAt\" AND \"ConsumedAt\" IS NULL) OR " +
                    "(\"Status\" = 4 AND \"ReleasedAt\" >= \"ExpiresAt\" AND \"ConsumedAt\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.InputFingerprint).HasMaxLength(128);
            builder.HasIndex(row => new { row.ReservationGroupId, row.RiskCounterId })
                .IsUnique()
                .HasDatabaseName("ux_economy_risk_counter_reservations_group_counter");
            builder.HasIndex(row => new { row.RiskDecisionId, row.RiskCounterId })
                .IsUnique()
                .HasDatabaseName("ux_economy_risk_counter_reservations_decision_counter");
            builder.HasOne<EconomyRiskDecisionRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyRiskCounterRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskCounterId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyProtectedChangeCooldownRow>(builder =>
        {
            builder.ToTable("economy_protected_change_cooldowns", table =>
            {
                table.HasCheckConstraint("ck_economy_protected_change_cooldowns_version", "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_protected_change_cooldowns_window",
                    "\"AvailableAt\" > \"ChangedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ValueHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.SubjectId, row.Kind, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_protected_change_cooldowns_subject_kind_version");
        });

        modelBuilder.Entity<EconomyHoldRow>(builder =>
        {
            builder.ToTable("economy_holds", table =>
            {
                table.HasCheckConstraint("ck_economy_holds_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_holds_state_timestamp",
                    "(\"Status\" = 1 AND \"ReleasedAt\" IS NULL) OR (\"Status\" <> 1 AND \"ReleasedAt\" >= \"EffectiveAt\")");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.WalletId, row.Status })
                .HasDatabaseName("ix_economy_holds_wallet_status");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // economy_hold_events merged into economy_events (#1092 Tier 2.1,
        // StreamKind = 4) — see the EconomyEventRow configuration above.

        modelBuilder.Entity<EconomyRiskReviewCaseRow>(builder =>
        {
            builder.ToTable("economy_risk_review_cases", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_risk_review_cases_approvals",
                    "\"RequiredApprovals\" BETWEEN 1 AND 2");
                table.HasCheckConstraint(
                    "ck_economy_risk_review_cases_state",
                    "(\"Status\" = 1 AND \"ResolvedAt\" IS NULL AND \"ResolvedBy\" IS NULL AND \"Resolution\" IS NULL) OR (\"Status\" IN (2, 3) AND \"ResolvedAt\" >= \"SubmittedAt\" AND \"ResolvedBy\" IS NOT NULL AND length(btrim(\"Resolution\")) > 0)");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.TenantId, row.RiskDecisionId })
                .IsUnique()
                .HasDatabaseName("ux_economy_risk_review_cases_tenant_decision");
            builder.HasOne<EconomyRiskDecisionRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyRiskReviewCaseRow>()
                .WithMany()
                .HasForeignKey(row => row.AppealOf)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // economy_risk_review_events merged into economy_events (#1092 Tier 2.1,
        // StreamKind = 5) — see the EconomyEventRow configuration above.

        modelBuilder.Entity<EconomyRiskAuditEvidenceRow>(builder =>
        {
            builder.ToTable("economy_risk_audit_evidence");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EventKind).HasMaxLength(100);
            builder.Property(row => row.OperationFingerprint).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.Payload).HasColumnType("jsonb");
            builder.HasIndex(row => new { row.RiskDecisionId, row.EvidenceHash })
                .IsUnique()
                .HasDatabaseName("ux_economy_risk_audit_evidence_decision_hash");
            builder.HasOne<EconomyRiskDecisionRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyTopUpIntentRow>(builder =>
        {
            builder.ToTable("economy_top_up_intents", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_top_up_intents_amount_positive",
                    "\"HardCoinUnits\" > 0 AND \"UsdMinorUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_top_up_intents_provider_binding",
                    "(\"Status\" = 1 AND \"ProviderEnvironment\" IS NULL AND \"ProviderAccountId\" IS NULL AND " +
                    "\"ProviderObjectId\" IS NULL AND \"ProviderObjectType\" IS NULL AND " +
                    "\"ProviderMonetaryLeg\" IS NULL AND \"ProviderBoundAt\" IS NULL) OR " +
                    "(\"Status\" <> 1 AND \"ProviderEnvironment\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND " +
                    "\"ProviderObjectId\" IS NOT NULL AND \"ProviderObjectType\" IS NOT NULL AND " +
                    "\"ProviderMonetaryLeg\" IS NOT NULL AND \"ProviderBoundAt\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_economy_top_up_intents_version_positive",
                    "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_top_up_intents_event_state",
                    "(\"LastProviderEventId\" IS NULL AND \"LastProviderEventAt\" IS NULL AND " +
                    "\"LastProviderEvidenceHash\" IS NULL) OR (\"LastProviderEventId\" IS NOT NULL AND " +
                    "\"LastProviderEventAt\" IS NOT NULL AND \"LastProviderEvidenceHash\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_economy_top_up_intents_posting_state",
                    "(\"Status\" = 5 AND \"PostingGroupId\" IS NOT NULL) OR " +
                    "(\"Status\" <> 5 AND \"PostingGroupId\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.PolicyHash).HasMaxLength(128);
            builder.Property(row => row.Provider).HasMaxLength(64);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ProviderEnvironment).HasMaxLength(32);
            builder.Property(row => row.ProviderAccountId).HasMaxLength(255);
            builder.Property(row => row.ProviderObjectId).HasMaxLength(255);
            builder.Property(row => row.ProviderObjectType).HasMaxLength(100);
            builder.Property(row => row.ProviderMonetaryLeg).HasMaxLength(100);
            builder.Property(row => row.LastProviderEventId).HasMaxLength(255);
            builder.Property(row => row.LastProviderEvidenceHash).HasMaxLength(128);
            builder.Property(row => row.FailureCode).HasMaxLength(100);
            builder.Property(row => row.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.TenantId, row.ActorId, row.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_economy_top_up_intents_actor_key");
            builder.HasIndex(row => row.PaymentId)
                .IsUnique()
                .HasDatabaseName("ux_economy_top_up_intents_payment");
            builder.HasIndex(row => new
                {
                    row.Provider,
                    row.ProviderEnvironment,
                    row.ProviderAccountId,
                    row.ProviderObjectId,
                    row.ProviderObjectType,
                    row.ProviderMonetaryLeg
                })
                .IsUnique()
                .HasFilter("\"ProviderObjectId\" IS NOT NULL")
                .HasDatabaseName("ux_economy_top_up_intents_provider_object");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSelfServiceTransfers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomySelfServiceTransferIntentRow>(builder =>
        {
            builder.ToTable("economy_self_service_transfer_intents", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_self_service_transfer_intents_amount_positive",
                    "\"AmountUnits\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_self_service_transfer_intents_type_valid",
                    "\"TransferType\" IN (1, 2, 3)");
                table.HasCheckConstraint(
                    "ck_economy_self_service_transfer_intents_currency_provenance",
                    "(\"Currency\" = 1 AND \"Provenance\" = 1) OR " +
                    "(\"Currency\" = 2 AND \"Provenance\" = 3)");
                table.HasCheckConstraint(
                    "ck_economy_self_service_transfer_intents_parties_distinct",
                    "\"ActorId\" <> \"RecipientUserId\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ProviderReferenceHash).HasMaxLength(128);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.ActorId, row.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_economy_self_service_transfer_intents_actor_key");
            builder.HasIndex(row => new { row.TenantId, row.RecipientUserId, row.RequestedAt })
                .HasDatabaseName("ix_economy_self_service_transfer_intents_recipient_time");
        });
    }

    private static void ConfigureEconomyWalletProvisioningReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyWalletProvisioningReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.WalletId).HasColumnName("wallet_id");
            builder.Property(row => row.Created).HasColumnName("created");
        });
    }

    private static void ConfigureLegacyShadowMigration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyLegacyShadowBatchRow>(builder =>
        {
            builder.ToTable("economy_legacy_shadow_batches", table =>
            {
                table.HasCheckConstraint("ck_economy_legacy_shadow_batches_counts",
                    "\"WalletCount\" >= 0 AND \"TransactionCount\" >= 0 AND \"FinancialLedgerEntryCount\" >= 0");
                table.HasCheckConstraint("ck_economy_legacy_shadow_batches_units",
                    "\"ExpectedHardUnits\" >= 0 AND \"BackfilledHardUnits\" >= 0 AND \"ReconciledHardUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_legacy_shadow_batches_state", "\"State\" BETWEEN 1 AND 8");
                table.HasCheckConstraint("ck_economy_legacy_shadow_batches_version", "\"Version\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.WalletSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.TransactionSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.FinancialLedgerSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.FailureCode).HasMaxLength(128);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.TenantId, row.State });
            builder.HasIndex(row => row.RequestHash).IsUnique();
        });

        modelBuilder.Entity<EconomyLegacyShadowWalletRow>(builder =>
        {
            builder.ToTable("economy_legacy_shadow_wallets", table =>
            {
                table.HasCheckConstraint("ck_economy_legacy_shadow_wallets_units",
                    "\"LegacyBalanceMinorUnits\" >= 0 AND \"CompletedCreditsMinorUnits\" >= 0 AND \"CompletedDebitsMinorUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_legacy_shadow_wallets_transactions", "\"TransactionCount\" >= 0");
                table.HasCheckConstraint("ck_economy_legacy_shadow_wallets_state", "\"State\" BETWEEN 1 AND 5");
                table.HasCheckConstraint("ck_economy_legacy_shadow_wallets_version", "\"Version\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SnapshotHash).HasMaxLength(128);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.ReconciliationHash).HasMaxLength(128);
            builder.Property(row => row.FailureCode).HasMaxLength(128);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => new { row.BatchId, row.LegacyWalletId }).IsUnique();
            builder.HasIndex(row => row.SourceStampId).IsUnique();
            builder.HasIndex(row => row.PostingId).IsUnique();
            builder.HasIndex(row => row.CreditLotId).IsUnique();
            builder.HasOne<EconomyLegacyShadowBatchRow>().WithMany().HasForeignKey(row => row.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>().WithMany().HasForeignKey(row => row.EconomyWalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyLegacyCutoverRow>(builder =>
        {
            builder.ToTable("economy_legacy_cutovers", table =>
            {
                table.HasCheckConstraint("ck_economy_legacy_cutovers_state", "\"State\" BETWEEN 1 AND 4");
                table.HasCheckConstraint("ck_economy_legacy_cutovers_epoch", "\"Epoch\" > 0 AND \"Version\" > 0");
            });
            builder.HasKey(row => row.TenantId);
            builder.Property(row => row.ReauthenticationHash).HasMaxLength(128);
            builder.Property(row => row.Reason).HasMaxLength(500);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => row.BatchId).IsUnique();
            builder.HasOne<EconomyLegacyShadowBatchRow>().WithOne().HasForeignKey<EconomyLegacyCutoverRow>(row => row.BatchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // economy_legacy_cutover_audit merged into economy_events (#1092 Tier 2.1,
        // StreamKind = 7) — see the EconomyEventRow configuration above.
    }

    private static void ConfigureControlPlane(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyCapabilityPolicyRow>(builder =>
        {
            builder.ToTable("economy_capability_policies", table =>
            {
                table.HasCheckConstraint("ck_economy_capability_policies_version", "\"Version\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_capability_policies_dual_control",
                    "(\"ApprovedBy\" IS NULL AND \"ApprovedAt\" IS NULL AND NOT \"IsActive\") OR " +
                    "(\"ApprovedBy\" IS NOT NULL AND \"ApprovedBy\" <> \"ProposedBy\" AND \"ApprovedAt\" >= \"ProposedAt\")");
                table.HasCheckConstraint(
                    "ck_economy_capability_policies_window",
                    "\"ExpiresAt\" > \"EffectiveAt\" AND (\"ApprovedAt\" IS NULL OR \"EffectiveAt\" >= \"ApprovedAt\")");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ScopeKey).HasMaxLength(256);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.CanonicalPayload).HasColumnType("jsonb");
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasMaxLength(2048);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.HasIndex(row => row.RequestHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_capability_policies_request_hash");
            builder.HasIndex(row => new { row.ScopeKey, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_capability_policies_scope_version");
            builder.HasIndex(row => row.ScopeKey)
                .IsUnique()
                .HasFilter("\"IsActive\"")
                .HasDatabaseName("ux_economy_capability_policies_active_scope");
        });

        // #1092 Tier 1.3 — the three dual-control approval tables (capability
        // policy, kill-switch release, projection generation) merged behind
        // ApprovalKind. Per-kind unique indexes preserve the dual-control
        // one-actor-one-vote rule for each subject family. The payout-request
        // dual control is not part of this family and stays untouched.
        modelBuilder.Entity<EconomyApprovalRow>(builder =>
        {
            builder.ToTable("economy_approvals");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ReauthenticationHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.ApprovalKind, row.SubjectId, row.ActorId })
                .IsUnique()
                .HasDatabaseName("ux_economy_approvals_kind_subject_actor");
            builder.HasIndex(row => new { row.ApprovalKind, row.Generation, row.ActorId })
                .IsUnique()
                .HasDatabaseName("ux_economy_approvals_kind_generation_actor");
        });

        modelBuilder.Entity<EconomyCapabilityReceiptRow>(builder =>
        {
            builder.ToTable("economy_capability_receipts", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_capability_receipts_versions",
                    "\"PolicyVersion\" > 0 AND \"ReserveVersion\" > 0 AND \"KillSwitchEpoch\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_capability_receipts_lifetime",
                    "\"ExpiresAt\" > \"IssuedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SubjectReference).HasMaxLength(256);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.OperationFingerprint).HasMaxLength(128);
            builder.Property(row => row.ProviderHash).HasMaxLength(128);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.Property(row => row.SourceRootHashes).HasColumnType("jsonb");
            builder.Property(row => row.EvidenceHashes).HasColumnType("jsonb");
            builder.Property(row => row.ReceiptHash).HasMaxLength(128);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasMaxLength(2048);
            builder.HasIndex(row => row.ReceiptHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_capability_receipts_hash");
            builder.HasIndex(row => new { row.TenantId, row.OperationFingerprint })
                .IsUnique()
                .HasDatabaseName("ux_economy_capability_receipts_tenant_operation");
            builder.HasOne<EconomyRiskDecisionRow>()
                .WithMany()
                .HasForeignKey(row => row.RiskDecisionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 2.2: economy_capability_receipt_consumptions merged into
        // economy_unit_consumptions (ConsumptionKind = 1 = CapabilityReceipt).
        // The registration lives with the unified row above.

        modelBuilder.Entity<EconomyKillSwitchRow>(builder =>
        {
            builder.ToTable("economy_kill_switches", table =>
            {
                table.HasCheckConstraint("ck_economy_kill_switches_epoch", "\"Epoch\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_kill_switches_state",
                    "(\"IsActive\" AND \"ReleasedAt\" IS NULL) OR (NOT \"IsActive\" AND \"ReleasedAt\" >= \"ActivatedAt\")");
                table.HasCheckConstraint(
                    "ck_economy_kill_switches_release_proposal",
                    "(\"ReleaseProposedBy\" IS NULL AND \"ReleaseProposedAt\" IS NULL AND \"ReleaseProposalReauthenticationHash\" IS NULL) OR " +
                    "(\"ReleaseProposedBy\" IS NOT NULL AND \"ReleaseProposedAt\" >= \"ActivatedAt\" AND length(btrim(\"ReleaseProposalReauthenticationHash\")) > 0)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ScopeKey).HasMaxLength(256);
            builder.Property(row => row.Reason).HasMaxLength(1000);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ReleaseProposalReauthenticationHash).HasMaxLength(128);
            builder.HasIndex(row => row.RequestHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_kill_switches_request_hash");
            builder.HasIndex(row => new { row.ScopeKey, row.Epoch })
                .IsUnique()
                .HasDatabaseName("ux_economy_kill_switches_scope_epoch");
            builder.HasIndex(row => row.ScopeKey)
                .IsUnique()
                .HasFilter("\"IsActive\"")
                .HasDatabaseName("ux_economy_kill_switches_active_scope");
        });

        // economy_kill_switch_release_approvals merged into economy_approvals
        // (#1092 Tier 1.3) — see the EconomyApprovalRow configuration above.

        modelBuilder.Entity<EconomyEntityGraphNodeRow>(builder =>
        {
            builder.ToTable("economy_entity_graph_nodes", table =>
                table.HasCheckConstraint("ck_economy_entity_graph_nodes_version", "\"Version\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.IdentityHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.Type, row.IdentityHash, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_entity_graph_nodes_identity_version");
        });

        modelBuilder.Entity<EconomyEntityGraphEdgeRow>(builder =>
        {
            builder.ToTable("economy_entity_graph_edges", table =>
            {
                table.HasCheckConstraint("ck_economy_entity_graph_edges_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_entity_graph_edges_distinct_nodes", "\"LeftNodeId\" <> \"RightNodeId\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Relationship).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.LeftNodeId, row.RightNodeId, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_entity_graph_edges_pair_version");
            builder.HasOne<EconomyEntityGraphNodeRow>()
                .WithMany()
                .HasForeignKey(row => row.LeftNodeId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyEntityGraphNodeRow>()
                .WithMany()
                .HasForeignKey(row => row.RightNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyComplianceEvidenceRow>(builder =>
        {
            builder.ToTable("economy_compliance_evidence", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_compliance_evidence_versions",
                    "\"Version\" > 0 AND \"PolicyVersion\" > 0 AND length(btrim(\"EvidenceKind\")) > 0");
                table.HasCheckConstraint(
                    "ck_economy_compliance_evidence_lifetime",
                    "\"ExpiresAt\" > \"IssuedAt\" AND \"ReceivedAt\" >= \"IssuedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_compliance_evidence_jurisdiction",
                    "\"JurisdictionCode\" IS NULL OR \"JurisdictionCode\" ~ '^[A-Z]{3}$'");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceKind).HasMaxLength(100);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(3).IsFixedLength();
            builder.Property(row => row.Result).HasMaxLength(100);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.RawObjectReference).HasMaxLength(1000);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.Provider, row.Environment, row.ProviderEventId })
                .IsUnique()
                .HasDatabaseName("ux_economy_compliance_evidence_provider_event");
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.EvidenceKind, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_compliance_evidence_subject_version");
        });

        // #1092 Tier 1.2 — the compliance inbox and the ad-reward
        // provider-proof inbox merged behind InboxKind. The table is
        // deliberately FK-free: it must accept events for aggregates under
        // rebuild (#1092 §3.3 guardrail).
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

        // economy_compliance_outbox was retired by the #1092 Tier 1.1 merge:
        // it never had a reader (evidence is consumed from the evidence
        // tables directly) and its rows now live in economy_outbox with
        // OutboxKind = 5 (Compliance).

        modelBuilder.Entity<EconomyComplianceHoldRow>(builder =>
        {
            builder.ToTable("economy_compliance_holds", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_compliance_holds_lifetime",
                    "\"ExpiresAt\" > \"ActivatedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_compliance_holds_release",
                    "(\"ReleasedAt\" IS NULL AND \"ReleasedBy\" IS NULL) OR " +
                    "(\"ReleasedAt\" >= \"ActivatedAt\" AND \"ReleasedBy\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_economy_compliance_holds_release_proposal",
                    "(\"ReleaseProposedAt\" IS NULL AND \"ReleaseProposedBy\" IS NULL AND " +
                    "\"RequiredReleaseApprovals\" IS NULL AND \"ReleasePolicyEvidenceHash\" IS NULL) OR " +
                    "(\"ReleaseProposedAt\" >= \"ActivatedAt\" AND \"ReleaseProposedBy\" IS NOT NULL AND " +
                    "\"RequiredReleaseApprovals\" BETWEEN 1 AND 2 AND " +
                    "length(btrim(\"ReleasePolicyEvidenceHash\")) > 0)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ScopeKey).HasMaxLength(512);
            builder.Property(row => row.SubjectHash).HasMaxLength(128);
            builder.Property(row => row.CaseReferenceHash).HasMaxLength(128);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.IdempotencyKeyHash).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ReleasePolicyEvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => row.IdempotencyKeyHash).IsUnique()
                .HasDatabaseName("ux_economy_compliance_holds_idempotency");
            builder.HasIndex(row => new { row.ScopeKey, row.ReleasedAt, row.ExpiresAt })
                .HasDatabaseName("ix_economy_compliance_holds_active_scope");
            builder.HasIndex(row => new { row.TenantId, row.SubjectHash, row.ExpiresAt });
        });

        // economy_compliance_hold_events merged into economy_events (#1092 Tier
        // 2.1, StreamKind = 6) — see the EconomyEventRow configuration above.

        modelBuilder.Entity<EconomyCustodyObservationRow>(builder =>
        {
            builder.ToTable("economy_custody_observations", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_custody_observations_values",
                    "\"Version\" > 0 AND \"EligibleUsdNanos\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_custody_observations_lifetime",
                    "\"ExpiresAt\" > \"ObservedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.AssetKey).HasMaxLength(256);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasMaxLength(2048);
            builder.HasIndex(row => new { row.Provider, row.AssetKey, row.Version })
                .IsUnique()
                .HasDatabaseName("ux_economy_custody_observations_provider_asset_version");
        });

        modelBuilder.Entity<EconomyCustodyReconciliationRow>(builder =>
        {
            builder.ToTable("economy_custody_reconciliations", table =>
                table.HasCheckConstraint(
                    "ck_economy_custody_reconciliations_values",
                    "\"ReserveVersion\" > 0 AND \"LiabilityUsdNanos\" >= 0 AND \"EligibleAssetUsdNanos\" >= 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ObservationIds).HasColumnType("jsonb");
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => row.ReserveVersion)
                .IsUnique()
                .HasDatabaseName("ux_economy_custody_reconciliations_reserve");
        });

        modelBuilder.Entity<EconomyReserveProposalRow>(builder =>
        {
            builder.ToTable("economy_reserve_proposals", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_reserve_proposals_values",
                    "\"Version\" > 0 AND \"PolicyVersion\" > 0 AND \"AuthorizationEpoch\" > 0 AND " +
                    "\"LiabilityUsdNanos\" >= 0 AND \"EligibleAssetUsdNanos\" >= 0 AND " +
                    "\"HardFaceValueUsdMinor\" >= 0 AND \"RequiredHardReserveUsdMinor\" >= 0 AND " +
                    "\"SoftFaceValueUsdNanos\" >= 0 AND \"RequiredSoftReserveUsdNanos\" >= 0 AND " +
                    "\"HardBackingUsdNanos\" >= 0 AND \"SoftBackingUsdNanos\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_reserve_proposals_dual_control",
                    "\"ApprovedBy\" IS NULL OR \"ApprovedBy\" <> \"ProposedBy\"");
                table.HasCheckConstraint(
                    "ck_economy_reserve_proposals_window",
                    "\"ExpiresAt\" > \"ObservedAt\" AND \"ProposedAt\" >= \"ObservedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SnapshotHash).HasMaxLength(128);
            builder.Property(row => row.ObservationIds).HasColumnType("jsonb");
            builder.Property(row => row.AssetAllocations).HasColumnType("jsonb");
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ApprovalReauthenticationHash).HasMaxLength(128);
            builder.Property(row => row.Status).HasMaxLength(50);
            builder.HasIndex(row => row.Version)
                .IsUnique()
                .HasDatabaseName("ux_economy_reserve_proposals_version");
        });

        modelBuilder.Entity<EconomyJournalVerificationCheckpointRow>(builder =>
        {
            builder.ToTable("economy_journal_verification_checkpoints", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_journal_verification_checkpoints_range",
                    "\"FromSequence\" >= 0 AND \"ToSequence\" >= \"FromSequence\"");
                table.HasCheckConstraint(
                    "ck_economy_journal_verification_checkpoints_time",
                    "\"CompletedAt\" >= \"StartedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.PreviousHash).HasMaxLength(128);
            builder.Property(row => row.CurrentHash).HasMaxLength(128);
            builder.Property(row => row.FailureCode).HasMaxLength(100);
            builder.HasIndex(row => new { row.ToSequence, row.CompletedAt })
                .HasDatabaseName("ix_economy_journal_verification_checkpoints_sequence");
        });

        modelBuilder.Entity<EconomyProjectionGenerationRow>(builder =>
        {
            builder.ToTable("economy_projection_generations", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_projection_generations_range",
                    "\"Generation\" > 0 AND \"FromSequence\" >= 0 AND \"ToSequence\" >= \"FromSequence\"");
                table.HasCheckConstraint(
                    "ck_economy_projection_generations_dual_control",
                    "(\"ApprovedBy\" IS NULL OR \"ApprovedBy\" <> \"ProposedBy\") AND " +
                    "(\"SecondApprovedBy\" IS NULL OR (\"SecondApprovedBy\" <> \"ProposedBy\" AND \"SecondApprovedBy\" <> \"ApprovedBy\"))");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.ProjectionHash).HasMaxLength(128);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.State).HasMaxLength(50);
            builder.HasIndex(row => row.Generation).IsUnique();
            builder.HasIndex(row => row.IsActive)
                .IsUnique()
                .HasFilter("\"IsActive\"")
                .HasDatabaseName("ux_economy_projection_generations_active");
        });

        modelBuilder.Entity<EconomyWalletProjectionGenerationRow>(builder =>
        {
            builder.ToTable("economy_wallet_projection_generations", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_wallet_projection_generations_amounts",
                    "\"Generation\" > 0 AND \"PendingHard\" >= 0 AND \"PendingSoft\" >= 0 AND " +
                    "\"PurchasedHard\" >= 0 AND \"EarnedHard\" >= 0 AND \"RestrictedHard\" >= 0 AND " +
                    "\"Soft\" >= 0 AND \"ImmatureEarnedHard\" >= 0 AND \"HeldHard\" >= 0 AND " +
                    "\"HeldSoft\" >= 0 AND \"AvailableHardToSpend\" >= 0 AND " +
                    "\"AvailableSoftToSpend\" >= 0 AND \"WithdrawableHard\" >= 0");
            });
            builder.HasKey(row => new { row.Generation, row.WalletId });
            builder.Property(row => row.ProjectionHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.Generation, row.MatchesLive });
            builder.HasOne<EconomyProjectionGenerationRow>()
                .WithMany().HasForeignKey(row => row.Generation).HasPrincipalKey(row => row.Generation)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany().HasForeignKey(row => row.WalletId).OnDelete(DeleteBehavior.Restrict);
        });

        // economy_projection_generation_approvals merged into economy_approvals
        // (#1092 Tier 1.3) — see the EconomyApprovalRow configuration above.

        modelBuilder.Entity<EconomyAnchorVerificationRow>(builder =>
        {
            builder.ToTable("economy_anchor_verifications");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.ObjectVersion).HasMaxLength(256);
            builder.Property(row => row.ETag).HasMaxLength(256);
            builder.Property(row => row.ObjectHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.ExternalAnchorId, row.VerifiedAt });
            builder.HasOne<EconomyExternalAnchorRow>()
                .WithMany()
                .HasForeignKey(row => row.ExternalAnchorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyWorkerLeaseRow>(builder =>
        {
            builder.ToTable("economy_worker_leases", table =>
            {
                table.HasCheckConstraint("ck_economy_worker_leases_fencing", "\"FencingToken\" > 0");
                table.HasCheckConstraint("ck_economy_worker_leases_lifetime", "\"ExpiresAt\" > \"AcquiredAt\"");
            });
            builder.HasKey(row => row.Name);
            builder.Property(row => row.Name).HasMaxLength(100);
            builder.Property(row => row.Owner).HasMaxLength(256);
            builder.HasIndex(row => row.Name)
                .IsUnique()
                .HasDatabaseName("ux_economy_worker_leases_name");
        });
    }

    private static void ConfigureFifoFragmentReservationReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FifoFragmentReservationReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.ReservationId).HasColumnName("reservation_id");
            builder.Property(row => row.ParentLotId).HasColumnName("parent_lot_id");
            builder.Property(row => row.RootSourceStampId).HasColumnName("root_source_stamp_id");
            builder.Property(row => row.ReversalEpoch).HasColumnName("reversal_epoch");
            builder.Property(row => row.StartInclusive).HasColumnName("start_inclusive");
            builder.Property(row => row.EndExclusive).HasColumnName("end_exclusive");
            builder.Property(row => row.AmountUnits).HasColumnName("amount_units");
        });
    }

    private static void ConfigureMarketplaceFifoReservationReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketplaceFifoReservationReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.ReservationId).HasColumnName("reservation_id");
            builder.Property(row => row.ParentLotId).HasColumnName("parent_lot_id");
            builder.Property(row => row.RootSourceStampId).HasColumnName("root_source_stamp_id");
            builder.Property(row => row.ReversalEpoch).HasColumnName("reversal_epoch");
            builder.Property(row => row.StartInclusive).HasColumnName("start_inclusive");
            builder.Property(row => row.EndExclusive).HasColumnName("end_exclusive");
            builder.Property(row => row.Currency).HasColumnName("currency");
            builder.Property(row => row.AmountUnits).HasColumnName("amount_units");
        });
    }

    private static void ConfigureRegisteredPostingReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegisteredPostingReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.PostingId).HasColumnName("posting_id");
            builder.Property(row => row.JournalSequence).HasColumnName("journal_sequence");
            builder.Property(row => row.JournalHash).HasColumnName("journal_hash");
            builder.Property(row => row.Duplicate).HasColumnName("duplicate");
        });
    }

    private static void ConfigureHardToSoftConversionRiskDecisionReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HardToSoftConversionRiskDecisionReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.RiskDecisionId).HasColumnName("risk_decision_id");
            builder.Property(row => row.SourceRoots).HasColumnName("source_roots");
        });
    }

    private static void ConfigureProviderReversalReceipt(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderReversalReceiptRow>(builder =>
        {
            builder.HasNoKey();
            builder.ToView(null);
            builder.Property(row => row.OperationId).HasColumnName("operation_id");
            builder.Property(row => row.RecoveredHardUnits).HasColumnName("recovered_hard_units");
            builder.Property(row => row.RecoveredConvertedSoftUnits).HasColumnName("recovered_converted_soft_units");
            builder.Property(row => row.ResponsibleDebtHardUnits).HasColumnName("responsible_debt_hard_units");
            builder.Property(row => row.PlatformLossHardUnits).HasColumnName("platform_loss_hard_units");
            builder.Property(row => row.Duplicate).HasColumnName("duplicate");
        });
    }
}
