using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ===================================================================
            // #1092 Economy Tier 2 — shared spines (docs/economy/06-consolidation-crossref.md).
            //
            // T2.2 economy_unit_consumptions (4 → 1): capability-receipt burns,
            //     risk-decision burns, ad-reward cap consumptions and ad-reward
            //     budget consumptions merged behind ConsumptionKind. The
            //     risk-decision kind keeps its insert-validation trigger and its
            //     decision/posting partial uniques; the capability-receipt kind
            //     keeps the receipt partial unique the SECURITY DEFINER posting
            //     writers join against; reads that filter on a kind-exclusive
            //     column (ReceiptId / RiskDecisionId / SessionId+Scope) stay
            //     kind-correct through the kind-shape check constraint.
            // T2.3 economy_policy_versions (2 → 1): the two signed dual-control
    //                             policy-version tables merged behind
            //     PolicyDomain (MarketplaceCurrency = product subject,
            //     AdNetwork = network-name subject). Per-domain partial uniques
            //     preserve the (TenantId, subject, Version) uniqueness that
            //     historical refund pricing depends on, and the effective-window
            //     indexes stay per domain.
            // T2.4 economy_operations (2 → 1): payout operations and
            //     admin-withdrawal runs merged behind OperationKind. The two
            //     independent monotonic fencing allocators stay per kind
            //     (economy_private.payout_fencing_sequence /
            //     admin_withdrawal_fencing_sequence), dispatch keeps verifying
            //     exact version + token + epoch, and Treasury's ExecutionEpoch
            //     (no payout twin) stays a kind-2-only column. economy_dispatch_
            //     snapshots and economy_payout_authorization_evidence remain
            //     separate 1:N child stores of the spine (keyed by posting group
            //     and (OperationId, Phase) respectively) — folding them into the
            //     operations row would break their key semantics, so #1092's
            //     "dispatch snapshots + authorization evidence" note is delivered
            //     as retargeted bindings instead of table drops.
            //
            // Sequencing mirrors Tier 1: the unified tables are created and
            // populated with kind/domain discriminators set, every SECURITY
            // DEFINER body is retargeted (including the reader functions whose
            // return type follows the table), the risk-consumption triggers
            // move over kind-scoped, the dependent foreign keys are re-pointed,
            // and only then are the eight legacy tables dropped.
            // ===================================================================

            // The audit-events foreign key was created by an earlier migration
            // under a name PostgreSQL truncated without EF's tilde marker; the
            // drop addresses the constraint as it exists in the database.
            migrationBuilder.DropForeignKey(
                name: "FK_economy_admin_withdrawal_audit_events_economy_admin_withdraw",
                table: "economy_admin_withdrawal_audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_outbox_economy_admin_withdrawal_runs_RunId",
                table: "economy_outbox");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_outbox_economy_payout_operations_OperationId",
                table: "economy_outbox");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_payout_authorization_evidence_economy_payout_operat~",
                table: "economy_payout_authorization_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_provider_events_economy_admin_withdrawal_runs_RunId",
                table: "economy_provider_events");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_provider_events_economy_payout_operations_Operation~",
                table: "economy_provider_events");

            migrationBuilder.CreateTable(
                name: "economy_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationKind = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReserveAuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderAccountId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderBindingHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EligibilityHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderPayoutId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: true),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAssetKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderTransferId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExecutionEpoch = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_operations", x => x.Id);
                    table.CheckConstraint("ck_economy_operations_approval", "(\"OperationKind\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"OperationKind\" = 2 AND ((\"State\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"State\" BETWEEN 2 AND 7 AND \"ApprovedBy\" IS NOT NULL)))");
                    table.CheckConstraint("ck_economy_operations_dispatch", "(\"OperationKind\" = 1 AND ((\"State\" = 1 AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 2 AND 6 AND \"DispatchSnapshotHash\" IS NOT NULL))) OR (\"OperationKind\" = 2 AND ((\"State\" IN (1, 2) AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 3 AND 7 AND \"DispatchSnapshotHash\" IS NOT NULL)))");
                    table.CheckConstraint("ck_economy_operations_positive_values", "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0 AND ((\"OperationKind\" = 1 AND \"KillSwitchEpoch\" >= 0) OR (\"OperationKind\" = 2 AND \"KillSwitchEpoch\" IS NULL AND \"ExecutionEpoch\" > 0))");
                    table.CheckConstraint("ck_economy_operations_state", "(\"OperationKind\" = 1 AND \"State\" BETWEEN 1 AND 6) OR (\"OperationKind\" = 2 AND \"State\" BETWEEN 1 AND 7)");
                    table.CheckConstraint("ck_economy_operations_subject_shape", "(\"OperationKind\" = 1 AND \"PayeeId\" IS NOT NULL AND \"WalletId\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"ProviderBindingHash\" IS NOT NULL AND \"EligibilityHash\" IS NOT NULL AND \"RiskDecisionId\" IS NOT NULL AND \"PeriodStart\" IS NULL AND \"PlatformFeeWalletId\" IS NULL AND \"SourceAssetKey\" IS NULL AND \"ExecutionEpoch\" IS NULL) OR (\"OperationKind\" = 2 AND \"PayeeId\" IS NULL AND \"WalletId\" IS NULL AND \"ProviderAccountId\" IS NULL AND \"ProviderBindingHash\" IS NULL AND \"EligibilityHash\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"KillSwitchEpoch\" IS NULL AND \"PeriodStart\" IS NOT NULL AND \"PlatformFeeWalletId\" IS NOT NULL AND \"SourceAssetKey\" IS NOT NULL AND \"ExecutionEpoch\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_operations_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_policy_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PolicyDomain = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: true),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: true),
                    HardPriceUnits = table.Column<long>(type: "bigint", nullable: true),
                    SoftPriceUnits = table.Column<long>(type: "bigint", nullable: true),
                    PlatformFeePpm = table.Column<int>(type: "integer", nullable: true),
                    PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundHoldTicks = table.Column<long>(type: "bigint", nullable: true),
                    IssuanceMode = table.Column<int>(type: "integer", nullable: true),
                    YieldState = table.Column<int>(type: "integer", nullable: true),
                    EstimatedNetEcpmUsdNanos = table.Column<long>(type: "bigint", nullable: true),
                    ContractedRevenueSharePpm = table.Column<int>(type: "integer", nullable: true),
                    SafetyBufferPpm = table.Column<int>(type: "integer", nullable: true),
                    MinimumVisiblePpm = table.Column<int>(type: "integer", nullable: true),
                    MaximumFocusLossTicks = table.Column<long>(type: "bigint", nullable: true),
                    MaximumRewardSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    ReportsCurrentThrough = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReportStaleAfterTicks = table.Column<long>(type: "bigint", nullable: true),
                    Ranking = table.Column<int>(type: "integer", nullable: true),
                    MaximumUserSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    MaximumDeviceSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    MaximumIpSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    MaximumAsnSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    MaximumNetworkSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    MaximumGlobalSoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    FundedLossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: true),
                    BudgetWindowTicks = table.Column<long>(type: "bigint", nullable: true),
                    ProviderHash = table.Column<string>(type: "text", nullable: true),
                    ProviderCertified = table.Column<bool>(type: "boolean", nullable: true),
                    CanonicalPayload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Signature = table.Column<string>(type: "text", nullable: false),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_policy_versions", x => x.Id);
                    table.CheckConstraint("ck_economy_policy_versions_adnetwork_shape", "(\"PolicyDomain\" = 1) OR (\"SubjectKey\" IS NOT NULL AND \"IssuanceMode\" IS NOT NULL AND \"YieldState\" IS NOT NULL AND \"ReportsCurrentThrough\" IS NOT NULL AND \"ProviderHash\" IS NOT NULL AND \"ProviderCertified\" IS NOT NULL AND \"ContractedRevenueSharePpm\" BETWEEN 0 AND 1000000 AND \"SafetyBufferPpm\" BETWEEN 0 AND 999999 AND \"MinimumVisiblePpm\" BETWEEN 0 AND 1000000 AND \"EstimatedNetEcpmUsdNanos\" > 0 AND \"MaximumRewardSoftUnits\" > 0 AND \"MaximumFocusLossTicks\" >= 0 AND \"ReportStaleAfterTicks\" > 0 AND \"Ranking\" >= 0 AND \"BudgetWindowTicks\" > 0 AND \"MaximumUserSoftUnits\" > 0 AND \"MaximumDeviceSoftUnits\" > 0 AND \"MaximumIpSoftUnits\" > 0 AND \"MaximumAsnSoftUnits\" > 0 AND \"MaximumNetworkSoftUnits\" > 0 AND \"MaximumGlobalSoftUnits\" > 0 AND \"FundedLossBudgetUsdNanos\" > 0)");
                    table.CheckConstraint("ck_economy_policy_versions_dual_control", "\"ProposedBy\" <> \"ApprovedBy\"");
                    table.CheckConstraint("ck_economy_policy_versions_marketplace_shape", "(\"PolicyDomain\" = 2) OR (\"SubjectId\" IS NOT NULL AND \"SellerId\" IS NOT NULL AND \"Mode\" IS NOT NULL AND \"PlatformFeeWalletId\" IS NOT NULL AND \"RefundHoldTicks\" IS NOT NULL AND \"PlatformFeePpm\" BETWEEN 0 AND 999999 AND \"RefundHoldTicks\" > 0 AND ((\"Mode\" = 1 AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" = 0) OR (\"Mode\" = 2 AND \"HardPriceUnits\" = 0 AND \"SoftPriceUnits\" > 0) OR (\"Mode\" IN (3, 4) AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" > 0)))");
                    table.CheckConstraint("ck_economy_policy_versions_version", "\"Version\" > 0");
                    table.CheckConstraint("ck_economy_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_unit_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumptionKind = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: true),
                    SubjectHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    WindowStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WindowEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SoftUnits = table.Column<long>(type: "bigint", nullable: true),
                    LossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceRiskHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_unit_consumptions", x => x.Id);
                    table.CheckConstraint("ck_economy_unit_consumptions_kind_shape", "(\"ConsumptionKind\" = 1 AND \"ReceiptId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"OperationFingerprint\" IS NOT NULL AND \"KillSwitchEpoch\" IS NOT NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"SessionId\" IS NULL AND \"Scope\" IS NULL AND \"SubjectHash\" IS NULL) OR (\"ConsumptionKind\" = 2 AND \"RiskDecisionId\" IS NOT NULL AND \"PostingGroupId\" IS NOT NULL AND \"OperationFingerprint\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"ActorId\" IS NULL AND \"KillSwitchEpoch\" IS NULL AND \"SessionId\" IS NULL AND \"Scope\" IS NULL AND \"SubjectHash\" IS NULL) OR (\"ConsumptionKind\" = 3 AND \"SessionId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"Scope\" IS NOT NULL AND \"SubjectHash\" IS NOT NULL AND \"WindowStartedAt\" IS NOT NULL AND \"WindowEndsAt\" IS NOT NULL AND \"SoftUnits\" IS NOT NULL AND \"LossBudgetUsdNanos\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"WindowEndsAt\" > \"WindowStartedAt\" AND \"ConsumedAt\" >= \"WindowStartedAt\" AND \"ConsumedAt\" < \"WindowEndsAt\" AND \"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" >= 0 AND \"Scope\" BETWEEN 1 AND 6) OR (\"ConsumptionKind\" = 4 AND \"SessionId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"UserId\" IS NOT NULL AND \"Network\" IS NOT NULL AND \"DeviceRiskHash\" IS NOT NULL AND \"SoftUnits\" IS NOT NULL AND \"LossBudgetUsdNanos\" IS NOT NULL AND \"ReceiptId\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"PostingGroupId\" IS NULL AND \"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" > 0)");
                    table.ForeignKey(
                        name: "FK_economy_unit_consumptions_economy_ad_reward_sessions_Sessio~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_unit_consumptions_economy_capability_receipts_Recei~",
                        column: x => x.ReceiptId,
                        principalTable: "economy_capability_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_unit_consumptions_economy_posting_groups_PostingGro~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_unit_consumptions_economy_risk_decisions_RiskDecisi~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_economy_operations_kind_state_updated",
                table: "economy_operations",
                columns: new[] { "OperationKind", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_operations_tenant_kind_state_updated",
                table: "economy_operations",
                columns: new[] { "TenantId", "OperationKind", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_operations_active_period",
                table: "economy_operations",
                columns: new[] { "TenantId", "PeriodStart" },
                unique: true,
                filter: "\"OperationKind\" = 2 AND \"State\" NOT IN (6, 7)");

            migrationBuilder.CreateIndex(
                name: "ux_economy_operations_kind_tenant_idempotency",
                table: "economy_operations",
                columns: new[] { "OperationKind", "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_policy_versions_adnetwork_window",
                table: "economy_policy_versions",
                columns: new[] { "TenantId", "SubjectKey", "EffectiveAt", "ExpiresAt" },
                filter: "\"PolicyDomain\" = 2");

            migrationBuilder.CreateIndex(
                name: "ix_economy_policy_versions_marketplace_window",
                table: "economy_policy_versions",
                columns: new[] { "TenantId", "SubjectId", "EffectiveAt", "ExpiresAt" },
                filter: "\"PolicyDomain\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_economy_policy_versions_adnetwork_version",
                table: "economy_policy_versions",
                columns: new[] { "TenantId", "SubjectKey", "Version" },
                unique: true,
                filter: "\"PolicyDomain\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_policy_versions_marketplace_version",
                table: "economy_policy_versions",
                columns: new[] { "TenantId", "SubjectId", "Version" },
                unique: true,
                filter: "\"PolicyDomain\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_economy_unit_consumptions_budget_device",
                table: "economy_unit_consumptions",
                columns: new[] { "DeviceRiskHash", "ConsumedAt" },
                filter: "\"ConsumptionKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ix_economy_unit_consumptions_budget_network",
                table: "economy_unit_consumptions",
                columns: new[] { "Network", "ConsumedAt" },
                filter: "\"ConsumptionKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ix_economy_unit_consumptions_budget_user",
                table: "economy_unit_consumptions",
                columns: new[] { "UserId", "ConsumedAt" },
                filter: "\"ConsumptionKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ix_economy_unit_consumptions_tenant_scope_subject",
                table: "economy_unit_consumptions",
                columns: new[] { "TenantId", "Scope", "SubjectHash", "ConsumedAt" },
                filter: "\"ConsumptionKind\" = 3");

            migrationBuilder.CreateIndex(
                name: "ux_economy_unit_consumptions_budget_session",
                table: "economy_unit_consumptions",
                column: "SessionId",
                unique: true,
                filter: "\"ConsumptionKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ux_economy_unit_consumptions_posting",
                table: "economy_unit_consumptions",
                column: "PostingGroupId",
                unique: true,
                filter: "\"ConsumptionKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_unit_consumptions_receipt",
                table: "economy_unit_consumptions",
                column: "ReceiptId",
                unique: true,
                filter: "\"ConsumptionKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_economy_unit_consumptions_risk_decision",
                table: "economy_unit_consumptions",
                column: "RiskDecisionId",
                unique: true,
                filter: "\"ConsumptionKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_unit_consumptions_session_scope",
                table: "economy_unit_consumptions",
                columns: new[] { "SessionId", "Scope" },
                unique: true,
                filter: "\"ConsumptionKind\" = 3");

            // --- carry every row over with its discriminator set -------------
            migrationBuilder.Sql("""
                -- T2.2: capability receipt burns (kind 1).
                INSERT INTO public.economy_unit_consumptions (
                    "Id", "ConsumptionKind", "ReceiptId", "TenantId", "ActorId",
                    "OperationFingerprint", "KillSwitchEpoch", "ConsumedAt")
                SELECT "Id", 1, "ReceiptId", "TenantId", "ActorId",
                       "OperationFingerprint", "KillSwitchEpoch", "ConsumedAt"
                FROM public.economy_capability_receipt_consumptions;

                -- T2.2: risk decision burns (kind 2).
                INSERT INTO public.economy_unit_consumptions (
                    "Id", "ConsumptionKind", "RiskDecisionId", "PostingGroupId",
                    "OperationFingerprint", "ConsumedAt")
                SELECT "Id", 2, "RiskDecisionId", "PostingGroupId",
                       "OperationFingerprint", "ConsumedAt"
                FROM public.economy_risk_decision_consumptions;

                -- T2.2: ad-reward cap consumptions (kind 3).
                INSERT INTO public.economy_unit_consumptions (
                    "Id", "ConsumptionKind", "SessionId", "TenantId", "Scope", "SubjectHash",
                    "WindowStartedAt", "WindowEndsAt", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt")
                SELECT "Id", 3, "SessionId", "TenantId", "Scope", "SubjectHash",
                       "WindowStartedAt", "WindowEndsAt", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt"
                FROM public.economy_ad_reward_cap_consumptions;

                -- T2.2: ad-reward budget consumptions (kind 4; the legacy table
                -- was keyed by session alone, the unified row gets a fresh id).
                INSERT INTO public.economy_unit_consumptions (
                    "Id", "ConsumptionKind", "SessionId", "TenantId", "UserId",
                    "Network", "DeviceRiskHash", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt")
                SELECT gen_random_uuid(), 4, "SessionId", "TenantId", "UserId",
                       "Network", "DeviceRiskHash", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt"
                FROM public.economy_ad_reward_budget_consumptions;

                -- T2.3: Marketplace currency policies (domain 1; the legacy
                -- composite key becomes the product subject column, the
                -- unified row gets a fresh id).
                INSERT INTO public.economy_policy_versions (
                    "Id", "PolicyDomain", "TenantId", "Version", "SubjectId", "EffectiveAt", "ExpiresAt",
                    "Mode", "SellerId", "HardPriceUnits", "SoftPriceUnits", "PlatformFeePpm",
                    "PlatformFeeWalletId", "RefundHoldTicks",
                    "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt")
                SELECT gen_random_uuid(), 1, "TenantId", "Version", "ProductId", "EffectiveAt", "ExpiresAt",
                       "Mode", "SellerId", "HardPriceUnits", "SoftPriceUnits", "PlatformFeePpm",
                       "PlatformFeeWalletId", "RefundHoldTicks",
                       "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt"
                FROM public.economy_marketplace_currency_policy_versions;

                -- T2.3: ad-network reward policies (domain 2; network name
                -- becomes the subject key).
                INSERT INTO public.economy_policy_versions (
                    "Id", "PolicyDomain", "TenantId", "Version", "SubjectKey", "EffectiveAt", "ExpiresAt",
                    "IssuanceMode", "YieldState", "EstimatedNetEcpmUsdNanos", "ContractedRevenueSharePpm",
                    "SafetyBufferPpm", "MinimumVisiblePpm", "MaximumFocusLossTicks", "MaximumRewardSoftUnits",
                    "ReportsCurrentThrough", "ReportStaleAfterTicks", "Ranking",
                    "MaximumUserSoftUnits", "MaximumDeviceSoftUnits", "MaximumIpSoftUnits", "MaximumAsnSoftUnits",
                    "MaximumNetworkSoftUnits", "MaximumGlobalSoftUnits", "FundedLossBudgetUsdNanos", "BudgetWindowTicks",
                    "ProviderHash", "ProviderCertified",
                    "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt")
                SELECT gen_random_uuid(), 2, "TenantId", "Version", "Network", "EffectiveAt", "ExpiresAt",
                       "IssuanceMode", "YieldState", "EstimatedNetEcpmUsdNanos", "ContractedRevenueSharePpm",
                       "SafetyBufferPpm", "MinimumVisiblePpm", "MaximumFocusLossTicks", "MaximumRewardSoftUnits",
                       "ReportsCurrentThrough", "ReportStaleAfterTicks", "Ranking",
                       "MaximumUserSoftUnits", "MaximumDeviceSoftUnits", "MaximumIpSoftUnits", "MaximumAsnSoftUnits",
                       "MaximumNetworkSoftUnits", "MaximumGlobalSoftUnits", "FundedLossBudgetUsdNanos", "BudgetWindowTicks",
                       "ProviderHash", "ProviderCertified",
                       "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt"
                FROM public.economy_ad_network_policy_versions;

                -- T2.4: payout operations (kind 1).
                INSERT INTO public.economy_operations (
                    "Id", "OperationKind", "TenantId", "IdempotencyKey", "RequestHash", "ActorId",
                    "AmountUnits", "DestinationHash", "DispatchSnapshotHash", "State", "Version", "FencingToken",
                    "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                    "PayeeId", "WalletId", "ProviderAccountId", "ProviderBindingHash", "EligibilityHash",
                    "ProviderPayoutId", "KillSwitchEpoch", "RiskDecisionId", "CreatedAt", "UpdatedAt")
                SELECT "Id", 1, "TenantId", "IdempotencyKey", "RequestHash", "ActorId",
                       "AmountUnits", "DestinationHash", "DispatchSnapshotHash", "State", "Version", "FencingToken",
                       "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                       "PayeeId", "WalletId", "ProviderAccountId", "ProviderBindingHash", "EligibilityHash",
                       "ProviderPayoutId", "KillSwitchEpoch", "RiskDecisionId", "CreatedAt", "UpdatedAt"
                FROM public.economy_payout_operations;

                -- T2.4: admin-withdrawal runs (kind 2; RequestedBy maps onto
                -- the shared ActorId column).
                INSERT INTO public.economy_operations (
                    "Id", "OperationKind", "TenantId", "IdempotencyKey", "RequestHash", "ActorId",
                    "AmountUnits", "DestinationHash", "DispatchSnapshotHash", "State", "Version", "FencingToken",
                    "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                    "PeriodStart", "ApprovedBy", "PlatformFeeWalletId", "SourceAssetKey",
                    "ProviderTransferId", "ExecutionEpoch", "CreatedAt", "UpdatedAt")
                SELECT "Id", 2, "TenantId", "IdempotencyKey", "RequestHash", "RequestedBy",
                       "AmountUnits", "DestinationHash", "DispatchSnapshotHash", "State", "Version", "FencingToken",
                       "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                       "PeriodStart", "ApprovedBy", "PlatformFeeWalletId", "SourceAssetKey",
                       "ProviderTransferId", "ExecutionEpoch", "CreatedAt", "UpdatedAt"
                FROM public.economy_admin_withdrawal_runs;
                """);

            // Retarget every SECURITY DEFINER body that still addresses a
            // retired table. The payout and withdrawal CRUD writers get their
            // kind discriminator injected into the INSERT column lists and
            // VALUES heads (the RequestedBy column rides onto the shared
            // ActorId); the posting writers get the consumption-table renames
            // (the risk-decision INSERT gains ConsumptionKind = 2; reads that
            // filter on kind-exclusive columns stay kind-correct through the
            // shape constraint) and the Marketplace policy verification gains
            // the domain discriminator plus the ProductId -> SubjectId rename.
            migrationBuilder.Sql("""
                DO $t2_writers$
                DECLARE
                    definition text;
                    nl text;
                    values_head text;
                    values_head_crlf text;
                    policy_head text;
                    policy_head_crlf text;
                    policy_replacement text;
                    policy_replacement_crlf text;
                    function_oid oid;
                    function_name text;
                    changed boolean;
                    economy_function_oids oid[];
                BEGIN
                    -- The plain economy_private functions are collected once
                    -- and pg_get_functiondef is only ever evaluated per oid
                    -- inside the loop: the planner can never evaluate it on
                    -- other schemas' aggregates while filtering (a bare
                    -- pg_get_functiondef() filter over pg_proc errors on
                    -- pg_catalog.array_agg). Multi-line needles are built
                    -- with explicit chr(10)/chr(13) concatenations in BOTH
                    -- line-ending styles and applied without normalizing the
                    -- fetched body: the rewrite must be byte-preserving so
                    -- the mirrored Down restores exactly the text earlier
                    -- migrations left (their own pattern rewrites are
                    -- whitespace-sensitive). The table-typed readers are
                    -- excluded by name (they are recreated against
                    -- economy_operations by the next statement), so only the
                    -- in-place writers are rewritten here.
                    SELECT array_agg(p.oid ORDER BY p.proname)
                    INTO economy_function_oids
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private'
                      AND p.prokind = 'f';

                    nl := chr(10);
                    values_head := 'VALUES (' || nl || '        p_id, ';
                    values_head_crlf := 'VALUES (' || chr(13) || nl || '        p_id, ';
                    policy_head := 'public.economy_marketplace_currency_policy_versions policy' || nl ||
                                   '    WHERE policy."TenantId" = p_tenant_id';
                    policy_head_crlf := 'public.economy_marketplace_currency_policy_versions policy' || chr(13) || nl ||
                                        '    WHERE policy."TenantId" = p_tenant_id';
                    policy_replacement := 'public.economy_policy_versions policy' || nl ||
                                          '    WHERE policy."PolicyDomain" = 1' || nl ||
                                          '      AND policy."TenantId" = p_tenant_id';
                    policy_replacement_crlf := 'public.economy_policy_versions policy' || chr(13) || nl ||
                                               '    WHERE policy."PolicyDomain" = 1' || chr(13) || nl ||
                                               '      AND policy."TenantId" = p_tenant_id';

                    FOREACH function_oid IN ARRAY economy_function_oids LOOP
                        SELECT p.proname INTO function_name
                        FROM pg_proc p WHERE p.oid = function_oid;
                        definition := pg_get_functiondef(function_oid);
                        changed := false;

                        -- === payout-side writers (OperationKind = 1) ========
                        IF function_name IN (
                               'create_payout_operation_v1', 'create_payout_operation_v2',
                               'transition_payout_operation_v1', 'complete_payout_provider_event_v1',
                               'append_payout_authorization_evidence_v1') THEN
                            definition := replace(definition,
                                'public.economy_payout_operations%ROWTYPE',
                                'public.economy_operations%ROWTYPE');
                            definition := replace(definition,
                                'public.economy_payout_operations', 'public.economy_operations');
                            -- The v2 create carries the tenant column; both
                            -- create column lists gain the discriminator.
                            definition := replace(definition,
                                '"Id", "TenantId", "IdempotencyKey"',
                                '"Id", "OperationKind", "TenantId", "IdempotencyKey"');
                            definition := replace(definition,
                                '"Id", "IdempotencyKey"',
                                '"Id", "OperationKind", "IdempotencyKey"');
                            definition := replace(definition,
                                values_head || 'p_tenant_id,',
                                values_head || '1, p_tenant_id,');
                            definition := replace(definition,
                                values_head_crlf || 'p_tenant_id,',
                                values_head_crlf || '1, p_tenant_id,');
                            definition := replace(definition,
                                values_head || 'pg_catalog.btrim(p_idempotency_key)',
                                values_head || '1, pg_catalog.btrim(p_idempotency_key)');
                            definition := replace(definition,
                                values_head_crlf || 'pg_catalog.btrim(p_idempotency_key)',
                                values_head_crlf || '1, pg_catalog.btrim(p_idempotency_key)');
                            changed := true;
                        END IF;

                        -- === withdrawal-side writers (OperationKind = 2) ====
                        IF function_name IN (
                               'create_admin_withdrawal_run_v1', 'create_admin_withdrawal_run_v2',
                               'transition_admin_withdrawal_run_v1', 'transition_admin_withdrawal_run_v2',
                               'complete_admin_withdrawal_provider_event_v1',
                               'complete_admin_withdrawal_provider_event_v2',
                               'append_admin_withdrawal_audit_event_v1',
                               'append_admin_withdrawal_audit_event_v2') THEN
                            definition := replace(definition,
                                'public.economy_admin_withdrawal_runs%ROWTYPE',
                                'public.economy_operations%ROWTYPE');
                            definition := replace(definition,
                                'public.economy_admin_withdrawal_runs', 'public.economy_operations');
                            definition := replace(definition,
                                '"Id", "TenantId", "IdempotencyKey"',
                                '"Id", "OperationKind", "TenantId", "IdempotencyKey"');
                            definition := replace(definition,
                                '"Id", "IdempotencyKey"',
                                '"Id", "OperationKind", "IdempotencyKey"');
                            -- The withdrawal requester rides onto the shared
                            -- actor column (only the create INSERT lists
                            -- reference it).
                            definition := replace(definition, '"RequestedBy"', '"ActorId"');
                            definition := replace(definition,
                                values_head || 'p_tenant_id,',
                                values_head || '2, p_tenant_id,');
                            definition := replace(definition,
                                values_head_crlf || 'p_tenant_id,',
                                values_head_crlf || '2, p_tenant_id,');
                            definition := replace(definition,
                                values_head || 'pg_catalog.btrim(p_idempotency_key)',
                                values_head || '2, pg_catalog.btrim(p_idempotency_key)');
                            definition := replace(definition,
                                values_head_crlf || 'pg_catalog.btrim(p_idempotency_key)',
                                values_head_crlf || '2, pg_catalog.btrim(p_idempotency_key)');
                            changed := true;
                        END IF;

                        -- === posting writers + risk counter (shared) ========
                        -- The risk-decision burn INSERTs gain ConsumptionKind
                        -- = 2; the capability-receipt joins only read
                        -- kind-exclusive columns, so the plain rename keeps
                        -- them kind-correct.
                        IF definition LIKE '%public.economy_risk_decision_consumptions%'
                           OR definition LIKE '%public.economy_capability_receipt_consumptions%'
                           OR definition LIKE '%public.economy_marketplace_currency_policy_versions%' THEN
                            definition := replace(definition,
                                'public.economy_capability_receipt_consumptions',
                                'public.economy_unit_consumptions');
                            definition := replace(definition,
                                'public.economy_risk_decision_consumptions',
                                'public.economy_unit_consumptions');
                            -- The risk-burn INSERT column lists appear in two
                            -- layouts across the live writer bodies: the list
                            -- starting on the paren's line, and the paren
                            -- alone followed by a newline + indent (the live
                            -- post_registered_posting_v1/v2 bodies and the
                            -- aggregate-component writer). A literal needle
                            -- only caught the first layout, which left the
                            -- VALUES rewrite (that needle always matches)
                            -- adding the kind expression to a column list that
                            -- never gained the kind column. The regex tolerates
                            -- any whitespace run between the paren and "Id"
                            -- and mirrors the captured run byte for byte, so
                            -- the mirrored Down restore stays exact.
                            definition := regexp_replace(definition,
                                '\((\s*)"Id", "RiskDecisionId", "PostingGroupId"',
                                '(\1"Id", "ConsumptionKind", "RiskDecisionId", "PostingGroupId"', 'g');
                            definition := replace(definition,
                                'VALUES (gen_random_uuid(), p_risk_decision_id',
                                'VALUES (gen_random_uuid(), 2, p_risk_decision_id');
                            -- The Marketplace policy verification blocks: the
                            -- domain predicate rides first (so the per-domain
                            -- partial uniques and window indexes stay
                            -- usable), then the subject column rename. Both
                            -- line-ending styles are tried; the replacement
                            -- mirrors the matched style byte for byte.
                            definition := replace(definition, policy_head, policy_replacement);
                            definition := replace(definition, policy_head_crlf, policy_replacement_crlf);
                            definition := replace(definition,
                                'public.economy_marketplace_currency_policy_versions',
                                'public.economy_policy_versions');
                            definition := replace(definition,
                                'policy."ProductId"', 'policy."SubjectId"');
                            changed := true;
                        END IF;

                        IF changed THEN
                            EXECUTE definition;
                        END IF;
                    END LOOP;
                END
                $t2_writers$;
                """);

            // The ten table-typed reader functions follow their table: the
            // return type moves to the unified row and every lookup that is
            // not a primary-key equality gains the kind predicate so the
            // partial uniques stay usable and cross-kind rows can never leak.
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_id_v1(uuid);
                CREATE FUNCTION economy_private.read_payout_operation_by_id_v1(p_id uuid)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1 AND "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_id_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_id_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_by_id_v1(uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_idempotency_v1(text);
                CREATE FUNCTION economy_private.read_payout_operation_by_idempotency_v1(p_idempotency_key text)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1
                      AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_idempotency_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v1(text) FROM PUBLIC;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_idempotency_v2(uuid, text);
                CREATE FUNCTION economy_private.read_payout_operation_by_idempotency_v2(p_tenant_id uuid, p_idempotency_key text)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1
                      AND "TenantId" = p_tenant_id
                      AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_for_tenant_v2(uuid, uuid);
                CREATE FUNCTION economy_private.read_payout_operation_for_tenant_v2(p_tenant_id uuid, p_id uuid)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1 AND "TenantId" = p_tenant_id AND "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v1(uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_by_payee_v1(p_payee_id uuid, p_take integer)
                RETURNS SETOF public.economy_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_payee_id IS NULL THEN
                        RAISE EXCEPTION 'Payee ID is required.' USING ERRCODE = '22023';
                    END IF;

                    IF p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'Take must be between 1 and 100.' USING ERRCODE = '22023';
                    END IF;

                    RETURN QUERY
                    SELECT *
                    FROM public.economy_operations
                    WHERE "OperationKind" = 1 AND "PayeeId" = p_payee_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END;
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer) FROM PUBLIC;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_by_payee_v2(p_tenant_id uuid, p_payee_id uuid, p_take integer)
                RETURNS SETOF public.economy_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_tenant_id IS NULL OR p_payee_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'payout payee query is invalid' USING ERRCODE = '22023';
                    END IF;
                    RETURN QUERY
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1
                      AND "TenantId" = p_tenant_id AND "PayeeId" = p_payee_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END;
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_for_tenant_v2(uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_for_tenant_v2(p_tenant_id uuid, p_take integer)
                RETURNS SETOF public.economy_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_tenant_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'payout tenant query is invalid' USING ERRCODE = '22023';
                    END IF;
                    RETURN QUERY
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 1 AND "TenantId" = p_tenant_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END;
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_id_v1(uuid);
                CREATE FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(p_id uuid)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 2 AND "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_idempotency_v1(text);
                CREATE FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(p_idempotency_key text)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 2
                      AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_active_admin_withdrawal_run_by_period_v1(date);
                CREATE FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(p_period_start date)
                RETURNS SETOF public.economy_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_operations
                    WHERE "OperationKind" = 2
                      AND "PeriodStart" = p_period_start AND "State" NOT IN (6, 7)
                $function$;
                ALTER FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date)
                    TO gameguild_economy_writer;
                """);

            // No SECURITY DEFINER body may keep addressing a retired table,
            // and every operation create writer must carry its discriminator.
            migrationBuilder.Sql("""
                DO $t2_verify$
                DECLARE
                    definition text;
                    function_oid oid;
                    function_name text;
                    economy_function_oids oid[];
                BEGIN
                    SELECT array_agg(p.oid ORDER BY p.proname)
                    INTO economy_function_oids
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private'
                      AND p.prokind = 'f';

                    FOREACH function_oid IN ARRAY economy_function_oids LOOP
                        definition := pg_get_functiondef(function_oid);
                        IF definition ~ 'economy_(payout_operations|admin_withdrawal_runs|risk_decision_consumptions|capability_receipt_consumptions|marketplace_currency_policy_versions|ad_network_policy_versions|ad_reward_cap_consumptions|ad_reward_budget_consumptions)' THEN
                            RAISE EXCEPTION 'an economy writer still references a Tier 2 retired table';
                        END IF;
                        SELECT p.proname INTO function_name
                        FROM pg_proc p WHERE p.oid = function_oid;
                        IF function_name IN ('create_payout_operation_v1', 'create_payout_operation_v2',
                                             'create_admin_withdrawal_run_v1', 'create_admin_withdrawal_run_v2')
                           AND strpos(definition, '"OperationKind"') = 0 THEN
                            RAISE EXCEPTION 'an operation create writer did not gain its kind discriminator';
                        END IF;
                    END LOOP;
                END
                $t2_verify$;
                """);

            // Move the risk-consumption guards onto the unified table,
            // scoped to the risk-decision kind (the other kinds were never
            // covered by these guards and must stay writable).
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS validate_risk_consumption
                    ON public.economy_risk_decision_consumptions;
                DROP TRIGGER IF EXISTS deny_immutable_mutation
                    ON public.economy_risk_decision_consumptions;
                CREATE TRIGGER validate_risk_consumption
                    BEFORE INSERT ON public.economy_unit_consumptions
                    FOR EACH ROW
                    WHEN (NEW."ConsumptionKind" = 2)
                    EXECUTE FUNCTION economy_private.validate_risk_consumption_v1();
                CREATE TRIGGER deny_risk_consumption_mutation
                    BEFORE UPDATE OR DELETE ON public.economy_unit_consumptions
                    FOR EACH ROW
                    WHEN (OLD."ConsumptionKind" = 2)
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                """);

            // Least-privilege grants for the unified relations (same posture
            // as every other economy_% table).
            migrationBuilder.Sql("""
                DO $production_grants$
                DECLARE
                    relation_name text;
                BEGIN
                    FOR relation_name IN
                        SELECT tablename FROM pg_tables
                        WHERE schemaname = 'public' AND tablename LIKE 'economy_%'
                    LOOP
                        EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', relation_name);
                        EXECUTE format('REVOKE ALL ON TABLE public.%I FROM gameguild_economy_writer', relation_name);
                        EXECUTE format('GRANT SELECT ON TABLE public.%I TO gameguild_economy_runtime', relation_name);
                        EXECUTE format(
                            'GRANT SELECT, INSERT, UPDATE ON TABLE public.%I TO gameguild_economy_procedure_owner',
                            relation_name);
                        EXECUTE format('GRANT ALL ON TABLE public.%I TO gameguild_economy_migration', relation_name);
                    END LOOP;
                END
                $production_grants$;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_admin_withdrawal_audit_events_economy_operations_Ru~",
                table: "economy_admin_withdrawal_audit_events",
                column: "RunId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_outbox_economy_operations_OperationId",
                table: "economy_outbox",
                column: "OperationId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_outbox_economy_operations_RunId",
                table: "economy_outbox",
                column: "RunId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_payout_authorization_evidence_economy_operations_Op~",
                table: "economy_payout_authorization_evidence",
                column: "OperationId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_provider_events_economy_operations_OperationId",
                table: "economy_provider_events",
                column: "OperationId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_provider_events_economy_operations_RunId",
                table: "economy_provider_events",
                column: "RunId",
                principalTable: "economy_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // The legacy tables are retired only after every row has been
            // carried over and every writer has been retargeted.
            migrationBuilder.DropTable(
                name: "economy_ad_network_policy_versions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_budget_consumptions");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_cap_consumptions");

            migrationBuilder.DropTable(
                name: "economy_admin_withdrawal_runs");

            migrationBuilder.DropTable(
                name: "economy_capability_receipt_consumptions");

            migrationBuilder.DropTable(
                name: "economy_marketplace_currency_policy_versions");

            migrationBuilder.DropTable(
                name: "economy_payout_operations");

            migrationBuilder.DropTable(
                name: "economy_risk_decision_consumptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy tables are recreated first so the unified rows can be
            // carried back before the unified tables are dropped. The SECURITY
            // DEFINER writers are restored to their legacy targets with the
            // mirrored token rewrites from Up, and the ten table-typed readers
            // return to their original shapes and grant posture.
            migrationBuilder.DropForeignKey(
                name: "FK_economy_admin_withdrawal_audit_events_economy_operations_Ru~",
                table: "economy_admin_withdrawal_audit_events");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_outbox_economy_operations_OperationId",
                table: "economy_outbox");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_outbox_economy_operations_RunId",
                table: "economy_outbox");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_payout_authorization_evidence_economy_operations_Op~",
                table: "economy_payout_authorization_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_provider_events_economy_operations_OperationId",
                table: "economy_provider_events");

            migrationBuilder.DropForeignKey(
                name: "FK_economy_provider_events_economy_operations_RunId",
                table: "economy_provider_events");

            migrationBuilder.CreateTable(
                name: "economy_ad_network_policy_versions",
                columns: table => new
                {
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IssuanceMode = table.Column<int>(type: "integer", nullable: false),
                    YieldState = table.Column<int>(type: "integer", nullable: false),
                    EstimatedNetEcpmUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ContractedRevenueSharePpm = table.Column<int>(type: "integer", nullable: false),
                    SafetyBufferPpm = table.Column<int>(type: "integer", nullable: false),
                    MinimumVisiblePpm = table.Column<int>(type: "integer", nullable: false),
                    MaximumFocusLossTicks = table.Column<long>(type: "bigint", nullable: false),
                    MaximumRewardSoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReportsCurrentThrough = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReportStaleAfterTicks = table.Column<long>(type: "bigint", nullable: false),
                    Ranking = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    BudgetWindowTicks = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CanonicalPayload = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    FundedLossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    KeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    MaximumAsnSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MaximumDeviceSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MaximumGlobalSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MaximumIpSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MaximumNetworkSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    MaximumUserSoftUnits = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    ProviderCertified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProviderHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero)),
                    Signature = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_network_policy_versions", x => new { x.TenantId, x.Network, x.Version });
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_caps", "\"MaximumUserSoftUnits\" > 0 AND \"MaximumDeviceSoftUnits\" > 0 AND \"MaximumIpSoftUnits\" > 0 AND \"MaximumAsnSoftUnits\" > 0 AND \"MaximumNetworkSoftUnits\" > 0 AND \"MaximumGlobalSoftUnits\" > 0 AND \"FundedLossBudgetUsdNanos\" > 0");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_dual_control", "\"ProposedBy\" <> \"ApprovedBy\"");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_ppm", "\"ContractedRevenueSharePpm\" BETWEEN 0 AND 1000000 AND \"SafetyBufferPpm\" BETWEEN 0 AND 999999 AND \"MinimumVisiblePpm\" BETWEEN 0 AND 1000000");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_values", "\"Version\" > 0 AND \"EstimatedNetEcpmUsdNanos\" > 0 AND \"MaximumRewardSoftUnits\" > 0 AND \"MaximumFocusLossTicks\" >= 0 AND \"ReportStaleAfterTicks\" > 0 AND \"Ranking\" >= 0 AND \"BudgetWindowTicks\" > 0");
                    table.CheckConstraint("ck_economy_ad_network_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_budget_consumptions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceRiskHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    LossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_budget_consumptions", x => x.SessionId);
                    table.CheckConstraint("ck_economy_ad_reward_budget_consumptions_positive", "\"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_cap_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    SubjectHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WindowStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SoftUnits = table.Column<long>(type: "bigint", nullable: false),
                    LossBudgetUsdNanos = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_cap_consumptions", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_reward_cap_consumptions_positive", "\"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" >= 0");
                    table.CheckConstraint("ck_economy_ad_reward_cap_consumptions_scope", "\"Scope\" BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_economy_ad_reward_cap_consumptions_window", "\"WindowEndsAt\" > \"WindowStartedAt\" AND \"ConsumedAt\" >= \"WindowStartedAt\" AND \"ConsumedAt\" < \"WindowEndsAt\"");
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_cap_consumptions_economy_ad_reward_sessio~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // The legacy tables are recreated with their ORIGINAL column order
            // (the historical migrations' order, not the EF-alphabetical one)
            // so the rollback restores a byte-identical schema.
            migrationBuilder.CreateTable(
                name: "economy_admin_withdrawal_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    SourceAssetKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    ExecutionEpoch = table.Column<long>(type: "bigint", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReserveAuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderTransferId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_admin_withdrawal_runs", x => x.Id);
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_approval", "(\"State\" = 1 AND \"ApprovedBy\" IS NULL) OR (\"State\" BETWEEN 2 AND 7 AND \"ApprovedBy\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_dispatch_snapshot", "(\"State\" IN (1, 2) AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 3 AND 7 AND \"DispatchSnapshotHash\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_positive_versions", "\"Version\" > 0 AND \"FencingToken\" > 0 AND \"ExecutionEpoch\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_state", "\"State\" BETWEEN 1 AND 7");
                    table.CheckConstraint("ck_economy_admin_withdrawal_runs_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_capability_receipt_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_capability_receipt_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_capability_receipt_consumptions_economy_capability_~",
                        column: x => x.ReceiptId,
                        principalTable: "economy_capability_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_currency_policy_versions",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    HardPriceUnits = table.Column<long>(type: "bigint", nullable: false),
                    SoftPriceUnits = table.Column<long>(type: "bigint", nullable: false),
                    PlatformFeePpm = table.Column<int>(type: "integer", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    CanonicalPayload = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero)),
                    KeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: ""),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    PlatformFeeWalletId = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000")),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero)),
                    RefundHoldTicks = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Signature = table.Column<string>(type: "text", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_currency_policy_versions", x => new { x.TenantId, x.ProductId, x.Version });
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_dual_control", "\"ProposedBy\" <> \"ApprovedBy\"");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_fee", "\"PlatformFeePpm\" BETWEEN 0 AND 999999");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_prices", "(\"Mode\" = 1 AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" = 0) OR (\"Mode\" = 2 AND \"HardPriceUnits\" = 0 AND \"SoftPriceUnits\" > 0) OR (\"Mode\" IN (3, 4) AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" > 0)");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_version", "\"Version\" > 0");
                    table.CheckConstraint("ck_economy_marketplace_currency_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\" AND \"RefundHoldTicks\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_payout_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    ProviderAccountId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderBindingHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EligibilityHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderPayoutId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReserveAuthorizationEpoch = table.Column<long>(type: "bigint", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_payout_operations", x => x.Id);
                    table.CheckConstraint("ck_economy_payout_operations_dispatch", "(\"State\" = 1 AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 2 AND 6 AND \"DispatchSnapshotHash\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_payout_operations_positive_values", "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"KillSwitchEpoch\" >= 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");
                    table.CheckConstraint("ck_economy_payout_operations_state", "\"State\" BETWEEN 1 AND 6");
                    table.CheckConstraint("ck_economy_payout_operations_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_decision_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_decision_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_risk_decision_consumptions_economy_posting_groups_P~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_risk_decision_consumptions_economy_risk_decisions_R~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_network_policy_versions_TenantId_Network_Effecti~",
                table: "economy_ad_network_policy_versions",
                columns: new[] { "TenantId", "Network", "EffectiveAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_DeviceRiskHash_Consum~",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "DeviceRiskHash", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_Network_ConsumedAt",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "Network", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_budget_consumptions_UserId_ConsumedAt",
                table: "economy_ad_reward_budget_consumptions",
                columns: new[] { "UserId", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_cap_consumptions_SessionId_Scope",
                table: "economy_ad_reward_cap_consumptions",
                columns: new[] { "SessionId", "Scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_cap_consumptions_TenantId_Scope_SubjectHa~",
                table: "economy_ad_reward_cap_consumptions",
                columns: new[] { "TenantId", "Scope", "SubjectHash", "ConsumedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_admin_withdrawal_runs_state_updated",
                table: "economy_admin_withdrawal_runs",
                columns: new[] { "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_admin_withdrawal_runs_active_period",
                table: "economy_admin_withdrawal_runs",
                columns: new[] { "TenantId", "PeriodStart" },
                unique: true,
                filter: "\"State\" NOT IN (6, 7)");

            migrationBuilder.CreateIndex(
                name: "ux_economy_admin_withdrawal_runs_idempotency",
                table: "economy_admin_withdrawal_runs",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_capability_receipt_consumptions_receipt",
                table: "economy_capability_receipt_consumptions",
                column: "ReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_currency_policy_versions_TenantId_Produ~",
                table: "economy_marketplace_currency_policy_versions",
                columns: new[] { "TenantId", "ProductId", "EffectiveAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_payout_operations_state_updated",
                table: "economy_payout_operations",
                columns: new[] { "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_payout_operations_tenant_state_updated",
                table: "economy_payout_operations",
                columns: new[] { "TenantId", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_payout_operations_tenant_idempotency",
                table: "economy_payout_operations",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_decision_consumptions_decision",
                table: "economy_risk_decision_consumptions",
                column: "RiskDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_decision_consumptions_posting",
                table: "economy_risk_decision_consumptions",
                column: "PostingGroupId",
                unique: true);

            // Two wallet bindings existed only in the database (they predate
            // the EF model knowledge of these tables); restore them so the
            // rollback yields a byte-identical schema.
            migrationBuilder.AddForeignKey(
                name: "FK_economy_admin_withdrawal_runs_economy_wallets_PlatformFeeWal",
                table: "economy_admin_withdrawal_runs",
                column: "PlatformFeeWalletId",
                principalTable: "economy_wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_payout_operations_economy_wallets_WalletId",
                table: "economy_payout_operations",
                column: "WalletId",
                principalTable: "economy_wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // --- carry every unified row back to its legacy table -----------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_capability_receipt_consumptions (
                    "Id", "ReceiptId", "TenantId", "ActorId", "OperationFingerprint", "KillSwitchEpoch", "ConsumedAt")
                SELECT "Id", "ReceiptId", "TenantId", "ActorId", "OperationFingerprint", "KillSwitchEpoch", "ConsumedAt"
                FROM public.economy_unit_consumptions
                WHERE "ConsumptionKind" = 1;

                INSERT INTO public.economy_risk_decision_consumptions (
                    "Id", "RiskDecisionId", "PostingGroupId", "OperationFingerprint", "ConsumedAt")
                SELECT "Id", "RiskDecisionId", "PostingGroupId", "OperationFingerprint", "ConsumedAt"
                FROM public.economy_unit_consumptions
                WHERE "ConsumptionKind" = 2;

                INSERT INTO public.economy_ad_reward_cap_consumptions (
                    "Id", "TenantId", "SessionId", "Scope", "SubjectHash",
                    "WindowStartedAt", "WindowEndsAt", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt")
                SELECT "Id", "TenantId", "SessionId", "Scope", "SubjectHash",
                       "WindowStartedAt", "WindowEndsAt", "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt"
                FROM public.economy_unit_consumptions
                WHERE "ConsumptionKind" = 3;

                INSERT INTO public.economy_ad_reward_budget_consumptions (
                    "SessionId", "TenantId", "UserId", "DeviceRiskHash", "Network",
                    "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt")
                SELECT "SessionId", "TenantId", "UserId", "DeviceRiskHash", "Network",
                       "SoftUnits", "LossBudgetUsdNanos", "ConsumedAt"
                FROM public.economy_unit_consumptions
                WHERE "ConsumptionKind" = 4;

                INSERT INTO public.economy_marketplace_currency_policy_versions (
                    "TenantId", "ProductId", "Version", "SellerId", "Mode", "HardPriceUnits", "SoftPriceUnits",
                    "PlatformFeePpm", "EffectiveAt", "ExpiresAt", "PlatformFeeWalletId", "RefundHoldTicks",
                    "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt")
                SELECT "TenantId", "SubjectId", "Version", "SellerId", "Mode", "HardPriceUnits", "SoftPriceUnits",
                       "PlatformFeePpm", "EffectiveAt", "ExpiresAt", "PlatformFeeWalletId", "RefundHoldTicks",
                       "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt"
                FROM public.economy_policy_versions
                WHERE "PolicyDomain" = 1;

                INSERT INTO public.economy_ad_network_policy_versions (
                    "TenantId", "Network", "Version", "EffectiveAt", "ExpiresAt",
                    "IssuanceMode", "YieldState", "EstimatedNetEcpmUsdNanos", "ContractedRevenueSharePpm",
                    "SafetyBufferPpm", "MinimumVisiblePpm", "MaximumFocusLossTicks", "MaximumRewardSoftUnits",
                    "ReportsCurrentThrough", "ReportStaleAfterTicks", "Ranking",
                    "MaximumUserSoftUnits", "MaximumDeviceSoftUnits", "MaximumIpSoftUnits", "MaximumAsnSoftUnits",
                    "MaximumNetworkSoftUnits", "MaximumGlobalSoftUnits", "FundedLossBudgetUsdNanos", "BudgetWindowTicks",
                    "ProviderHash", "ProviderCertified",
                    "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt")
                SELECT "TenantId", "SubjectKey", "Version", "EffectiveAt", "ExpiresAt",
                       "IssuanceMode", "YieldState", "EstimatedNetEcpmUsdNanos", "ContractedRevenueSharePpm",
                       "SafetyBufferPpm", "MinimumVisiblePpm", "MaximumFocusLossTicks", "MaximumRewardSoftUnits",
                       "ReportsCurrentThrough", "ReportStaleAfterTicks", "Ranking",
                       "MaximumUserSoftUnits", "MaximumDeviceSoftUnits", "MaximumIpSoftUnits", "MaximumAsnSoftUnits",
                       "MaximumNetworkSoftUnits", "MaximumGlobalSoftUnits", "FundedLossBudgetUsdNanos", "BudgetWindowTicks",
                       "ProviderHash", "ProviderCertified",
                       "CanonicalPayload", "PayloadHash", "KeyId", "Signature", "ProposedBy", "ApprovedBy", "PublishedAt"
                FROM public.economy_policy_versions
                WHERE "PolicyDomain" = 2;

                INSERT INTO public.economy_payout_operations (
                    "Id", "TenantId", "IdempotencyKey", "RequestHash", "ActorId", "PayeeId", "WalletId",
                    "AmountUnits", "ProviderAccountId", "DestinationHash", "ProviderBindingHash", "EligibilityHash",
                    "DispatchSnapshotHash", "ProviderPayoutId", "State", "Version", "FencingToken", "KillSwitchEpoch",
                    "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion", "RiskDecisionId",
                    "CreatedAt", "UpdatedAt")
                SELECT "Id", "TenantId", "IdempotencyKey", "RequestHash", "ActorId", "PayeeId", "WalletId",
                       "AmountUnits", "ProviderAccountId", "DestinationHash", "ProviderBindingHash", "EligibilityHash",
                       "DispatchSnapshotHash", "ProviderPayoutId", "State", "Version", "FencingToken", "KillSwitchEpoch",
                       "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion", "RiskDecisionId",
                       "CreatedAt", "UpdatedAt"
                FROM public.economy_operations
                WHERE "OperationKind" = 1;

                INSERT INTO public.economy_admin_withdrawal_runs (
                    "Id", "TenantId", "IdempotencyKey", "RequestHash", "PeriodStart", "RequestedBy", "ApprovedBy",
                    "PlatformFeeWalletId", "AmountUnits", "SourceAssetKey", "DestinationHash", "State", "Version",
                    "FencingToken", "ExecutionEpoch", "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                    "DispatchSnapshotHash", "ProviderTransferId", "CreatedAt", "UpdatedAt")
                SELECT "Id", "TenantId", "IdempotencyKey", "RequestHash", "PeriodStart", "ActorId", "ApprovedBy",
                       "PlatformFeeWalletId", "AmountUnits", "SourceAssetKey", "DestinationHash", "State", "Version",
                       "FencingToken", "ExecutionEpoch", "ReserveVersion", "ReserveAuthorizationEpoch", "PolicyVersion",
                       "DispatchSnapshotHash", "ProviderTransferId", "CreatedAt", "UpdatedAt"
                FROM public.economy_operations
                WHERE "OperationKind" = 2;
                """);

            // Mirror of the Up writer rewrites: strip the discriminator
            // tokens, restore the withdrawal RequestedBy column and point
            // every body back at its legacy table. Same oid-array/FOREACH
            // discipline as Up (readers are excluded by name; they are
            // recreated by the next statement).
            migrationBuilder.Sql("""
                DO $t2_writers_down$
                DECLARE
                    definition text;
                    values_head text;
                    values_head_crlf text;
                    unified_policy_head text;
                    unified_policy_head_crlf text;
                    legacy_policy_head text;
                    legacy_policy_head_crlf text;
                    capability_join text;
                    capability_join_crlf text;
                    legacy_capability_join text;
                    legacy_capability_join_crlf text;
                    function_oid oid;
                    function_name text;
                    changed boolean;
                    economy_function_oids oid[];
                BEGIN
                    SELECT array_agg(p.oid ORDER BY p.proname)
                    INTO economy_function_oids
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private'
                      AND p.prokind = 'f';

                    values_head := 'VALUES (' || chr(10) || '        p_id, ';
                    values_head_crlf := 'VALUES (' || chr(13) || chr(10) || '        p_id, ';
                    unified_policy_head := 'public.economy_policy_versions policy' || chr(10) ||
                                           '    WHERE policy."PolicyDomain" = 1' || chr(10) ||
                                           '      AND policy."TenantId" = p_tenant_id';
                    unified_policy_head_crlf := 'public.economy_policy_versions policy' || chr(13) || chr(10) ||
                                                '    WHERE policy."PolicyDomain" = 1' || chr(13) || chr(10) ||
                                                '      AND policy."TenantId" = p_tenant_id';
                    legacy_policy_head := 'public.economy_marketplace_currency_policy_versions policy' || chr(10) ||
                                          '    WHERE policy."TenantId" = p_tenant_id';
                    legacy_policy_head_crlf := 'public.economy_marketplace_currency_policy_versions policy' || chr(13) || chr(10) ||
                                               '    WHERE policy."TenantId" = p_tenant_id';
                    capability_join := 'JOIN public.economy_unit_consumptions consumption' || chr(10) ||
                                       '      ON consumption."ReceiptId"';
                    capability_join_crlf := 'JOIN public.economy_unit_consumptions consumption' || chr(13) || chr(10) ||
                                            '      ON consumption."ReceiptId"';
                    legacy_capability_join := 'JOIN public.economy_capability_receipt_consumptions consumption' || chr(10) ||
                                              '      ON consumption."ReceiptId"';
                    legacy_capability_join_crlf := 'JOIN public.economy_capability_receipt_consumptions consumption' || chr(13) || chr(10) ||
                                                   '      ON consumption."ReceiptId"';

                    FOREACH function_oid IN ARRAY economy_function_oids LOOP
                        SELECT p.proname INTO function_name
                        FROM pg_proc p WHERE p.oid = function_oid;
                        definition := pg_get_functiondef(function_oid);
                        changed := false;

                        -- === payout-side writers ==============================
                        IF function_name IN (
                               'create_payout_operation_v1', 'create_payout_operation_v2',
                               'transition_payout_operation_v1', 'complete_payout_provider_event_v1',
                               'append_payout_authorization_evidence_v1') THEN
                            definition := replace(definition,
                                '"Id", "OperationKind", "TenantId", "IdempotencyKey"',
                                '"Id", "TenantId", "IdempotencyKey"');
                            definition := replace(definition,
                                '"Id", "OperationKind", "IdempotencyKey"',
                                '"Id", "IdempotencyKey"');
                            definition := replace(definition,
                                values_head || '1, p_tenant_id,',
                                values_head || 'p_tenant_id,');
                            definition := replace(definition,
                                values_head_crlf || '1, p_tenant_id,',
                                values_head_crlf || 'p_tenant_id,');
                            definition := replace(definition,
                                values_head || '1, pg_catalog.btrim(p_idempotency_key)',
                                values_head || 'pg_catalog.btrim(p_idempotency_key)');
                            definition := replace(definition,
                                values_head_crlf || '1, pg_catalog.btrim(p_idempotency_key)',
                                values_head_crlf || 'pg_catalog.btrim(p_idempotency_key)');
                            definition := replace(definition,
                                'public.economy_operations%ROWTYPE',
                                'public.economy_payout_operations%ROWTYPE');
                            definition := replace(definition,
                                'public.economy_operations', 'public.economy_payout_operations');
                            changed := true;
                        END IF;

                        -- === withdrawal-side writers ==========================
                        IF function_name IN (
                               'create_admin_withdrawal_run_v1', 'create_admin_withdrawal_run_v2',
                               'transition_admin_withdrawal_run_v1', 'transition_admin_withdrawal_run_v2',
                               'complete_admin_withdrawal_provider_event_v1',
                               'complete_admin_withdrawal_provider_event_v2',
                               'append_admin_withdrawal_audit_event_v1',
                               'append_admin_withdrawal_audit_event_v2') THEN
                            definition := replace(definition,
                                '"Id", "OperationKind", "TenantId", "IdempotencyKey"',
                                '"Id", "TenantId", "IdempotencyKey"');
                            definition := replace(definition,
                                '"Id", "OperationKind", "IdempotencyKey"',
                                '"Id", "IdempotencyKey"');
                            definition := replace(definition,
                                values_head || '2, p_tenant_id,',
                                values_head || 'p_tenant_id,');
                            definition := replace(definition,
                                values_head_crlf || '2, p_tenant_id,',
                                values_head_crlf || 'p_tenant_id,');
                            definition := replace(definition,
                                values_head || '2, pg_catalog.btrim(p_idempotency_key)',
                                values_head || 'pg_catalog.btrim(p_idempotency_key)');
                            definition := replace(definition,
                                values_head_crlf || '2, pg_catalog.btrim(p_idempotency_key)',
                                values_head_crlf || 'pg_catalog.btrim(p_idempotency_key)');
                            -- Only the two create INSERT lists carry the actor
                            -- column (the audit appends expose an ActorId of
                            -- their own in RETURNS TABLE, untouched here).
                            IF strpos(definition, 'INSERT INTO public.economy_operations') > 0 THEN
                                definition := replace(definition, '"ActorId"', '"RequestedBy"');
                            END IF;
                            definition := replace(definition,
                                'public.economy_operations%ROWTYPE',
                                'public.economy_admin_withdrawal_runs%ROWTYPE');
                            definition := replace(definition,
                                'public.economy_operations', 'public.economy_admin_withdrawal_runs');
                            changed := true;
                        END IF;

                        -- === posting writers + risk counter ===================
                        IF definition LIKE '%public.economy_unit_consumptions%'
                           OR definition LIKE '%public.economy_policy_versions%' THEN
                            -- Mirrors the Up regex: tolerates the whitespace
                            -- run between the paren and "Id" (same-line and
                            -- paren-newline layouts) and re-emits it byte for
                            -- byte, so the restored body matches the original
                            -- writer definition exactly.
                            definition := regexp_replace(definition,
                                '\((\s*)"Id", "ConsumptionKind", "RiskDecisionId", "PostingGroupId"',
                                '(\1"Id", "RiskDecisionId", "PostingGroupId"', 'g');
                            definition := replace(definition,
                                'VALUES (gen_random_uuid(), 2, p_risk_decision_id',
                                'VALUES (gen_random_uuid(), p_risk_decision_id');
                            definition := replace(definition, unified_policy_head, legacy_policy_head);
                            definition := replace(definition, unified_policy_head_crlf, legacy_policy_head_crlf);
                            definition := replace(definition,
                                'policy."SubjectId"', 'policy."ProductId"');
                            definition := replace(definition,
                                'public.economy_policy_versions',
                                'public.economy_marketplace_currency_policy_versions');
                            -- The capability-receipt joins are the only
                            -- references qualified by the receipt column;
                            -- everything else still pointing at the unified
                            -- consumption table is a risk-decision read or
                            -- insert.
                            definition := replace(definition, capability_join, legacy_capability_join);
                            definition := replace(definition, capability_join_crlf, legacy_capability_join_crlf);
                            definition := replace(definition,
                                'public.economy_unit_consumptions',
                                'public.economy_risk_decision_consumptions');
                            changed := true;
                        END IF;

                        IF changed THEN
                            EXECUTE definition;
                        END IF;
                    END LOOP;
                END
                $t2_writers_down$;
                """);

            // Restore the ten table-typed readers to their legacy shapes and
            // grant posture.
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_id_v1(uuid);
                CREATE FUNCTION economy_private.read_payout_operation_by_id_v1(p_id uuid)
                RETURNS SETOF economy_payout_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_payout_operations WHERE "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_id_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_id_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_by_id_v1(uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_idempotency_v1(text);
                CREATE FUNCTION economy_private.read_payout_operation_by_idempotency_v1(p_idempotency_key text)
                RETURNS SETOF economy_payout_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_payout_operations
                    WHERE "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_idempotency_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v1(text) FROM PUBLIC;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_by_idempotency_v2(uuid, text);
                CREATE FUNCTION economy_private.read_payout_operation_by_idempotency_v2(p_tenant_id uuid, p_idempotency_key text)
                RETURNS SETOF economy_payout_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_payout_operations
                    WHERE "TenantId" = p_tenant_id
                      AND "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_by_idempotency_v2(uuid, text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operation_for_tenant_v2(uuid, uuid);
                CREATE FUNCTION economy_private.read_payout_operation_for_tenant_v2(p_tenant_id uuid, p_id uuid)
                RETURNS SETOF economy_payout_operations
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_payout_operations
                    WHERE "TenantId" = p_tenant_id AND "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operation_for_tenant_v2(uuid, uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v1(uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_by_payee_v1(p_payee_id uuid, p_take integer)
                RETURNS SETOF economy_payout_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_payee_id IS NULL THEN
                        RAISE EXCEPTION 'Payee ID is required.' USING ERRCODE = '22023';
                    END IF;

                    IF p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'Take must be between 1 and 100.' USING ERRCODE = '22023';
                    END IF;

                    RETURN QUERY
                    SELECT *
                    FROM public.economy_payout_operations
                    WHERE "PayeeId" = p_payee_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END;
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer) FROM PUBLIC;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_by_payee_v2(p_tenant_id uuid, p_payee_id uuid, p_take integer)
                RETURNS SETOF economy_payout_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_tenant_id IS NULL OR p_payee_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'payout payee query is invalid' USING ERRCODE = '22023';
                    END IF;
                    RETURN QUERY
                    SELECT * FROM public.economy_payout_operations
                    WHERE "TenantId" = p_tenant_id AND "PayeeId" = p_payee_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operations_by_payee_v2(uuid, uuid, integer)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_payout_operations_for_tenant_v2(uuid, integer);
                CREATE FUNCTION economy_private.read_payout_operations_for_tenant_v2(p_tenant_id uuid, p_take integer)
                RETURNS SETOF economy_payout_operations
                LANGUAGE plpgsql
                STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                BEGIN
                    IF p_tenant_id IS NULL OR p_take < 1 OR p_take > 100 THEN
                        RAISE EXCEPTION 'payout tenant query is invalid' USING ERRCODE = '22023';
                    END IF;
                    RETURN QUERY
                    SELECT * FROM public.economy_payout_operations
                    WHERE "TenantId" = p_tenant_id
                    ORDER BY "CreatedAt" DESC, "Id" DESC
                    LIMIT p_take;
                END
                $function$;
                ALTER FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_operations_for_tenant_v2(uuid, integer)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_id_v1(uuid);
                CREATE FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(p_id uuid)
                RETURNS SETOF economy_admin_withdrawal_runs
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_admin_withdrawal_runs WHERE "Id" = p_id
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_idempotency_v1(text);
                CREATE FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(p_idempotency_key text)
                RETURNS SETOF economy_admin_withdrawal_runs
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_admin_withdrawal_runs
                    WHERE "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_active_admin_withdrawal_run_by_period_v1(date);
                CREATE FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(p_period_start date)
                RETURNS SETOF economy_admin_withdrawal_runs
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_admin_withdrawal_runs
                    WHERE "PeriodStart" = p_period_start AND "State" NOT IN (6, 7)
                $function$;
                ALTER FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date)
                    TO gameguild_economy_writer;
                """);

            // Restore the legacy risk-consumption guards and drop the
            // kind-scoped copies from the unified table (which is about to
            // disappear).
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS validate_risk_consumption
                    ON public.economy_unit_consumptions;
                DROP TRIGGER IF EXISTS deny_risk_consumption_mutation
                    ON public.economy_unit_consumptions;
                CREATE TRIGGER validate_risk_consumption
                    BEFORE INSERT ON public.economy_risk_decision_consumptions
                    FOR EACH ROW
                    EXECUTE FUNCTION economy_private.validate_risk_consumption_v1();
                CREATE TRIGGER deny_immutable_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_risk_decision_consumptions
                    FOR EACH ROW
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                """);

            // Least-privilege grants for the restored relations.
            migrationBuilder.Sql("""
                DO $production_grants$
                DECLARE
                    relation_name text;
                BEGIN
                    FOR relation_name IN
                        SELECT tablename FROM pg_tables
                        WHERE schemaname = 'public' AND tablename LIKE 'economy_%'
                    LOOP
                        EXECUTE format('REVOKE ALL ON TABLE public.%I FROM PUBLIC', relation_name);
                        EXECUTE format('REVOKE ALL ON TABLE public.%I FROM gameguild_economy_writer', relation_name);
                        EXECUTE format('GRANT SELECT ON TABLE public.%I TO gameguild_economy_runtime', relation_name);
                        EXECUTE format(
                            'GRANT SELECT, INSERT, UPDATE ON TABLE public.%I TO gameguild_economy_procedure_owner',
                            relation_name);
                        EXECUTE format('GRANT ALL ON TABLE public.%I TO gameguild_economy_migration', relation_name);
                    END LOOP;
                END
                $production_grants$;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_admin_withdrawal_audit_events_economy_admin_withdraw",
                table: "economy_admin_withdrawal_audit_events",
                column: "RunId",
                principalTable: "economy_admin_withdrawal_runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_outbox_economy_admin_withdrawal_runs_RunId",
                table: "economy_outbox",
                column: "RunId",
                principalTable: "economy_admin_withdrawal_runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_outbox_economy_payout_operations_OperationId",
                table: "economy_outbox",
                column: "OperationId",
                principalTable: "economy_payout_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_payout_authorization_evidence_economy_payout_operat~",
                table: "economy_payout_authorization_evidence",
                column: "OperationId",
                principalTable: "economy_payout_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_provider_events_economy_admin_withdrawal_runs_RunId",
                table: "economy_provider_events",
                column: "RunId",
                principalTable: "economy_admin_withdrawal_runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_economy_provider_events_economy_payout_operations_Operation~",
                table: "economy_provider_events",
                column: "OperationId",
                principalTable: "economy_payout_operations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "economy_operations");

            migrationBuilder.DropTable(
                name: "economy_policy_versions");

            migrationBuilder.DropTable(
                name: "economy_unit_consumptions");
        }
    }
}
