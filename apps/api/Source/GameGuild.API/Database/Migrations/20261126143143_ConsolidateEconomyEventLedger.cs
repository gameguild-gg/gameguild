using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyEventLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StreamKind = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: true),
                    KindText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderDisputeReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CumulativeDisputedHardUnits = table.Column<long>(type: "bigint", nullable: true),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DebtWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeltaHardUnits = table.Column<long>(type: "bigint", nullable: true),
                    OutstandingHardUnits = table.Column<long>(type: "bigint", nullable: true),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    RiskReviewCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceHashes = table.Column<string>(type: "jsonb", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    DecisionCode = table.Column<int>(type: "integer", nullable: true),
                    ComplianceHoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegacyCutoverTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Percentage = table.Column<int>(type: "integer", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BountyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BountyVersion = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_events", x => x.Id);
                    table.CheckConstraint("ck_economy_events_sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("ck_economy_events_stream_shape", "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL)");
                    table.CheckConstraint("ck_economy_events_values", "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND (\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND (\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND (\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND (\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND (\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\"))");
                    table.ForeignKey(
                        name: "FK_economy_events_economy_ad_reward_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_bounties_BountyId",
                        column: x => x.BountyId,
                        principalTable: "economy_bounties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_compliance_holds_ComplianceHoldId",
                        column: x => x.ComplianceHoldId,
                        principalTable: "economy_compliance_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_holds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "economy_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_legacy_cutovers_LegacyCutoverTenantId",
                        column: x => x.LegacyCutoverTenantId,
                        principalTable: "economy_legacy_cutovers",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_marketplace_settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_operations_RunId",
                        column: x => x.RunId,
                        principalTable: "economy_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_provider_disputes_ProviderDisputeRef~",
                        column: x => x.ProviderDisputeReference,
                        principalTable: "economy_provider_disputes",
                        principalColumn: "ProviderDisputeReference",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_risk_review_cases_RiskReviewCaseId",
                        column: x => x.RiskReviewCaseId,
                        principalTable: "economy_risk_review_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_source_stamps_SourceStampId",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_events_economy_wallet_debts_DebtWalletId",
                        column: x => x.DebtWalletId,
                        principalTable: "economy_wallet_debts",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_bounty_expiration_recorded",
                table: "economy_events",
                columns: new[] { "OccurredAt", "Id" },
                filter: "\"StreamKind\" = 12");

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_session",
                table: "economy_events",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_source_stamp",
                table: "economy_events",
                column: "SourceStampId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_ad_reward_milestone_sequence",
                table: "economy_events",
                columns: new[] { "SessionId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 10");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_ad_reward_session_sequence",
                table: "economy_events",
                columns: new[] { "SessionId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 9");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_admin_withdrawal_hash",
                table: "economy_events",
                column: "Hash",
                unique: true,
                filter: "\"StreamKind\" = 11");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_admin_withdrawal_sequence",
                table: "economy_events",
                columns: new[] { "RunId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 11");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_bounty_expiration",
                table: "economy_events",
                column: "BountyId",
                unique: true,
                filter: "\"StreamKind\" = 12");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_compliance_hold_kind_actor",
                table: "economy_events",
                columns: new[] { "ComplianceHoldId", "KindText", "ActorId" },
                unique: true,
                filter: "\"StreamKind\" = 6");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_compliance_hold_sequence",
                table: "economy_events",
                columns: new[] { "ComplianceHoldId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 6");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_debt_sequence",
                table: "economy_events",
                columns: new[] { "DebtWalletId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 3");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_dispute_sequence",
                table: "economy_events",
                columns: new[] { "ProviderDisputeReference", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_hold_sequence",
                table: "economy_events",
                columns: new[] { "HoldId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_legacy_cutover_sequence",
                table: "economy_events",
                columns: new[] { "LegacyCutoverTenantId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 7");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_provider_event",
                table: "economy_events",
                column: "ProviderEventId",
                unique: true,
                filter: "\"StreamKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_review_case_sequence",
                table: "economy_events",
                columns: new[] { "RiskReviewCaseId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 5");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_settlement_sequence",
                table: "economy_events",
                columns: new[] { "SettlementId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 8");

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_source_stamp_sequence",
                table: "economy_events",
                columns: new[] { "SourceStampId", "Sequence" },
                unique: true,
                filter: "\"StreamKind\" = 1");

            // ===================================================================
            // #1092 Tier 2.1 and 2.5 — eleven Economy event and audit tables
            // merge into economy_events behind EconomyEventStreamKind. The
            // ledger is created above; its rows are carried over with their
            // kind, the audit reader and every SECURITY DEFINER writer are
            // retargeted, the per-table immutability triggers are replaced by
            // one kind-scoped trigger, and only then are the legacy tables
            // dropped. Down mirrors every step, so a rollback restores the
            // original rows and function text.
            // ===================================================================

            // Carry every legacy row into the ledger. Id keeps its value
            // wherever the legacy table had one; the two tables keyed another
            // way (provider dispute events by ProviderEventId, admin withdrawal
            // audit entries by RunId + Sequence) get a fresh Id — neither is
            // ever read back through Id.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "SourceStampId", "Sequence", "Kind", "EvidenceHash", "OccurredAt")
                SELECT "Id", 1, "SourceStampId", "Sequence", "State", "EvidenceHash", "OccurredAt"
                FROM public.economy_source_stamp_events;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "ProviderEventId", "ProviderDisputeReference", "SourceStampId", "Sequence",
                    "Status", "CumulativeDisputedHardUnits", "RequestHash", "OccurredAt")
                SELECT gen_random_uuid(), 2, "ProviderEventId", "ProviderDisputeReference", "SourceStampId",
                       "ProviderSequence", "Status", "CumulativeDisputedHardUnits", "RequestHash", "OccurredAt"
                FROM public.economy_provider_dispute_events;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "DebtWalletId", "SourceStampId", "Sequence", "DeltaHardUnits",
                    "OutstandingHardUnits", "OccurredAt")
                SELECT "Id", 3, "WalletId", "SourceStampId", "Sequence", "DeltaHardUnits", "OutstandingHardUnits",
                       "OccurredAt"
                FROM public.economy_wallet_debt_events;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt")
                SELECT "Id", 4, "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"
                FROM public.economy_hold_events;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "RiskReviewCaseId", "Sequence", "Kind", "ActorId", "EvidenceHashes",
                    "Resolution", "DecisionCode", "OccurredAt")
                SELECT "Id", 5, "RiskReviewCaseId", "Sequence", "Kind", "ActorId", "EvidenceHashes", "Resolution",
                       "DecisionCode", "OccurredAt"
                FROM public.economy_risk_review_events;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "ComplianceHoldId", "Sequence", "KindText", "ActorId", "EvidenceHash",
                    "OccurredAt")
                SELECT "Id", 6, "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"
                FROM public.economy_compliance_hold_events;

                -- RecordedAt is the row's OccurredAt.
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "LegacyCutoverTenantId", "BatchId", "Sequence", "Kind", "ActorId", "Reason",
                    "ReauthenticationHash", "EvidenceHash", "OccurredAt")
                SELECT "Id", 7, "TenantId", "BatchId", "Sequence", "State", "ActorId", "Reason",
                       "ReauthenticationHash", "EvidenceHash", "RecordedAt"
                FROM public.economy_legacy_cutover_audit;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "TenantId", "SettlementId", "Sequence", "KindText", "EvidenceHash",
                    "OccurredAt")
                SELECT "Id", 8, "TenantId", "SettlementId", "Sequence", "EventKind", "EvidenceHash", "OccurredAt"
                FROM public.economy_marketplace_events;

                -- The session timeline (Tier 1.5) held two independently
                -- numbered streams; each becomes its own kind.
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "SessionId", "Sequence", "Kind", "EvidenceHash", "OccurredAt")
                SELECT "Id", 9, "SessionId", "Sequence", "State", "EvidenceHash", "OccurredAt"
                FROM public.economy_ad_reward_session_timeline
                WHERE "TimelineKind" = 1;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "SessionId", "Sequence", "Percentage", "EvidenceHash", "OccurredAt")
                SELECT "Id", 10, "SessionId", "Sequence", "Percentage", "EvidenceHash", "OccurredAt"
                FROM public.economy_ad_reward_session_timeline
                WHERE "TimelineKind" = 2;

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "TenantId", "RunId", "Sequence", "KindText", "ActorId", "Evidence",
                    "OccurredAt", "PreviousHash", "Hash")
                SELECT gen_random_uuid(), 11, "TenantId", "RunId", "Sequence", "Kind", "ActorId", "Evidence",
                       "OccurredAt", "PreviousHash", "Hash"
                FROM public.economy_admin_withdrawal_audit_events;

                -- One expiration record per bounty: the first entry of the
                -- bounty's stream, recorded when it expired.
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "BountyId", "ExpiresAt", "Sequence", "OccurredAt", "BountyVersion")
                SELECT "Id", 12, "BountyId", "ExpiresAt", 1, "RecordedAt", "BountyVersion"
                FROM public.economy_bounty_expiration_events;
                """);

            // The audit reader RETURNS SETOF its table, so it cannot be
            // replaced in place: it is dropped and recreated against the ledger
            // with the kind predicate in the lookup, keeping owner and grants.
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_audit_events_v1(uuid);
                CREATE FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(p_run_id uuid)
                    RETURNS SETOF public.economy_events
                    LANGUAGE sql STABLE SECURITY DEFINER
                    SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_events
                    WHERE "StreamKind" = 11 AND "RunId" = p_run_id ORDER BY "Sequence"
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                    TO gameguild_economy_writer;
                """);

            // Retarget every SECURITY DEFINER writer that appended to a legacy
            // event table. Each rewrite must match its function text exactly
            // once: a writer whose shape has drifted aborts the migration
            // instead of silently keeping a statement that addresses a table
            // this migration drops. The rewrites keep the original statement
            // layout, and Down applies their exact inverses.
            migrationBuilder.Sql("""
                CREATE FUNCTION pg_temp.rewrite_economy_function_text_v1(
                    p_definition text, p_pattern text, p_replacement text, p_label text)
                RETURNS text
                LANGUAGE plpgsql
                AS $rewrite$
                DECLARE
                    match_count integer;
                BEGIN
                    match_count := (SELECT count(*) FROM regexp_matches(p_definition, p_pattern, 'g'));
                    IF match_count <> 1 THEN
                        RAISE EXCEPTION 'economy event writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $events_writers$
                DECLARE
                    writer_name text;
                    definition text;
                    rewrite record;
                BEGIN
                    FOR writer_name IN
                        SELECT unnest(ARRAY[
                            'observe_hard_coin_top_up_v1', 'confirm_observed_hard_coin_top_up_v1',
                            'post_ad_reward_issuance_v1', 'post_legacy_balance_backfill_v1',
                            'complete_bounty_claim_v1', 'post_provider_reversal_v2',
                            'post_marketplace_settlement_v1', 'post_marketplace_refund_v1',
                            'append_admin_withdrawal_audit_event_v2', 'append_admin_withdrawal_audit_event_v1'])
                    LOOP
                        SELECT pg_get_functiondef(p.oid) INTO definition
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private' AND p.prokind = 'f' AND p.proname = writer_name;
                        IF definition IS NULL THEN
                            RAISE EXCEPTION 'economy event writer % is absent', writer_name;
                        END IF;

                        FOR rewrite IN
                            SELECT * FROM (VALUES
                                ('observe_hard_coin_top_up_v1', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('confirm_observed_hard_coin_top_up_v1', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('post_ad_reward_issuance_v1', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('post_legacy_balance_backfill_v1', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('complete_bounty_claim_v1', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('post_provider_reversal_v2', 1,
                                 'INSERT INTO public\.economy_source_stamp_events \((\s*)"Id", "SourceStampId", "Sequence", "State"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"\2\3 1,',
                                 'source stamp event append'),
                                ('post_provider_reversal_v2', 2,
                                 'FROM public\.economy_source_stamp_events WHERE "SourceStampId" = p_root_source_stamp_id',
                                 'FROM public.economy_events WHERE "StreamKind" = 1 AND "SourceStampId" = p_root_source_stamp_id',
                                 'source stamp event sequence'),
                                ('post_provider_reversal_v2', 3,
                                 'FROM public\.economy_wallet_debt_events(\s+)WHERE "WalletId" = claim\."WalletId"',
                                 'FROM public.economy_events\1WHERE "StreamKind" = 3 AND "DebtWalletId" = claim."WalletId"',
                                 'wallet debt event sequence'),
                                ('post_provider_reversal_v2', 4,
                                 'INSERT INTO public\.economy_wallet_debt_events \((\s*)"Id", "WalletId", "SourceStampId", "Sequence", "DeltaHardUnits", "OutstandingHardUnits", "OccurredAt"\)(\s*VALUES \(\s*)(gen_random_uuid\(\),) claim\."WalletId", ',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "DebtWalletId", "SourceStampId", "Sequence", "DeltaHardUnits", "OutstandingHardUnits", "OccurredAt")\2\3 3, claim."WalletId", ',
                                 'wallet debt event append'),
                                ('post_marketplace_settlement_v1', 1,
                                 'INSERT INTO public\.economy_hold_events \((\s*)"Id", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"\)(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(\s*[^,]+, ''[^'']+''\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt")\2VALUES (\3 4,',
                                 'hold event append'),
                                ('post_marketplace_settlement_v1', 2,
                                 'INSERT INTO public\.economy_marketplace_events \((\s*)"Id", "TenantId", "SettlementId", "Sequence", "EventKind"(, "EvidenceHash", "OccurredAt"\))(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(p_[a-z_]+, ''[^'']+''\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "TenantId", "SettlementId", "Sequence", "KindText"\2\3VALUES (\4 8,',
                                 'marketplace event append'),
                                ('post_marketplace_refund_v1', 1,
                                 'INSERT INTO public\.economy_hold_events \((\s*)"Id", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"\)(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(\s*[^,]+, ''[^'']+''\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt")\2VALUES (\3 4,',
                                 'hold event append'),
                                ('post_marketplace_refund_v1', 2,
                                 'INSERT INTO public\.economy_marketplace_events \((\s*)"Id", "TenantId", "SettlementId", "Sequence", "EventKind"(, "EvidenceHash", "OccurredAt"\))(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(p_[a-z_]+, ''[^'']+''\),)',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "TenantId", "SettlementId", "Sequence", "KindText"\2\3VALUES (\4 8,',
                                 'marketplace event append'),
                                ('post_marketplace_refund_v1', 3,
                                 'FROM public\.economy_marketplace_events event(\s+)WHERE event\."SettlementId" = p_settlement_id',
                                 'FROM public.economy_events event\1WHERE event."StreamKind" = 8 AND event."SettlementId" = p_settlement_id',
                                 'marketplace event sequence'),
                                ('append_admin_withdrawal_audit_event_v2', 1,
                                 'FROM public\.economy_admin_withdrawal_audit_events AS audit(\s+)WHERE audit\."TenantId" = p_tenant_id AND audit\."RunId" = p_run_id',
                                 'FROM public.economy_events AS audit\1WHERE audit."StreamKind" = 11 AND audit."TenantId" = p_tenant_id AND audit."RunId" = p_run_id',
                                 'audit event sequence'),
                                ('append_admin_withdrawal_audit_event_v2', 2,
                                 'INSERT INTO public\.economy_admin_withdrawal_audit_events \((\s*)"TenantId", "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash"\)(\s*VALUES \(\s*)p_tenant_id, p_run_id, next_sequence, pg_catalog\.btrim\(p_kind\), ',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "TenantId", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")\2gen_random_uuid(), 11, p_tenant_id, p_run_id, next_sequence, pg_catalog.btrim(p_kind), ',
                                 'audit event append'),
                                ('append_admin_withdrawal_audit_event_v1', 1,
                                 'FROM public\.economy_admin_withdrawal_audit_events AS audit_event(\s+)WHERE audit_event\."RunId" = p_run_id',
                                 'FROM public.economy_events AS audit_event\1WHERE audit_event."StreamKind" = 11 AND audit_event."RunId" = p_run_id',
                                 'audit event sequence'),
                                ('append_admin_withdrawal_audit_event_v1', 2,
                                 'INSERT INTO public\.economy_admin_withdrawal_audit_events \((\s*)"RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash"\)(\s*VALUES \(\s*)p_run_id, next_sequence, pg_catalog\.btrim\(p_kind\), ',
                                 'INSERT INTO public.economy_events (\1"Id", "StreamKind", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")\2gen_random_uuid(), 11, p_run_id, next_sequence, pg_catalog.btrim(p_kind), ',
                                 'audit event append')
                            ) AS r(proname, step, pattern, replacement, label)
                            WHERE r.proname = writer_name
                            ORDER BY r.step
                        LOOP
                            definition := pg_temp.rewrite_economy_function_text_v1(
                                definition, rewrite.pattern, rewrite.replacement, rewrite.label || ' in ' || writer_name);
                        END LOOP;

                        EXECUTE definition;
                    END LOOP;

                    -- post_provider_reversal_v1 is retired dead code:
                    -- HardenEconomyProviderReversalWriter revoked its EXECUTE, and
                    -- its body was already stale (it names columns that no longer
                    -- exist and calls a function that was removed). It is the one
                    -- body left naming a retired table; nothing else may be.
                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.proname <> 'post_provider_reversal_v1'
                          AND p.prosrc ~
                              'economy_(source_stamp_events|provider_dispute_events|wallet_debt_events|hold_events|risk_review_events|legacy_cutover_audit|compliance_hold_events|marketplace_events|ad_reward_session_timeline|admin_withdrawal_audit_events|bounty_expiration_events)')
                    THEN
                        RAISE EXCEPTION 'an economy_private function still addresses a retired event table';
                    END IF;
                END
                $events_writers$;
                """);

            // Five of the legacy tables rejected UPDATE and DELETE outright.
            // The ledger keeps that guarantee for exactly those five streams
            // (source stamp 1, funding hold 4, risk review 5, legacy cutover 7,
            // admin withdrawal audit 11); every other kind stays as mutable as
            // its table was.
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS deny_immutable_mutation ON public.economy_source_stamp_events;
                DROP TRIGGER IF EXISTS deny_immutable_mutation ON public.economy_hold_events;
                DROP TRIGGER IF EXISTS deny_immutable_mutation ON public.economy_risk_review_events;
                DROP TRIGGER IF EXISTS deny_admin_withdrawal_audit_event_mutation
                    ON public.economy_admin_withdrawal_audit_events;
                DROP TRIGGER IF EXISTS deny_economy_legacy_cutover_audit_mutation
                    ON public.economy_legacy_cutover_audit;
                CREATE TRIGGER deny_immutable_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_events
                    FOR EACH ROW
                    WHEN (OLD."StreamKind" IN (1, 4, 5, 7, 11))
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                """);

            // Least-privilege grants for the unified relation (same posture as
            // every other economy_% table: runtime read-only, procedure owner
            // writes, migration role full control).
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

            // The legacy tables are retired only after every row has been
            // carried over and every function has been retargeted.
            migrationBuilder.DropTable(
                name: "economy_ad_reward_session_timeline");

            migrationBuilder.DropTable(
                name: "economy_admin_withdrawal_audit_events");

            migrationBuilder.DropTable(
                name: "economy_bounty_expiration_events");

            migrationBuilder.DropTable(
                name: "economy_compliance_hold_events");

            migrationBuilder.DropTable(
                name: "economy_hold_events");

            migrationBuilder.DropTable(
                name: "economy_legacy_cutover_audit");

            migrationBuilder.DropTable(
                name: "economy_marketplace_events");

            migrationBuilder.DropTable(
                name: "economy_provider_dispute_events");

            migrationBuilder.DropTable(
                name: "economy_risk_review_events");

            migrationBuilder.DropTable(
                name: "economy_source_stamp_events");

            migrationBuilder.DropTable(
                name: "economy_wallet_debt_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy tables come back exactly as the migration chain built
            // them, not as the EF model lists them: columns in their original
            // physical order, so a rollback restores the pre-migration schema.
            migrationBuilder.CreateTable(
                name: "economy_ad_reward_session_timeline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimelineKind = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: true),
                    Percentage = table.Column<int>(type: "integer", nullable: true),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_session_timeline", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_reward_session_timeline_kind", "(\"TimelineKind\" = 1 AND \"State\" BETWEEN 1 AND 7 AND \"Percentage\" IS NULL) OR (\"TimelineKind\" = 2 AND \"Percentage\" BETWEEN 0 AND 100 AND \"State\" IS NULL)");
                    table.CheckConstraint("ck_economy_ad_reward_session_timeline_sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_session_timeline_economy_ad_reward_sessio~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_admin_withdrawal_audit_events",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_admin_withdrawal_audit_events", x => new { x.RunId, x.Sequence });
                    table.CheckConstraint("ck_economy_admin_withdrawal_audit_events_sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_admin_withdrawal_audit_events_economy_operations_Ru~",
                        column: x => x.RunId,
                        principalTable: "economy_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_bounty_expiration_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BountyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BountyVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_bounty_expiration_events", x => x.Id);
                    table.CheckConstraint("ck_economy_bounty_expiration_events_time", "\"RecordedAt\" >= \"ExpiresAt\"");
                    table.CheckConstraint("ck_economy_bounty_expiration_events_version", "\"BountyVersion\" > 1");
                    table.ForeignKey(
                        name: "FK_economy_bounty_expiration_events_economy_bounties_BountyId",
                        column: x => x.BountyId,
                        principalTable: "economy_bounties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_compliance_hold_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_compliance_hold_events", x => x.Id);
                    table.CheckConstraint("ck_economy_compliance_hold_events_sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_compliance_hold_events_economy_compliance_holds_Hol~",
                        column: x => x.HoldId,
                        principalTable: "economy_compliance_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_hold_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_hold_events", x => x.Id);
                    table.CheckConstraint("ck_economy_hold_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_hold_events_economy_holds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "economy_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_legacy_cutover_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_legacy_cutover_audit", x => x.Id);
                    table.CheckConstraint("ck_economy_legacy_cutover_audit_sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("ck_economy_legacy_cutover_audit_state", "\"State\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_economy_legacy_cutover_audit_economy_legacy_cutovers_Tenant~",
                        column: x => x.TenantId,
                        principalTable: "economy_legacy_cutovers",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_events", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_events_sequence", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_events_economy_marketplace_settlements_~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_provider_dispute_events",
                columns: table => new
                {
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CumulativeDisputedHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProviderDisputeReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderSequence = table.Column<long>(type: "bigint", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_provider_dispute_events", x => x.ProviderEventId);
                    table.CheckConstraint("ck_economy_provider_dispute_events_amount_positive", "\"CumulativeDisputedHardUnits\" > 0");
                    table.CheckConstraint("ck_economy_provider_dispute_events_sequence_positive", "\"ProviderSequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_provider_dispute_events_economy_provider_disputes_P~",
                        column: x => x.ProviderDisputeReference,
                        principalTable: "economy_provider_disputes",
                        principalColumn: "ProviderDisputeReference",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_provider_dispute_events_economy_source_stamps_Sourc~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_review_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskReviewCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHashes = table.Column<string>(type: "jsonb", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    DecisionCode = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_review_events", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_review_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_risk_review_events_economy_risk_review_cases_RiskRe~",
                        column: x => x.RiskReviewCaseId,
                        principalTable: "economy_risk_review_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_source_stamp_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_source_stamp_events", x => x.Id);
                    table.CheckConstraint("ck_economy_source_stamp_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_source_stamp_events_economy_source_stamps_SourceSta~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_wallet_debt_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    DeltaHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    OutstandingHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    // ReconcileEconomyWalletDebtPersistence installed these
                    // under the "_current" names; the rollback reproduces the
                    // live schema rather than the model's default names.
                    table.PrimaryKey("PK_economy_wallet_debt_events_current", x => x.Id);
                    table.CheckConstraint("ck_economy_wallet_debt_events_delta_nonzero", "\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0");
                    table.CheckConstraint("ck_economy_wallet_debt_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_wallet_debt_events_current_economy_source_stamps_Sou",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_wallet_debt_events_current_economy_wallet_debts_Wall",
                        column: x => x.WalletId,
                        principalTable: "economy_wallet_debts",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_session_timeline_SessionId",
                table: "economy_ad_reward_session_timeline",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_ad_reward_session_timeline_kind_session_sequence",
                table: "economy_ad_reward_session_timeline",
                columns: new[] { "TimelineKind", "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_admin_withdrawal_audit_events_hash",
                table: "economy_admin_withdrawal_audit_events",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounty_expiration_events_BountyId",
                table: "economy_bounty_expiration_events",
                column: "BountyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounty_expiration_events_RecordedAt_Id",
                table: "economy_bounty_expiration_events",
                columns: new[] { "RecordedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_compliance_hold_events_HoldId_Kind_ActorId",
                table: "economy_compliance_hold_events",
                columns: new[] { "HoldId", "Kind", "ActorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_compliance_hold_events_HoldId_Sequence",
                table: "economy_compliance_hold_events",
                columns: new[] { "HoldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_hold_events_hold_sequence",
                table: "economy_hold_events",
                columns: new[] { "HoldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_cutover_audit_TenantId_Sequence",
                table: "economy_legacy_cutover_audit",
                columns: new[] { "TenantId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_events_SettlementId_Sequence",
                table: "economy_marketplace_events",
                columns: new[] { "SettlementId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_provider_dispute_events_SourceStampId",
                table: "economy_provider_dispute_events",
                column: "SourceStampId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_dispute_events_dispute_sequence",
                table: "economy_provider_dispute_events",
                columns: new[] { "ProviderDisputeReference", "ProviderSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_review_events_case_sequence",
                table: "economy_risk_review_events",
                columns: new[] { "RiskReviewCaseId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_source_stamp_events_source_sequence",
                table: "economy_source_stamp_events",
                columns: new[] { "SourceStampId", "Sequence" },
                unique: true);

            // IX_economy_wallet_debt_events_SourceStampId is deliberately not
            // recreated: ReconcileEconomyWalletDebtPersistence built that table
            // by hand without it, so no database ever had the index the model
            // lists for the row.
            migrationBuilder.CreateIndex(
                name: "ux_economy_wallet_debt_events_wallet_sequence",
                table: "economy_wallet_debt_events",
                columns: new[] { "WalletId", "Sequence" },
                unique: true);

            // Mirror of Up, in reverse order: the legacy tables exist again
            // (created above), so the ledger's rows go back to their original
            // tables, the functions return to their original targets, the
            // per-table triggers come back, and only then is the ledger dropped.
            // Rows written through the ledger after the upgrade are carried
            // back as well: every kind stays representable in its legacy shape.

            migrationBuilder.Sql("""
                INSERT INTO public.economy_source_stamp_events (
                    "Id", "SourceStampId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                SELECT "Id", "SourceStampId", "Sequence", "Kind", "EvidenceHash", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 1;

                INSERT INTO public.economy_provider_dispute_events (
                    "ProviderEventId", "CumulativeDisputedHardUnits", "OccurredAt", "ProviderDisputeReference",
                    "ProviderSequence", "RequestHash", "SourceStampId", "Status")
                SELECT "ProviderEventId", "CumulativeDisputedHardUnits", "OccurredAt", "ProviderDisputeReference",
                       "Sequence", "RequestHash", "SourceStampId", "Status"
                FROM public.economy_events
                WHERE "StreamKind" = 2;

                INSERT INTO public.economy_wallet_debt_events (
                    "Id", "DeltaHardUnits", "OccurredAt", "OutstandingHardUnits", "Sequence", "SourceStampId", "WalletId")
                SELECT "Id", "DeltaHardUnits", "OccurredAt", "OutstandingHardUnits", "Sequence", "SourceStampId",
                       "DebtWalletId"
                FROM public.economy_events
                WHERE "StreamKind" = 3;

                INSERT INTO public.economy_hold_events (
                    "Id", "ActorId", "EvidenceHash", "HoldId", "Kind", "OccurredAt", "Sequence")
                SELECT "Id", "ActorId", "EvidenceHash", "HoldId", "Kind", "OccurredAt", "Sequence"
                FROM public.economy_events
                WHERE "StreamKind" = 4;

                INSERT INTO public.economy_risk_review_events (
                    "Id", "ActorId", "DecisionCode", "EvidenceHashes", "Kind", "OccurredAt", "Resolution",
                    "RiskReviewCaseId", "Sequence")
                SELECT "Id", "ActorId", "DecisionCode", "EvidenceHashes", "Kind", "OccurredAt", "Resolution",
                       "RiskReviewCaseId", "Sequence"
                FROM public.economy_events
                WHERE "StreamKind" = 5;

                INSERT INTO public.economy_compliance_hold_events (
                    "Id", "ActorId", "EvidenceHash", "HoldId", "Kind", "OccurredAt", "Sequence")
                SELECT "Id", "ActorId", "EvidenceHash", "ComplianceHoldId", "KindText", "OccurredAt", "Sequence"
                FROM public.economy_events
                WHERE "StreamKind" = 6;

                INSERT INTO public.economy_legacy_cutover_audit (
                    "Id", "ActorId", "BatchId", "EvidenceHash", "Reason", "ReauthenticationHash", "RecordedAt",
                    "Sequence", "State", "TenantId")
                SELECT "Id", "ActorId", "BatchId", "EvidenceHash", "Reason", "ReauthenticationHash", "OccurredAt",
                       "Sequence", "Kind", "LegacyCutoverTenantId"
                FROM public.economy_events
                WHERE "StreamKind" = 7;

                INSERT INTO public.economy_marketplace_events (
                    "Id", "EventKind", "EvidenceHash", "OccurredAt", "Sequence", "SettlementId", "TenantId")
                SELECT "Id", "KindText", "EvidenceHash", "OccurredAt", "Sequence", "SettlementId", "TenantId"
                FROM public.economy_events
                WHERE "StreamKind" = 8;

                INSERT INTO public.economy_ad_reward_session_timeline (
                    "Id", "EvidenceHash", "OccurredAt", "Sequence", "SessionId", "State", "TimelineKind")
                SELECT "Id", "EvidenceHash", "OccurredAt", "Sequence", "SessionId", "Kind", 1
                FROM public.economy_events
                WHERE "StreamKind" = 9;

                INSERT INTO public.economy_ad_reward_session_timeline (
                    "Id", "EvidenceHash", "OccurredAt", "Percentage", "Sequence", "SessionId", "TimelineKind")
                SELECT "Id", "EvidenceHash", "OccurredAt", "Percentage", "Sequence", "SessionId", 2
                FROM public.economy_events
                WHERE "StreamKind" = 10;

                INSERT INTO public.economy_admin_withdrawal_audit_events (
                    "ActorId", "Evidence", "Hash", "Kind", "OccurredAt", "PreviousHash", "RunId", "Sequence", "TenantId")
                SELECT "ActorId", "Evidence", "Hash", "KindText", "OccurredAt", "PreviousHash", "RunId", "Sequence",
                       "TenantId"
                FROM public.economy_events
                WHERE "StreamKind" = 11;

                INSERT INTO public.economy_bounty_expiration_events (
                    "Id", "BountyId", "BountyVersion", "ExpiresAt", "RecordedAt")
                SELECT "Id", "BountyId", "BountyVersion", "ExpiresAt", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 12;
                """);

            // The audit reader returns to the legacy table with its original
            // definition, owner and grants.
            migrationBuilder.Sql("""
                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_audit_events_v1(uuid);
                CREATE FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(p_run_id uuid)
                    RETURNS SETOF economy_admin_withdrawal_audit_events
                    LANGUAGE sql STABLE SECURITY DEFINER
                    SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_admin_withdrawal_audit_events
                    WHERE "RunId" = p_run_id ORDER BY "Sequence"
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                    TO gameguild_economy_writer;
                """);

            // Restore the SECURITY DEFINER writers to their legacy targets: the
            // exact inverse of every Up rewrite, which puts the original
            // function text back byte for byte.
            migrationBuilder.Sql("""
                CREATE FUNCTION pg_temp.rewrite_economy_function_text_v1(
                    p_definition text, p_pattern text, p_replacement text, p_label text)
                RETURNS text
                LANGUAGE plpgsql
                AS $rewrite$
                DECLARE
                    match_count integer;
                BEGIN
                    match_count := (SELECT count(*) FROM regexp_matches(p_definition, p_pattern, 'g'));
                    IF match_count <> 1 THEN
                        RAISE EXCEPTION 'economy event writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $events_writers$
                DECLARE
                    writer_name text;
                    definition text;
                    rewrite record;
                BEGIN
                    FOR writer_name IN
                        SELECT unnest(ARRAY[
                            'observe_hard_coin_top_up_v1', 'confirm_observed_hard_coin_top_up_v1',
                            'post_ad_reward_issuance_v1', 'post_legacy_balance_backfill_v1',
                            'complete_bounty_claim_v1', 'post_provider_reversal_v2',
                            'post_marketplace_settlement_v1', 'post_marketplace_refund_v1',
                            'append_admin_withdrawal_audit_event_v2', 'append_admin_withdrawal_audit_event_v1'])
                    LOOP
                        SELECT pg_get_functiondef(p.oid) INTO definition
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private' AND p.prokind = 'f' AND p.proname = writer_name;
                        IF definition IS NULL THEN
                            RAISE EXCEPTION 'economy event writer % is absent', writer_name;
                        END IF;

                        FOR rewrite IN
                            SELECT * FROM (VALUES
                                ('observe_hard_coin_top_up_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('confirm_observed_hard_coin_top_up_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('post_ad_reward_issuance_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('post_legacy_balance_backfill_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('complete_bounty_claim_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('post_provider_reversal_v2', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "SourceStampId", "Sequence", "Kind"(, "EvidenceHash", "OccurredAt"\)\s*VALUES \()(gen_random_uuid\(\),) 1,',
                                 'INSERT INTO public.economy_source_stamp_events (\1"Id", "SourceStampId", "Sequence", "State"\2\3',
                                 'source stamp event append'),
                                ('post_provider_reversal_v2', 2,
                                 'FROM public\.economy_events WHERE "StreamKind" = 1 AND "SourceStampId" = p_root_source_stamp_id',
                                 'FROM public.economy_source_stamp_events WHERE "SourceStampId" = p_root_source_stamp_id',
                                 'source stamp event sequence'),
                                ('post_provider_reversal_v2', 3,
                                 'FROM public\.economy_events(\s+)WHERE "StreamKind" = 3 AND "DebtWalletId" = claim\."WalletId"',
                                 'FROM public.economy_wallet_debt_events\1WHERE "WalletId" = claim."WalletId"',
                                 'wallet debt event sequence'),
                                ('post_provider_reversal_v2', 4,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "DebtWalletId", "SourceStampId", "Sequence", "DeltaHardUnits", "OutstandingHardUnits", "OccurredAt"\)(\s*VALUES \(\s*)(gen_random_uuid\(\),) 3, claim\."WalletId", ',
                                 'INSERT INTO public.economy_wallet_debt_events (\1"Id", "WalletId", "SourceStampId", "Sequence", "DeltaHardUnits", "OutstandingHardUnits", "OccurredAt")\2\3 claim."WalletId", ',
                                 'wallet debt event append'),
                                ('post_marketplace_settlement_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"\)(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(\s*[^,]+, ''[^'']+''\),) 4,',
                                 'INSERT INTO public.economy_hold_events (\1"Id", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt")\2VALUES (\3',
                                 'hold event append'),
                                ('post_marketplace_settlement_v1', 2,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "TenantId", "SettlementId", "Sequence", "KindText"(, "EvidenceHash", "OccurredAt"\))(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(p_[a-z_]+, ''[^'']+''\),) 8,',
                                 'INSERT INTO public.economy_marketplace_events (\1"Id", "TenantId", "SettlementId", "Sequence", "EventKind"\2\3VALUES (\4',
                                 'marketplace event append'),
                                ('post_marketplace_refund_v1', 1,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt"\)(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(\s*[^,]+, ''[^'']+''\),) 4,',
                                 'INSERT INTO public.economy_hold_events (\1"Id", "HoldId", "Sequence", "Kind", "ActorId", "EvidenceHash", "OccurredAt")\2VALUES (\3',
                                 'hold event append'),
                                ('post_marketplace_refund_v1', 2,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "TenantId", "SettlementId", "Sequence", "KindText"(, "EvidenceHash", "OccurredAt"\))(\s*)VALUES \((economy_private\.derive_economy_uuid_v1\(p_[a-z_]+, ''[^'']+''\),) 8,',
                                 'INSERT INTO public.economy_marketplace_events (\1"Id", "TenantId", "SettlementId", "Sequence", "EventKind"\2\3VALUES (\4',
                                 'marketplace event append'),
                                ('post_marketplace_refund_v1', 3,
                                 'FROM public\.economy_events event(\s+)WHERE event\."StreamKind" = 8 AND event\."SettlementId" = p_settlement_id',
                                 'FROM public.economy_marketplace_events event\1WHERE event."SettlementId" = p_settlement_id',
                                 'marketplace event sequence'),
                                ('append_admin_withdrawal_audit_event_v2', 1,
                                 'FROM public\.economy_events AS audit(\s+)WHERE audit\."StreamKind" = 11 AND audit\."TenantId" = p_tenant_id AND audit\."RunId" = p_run_id',
                                 'FROM public.economy_admin_withdrawal_audit_events AS audit\1WHERE audit."TenantId" = p_tenant_id AND audit."RunId" = p_run_id',
                                 'audit event sequence'),
                                ('append_admin_withdrawal_audit_event_v2', 2,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "TenantId", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash"\)(\s*VALUES \(\s*)gen_random_uuid\(\), 11, p_tenant_id, p_run_id, next_sequence, pg_catalog\.btrim\(p_kind\), ',
                                 'INSERT INTO public.economy_admin_withdrawal_audit_events (\1"TenantId", "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")\2p_tenant_id, p_run_id, next_sequence, pg_catalog.btrim(p_kind), ',
                                 'audit event append'),
                                ('append_admin_withdrawal_audit_event_v1', 1,
                                 'FROM public\.economy_events AS audit_event(\s+)WHERE audit_event\."StreamKind" = 11 AND audit_event\."RunId" = p_run_id',
                                 'FROM public.economy_admin_withdrawal_audit_events AS audit_event\1WHERE audit_event."RunId" = p_run_id',
                                 'audit event sequence'),
                                ('append_admin_withdrawal_audit_event_v1', 2,
                                 'INSERT INTO public\.economy_events \((\s*)"Id", "StreamKind", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash"\)(\s*VALUES \(\s*)gen_random_uuid\(\), 11, p_run_id, next_sequence, pg_catalog\.btrim\(p_kind\), ',
                                 'INSERT INTO public.economy_admin_withdrawal_audit_events (\1"RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")\2p_run_id, next_sequence, pg_catalog.btrim(p_kind), ',
                                 'audit event append')
                            ) AS r(proname, step, pattern, replacement, label)
                            WHERE r.proname = writer_name
                            ORDER BY r.step
                        LOOP
                            definition := pg_temp.rewrite_economy_function_text_v1(
                                definition, rewrite.pattern, rewrite.replacement, rewrite.label || ' in ' || writer_name);
                        END LOOP;

                        EXECUTE definition;
                    END LOOP;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.prosrc ~ 'public\.economy_events')
                    THEN
                        RAISE EXCEPTION 'an economy_private function still addresses the unified event table';
                    END IF;
                END
                $events_writers$;
                """);

            // The per-table immutability triggers return with their original
            // names; the kind-scoped trigger leaves with the ledger.
            migrationBuilder.Sql("""
                CREATE TRIGGER deny_immutable_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_source_stamp_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_immutable_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_hold_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_immutable_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_risk_review_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_admin_withdrawal_audit_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_admin_withdrawal_audit_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_economy_legacy_cutover_audit_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_legacy_cutover_audit
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                """);

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

            migrationBuilder.DropTable(
                name: "economy_events");
        }
    }
}
