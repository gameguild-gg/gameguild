using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier3RefundShortfalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events");

            migrationBuilder.AddColumn<long>(
                name: "DebtAmountUnits",
                table: "economy_events",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DebtCurrency",
                table: "economy_events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundId",
                table: "economy_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleWalletId",
                table: "economy_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_refund_shortfall_refund",
                table: "economy_events",
                columns: new[] { "RefundId", "OccurredAt" },
                filter: "\"StreamKind\" = 16");

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_refund_shortfall_wallet",
                table: "economy_events",
                columns: new[] { "TenantId", "ResponsibleWalletId", "OccurredAt" },
                filter: "\"StreamKind\" = 16");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_refund_shortfall_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 16 AND \"RefundId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"TenantId\" IS NOT NULL AND \"ResponsibleWalletId\" IS NOT NULL AND \"DebtCurrency\" IS NOT NULL AND \"DebtAmountUnits\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Sequence\" = 0 AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ETag\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL AND \"RiskDecisionId\" IS NULL AND \"OperationFingerprint\" IS NULL AND \"Payload\" IS NULL) OR (\"StreamKind\" <> 16 AND \"RefundId\" IS NULL AND \"ResponsibleWalletId\" IS NULL AND \"DebtCurrency\" IS NULL AND \"DebtAmountUnits\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" IN (13, 14, 15, 16) OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 14 AND \"ExternalAnchorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"KeyId\" IS NOT NULL AND \"ObjectVersion\" IS NOT NULL AND \"ETag\" IS NOT NULL AND \"SignatureValid\" IS NOT NULL AND \"ObjectMatches\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 15 AND \"RiskDecisionId\" IS NOT NULL) OR (\"StreamKind\" = 16 AND \"RefundId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events",
                sql: "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND (\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND (\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND (\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND (\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND (\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\")) AND (\"StreamKind\" <> 13 OR \"SourceJournalSequence\" >= 0) AND (\"StreamKind\" <> 16 OR \"DebtAmountUnits\" > 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_economy_events_economy_marketplace_refunds_RefundId",
                table: "economy_events",
                column: "RefundId",
                principalTable: "economy_marketplace_refunds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // The marketplace refund function appends the refund's shortfalls
            // to the ledger as kind 16. Each rewrite must match the function
            // text exactly once, so a drifted body aborts the migration
            // instead of keeping a statement that addresses a table this
            // migration drops. Only the target with the head of its column
            // list, the rest of that list and the head of the select list are
            // rewritten, so the body keeps its layout and Down restores it
            // exactly. The helper is replaced rather than created: when this
            // migration is applied in the same session as an earlier one that
            // defined it, that copy of the session-local helper already exists.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION pg_temp.rewrite_economy_function_text_v1(
                    p_definition text, p_pattern text, p_replacement text, p_label text)
                RETURNS text
                LANGUAGE plpgsql
                AS $rewrite$
                DECLARE
                    match_count integer;
                BEGIN
                    match_count := (SELECT count(*) FROM regexp_matches(p_definition, p_pattern, 'g'));
                    IF match_count <> 1 THEN
                        RAISE EXCEPTION 'economy refund shortfall writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $refund_shortfall_writer$
                DECLARE
                    definition text;
                    signature_count integer;
                    rewrite record;
                BEGIN
                    SELECT pg_get_functiondef(p.oid), count(*) OVER ()
                    INTO definition, signature_count
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                      AND p.proname = 'post_marketplace_refund_v1';
                    IF signature_count IS DISTINCT FROM 1 THEN
                        RAISE EXCEPTION 'economy refund shortfall writer matches % signatures, expected exactly 1',
                            coalesce(signature_count, 0);
                    END IF;

                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            (1,
                             'INSERT INTO public\.economy_marketplace_refund_debts \(\s+"Id", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",',
                             E'INSERT INTO public.economy_events (\n        "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",',
                             'refund shortfall append target and columns'),
                            (2,
                             '"Currency", "AmountUnits", "EvidenceHash", "RecordedAt"\)',
                             '"DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")',
                             'refund shortfall append amount columns'),
                            (3,
                             'SELECT \(row->>''id''\)::uuid, p_tenant_id, p_refund_id, p_settlement_id,',
                             'SELECT (row->>''id'')::uuid, 16, 0, p_tenant_id, p_refund_id, p_settlement_id,',
                             'refund shortfall append values')
                        ) AS r(step, pattern, replacement, label)
                        ORDER BY r.step
                    LOOP
                        definition := pg_temp.rewrite_economy_function_text_v1(
                            definition, rewrite.pattern, rewrite.replacement, rewrite.label);
                    END LOOP;

                    EXECUTE definition;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.prosrc ~ 'economy_marketplace_refund_debts')
                    THEN
                        RAISE EXCEPTION 'an economy_private function still addresses the retired refund shortfall table';
                    END IF;
                END
                $refund_shortfall_writer$;
                """);

            // Every shortfall is carried over: Id, tenant, refund, settlement,
            // responsible wallet, currency, amount and evidence hash are kept,
            // RecordedAt is the row's OccurredAt, and Sequence — a column the
            // table never had — is 0. The writer was retargeted first, so
            // nothing can still append to the table while it is carried.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                    "DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")
                SELECT "Id", 16, 0, "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                       "Currency", "AmountUnits", "EvidenceHash", "RecordedAt"
                FROM public.economy_marketplace_refund_debts;
                """);

            // The legacy table is retired only after every row has been carried
            // over and the writer has been retargeted.
            migrationBuilder.DropTable(
                name: "economy_marketplace_refund_debts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirror of Up: the legacy table exists again (created below with
            // the column order the migration chain gave it), the writer returns
            // to its former shape, the ledger's shortfall rows go back to it,
            // and only then does the ledger give up the stream's columns and
            // kind branch.
            migrationBuilder.CreateTable(
                name: "economy_marketplace_refund_debts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsibleWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_refund_debts", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_refund_debts_amount", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_debts_economy_marketplace_refund~",
                        column: x => x.RefundId,
                        principalTable: "economy_marketplace_refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_debts_economy_marketplace_settle~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refund_debts_RefundId",
                table: "economy_marketplace_refund_debts",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refund_debts_SettlementId",
                table: "economy_marketplace_refund_debts",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refund_debts_TenantId_ResponsibleWallet~",
                table: "economy_marketplace_refund_debts",
                columns: new[] { "TenantId", "ResponsibleWalletId", "RecordedAt" });

            // Least-privilege grants, as every economy_% table has them.
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

            // Restore the writer to its legacy target: the exact inverse of
            // each Up rewrite. It is restored before the rows go back, so
            // nothing can still append to the ledger while the shortfalls are
            // carried. The target is matched together with the head of the
            // column list, because the function inserts into the ledger twice
            // more on its own. CREATE OR REPLACE for the same reason as in Up.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION pg_temp.rewrite_economy_function_text_v1(
                    p_definition text, p_pattern text, p_replacement text, p_label text)
                RETURNS text
                LANGUAGE plpgsql
                AS $rewrite$
                DECLARE
                    match_count integer;
                BEGIN
                    match_count := (SELECT count(*) FROM regexp_matches(p_definition, p_pattern, 'g'));
                    IF match_count <> 1 THEN
                        RAISE EXCEPTION 'economy refund shortfall writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $refund_shortfall_writer$
                DECLARE
                    definition text;
                    signature_count integer;
                    rewrite record;
                BEGIN
                    SELECT pg_get_functiondef(p.oid), count(*) OVER ()
                    INTO definition, signature_count
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                      AND p.proname = 'post_marketplace_refund_v1';
                    IF signature_count IS DISTINCT FROM 1 THEN
                        RAISE EXCEPTION 'economy refund shortfall writer matches % signatures, expected exactly 1',
                            coalesce(signature_count, 0);
                    END IF;

                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            (1,
                             'INSERT INTO public\.economy_events \(\s+"Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",',
                             E'INSERT INTO public.economy_marketplace_refund_debts (\n        "Id", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",',
                             'refund shortfall append target and columns'),
                            (2,
                             '"DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt"\)',
                             '"Currency", "AmountUnits", "EvidenceHash", "RecordedAt")',
                             'refund shortfall append amount columns'),
                            (3,
                             'SELECT \(row->>''id''\)::uuid, 16, 0, p_tenant_id, p_refund_id, p_settlement_id,',
                             'SELECT (row->>''id'')::uuid, p_tenant_id, p_refund_id, p_settlement_id,',
                             'refund shortfall append values')
                        ) AS r(step, pattern, replacement, label)
                        ORDER BY r.step
                    LOOP
                        definition := pg_temp.rewrite_economy_function_text_v1(
                            definition, rewrite.pattern, rewrite.replacement, rewrite.label);
                    END LOOP;

                    EXECUTE definition;
                END
                $refund_shortfall_writer$;
                """);

            // Every shortfall goes back, including the ones the writer
            // appended to the ledger after the upgrade. The stream never was
            // immutable, so the rows simply leave the ledger.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_marketplace_refund_debts (
                    "Id", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId", "Currency", "AmountUnits",
                    "EvidenceHash", "RecordedAt")
                SELECT "Id", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId", "DebtCurrency",
                       "DebtAmountUnits", "EvidenceHash", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 16;

                DELETE FROM public.economy_events WHERE "StreamKind" = 16;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_economy_events_economy_marketplace_refunds_RefundId",
                table: "economy_events");

            migrationBuilder.DropIndex(
                name: "ix_economy_events_refund_shortfall_refund",
                table: "economy_events");

            migrationBuilder.DropIndex(
                name: "ix_economy_events_refund_shortfall_wallet",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_refund_shortfall_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "DebtAmountUnits",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "DebtCurrency",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "RefundId",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ResponsibleWalletId",
                table: "economy_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" IN (13, 14, 15) OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 14 AND \"ExternalAnchorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"KeyId\" IS NOT NULL AND \"ObjectVersion\" IS NOT NULL AND \"ETag\" IS NOT NULL AND \"SignatureValid\" IS NOT NULL AND \"ObjectMatches\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 15 AND \"RiskDecisionId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events",
                sql: "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND (\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND (\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND (\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND (\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND (\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\")) AND (\"StreamKind\" <> 13 OR \"SourceJournalSequence\" >= 0)");
        }
    }
}
