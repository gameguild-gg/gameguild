using System.Text.RegularExpressions;
using FluentAssertions;
using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.Finance.Economy.UnitTests.Persistence;

public sealed class EconomyPersistenceModelTests
{
    private static readonly string[] ExpectedTables =
    [
        "ai_provider_cost_facts",
        "economy_accounts",
        "economy_approvals",
        "economy_capability_policies",
        "economy_capability_receipts",
        "economy_chain_head",
        "economy_compliance_evidence",
        "economy_compliance_holds",
        "economy_credit_lots",
        "economy_custody_observations",
        "economy_custody_reconciliations",
        "economy_dispatch_snapshots",
        "economy_dispute_fragment_freezes",
        "economy_dispute_fragment_ranges",
        "economy_entity_graph_edges",
        "economy_entity_graph_nodes",
        "economy_entry_allocations",
        "economy_events",
        "economy_external_anchors",
        "economy_fragment_root_ranges",
        "economy_funding_claims",
        "economy_holds",
        "economy_idempotency_records",
        "economy_inbox",
        "economy_journal_entries",
        "economy_journal_lines",
        "economy_journal_verification_checkpoints",
        "economy_kill_switches",
        "economy_legacy_cutovers",
        "economy_legacy_shadow_batches",
        "economy_legacy_shadow_wallets",
        "economy_lot_lineage_edges",
        "economy_operations",
        "economy_outbox",
        "economy_policy_versions",
        "economy_posting_groups",
        "economy_projection_generations",
        "economy_protected_change_cooldowns",
        "economy_provider_disputes",
        "economy_provider_events",
        "economy_provider_fact_allocations",
        "economy_registered_capabilities",
        "economy_reserve_asset_allocations",
        "economy_reserve_heads",
        "economy_reserve_proposals",
        "economy_risk_audit_evidence",
        "economy_risk_counter_reservations",
        "economy_risk_counters",
        "economy_risk_decisions",
        "economy_risk_review_cases",
        "economy_root_reversal_states",
        "economy_self_service_transfer_intents",
        "economy_source_stamps",
        "economy_top_up_intents",
        "economy_unit_consumptions",
        "economy_wallet_balance_projections",
        "economy_wallet_debts",
        "economy_wallet_projection_generations",
        "economy_wallets",
        "economy_worker_leases"
    ];

