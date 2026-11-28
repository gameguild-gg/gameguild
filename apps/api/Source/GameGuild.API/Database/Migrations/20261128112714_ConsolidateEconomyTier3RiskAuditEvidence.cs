using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier3RiskAuditEvidence : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "OperationFingerprint",
                table: "economy_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Payload",
                table: "economy_events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RiskDecisionId",
                table: "economy_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_events_risk_evidence",
                table: "economy_events",
                columns: new[] { "RiskDecisionId", "EvidenceHash" },
                unique: true,
                filter: "\"StreamKind\" = 15");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_risk_evidence_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 15 AND \"RiskDecisionId\" IS NOT NULL AND \"OperationFingerprint\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Payload\" IS NOT NULL AND \"Sequence\" = 0 AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ETag\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" <> 15 AND \"RiskDecisionId\" IS NULL AND \"OperationFingerprint\" IS NULL AND \"Payload\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" IN (13, 14, 15) OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 14 AND \"ExternalAnchorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"KeyId\" IS NOT NULL AND \"ObjectVersion\" IS NOT NULL AND \"ETag\" IS NOT NULL AND \"SignatureValid\" IS NOT NULL AND \"ObjectMatches\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 15 AND \"RiskDecisionId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_economy_events_economy_risk_decisions_RiskDecisionId",
                table: "economy_events",
                column: "RiskDecisionId",
                principalTable: "economy_risk_decisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // The five functions that append risk audit evidence now append to
            // the ledger as kind 15. Each rewrite must match the function text
            // exactly once, so a drifted body aborts the migration instead of
            // keeping a statement that addresses a table this migration drops.
            // Only the target, the column list and the head of the value list
            // are rewritten: each body keeps its own layout, and Down restores
            // it exactly. The helper is replaced rather than created: when
            // this migration is applied in the same session as an earlier one
            // that defined it (a fresh database, or a deployment that skipped
            // it), that copy of the session-local helper already exists.
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
                        RAISE EXCEPTION 'economy risk evidence writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $risk_evidence_writers$
                DECLARE
                    definition text;
                    signature_count integer;
                    rewrite record;
                BEGIN
                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 1,
                             'INSERT INTO public\.economy_risk_audit_evidence \(',
                             'INSERT INTO public.economy_events (',
                             'self-service risk decision evidence append target'),
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 2,
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt"\)',
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt")',
                             'self-service risk decision evidence append columns'),
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 3,
                             'gen_random_uuid\(\), selected_decision_id, ''external-risk-evidence'',',
                             'gen_random_uuid(), 15, 0, selected_decision_id, ''external-risk-evidence'',',
                             'self-service risk decision evidence append values'),
                            ('post_registered_aggregate_component_v1', 1,
                             'INSERT INTO public\.economy_risk_audit_evidence \(',
                             'INSERT INTO public.economy_events (',
                             'aggregate component posting evidence append target'),
                            ('post_registered_aggregate_component_v1', 2,
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt"\)',
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt")',
                             'aggregate component posting evidence append columns'),
                            ('post_registered_aggregate_component_v1', 3,
                             'gen_random_uuid\(\), p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'aggregate component posting evidence append values'),
                            ('post_registered_posting_before_risk_aggregation_v1', 1,
                             'INSERT INTO public\.economy_risk_audit_evidence \(',
                             'INSERT INTO public.economy_events (',
                             'posting before risk aggregation evidence append target'),
                            ('post_registered_posting_before_risk_aggregation_v1', 2,
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt"\)',
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt")',
                             'posting before risk aggregation evidence append columns'),
                            ('post_registered_posting_before_risk_aggregation_v1', 3,
                             'gen_random_uuid\(\), p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'posting before risk aggregation evidence append values'),
                            ('post_registered_posting_v1', 1,
                             'INSERT INTO public\.economy_risk_audit_evidence \(',
                             'INSERT INTO public.economy_events (',
                             'posting evidence append target'),
                            ('post_registered_posting_v1', 2,
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt"\)',
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt")',
                             'posting evidence append columns'),
                            ('post_registered_posting_v1', 3,
                             'gen_random_uuid\(\), p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'posting evidence append values'),
                            ('post_registered_posting_v2', 1,
                             'INSERT INTO public\.economy_risk_audit_evidence \(',
                             'INSERT INTO public.economy_events (',
                             'second-generation posting evidence append target'),
                            ('post_registered_posting_v2', 2,
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt"\)',
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt")',
                             'second-generation posting evidence append columns'),
                            ('post_registered_posting_v2', 3,
                             'gen_random_uuid\(\), p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'second-generation posting evidence append values')
                        ) AS r(proname, step, pattern, replacement, label)
                        ORDER BY r.proname, r.step
                    LOOP
                        SELECT pg_get_functiondef(p.oid), count(*) OVER ()
                        INTO definition, signature_count
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                          AND p.proname = rewrite.proname;
                        IF signature_count IS DISTINCT FROM 1 THEN
                            RAISE EXCEPTION 'economy risk evidence writer % matches % signatures, expected exactly 1',
                                rewrite.proname, coalesce(signature_count, 0);
                        END IF;

                        definition := pg_temp.rewrite_economy_function_text_v1(
                            definition, rewrite.pattern, rewrite.replacement, rewrite.label);

                        EXECUTE definition;
                    END LOOP;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.prosrc ~ 'economy_risk_audit_evidence')
                    THEN
                        RAISE EXCEPTION 'an economy_private function still addresses the retired risk audit evidence table';
                    END IF;
                END
                $risk_evidence_writers$;
                """);

            // Evidence was immutable in its own table; as kind 15 it joins the
            // kind-scoped trigger. Then every row is carried over: Id and the
            // decision, kind, fingerprint, hash and payload are kept,
            // RecordedAt is the row's OccurredAt, and Sequence — a column the
            // table never had — is 0. The writers were retargeted first, so
            // nothing can still append to the table while it is carried.
            migrationBuilder.Sql("""
                DROP TRIGGER deny_immutable_event_mutation ON public.economy_events;
                CREATE TRIGGER deny_immutable_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_events
                    FOR EACH ROW
                    WHEN (OLD."StreamKind" IN (1, 4, 5, 7, 11, 13, 15))
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();

                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint",
                    "EvidenceHash", "Payload", "OccurredAt")
                SELECT "Id", 15, 0, "RiskDecisionId", "EventKind", "OperationFingerprint",
                       "EvidenceHash", "Payload", "RecordedAt"
                FROM public.economy_risk_audit_evidence;
                """);

            // The legacy table is retired only after every row has been carried
            // over and the writers have been retargeted.
            migrationBuilder.DropTable(
                name: "economy_risk_audit_evidence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirror of Up: the legacy table exists again (created below with
            // the column order the migration chain gave it), the writers and
            // both triggers return to their former shape, the ledger's
            // evidence rows go back to it, and only then does the ledger give
            // up the stream's columns and kind branch.
            migrationBuilder.CreateTable(
                name: "economy_risk_audit_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_audit_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_risk_audit_evidence_economy_risk_decisions_RiskDeci~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_audit_evidence_decision_hash",
                table: "economy_risk_audit_evidence",
                columns: new[] { "RiskDecisionId", "EvidenceHash" },
                unique: true);

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

            // Restore the five writers to their legacy target: the exact
            // inverse of each Up rewrite. They are restored before the rows
            // go back, so nothing can still append to the ledger while its
            // evidence rows are carried. CREATE OR REPLACE for the same reason
            // as in Up.
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
                        RAISE EXCEPTION 'economy risk evidence writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $risk_evidence_writers$
                DECLARE
                    definition text;
                    signature_count integer;
                    rewrite record;
                BEGIN
                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_risk_audit_evidence (',
                             'self-service risk decision evidence append target'),
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 2,
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)',
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")',
                             'self-service risk decision evidence append columns'),
                            ('issue_self_service_hard_to_soft_risk_decision_v1', 3,
                             'gen_random_uuid\(\), 15, 0, selected_decision_id, ''external-risk-evidence'',',
                             'gen_random_uuid(), selected_decision_id, ''external-risk-evidence'',',
                             'self-service risk decision evidence append values'),
                            ('post_registered_aggregate_component_v1', 1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_risk_audit_evidence (',
                             'aggregate component posting evidence append target'),
                            ('post_registered_aggregate_component_v1', 2,
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)',
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")',
                             'aggregate component posting evidence append columns'),
                            ('post_registered_aggregate_component_v1', 3,
                             'gen_random_uuid\(\), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), p_risk_decision_id, ''posting-authorized'',',
                             'aggregate component posting evidence append values'),
                            ('post_registered_posting_before_risk_aggregation_v1', 1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_risk_audit_evidence (',
                             'posting before risk aggregation evidence append target'),
                            ('post_registered_posting_before_risk_aggregation_v1', 2,
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)',
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")',
                             'posting before risk aggregation evidence append columns'),
                            ('post_registered_posting_before_risk_aggregation_v1', 3,
                             'gen_random_uuid\(\), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), p_risk_decision_id, ''posting-authorized'',',
                             'posting before risk aggregation evidence append values'),
                            ('post_registered_posting_v1', 1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_risk_audit_evidence (',
                             'posting evidence append target'),
                            ('post_registered_posting_v1', 2,
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)',
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")',
                             'posting evidence append columns'),
                            ('post_registered_posting_v1', 3,
                             'gen_random_uuid\(\), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), p_risk_decision_id, ''posting-authorized'',',
                             'posting evidence append values'),
                            ('post_registered_posting_v2', 1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_risk_audit_evidence (',
                             'second-generation posting evidence append target'),
                            ('post_registered_posting_v2', 2,
                             '"Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)',
                             '"Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")',
                             'second-generation posting evidence append columns'),
                            ('post_registered_posting_v2', 3,
                             'gen_random_uuid\(\), 15, 0, p_risk_decision_id, ''posting-authorized'',',
                             'gen_random_uuid(), p_risk_decision_id, ''posting-authorized'',',
                             'second-generation posting evidence append values')
                        ) AS r(proname, step, pattern, replacement, label)
                        ORDER BY r.proname, r.step
                    LOOP
                        SELECT pg_get_functiondef(p.oid), count(*) OVER ()
                        INTO definition, signature_count
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                          AND p.proname = rewrite.proname;
                        IF signature_count IS DISTINCT FROM 1 THEN
                            RAISE EXCEPTION 'economy risk evidence writer % matches % signatures, expected exactly 1',
                                rewrite.proname, coalesce(signature_count, 0);
                        END IF;

                        definition := pg_temp.rewrite_economy_function_text_v1(
                            definition, rewrite.pattern, rewrite.replacement, rewrite.label);

                        EXECUTE definition;
                    END LOOP;
                END
                $risk_evidence_writers$;
                """);

            // Every evidence row goes back, including the ones the writers
            // appended to the ledger after the upgrade.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_risk_audit_evidence (
                    "Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")
                SELECT "Id", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 15;
                """);

            // Both triggers return to their former shape. The evidence rows
            // live in the legacy table again, so the ledger gives them up: the
            // restored trigger no longer covers kind 15, and nothing is lost
            // because every row was carried back above.
            migrationBuilder.Sql("""
                DROP TRIGGER deny_immutable_event_mutation ON public.economy_events;
                CREATE TRIGGER deny_immutable_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_events
                    FOR EACH ROW
                    WHEN (OLD."StreamKind" IN (1, 4, 5, 7, 11, 13))
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_immutable_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_risk_audit_evidence
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                DELETE FROM public.economy_events WHERE "StreamKind" = 15;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_economy_events_economy_risk_decisions_RiskDecisionId",
                table: "economy_events");

            migrationBuilder.DropIndex(
                name: "ux_economy_events_risk_evidence",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_risk_evidence_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "OperationFingerprint",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "Payload",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "RiskDecisionId",
                table: "economy_events");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" IN (13, 14) OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 14 AND \"ExternalAnchorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"KeyId\" IS NOT NULL AND \"ObjectVersion\" IS NOT NULL AND \"ETag\" IS NOT NULL AND \"SignatureValid\" IS NOT NULL AND \"ObjectMatches\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL)");
        }
    }
}
