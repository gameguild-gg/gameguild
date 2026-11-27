using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier3AnchorVerifications : Migration
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
                name: "ETag",
                table: "economy_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExternalAnchorId",
                table: "economy_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyId",
                table: "economy_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ObjectMatches",
                table: "economy_events",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectVersion",
                table: "economy_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SignatureValid",
                table: "economy_events",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_events_anchor_verification",
                table: "economy_events",
                columns: new[] { "ExternalAnchorId", "OccurredAt" },
                filter: "\"StreamKind\" = 14");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" IN (13, 14) OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ExternalAnchorId\" IS NULL AND \"KeyId\" IS NULL AND \"ObjectVersion\" IS NULL AND \"ETag\" IS NULL AND \"SignatureValid\" IS NULL AND \"ObjectMatches\" IS NULL) OR (\"StreamKind\" = 14 AND \"ExternalAnchorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"KeyId\" IS NOT NULL AND \"ObjectVersion\" IS NOT NULL AND \"ETag\" IS NOT NULL AND \"SignatureValid\" IS NOT NULL AND \"ObjectMatches\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_economy_events_economy_external_anchors_ExternalAnchorId",
                table: "economy_events",
                column: "ExternalAnchorId",
                principalTable: "economy_external_anchors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // #1092 Tier 3: every verification run becomes a record of its
            // anchor's stream in the ledger and keeps its own id. VerifiedAt
            // is the row's OccurredAt, RetainUntil its ExpiresAt and
            // ObjectHash its EvidenceHash. The stream is ordered by time, so
            // the records carry no sequence. The stream is not added to
            // deny_immutable_event_mutation: its table never rejected
            // mutation.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_events (
                    "Id", "StreamKind", "Sequence", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag",
                    "ExpiresAt", "EvidenceHash", "SignatureValid", "ObjectMatches", "OccurredAt")
                SELECT "Id", 14, 0, "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag",
                       "RetainUntil", "ObjectHash", "SignatureValid", "ObjectMatches", "VerifiedAt"
                FROM public.economy_anchor_verifications;
                """);

            // The legacy table is retired only after every row has been
            // carried over.
            migrationBuilder.DropTable(
                name: "economy_anchor_verifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy table comes back in its original column order, under
            // its original key, foreign key and index names.
            migrationBuilder.CreateTable(
                name: "economy_anchor_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalAnchorId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ObjectVersion = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ETag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RetainUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObjectHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SignatureValid = table.Column<bool>(type: "boolean", nullable: false),
                    ObjectMatches = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_anchor_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_anchor_verifications_economy_external_anchors_Exter~",
                        column: x => x.ExternalAnchorId,
                        principalTable: "economy_external_anchors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_anchor_verifications_ExternalAnchorId_VerifiedAt",
                table: "economy_anchor_verifications",
                columns: new[] { "ExternalAnchorId", "VerifiedAt" });

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

            // Every run goes back, including those recorded through the
            // ledger after the upgrade — nothing is lost. The rows then leave
            // the ledger: the previous application does not know this stream.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_anchor_verifications (
                    "Id", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "RetainUntil", "ObjectHash",
                    "SignatureValid", "ObjectMatches", "VerifiedAt")
                SELECT "Id", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "ExpiresAt", "EvidenceHash",
                       "SignatureValid", "ObjectMatches", "OccurredAt"
                FROM public.economy_events
                WHERE "StreamKind" = 14;

                DELETE FROM public.economy_events WHERE "StreamKind" = 14;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_economy_events_economy_external_anchors_ExternalAnchorId",
                table: "economy_events");

            migrationBuilder.DropIndex(
                name: "ix_economy_events_anchor_verification",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ETag",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ExternalAnchorId",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "KeyId",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ObjectMatches",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "ObjectVersion",
                table: "economy_events");

            migrationBuilder.DropColumn(
                name: "SignatureValid",
                table: "economy_events");

            // With the runs back in their own table, only the projection
            // reconciliation stream is left without an ordinal.
            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_sequence",
                table: "economy_events",
                sql: "\"StreamKind\" = 13 OR \"Sequence\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_events_stream_shape",
                table: "economy_events",
                sql: "(\"StreamKind\" = 1 AND \"SourceStampId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 2 AND \"ProviderEventId\" IS NOT NULL AND \"ProviderDisputeReference\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"Status\" IS NOT NULL AND \"CumulativeDisputedHardUnits\" IS NOT NULL AND \"RequestHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 3 AND \"DebtWalletId\" IS NOT NULL AND \"SourceStampId\" IS NOT NULL AND \"DeltaHardUnits\" IS NOT NULL AND \"OutstandingHardUnits\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 4 AND \"HoldId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 5 AND \"RiskReviewCaseId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHashes\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 6 AND \"ComplianceHoldId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 7 AND \"LegacyCutoverTenantId\" IS NOT NULL AND \"BatchId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"ActorId\" IS NOT NULL AND \"Reason\" IS NOT NULL AND \"ReauthenticationHash\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 8 AND \"TenantId\" IS NOT NULL AND \"SettlementId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"Kind\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 9 AND \"SessionId\" IS NOT NULL AND \"Kind\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 10 AND \"SessionId\" IS NOT NULL AND \"Percentage\" IS NOT NULL AND \"EvidenceHash\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 11 AND \"TenantId\" IS NOT NULL AND \"RunId\" IS NOT NULL AND \"KindText\" IS NOT NULL AND \"Evidence\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"Hash\" IS NOT NULL AND \"Kind\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 12 AND \"BountyId\" IS NOT NULL AND \"ExpiresAt\" IS NOT NULL AND \"BountyVersion\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"PreviousHash\" IS NULL AND \"Hash\" IS NULL AND \"ProjectionWalletId\" IS NULL AND \"RebuiltHash\" IS NULL AND \"SourceJournalSequence\" IS NULL) OR (\"StreamKind\" = 13 AND \"ProjectionWalletId\" IS NOT NULL AND \"PreviousHash\" IS NOT NULL AND \"RebuiltHash\" IS NOT NULL AND \"SourceJournalSequence\" IS NOT NULL AND \"TenantId\" IS NULL AND \"Kind\" IS NULL AND \"KindText\" IS NULL AND \"ActorId\" IS NULL AND \"EvidenceHash\" IS NULL AND \"SourceStampId\" IS NULL AND \"ProviderEventId\" IS NULL AND \"ProviderDisputeReference\" IS NULL AND \"Status\" IS NULL AND \"CumulativeDisputedHardUnits\" IS NULL AND \"RequestHash\" IS NULL AND \"DebtWalletId\" IS NULL AND \"DeltaHardUnits\" IS NULL AND \"OutstandingHardUnits\" IS NULL AND \"HoldId\" IS NULL AND \"RiskReviewCaseId\" IS NULL AND \"EvidenceHashes\" IS NULL AND \"Resolution\" IS NULL AND \"DecisionCode\" IS NULL AND \"ComplianceHoldId\" IS NULL AND \"LegacyCutoverTenantId\" IS NULL AND \"BatchId\" IS NULL AND \"Reason\" IS NULL AND \"ReauthenticationHash\" IS NULL AND \"SettlementId\" IS NULL AND \"SessionId\" IS NULL AND \"Percentage\" IS NULL AND \"RunId\" IS NULL AND \"Evidence\" IS NULL AND \"Hash\" IS NULL AND \"BountyId\" IS NULL AND \"ExpiresAt\" IS NULL AND \"BountyVersion\" IS NULL)");
        }
    }
}