    private static readonly string[] CriticalIndexes =
    [
        "ix_economy_dispute_fragment_freezes_root_status",
        "ix_economy_entry_allocations_parent_lot",
        "ix_economy_external_anchors_chain_sequence",
        "ix_economy_fragment_root_ranges_root_epoch",
        "ix_economy_holds_wallet_status",
        "ix_economy_lot_lineage_edges_parent_lot",
        "ix_economy_events_anchor_verification",
        "ix_economy_events_projection_wallet_detected",
        "ix_economy_self_service_transfer_intents_recipient_time",
        "ix_economy_wallet_balance_projections_review_state",
        "ix_economy_credit_lots_root_source",
        "ux_economy_approvals_kind_generation_actor",
        "ux_economy_approvals_kind_subject_actor",
        "ux_economy_capability_policies_scope_version",
        "ux_economy_unit_consumptions_receipt",
        "ux_economy_capability_receipts_hash",
        "ux_economy_compliance_evidence_provider_event",
        "ux_economy_compliance_holds_idempotency",
        "ux_economy_inbox_provider_event",
        "ux_economy_inbox_tenant_provider_event",
        "ux_economy_custody_observations_provider_asset_version",
        "ux_economy_dispatch_snapshots_hash",
        "ux_economy_dispute_fragment_ranges_freeze_interval",
        "ux_economy_events_dispute_sequence",
        "ux_economy_provider_disputes_active_source",
        "ux_economy_entry_allocations_line_parent",
        "ux_economy_entity_graph_edges_pair_version",
        "ux_economy_entity_graph_nodes_identity_version",
        "ux_economy_funding_claims_provider_leg",
        "ux_economy_funding_claims_posting_group",
        "ux_economy_funding_claims_root_lot",
        "ux_economy_idempotency_records_key",
        "ux_economy_journal_entries_posting_group_id",
        "ux_economy_journal_entries_sequence",
        "ux_economy_outbox_payload_hash",
        "ux_economy_provider_events_payout_event",
        "ix_economy_journal_verification_checkpoints_sequence",
        "ux_economy_kill_switches_active_scope",
        "ux_economy_lot_lineage_edges_parent_child",
        "ux_economy_posting_groups_idempotency_key",
        "ux_economy_posting_groups_source_stamp",
        "ux_economy_projection_generations_active",
        "ux_economy_provider_fact_allocations_provider_leg",
        "ux_economy_registered_capabilities_name",
        "ux_economy_reserve_asset_allocations_version_asset",
        "ux_economy_reserve_heads_active",
        "ux_economy_reserve_heads_authorization_epoch",
        "ux_economy_reserve_proposals_version",
        "ux_economy_protected_change_cooldowns_subject_kind_version",
        "ux_economy_risk_audit_evidence_decision_hash",
        "ux_economy_risk_counter_reservations_decision_counter",
        "ux_economy_risk_counters_scope_window",
        "ux_economy_unit_consumptions_risk_decision",
        "ux_economy_unit_consumptions_posting",
        "ux_economy_risk_review_cases_tenant_decision",
        "ux_economy_events_review_case_sequence",
        "ux_economy_self_service_transfer_intents_actor_key",
        "ux_economy_events_hold_sequence",
        "ux_economy_root_reversal_states_root_epoch",
        "ux_economy_events_source_stamp_sequence",
        "ux_economy_source_stamps_internal_leg",
        "ux_economy_source_stamps_provider_reference",
        "ux_economy_top_up_intents_actor_key",
        "ux_economy_top_up_intents_payment",
        "ux_economy_top_up_intents_provider_object",
        "ux_economy_events_debt_sequence",
        "ux_economy_events_provider_event",
        "ux_economy_events_compliance_hold_sequence",
        "ux_economy_events_compliance_hold_kind_actor",
        "ux_economy_events_legacy_cutover_sequence",
        "ux_economy_events_settlement_sequence",
        "ux_economy_events_ad_reward_session_sequence",
        "ux_economy_events_ad_reward_milestone_sequence",
        "ux_economy_events_admin_withdrawal_sequence",
        "ux_economy_events_admin_withdrawal_hash",
        "ux_economy_events_bounty_expiration",
        "ux_economy_worker_leases_name"
    ];

    [Fact]
    public void ModelMapsEveryFoundationTableAndCriticalIndex()
    {
        using var context = CreateContext();
        var model = context.Model;

        model.GetEntityTypes()
            .Select(entity => entity.GetTableName())
            .Where(name => name is not null)
            .Order(StringComparer.Ordinal)
            .Should()
            .Equal(ExpectedTables);

        model.GetEntityTypes()
            .SelectMany(entity => entity.GetIndexes())
            .Select(index => index.GetDatabaseName())
            .Where(name => name is not null)
            .Should()
            .Contain(CriticalIndexes);
    }

