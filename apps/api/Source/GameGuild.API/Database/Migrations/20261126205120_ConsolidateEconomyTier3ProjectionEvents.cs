using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier3ProjectionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // #1092 Tier 3 — economy_projection_reconciliation_events folds into
            // economy_events as StreamKind = 13. The ledger gains the stream's
            // three own columns and its kind branch; the rows are carried over,
            // the rebuild function's single insert is retargeted in place and
            // the record joins the kind-scoped immutability trigger before the
            // legacy table is dropped. Down mirrors every step.
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events");

            // Reconciliation records own no ordinal: the sequence rule is
            // re-added for every other stream only.
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectionWalletId",
                table: "economy_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RebuiltHash",
                table: "economy_events",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SourceJournalSequence",
                table: "economy_events",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" = 13 OR \"Sequence\" > 0");

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_projection_wallet_detected",
                table: "economy_events",
                columns: new[] { "ProjectionWalletId", "OccurredAt" },
                filter: "\"StreamKind\" = 13");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events",
                sql: "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND (\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND (\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND (\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND (\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND (\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\")) AND (\"StreamKind\" <> 13 OR \"SourceJournalSequence\" >= 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_economy_events_economy_wallets_ProjectionWalletId",
                table: "economy_events",
                column: "ProjectionWalletId",
                principalTable: "economy_wallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Carry every reconciliation record into the ledger. The legacy Id
            // is kept; DetectedAt is the row's OccurredAt, and Sequence — a
            // column the stream never had — is 0.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "Sequence", "ProjectionWalletId", "PreviousHash", "RebuiltHash",
                    "SourceJournalSequence", "OccurredAt")
                SELECT "Id", 13, 0, "WalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "DetectedAt"
                FROM public.economy_projection_reconciliation_events;
                """);

            // Retarget the projection rebuild function: its one insert appends
            // to the ledger. Each rewrite must match the function text exactly
            // once, so a drifted body aborts the migration instead of keeping a
            // statement that addresses a table this migration drops. The
            // helper is replaced rather than created: when this migration is
            // applied in the same session as the previous one (a fresh
            // database, or a deployment that skipped it), that migration's
            // copy of the session-local helper already exists.
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
                        RAISE EXCEPTION 'economy event writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $projection_rebuild$
                DECLARE
                    definition text;
                    rewrite record;
                BEGIN
                    SELECT pg_get_functiondef(p.oid) INTO definition
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                      AND p.proname = 'rebuild_wallet_projection_v1';
                    IF definition IS NULL THEN
                        RAISE EXCEPTION 'economy event writer rebuild_wallet_projection_v1 is absent';
                    END IF;

                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            (1,
                             'INSERT INTO public\.economy_projection_reconciliation_events \(',
                             'INSERT INTO public.economy_events (',
                             'reconciliation event append target'),
                            (2,
                             '"Id", "WalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "DetectedAt"\)',
                             '"Id", "StreamKind", "ProjectionWalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "OccurredAt", "Sequence")',
                             'reconciliation event append columns'),
                            (3,
                             'gen_random_uuid\(\), p_wallet_id, existing_projection\."ProjectionHash", rebuilt_hash,',
                             'gen_random_uuid(), 13, p_wallet_id, existing_projection."ProjectionHash", rebuilt_hash,',
                             'reconciliation event append values'),
                            (4,
                             'source_sequence, p_as_of\);',
                             'source_sequence, p_as_of, 0);',
                             'reconciliation event append sequence')
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
                          AND p.prosrc ~ 'economy_projection_reconciliation_events')
                    THEN
                        RAISE EXCEPTION 'an economy_private function still addresses the retired reconciliation table';
                    END IF;
                END
                $projection_rebuild$;
                """);

            // The reconciliation record was immutable; its kind joins the
            // kind-scoped trigger. The legacy table's trigger leaves with the
            // table.
            migrationBuilder.Sql("""
                DROP TRIGGER deny_immutable_event_mutation ON public.economy_events;
                CREATE TRIGGER deny_immutable_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_events
                    FOR EACH ROW
                    WHEN (OLD."StreamKind" IN (1, 4, 5, 7, 11, 13))
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                DROP TRIGGER deny_projection_reconciliation_event_mutation
                    ON public.economy_projection_reconciliation_events;
                """);

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

            // The legacy table is retired only after every row has been carried
            // over and the writer has been retargeted.
            migrationBuilder.DropTable(
                name: "economy_projection_reconciliation_events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirror of Up: the legacy table exists again (created below with
            // the column order the migration chain gave it), the ledger's
            // reconciliation rows go back to it, the rebuild function and both
            // triggers return to their former shape, and only then does the
            // ledger give up the stream's columns and kind branch.
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.CreateTable(
                name: "economy_projection_reconciliation_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RebuiltHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceJournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_projection_reconciliation_events", x => x.Id);
                    table.CheckConstraint("ck_economy_projection_events_sequence_nonnegative", "\"SourceJournalSequence\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_projection_reconciliation_events_economy_wallets_Wa~",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_economy_projection_reconciliation_events_wallet_detected",
                table: "economy_projection_reconciliation_events",
                columns: new[] { "WalletId", "DetectedAt" });

            // Every reconciliation row goes back, including the ones the
            // rebuild function appended to the ledger after the upgrade.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_projection_reconciliation_events (
                    "Id", "WalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "DetectedAt")
                SELECT "Id", "ProjectionWalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 13;
                """);

            // Restore the projection rebuild function to its legacy target:
            // the exact inverse of each Up rewrite. CREATE OR REPLACE for the
            // same reason as in Up: several migrations can revert in one
            // session.
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
                        RAISE EXCEPTION 'economy event writer rewrite "%" matched % occurrences, expected exactly 1',
                            p_label, match_count USING ERRCODE = 'P0001';
                    END IF;
                    RETURN regexp_replace(p_definition, p_pattern, p_replacement);
                END
                $rewrite$;

                DO $projection_rebuild$
                DECLARE
                    definition text;
                    rewrite record;
                BEGIN
                    SELECT pg_get_functiondef(p.oid) INTO definition
                    FROM pg_proc p
                    JOIN pg_namespace n ON n.oid = p.pronamespace
                    WHERE n.nspname = 'economy_private' AND p.prokind = 'f'
                      AND p.proname = 'rebuild_wallet_projection_v1';
                    IF definition IS NULL THEN
                        RAISE EXCEPTION 'economy event writer rebuild_wallet_projection_v1 is absent';
                    END IF;

                    FOR rewrite IN
                        SELECT * FROM (VALUES
                            (1,
                             'INSERT INTO public\.economy_events \(',
                             'INSERT INTO public.economy_projection_reconciliation_events (',
                             'reconciliation event append target'),
                            (2,
                             '"Id", "StreamKind", "ProjectionWalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "OccurredAt", "Sequence"\)',
                             '"Id", "WalletId", "PreviousHash", "RebuiltHash", "SourceJournalSequence", "DetectedAt")',
                             'reconciliation event append columns'),
                            (3,
                             'gen_random_uuid\(\), 13, p_wallet_id, existing_projection\."ProjectionHash", rebuilt_hash,',
                             'gen_random_uuid(), p_wallet_id, existing_projection."ProjectionHash", rebuilt_hash,',
                             'reconciliation event append values'),
                            (4,
                             'source_sequence, p_as_of, 0\);',
                             'source_sequence, p_as_of);',
                             'reconciliation event append sequence')
                        ) AS r(step, pattern, replacement, label)
                        ORDER BY r.step
                    LOOP
                        definition := pg_temp.rewrite_economy_function_text_v1(
                            definition, rewrite.pattern, rewrite.replacement, rewrite.label);
                    END LOOP;

                    EXECUTE definition;
                END
                $projection_rebuild$;

                -- The previous migration's Down creates the same session-local
                -- helper with a plain CREATE: remove this copy so the two can
                -- revert in one session.
                DROP FUNCTION pg_temp.rewrite_economy_function_text_v1(text, text, text, text);
                """);

            // Both triggers return to their former shape. The reconciliation
            // rows live in the legacy table again, so the ledger gives them up:
            // the restored trigger no longer covers kind 13, and nothing is
            // lost because every row was carried back above.
            migrationBuilder.Sql("""
                DROP TRIGGER deny_immutable_event_mutation ON public.economy_events;
                CREATE TRIGGER deny_immutable_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_events
                    FOR EACH ROW
                    WHEN (OLD."StreamKind" IN (1, 4, 5, 7, 11))
                    EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_projection_reconciliation_event_mutation
                    BEFORE DELETE OR UPDATE ON public.economy_projection_reconciliation_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                DELETE FROM public.economy_events WHERE "StreamKind" = 13;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_economy_events_economy_wallets_ProjectionWalletId",
                table: "economy_events");

            migrationBuilder.DropIndex(
                name: "ix_economy_events_projection_wallet_detected",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ProjectionWalletId",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "RebuiltHash",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "SourceJournalSequence",
                table: "economy_events");

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

            // With the reconciliation rows back in their own table, every
            // remaining ledger stream owns an ordinal again.
            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_values",
                table: "economy_events",
                sql: "(\"StreamKind\" <> 2 OR \"CumulativeDisputedHardUnits\" > 0) AND (\"StreamKind\" <> 3 OR (\"DeltaHardUnits\" <> 0 AND \"OutstandingHardUnits\" >= 0)) AND (\"StreamKind\" <> 7 OR \"Kind\" BETWEEN 1 AND 4) AND (\"StreamKind\" <> 9 OR \"Kind\" BETWEEN 1 AND 7) AND (\"StreamKind\" <> 10 OR \"Percentage\" BETWEEN 0 AND 100) AND (\"StreamKind\" <> 12 OR (\"BountyVersion\" > 1 AND \"OccurredAt\" >= \"ExpiresAt\"))");
        }
    }
}
