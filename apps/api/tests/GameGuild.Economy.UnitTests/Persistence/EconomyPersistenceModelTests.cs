using FluentAssertions;
using GameGuild.Economy.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.Economy.UnitTests.Persistence;

public sealed class EconomyPersistenceModelTests
{
    private static readonly string[] ExpectedTables =
    [
        "economy_accounts",
        "economy_chain_head",
        "economy_credit_lots",
        "economy_dispatch_snapshots",
        "economy_dispute_fragment_freezes",
        "economy_dispute_fragment_ranges",
        "economy_entry_allocations",
        "economy_external_anchors",
        "economy_fragment_root_ranges",
        "economy_funding_claims",
        "economy_hold_events",
        "economy_holds",
        "economy_idempotency_records",
        "economy_journal_entries",
        "economy_journal_lines",
        "economy_lot_lineage_edges",
        "economy_outbox_messages",
        "economy_posting_groups",
        "economy_projection_reconciliation_events",
        "economy_protected_change_cooldowns",
        "economy_provider_dispute_events",
        "economy_provider_disputes",
        "economy_provider_fact_allocations",
        "economy_registered_capabilities",
        "economy_reserve_asset_allocations",
        "economy_reserve_heads",
        "economy_risk_audit_evidence",
        "economy_risk_counter_reservations",
        "economy_risk_counters",
        "economy_risk_decision_consumptions",
        "economy_risk_decisions",
        "economy_risk_review_cases",
        "economy_risk_review_events",
        "economy_root_reversal_states",
        "economy_source_stamp_events",
        "economy_source_stamps",
        "economy_wallet_balance_projections",
        "economy_wallet_debt_events",
        "economy_wallet_debts",
        "economy_wallets"
    ];