    [Fact]
    public void ModelEnforcesFoundationIntegrityConstraints()
    {
        using var context = CreateContext();
        var constraints = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .SelectMany(entity => entity.GetCheckConstraints())
            .Select(constraint => constraint.Name)
            .ToArray();

        constraints.Should().Contain(
        [
            "ck_economy_accounts_wallet_partition",
            "ck_economy_chain_head_singleton",
            "ck_economy_credit_lots_amount_positive",
            "ck_economy_credit_lots_maturity_order",
            "ck_economy_credit_lots_maturity_policy",
            "ck_economy_dispatch_snapshots_amount_positive",
            "ck_economy_dispute_fragment_freezes_amount_positive",
            "ck_economy_dispute_fragment_freezes_state_timestamp",
            "ck_economy_dispute_fragment_ranges_half_open",
            "ck_economy_entry_allocations_amount_positive",
            "ck_economy_fragment_root_ranges_half_open",
            "ck_economy_fragment_root_ranges_single_owner",
            "ck_economy_funding_claims_amount_positive",
            "ck_economy_funding_claims_lifecycle",
            "ck_economy_funding_claims_provider_reversal_bounds",
            "ck_economy_funding_claims_version_positive",
            "ck_economy_events_sequence",
            "ck_economy_events_values",
            "ck_economy_events_stream_shape",
            "ck_economy_holds_amount_positive",
            "ck_economy_holds_state_timestamp",
            "ck_economy_journal_lines_amount_positive",
            "ck_economy_legacy_cutovers_epoch",
            "ck_economy_legacy_cutovers_state",
            "ck_economy_legacy_shadow_batches_counts",
            "ck_economy_legacy_shadow_batches_state",
            "ck_economy_legacy_shadow_batches_units",
            "ck_economy_legacy_shadow_batches_version",
            "ck_economy_legacy_shadow_wallets_state",
            "ck_economy_legacy_shadow_wallets_transactions",
            "ck_economy_legacy_shadow_wallets_units",
            "ck_economy_legacy_shadow_wallets_version",
            "ck_economy_lot_lineage_edges_amount_positive",
            "ck_economy_posting_groups_authority_template",
            "ck_economy_posting_groups_reserve_authorization",
            "ck_economy_posting_groups_source_requirement",
            "ck_economy_posting_groups_template_state",
            "ck_economy_provider_disputes_amount_partition",
            "ck_economy_provider_disputes_lifecycle",
            "ck_economy_provider_disputes_sequence_positive",
            "ck_economy_provider_disputes_version_positive",
            "ck_economy_wallet_balance_projections_amounts_nonnegative",
            "ck_economy_wallet_balance_projections_sequence_nonnegative",
            "ck_economy_provider_fact_allocations_cumulative_bounds",
            "ck_economy_registered_capabilities_state",
            "ck_economy_self_service_transfer_intents_amount_positive",
            "ck_economy_self_service_transfer_intents_currency_provenance",
            "ck_economy_self_service_transfer_intents_parties_distinct",
            "ck_economy_self_service_transfer_intents_type_valid",
            "ck_economy_reserve_asset_allocations_value_positive",
            "ck_economy_reserve_asset_allocations_values_valid",
            "ck_economy_reserve_heads_amounts_nonnegative",
            "ck_economy_reserve_heads_values_valid",
            "ck_economy_reserve_heads_versions_positive",
            "ck_economy_reserve_heads_window",
            "ck_economy_dispatch_snapshots_reserve_authorization",
            "ck_economy_protected_change_cooldowns_version",
            "ck_economy_protected_change_cooldowns_window",
            "ck_economy_risk_counter_reservations_amount_positive",
            "ck_economy_risk_counter_reservations_lifetime",
            "ck_economy_risk_counter_reservations_state",
            "ck_economy_risk_counters_bounds",
            "ck_economy_risk_counters_window",
            "ck_economy_risk_decisions_amount_positive",
            "ck_economy_risk_decisions_lifetime",
            "ck_economy_risk_decisions_versions_positive",
            "ck_economy_risk_review_cases_approvals",
            "ck_economy_risk_review_cases_state",
            "ck_economy_root_reversal_states_cumulative_bounds",
            "ck_economy_root_reversal_states_epoch_nonnegative",
            "ck_economy_top_up_intents_amount_positive",
            "ck_economy_top_up_intents_provider_binding",
            "ck_economy_top_up_intents_version_positive",
            "ck_economy_wallet_debts_nonnegative",
            "ck_economy_wallet_debts_version_positive"
        ]);
    }

    [Fact]
    public void ModelUsesCanonicalInternalProviderAndRootIdentities()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertUniqueIndex(
            model,
            "economy_source_stamps",
            "ux_economy_source_stamps_internal_leg",
            "SourceKind", "InternalSourceId", "SourceLegId");
        AssertUniqueIndex(
            model,
            "economy_funding_claims",
            "ux_economy_funding_claims_provider_leg",
            "Provider", "Environment", "ConnectedAccount", "ProviderObject", "ProviderMonetaryLeg");
        AssertUniqueIndex(
            model,
            "economy_provider_fact_allocations",
            "ux_economy_provider_fact_allocations_provider_leg",
            "Provider", "Environment", "ConnectedAccount", "ProviderObject", "ProviderMonetaryLeg");
        var rootLot = model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_credit_lots");
        var rootLotIndex = rootLot.GetIndexes().Single(index => index.GetDatabaseName() == "ix_economy_credit_lots_root_source");
        rootLotIndex.IsUnique.Should().BeFalse();
        rootLotIndex.Properties.Select(property => property.Name).Should().Equal("RootSourceStampId");
        AssertUniqueIndex(
            model,
            "economy_reserve_asset_allocations",
            "ux_economy_reserve_asset_allocations_version_asset",
            "ReserveVersion", "AssetKey");