    private static readonly string[] CriticalIndexes =
    [
        "ix_economy_dispute_fragment_freezes_root_status",
        "ix_economy_entry_allocations_parent_lot",
        "ix_economy_external_anchors_chain_sequence",
        "ix_economy_fragment_root_ranges_root_epoch",
        "ix_economy_holds_wallet_status",
        "ix_economy_lot_lineage_edges_parent_lot",
        "ix_economy_projection_reconciliation_events_wallet_detected",
        "ix_economy_wallet_balance_projections_review_state",
        "ix_economy_credit_lots_root_source",
        "ux_economy_dispatch_snapshots_hash",
        "ux_economy_dispute_fragment_ranges_freeze_interval",
        "ux_economy_provider_dispute_events_dispute_sequence",
        "ux_economy_provider_disputes_active_source",
        "ux_economy_entry_allocations_line_parent",
        "ux_economy_funding_claims_provider_leg",
        "ux_economy_funding_claims_posting_group",
        "ux_economy_funding_claims_root_lot",
        "ux_economy_idempotency_records_key",
        "ux_economy_journal_entries_posting_group_id",
        "ux_economy_journal_entries_sequence",
        "ux_economy_lot_lineage_edges_parent_child",
        "ux_economy_outbox_messages_payload_hash",
        "ux_economy_posting_groups_idempotency_key",
        "ux_economy_posting_groups_source_stamp",
        "ux_economy_provider_fact_allocations_provider_leg",
        "ux_economy_registered_capabilities_name",
        "ux_economy_reserve_asset_allocations_version_asset",
        "ux_economy_reserve_heads_active",
        "ux_economy_reserve_heads_authorization_epoch",
        "ux_economy_protected_change_cooldowns_subject_kind",
        "ux_economy_risk_audit_evidence_decision_hash",
        "ux_economy_risk_counter_reservations_decision_counter",
        "ux_economy_risk_counters_scope_window",
        "ux_economy_risk_decision_consumptions_decision",
        "ux_economy_risk_decision_consumptions_posting",
        "ux_economy_risk_review_cases_decision",
        "ux_economy_risk_review_events_case_sequence",
        "ux_economy_hold_events_hold_sequence",
        "ux_economy_root_reversal_states_root_epoch",
        "ux_economy_source_stamp_events_source_sequence",
        "ux_economy_source_stamps_internal_leg",
        "ux_economy_source_stamps_provider_reference",
        "ux_economy_wallet_debt_events_wallet_sequence"
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
            "ck_economy_hold_events_sequence_positive",
            "ck_economy_holds_amount_positive",
            "ck_economy_holds_state_timestamp",
            "ck_economy_journal_lines_amount_positive",
            "ck_economy_lot_lineage_edges_amount_positive",
            "ck_economy_posting_groups_authority_template",
            "ck_economy_posting_groups_reserve_authorization",
            "ck_economy_posting_groups_source_requirement",
            "ck_economy_posting_groups_template_state",
            "ck_economy_projection_events_sequence_nonnegative",
            "ck_economy_provider_dispute_events_amount_positive",
            "ck_economy_provider_dispute_events_sequence_positive",
            "ck_economy_provider_disputes_amount_partition",
            "ck_economy_provider_disputes_lifecycle",
            "ck_economy_provider_disputes_sequence_positive",
            "ck_economy_provider_disputes_version_positive",
            "ck_economy_wallet_balance_projections_amounts_nonnegative",
            "ck_economy_wallet_balance_projections_sequence_nonnegative",
            "ck_economy_provider_fact_allocations_cumulative_bounds",
            "ck_economy_registered_capabilities_state",
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
            "ck_economy_risk_counters_bounds",
            "ck_economy_risk_counters_window",
            "ck_economy_risk_decisions_amount_positive",
            "ck_economy_risk_decisions_lifetime",
            "ck_economy_risk_decisions_versions_positive",
            "ck_economy_risk_review_cases_approvals",
            "ck_economy_risk_review_cases_state",
            "ck_economy_risk_review_events_sequence_positive",
            "ck_economy_root_reversal_states_cumulative_bounds",
            "ck_economy_root_reversal_states_epoch_nonnegative",
            "ck_economy_source_stamp_events_sequence_positive",
            "ck_economy_wallet_debt_events_delta_nonzero",
            "ck_economy_wallet_debt_events_sequence_positive",
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
        AssertProperties(
            model,
            "economy_provider_dispute_events",
            "ProviderEventId",
            "ProviderDisputeReference",
            "SourceStampId",
            "ProviderSequence",
            "Status",
            "CumulativeDisputedHardUnits",
            "RequestHash",
            "OccurredAt");
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
        AssertProperties(
            model,
            "economy_wallet_debt_events",
            "Id",
            "WalletId",
            "SourceStampId",
            "Sequence",
            "DeltaHardUnits",
            "OutstandingHardUnits",
            "OccurredAt");

        model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_provider_disputes")
            .FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
        model.GetEntityTypes().Single(entity => entity.GetTableName() == "economy_wallet_debts")
            .FindProperty("Version")!.IsConcurrencyToken.Should().BeTrue();
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
        AssertProperties(
            model,
            "economy_projection_reconciliation_events",
            "Id",
            "WalletId",
            "PreviousHash",
            "RebuiltHash",
            "SourceJournalSequence",
            "DetectedAt");
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
            .Where(type => type.Namespace == "GameGuild.Economy.Persistence"
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
            return $"value-{propertyName}";
        if (valueType == typeof(Guid))
            return Guid.Parse("b66a0a03-8e43-4c28-b1d0-13b0c9c0d2ab");
        if (valueType == typeof(DateTimeOffset))
            return new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);
        if (valueType == typeof(bool))
            return true;
        if (valueType == typeof(short))
            return (short)7;
        if (valueType == typeof(int))
            return 11;
        if (valueType == typeof(long))
            return 13L;
        if (valueType.IsEnum)
            return Enum.GetValues(valueType).GetValue(Math.Min(1, Enum.GetValues(valueType).Length - 1))!;

        throw new NotSupportedException($"No persistence round-trip value is defined for {propertyType}.");
    }

    private static bool InheritsEntityBase(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition().Name.StartsWith("EntityBase", StringComparison.Ordinal))
                return true;
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