        var source = model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_source_stamps");
        source.GetIndexes().Single(index => index.GetDatabaseName() == "ux_economy_source_stamps_provider_reference")
            .GetFilter()
            .Should().Be("\"Provider\" IS NOT NULL AND \"ProviderReference\" IS NOT NULL");
    }

    [Fact]
    public void ModelUsesRestrictiveForeignKeysAndNoMutableBaseEntity()
    {
        using var context = CreateContext();
        var entityTypes = context.Model.GetEntityTypes().ToArray();

        entityTypes.SelectMany(entity => entity.GetForeignKeys())
            .Should()
            .OnlyContain(foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        entityTypes.Select(entity => entity.ClrType)
            .Should()
            .NotContain(type => InheritsEntityBase(type));
    }

    [Fact]
    public void ProtectedPersistenceCarriesReserveVersionEpochAndRiskDecision()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertProperties(model, "economy_posting_groups", "ReserveVersion", "ReserveAuthorizationEpoch", "RiskDecisionId");
        AssertProperties(model, "economy_risk_decisions", "ReserveVersion", "ReserveAuthorizationEpoch");
        AssertProperties(model, "economy_dispatch_snapshots", "ReserveVersion", "ReserveAuthorizationEpoch");
    }

    [Fact]
    public void FundingClaimPersistsPendingAndTerminalProviderLifecycle()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertProperties(
            model,
            "economy_funding_claims",
            "SourceStampId",
            "WalletId",
            "AuthoritativeUsdMinorUnits",
            "State",
            "ObservedAt",
            "ConfirmedAt",
            "StateChangedAt",
            "PostingGroupId",
            "RootCreditLotId",
            "CumulativeProviderReversalUnits",
            "Version");

        var fundingClaim = model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "economy_funding_claims");
        fundingClaim.FindPrimaryKey()!.Properties.Select(property => property.Name)
            .Should().Equal("SourceStampId");
        fundingClaim.FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void TopUpIntentPersistsAuthorityProviderBindingAndConcurrency()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertProperties(
            model,
            "economy_top_up_intents",
            "Id",
            "PaymentId",
            "TenantId",
            "ActorId",
            "WalletId",
            "HardCoinUnits",
            "UsdMinorUnits",
            "JurisdictionCode",
            "PolicyVersion",
            "PolicyHash",
            "Provider",
            "IdempotencyKey",
            "RequestHash",
            "ProviderEnvironment",
            "ProviderAccountId",
            "ProviderObjectId",
            "ProviderObjectType",
            "ProviderMonetaryLeg",
            "Status",
            "RequestedAt",
            "ProviderBoundAt",
            "Version");
        AssertUniqueIndex(
            model,
            "economy_top_up_intents",
            "ux_economy_top_up_intents_actor_key",
            "TenantId", "ActorId", "IdempotencyKey");
        AssertUniqueIndex(
            model,
            "economy_top_up_intents",
            "ux_economy_top_up_intents_payment",
            "PaymentId");
        AssertUniqueIndex(
            model,
            "economy_top_up_intents",
            "ux_economy_top_up_intents_provider_object",
            "Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId",
            "ProviderObjectType", "ProviderMonetaryLeg");
        var topUp = model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "economy_top_up_intents");
        topUp.FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
        topUp.GetIndexes().Single(index =>
                index.GetDatabaseName() == "ux_economy_top_up_intents_provider_object")
            .GetFilter().Should().Be("\"ProviderObjectId\" IS NOT NULL");
    }

    [Fact]
    public void ProviderDisputesPersistExactFreezesOrderedEventsAndWalletDebt()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertProperties(
            model,
            "economy_provider_disputes",
            "ProviderDisputeReference",
            "SourceStampId",
            "ResponsibleWalletId",
            "Status",
            "LatestProviderSequence",
            "CumulativeDisputedHardUnits",
            "BaselineReversedHardUnits",
            "FrozenHardEquivalentUnits",
            "ReversalIdempotencyKey",
            "UpdatedAt",
            "Version");
        // #1092 Tier 2.1: dispute events (StreamKind 2) and wallet debt events
        // (StreamKind 3) are rows of the unified event ledger.
        AssertProperties(
            model,
            "economy_events",
            "Id",
            "StreamKind",
            "Sequence",
            "ProviderEventId",
            "ProviderDisputeReference",
            "SourceStampId",
            "Status",
            "CumulativeDisputedHardUnits",
            "RequestHash",
            "DebtWalletId",
            "DeltaHardUnits",
            "OutstandingHardUnits",
            "OccurredAt");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_dispute_sequence", "ProviderDisputeReference", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_debt_sequence", "DebtWalletId", "Sequence");
        AssertProperties(
            model,
            "economy_dispute_fragment_freezes",
            "Id",
            "ProviderDisputeReference",
            "RootSourceStampId",
            "CreditLotId",
            "WalletId",
            "Currency",
            "AmountUnits",
            "Status",
            "PlacedAt",
            "TerminalAt");
        AssertProperties(
            model,
            "economy_dispute_fragment_ranges",
            "Id",
            "DisputeFragmentFreezeId",
            "StartInclusive",
            "EndExclusive",
            "ReversalEpoch");
        AssertProperties(model, "economy_wallet_debts", "WalletId", "OutstandingHardUnits", "UpdatedAt", "Version");

        model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_provider_disputes")
            .FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
        model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_wallet_debts")
            .FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
    }

    [Fact]
    public void EventLedgerKeepsOneKindScopedUniqueStreamPerEventFamily()
    {
        using var context = CreateContext();
        var model = context.Model;
        var ledger = model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_events");

        // One flat row for every event family: a single primary key and no
        // inheritance discriminator.
        ledger.FindPrimaryKey()!.Properties.Select(property => property.Name).Should().Equal("Id");
        ledger.FindDiscriminatorProperty().Should().BeNull();
        foreach (var shared in new[] { "StreamKind", "Sequence", "OccurredAt" })
        {
            ledger.FindProperty(shared)!.IsNullable.Should().BeFalse();
        }

        AssertUniqueIndex(model, "economy_events", "ux_economy_events_source_stamp_sequence", "SourceStampId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_provider_event", "ProviderEventId");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_debt_sequence", "DebtWalletId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_hold_sequence", "HoldId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_review_case_sequence", "RiskReviewCaseId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_compliance_hold_sequence", "ComplianceHoldId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_compliance_hold_kind_actor", "ComplianceHoldId", "KindText", "ActorId");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_legacy_cutover_sequence", "LegacyCutoverTenantId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_settlement_sequence", "SettlementId", "Sequence");
        // Session events and playback milestones are both numbered from 1 per
        // session, so they must stay separate unique streams.
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_ad_reward_session_sequence", "SessionId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_ad_reward_milestone_sequence", "SessionId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_admin_withdrawal_sequence", "RunId", "Sequence");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_admin_withdrawal_hash", "Hash");
        AssertUniqueIndex(model, "economy_events", "ux_economy_events_bounty_expiration", "BountyId");

        // No unique index may compare rows across two event families.
        ledger.GetIndexes().Where(index => index.IsUnique)
            .Should().OnlyContain(index =>
                index.GetFilter()!.StartsWith("\"StreamKind\" = ", StringComparison.Ordinal));

        AssertFilteredUniqueIndex(ledger, "ux_economy_events_ad_reward_session_sequence", "\"StreamKind\" = 9", "SessionId", "Sequence");
        AssertFilteredUniqueIndex(ledger, "ux_economy_events_ad_reward_milestone_sequence", "\"StreamKind\" = 10", "SessionId", "Sequence");
        AssertFilteredUniqueIndex(ledger, "ux_economy_events_bounty_expiration", "\"StreamKind\" = 12", "BountyId");
        AssertFilteredUniqueIndex(ledger, "ux_economy_events_provider_event", "\"StreamKind\" = 2", "ProviderEventId");

        ledger.GetForeignKeys()
            .Select(foreignKey => (
                Column: foreignKey.Properties.Single().Name,
                Principal: foreignKey.PrincipalEntityType.GetTableName(),
                foreignKey.DeleteBehavior))
            .Should().BeEquivalentTo(
            [
                ("SourceStampId", "economy_source_stamps", DeleteBehavior.Restrict),
                ("ProviderDisputeReference", "economy_provider_disputes", DeleteBehavior.Restrict),
                ("DebtWalletId", "economy_wallet_debts", DeleteBehavior.Restrict),
                ("HoldId", "economy_holds", DeleteBehavior.Restrict),
                ("RiskReviewCaseId", "economy_risk_review_cases", DeleteBehavior.Restrict),
                ("ComplianceHoldId", "economy_compliance_holds", DeleteBehavior.Restrict),
                ("LegacyCutoverTenantId", "economy_legacy_cutovers", DeleteBehavior.Restrict),
                ("ProjectionWalletId", "economy_wallets", DeleteBehavior.Restrict),
                ("ExternalAnchorId", "economy_external_anchors", DeleteBehavior.Restrict)
            ]);
    }

    [Fact]
    public void EventLedgerShapeConstraintCoversEveryStreamKindCompletely()
    {
        using var context = CreateContext();
        var ledger = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "economy_events");
        var shape = ledger.GetCheckConstraints()
            .Single(constraint => constraint.Name == "ck_economy_events_stream_shape")
            .Sql;

        // The columns the event families do not share. Id, StreamKind,
        // Sequence and OccurredAt are common to every row and never branch.
        var streamColumns = ledger.GetProperties()
            .Select(property => property.Name)
            .Where(name => name is not ("Id" or "StreamKind" or "Sequence" or "OccurredAt"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var requiredByKind = new Dictionary<EconomyEventStreamKind, string[]>
        {
            [EconomyEventStreamKind.SourceStamp] = ["SourceStampId", "Kind", "EvidenceHash"],
            [EconomyEventStreamKind.ProviderDispute] =
                ["ProviderEventId", "ProviderDisputeReference", "SourceStampId", "Status", "CumulativeDisputedHardUnits", "RequestHash"],
            [EconomyEventStreamKind.WalletDebt] = ["DebtWalletId", "SourceStampId", "DeltaHardUnits", "OutstandingHardUnits"],
            [EconomyEventStreamKind.FundingHold] = ["HoldId", "Kind", "ActorId", "EvidenceHash"],
            [EconomyEventStreamKind.RiskReview] = ["RiskReviewCaseId", "Kind", "ActorId", "EvidenceHashes"],
            [EconomyEventStreamKind.ComplianceHold] = ["ComplianceHoldId", "KindText", "ActorId", "EvidenceHash"],
            [EconomyEventStreamKind.LegacyCutover] =
                ["LegacyCutoverTenantId", "BatchId", "Kind", "ActorId", "Reason", "ReauthenticationHash", "EvidenceHash"],
            [EconomyEventStreamKind.MarketplaceSettlement] = ["TenantId", "SettlementId", "KindText", "EvidenceHash"],
            [EconomyEventStreamKind.AdRewardSessionEvent] = ["SessionId", "Kind", "EvidenceHash"],
            [EconomyEventStreamKind.AdRewardPlaybackMilestone] = ["SessionId", "Percentage", "EvidenceHash"],
            [EconomyEventStreamKind.AdminWithdrawalAudit] = ["TenantId", "RunId", "KindText", "Evidence", "PreviousHash", "Hash"],
            [EconomyEventStreamKind.BountyExpiration] = ["BountyId", "ExpiresAt", "BountyVersion"],
            [EconomyEventStreamKind.ProjectionReconciliation] =
                ["ProjectionWalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence"],
            // The WORM object's hash is the stream's evidence hash and the end
            // of its retention the row's ExpiresAt.
            [EconomyEventStreamKind.AnchorVerification] =
                ["ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "ExpiresAt", "EvidenceHash", "SignatureValid", "ObjectMatches"]
        };
        var optionalByKind = new Dictionary<EconomyEventStreamKind, string[]>
        {
            [EconomyEventStreamKind.RiskReview] = ["Resolution", "DecisionCode"],
            [EconomyEventStreamKind.AdminWithdrawalAudit] = ["ActorId"]
        };

        requiredByKind.Keys.Should().BeEquivalentTo(Enum.GetValues<EconomyEventStreamKind>());
        shape.Should().StartWith("(\"StreamKind\" = 1 AND ");
        var branches = shape.Split([") OR ("], StringSplitOptions.None);
        branches.Should().HaveCount(requiredByKind.Count);

        foreach (var (kind, required) in requiredByKind)
        {
            var optional = optionalByKind.GetValueOrDefault(kind) ?? [];
            var branch = branches.Single(candidate =>
                candidate.StartsWith($"(\"StreamKind\" = {(int)kind} AND ", StringComparison.Ordinal) ||
                candidate.StartsWith($"\"StreamKind\" = {(int)kind} AND ", StringComparison.Ordinal));
            var mentioned = Regex.Matches(branch, "\"([A-Za-z]+)\" IS (NOT )?NULL")
                .Select(match => (Column: match.Groups[1].Value, Required: match.Groups[2].Length > 0))
                .ToArray();
            var because = $"stream {(int)kind} must constrain every stream column exactly once";

            // Sorted joins (not set comparison) so a duplicated or missing column fails.
            // Optional columns are, by design, absent from the branch: a shape
            // constraint can neither require nor forbid them.
            Join(mentioned.Select(item => item.Column))
                .Should().Be(Join(streamColumns.Except(optional)), because);
            Join(mentioned.Where(item => item.Required).Select(item => item.Column))
                .Should().Be(Join(required), because);
            Join(mentioned.Where(item => !item.Required).Select(item => item.Column))
                .Should().Be(Join(streamColumns.Except(required).Except(optional)), because);
        }

        static string Join(IEnumerable<string> columns) =>
            string.Join(" ", columns.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EventLedgerCarriesTheValueRulesOfEveryLegacyEventTable()
    {
        using var context = CreateContext();
        // The design-time check-constraint interface declares Name and Sql as
        // nullable; the model always stores both.
        var constraints = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "economy_events")
            .GetCheckConstraints()
            .ToDictionary(constraint => constraint.Name!, constraint => constraint.Sql!);

        // Projection reconciliation and anchor verification records own no
        // ordinal, so the sequence rule covers every other stream only.
        constraints["ck_economy_events_sequence"].Should().Be("\"StreamKind\" IN (13, 14) OR \"Sequence\" > 0");
        // One clause per stream that had a value rule before the merge.
        foreach (var clause in new[]
                 {
                     "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0)",
                     "(\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0))",
                     "(\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4)",
                     "(\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7)",
                     "(\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100)",
                     "(\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\"))",
                     "(\"StreamKind\" <> 13 OR \"SourceJournalSequence\" >= 0)"
                 })
        {
            constraints["ck_economy_events_values"].Should().Contain(clause);
        }
    }

    [Fact]
    public void WalletProjectionPersistsOnlyDerivedRecoveryState()
    {
        using var context = CreateContext();
        var model = context.Model;

        AssertProperties(
            model,
            "economy_wallet_balance_projections",
            "WalletId",
            "PendingHard",
            "PendingSoft",
            "PurchasedHard",
            "EarnedHard",
            "RestrictedHard",
            "Soft",
            "ImmatureEarnedHard",
            "HeldHard",
            "HeldSoft",
            "AvailableHardToSpend",
            "AvailableSoftToSpend",
            "WithdrawableHard",
            "ReviewState",
            "SourceJournalSequence",
            "ProjectionHash",
            "RebuiltAt");
        // #1092 Tier 3: the reconciliation record is a row of the unified event
        // ledger; DetectedAt is the row's OccurredAt.
        AssertProperties(
            model,
            "economy_events",
            "ProjectionWalletId",
            "PreviousHash",
            "RebuiltHash",
            "SourceJournalSequence",
            "OccurredAt");
    }

    [Fact]
    public void AnchorVerificationIsARecordOfTheEventLedger()
    {
        using var context = CreateContext();
        var model = context.Model;

        // #1092 Tier 3: the verification record is a row of the unified event
        // ledger; VerifiedAt is the row's OccurredAt, RetainUntil its ExpiresAt
        // and ObjectHash its EvidenceHash.
        AssertProperties(
            model,
            "economy_events",
            "ExternalAnchorId",
            "KeyId",
            "ObjectVersion",
            "ETag",
            "SignatureValid",
            "ObjectMatches",
            "ExpiresAt",
            "EvidenceHash",
            "OccurredAt");
        model.GetEntityTypes()
            .Single(entity => entity.GetTableName() == "economy_events").GetProperties()
            .Where(property => property.Name is "KeyId" or "ObjectVersion" or "ETag")
            .Should().OnlyContain(property => property.GetMaxLength() == 256);
    }

    [Fact]
    public void EconomyAssemblyOwnsMappingsButNoCentralizedMigration()
    {
        var assemblyTypes = typeof(EconomyModelConfiguration).Assembly.GetTypes();

        assemblyTypes.Should().Contain(typeof(EconomyModelConfiguration));
        assemblyTypes.Should().NotContain(type => typeof(Migration).IsAssignableFrom(type));
        FluentActions.Invoking(() => new EconomyModelConfiguration().Configure(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PersistenceRowsRoundTripEveryMappedProperty()
    {
        using var context = CreateContext();
        var rowTypes = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType)
            .Where(type => type.Namespace == "GameGuild.Finance.Economy.Persistence"
                && type.Name.StartsWith("Economy", StringComparison.Ordinal)
                && type.Name.EndsWith("Row", StringComparison.Ordinal))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        rowTypes.Should().NotBeEmpty();

        foreach (var rowType in rowTypes)
        {
            var row = Activator.CreateInstance(rowType, nonPublic: true);
            row.Should().NotBeNull($"{rowType.Name} must remain materializable by EF Core");

            foreach (var property in rowType.GetProperties())
            {
                property.CanRead.Should().BeTrue($"{rowType.Name}.{property.Name} must be readable");
                property.CanWrite.Should().BeTrue($"{rowType.Name}.{property.Name} must be writable");

                var value = CreateRoundTripValue(property.PropertyType, property.Name);
                property.SetValue(row, value);
                property.GetValue(row).Should().Be(value, $"{rowType.Name}.{property.Name} must preserve persisted values");
            }
        }
    }

    private static object CreateRoundTripValue(Type propertyType, string propertyName)
    {
        var valueType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (valueType == typeof(string))
        {
            return $"value-{propertyName}";
        }

        if (valueType == typeof(Guid))
        {
            return Guid.Parse("b66a0a03-8e43-4c28-b1d0-13b0c9c0d2ab");
        }

        if (valueType == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        }

        if (valueType == typeof(DateOnly))
        {
            // The unified policy spine carries the ad-network domain's
            // DateOnly columns (e.g. ReportsCurrentThrough) as nullable.
            return new DateOnly(2026, 7, 30);
        }

        if (valueType == typeof(DateTime))
        {
            return new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
        }

        if (valueType == typeof(bool))
        {
            return true;
        }

        if (valueType == typeof(short))
        {
            return (short)7;
        }

        if (valueType == typeof(int))
        {
            return 11;
        }

        if (valueType == typeof(long))
        {
            return 13L;
        }

        if (valueType.IsEnum)
        {
            return Enum.GetValues(valueType).GetValue(Math.Min(1, Enum.GetValues(valueType).Length - 1))!;
        }

        throw new NotSupportedException($"No persistence round-trip value is defined for {propertyType}.");
    }

    private static bool InheritsEntityBase(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("EntityBase", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void AssertUniqueIndex(
        IModel model,
        string table,
        string indexName,
        params string[] properties)
    {
        var entity = model.GetEntityTypes().Single(candidate => candidate.GetTableName() == table);
        var index = entity.GetIndexes().Single(candidate => candidate.GetDatabaseName() == indexName);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(property => property.Name).Should().Equal(properties);
    }

    private static void AssertFilteredUniqueIndex(
        IReadOnlyEntityType entity,
        string indexName,
        string filter,
        params string[] properties)
    {
        var index = entity.GetIndexes().Single(candidate => candidate.GetDatabaseName() == indexName);

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be(filter);
        index.Properties.Select(property => property.Name).Should().Equal(properties);
    }

    private static void AssertProperties(IModel model, string table, params string[] properties)
    {
        var entity = model.GetEntityTypes().Single(candidate => candidate.GetTableName() == table);
        entity.GetProperties().Select(property => property.Name).Should().Contain(properties);
    }

    private static EconomySchemaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EconomySchemaDbContext>()
            .UseNpgsql("Host=localhost;Database=economy_contract;Username=contract;Password=contract")
            .Options;
        return new EconomySchemaDbContext(options);
    }

    private sealed class EconomySchemaDbContext(DbContextOptions<EconomySchemaDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new EconomyModelConfiguration().Configure(modelBuilder);
    }
}
