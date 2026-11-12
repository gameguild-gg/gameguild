using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEconomyTier1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "economy_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalKind = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Generation = table.Column<long>(type: "bigint", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_approvals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_inbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InboxKind = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SignatureVerified = table.Column<bool>(type: "boolean", nullable: true),
                    RawObjectReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_inbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_value_legs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    LegKind = table.Column<int>(type: "integer", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<long>(type: "bigint", nullable: false),
                    SellerUnits = table.Column<long>(type: "bigint", nullable: true),
                    PlatformFeeUnits = table.Column<long>(type: "bigint", nullable: true),
                    RefundedUnits = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_value_legs", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_value_legs_conservation", "(\"LegKind\" = 1 AND \"Units\" > 0 AND \"SellerUnits\" >= 0 AND \"PlatformFeeUnits\" >= 0 AND \"SellerUnits\" + \"PlatformFeeUnits\" = \"Units\" AND \"RefundedUnits\" BETWEEN 0 AND \"Units\" AND \"RefundId\" IS NULL) OR (\"LegKind\" = 2 AND \"Units\" > 0 AND \"SellerUnits\" IS NULL AND \"PlatformFeeUnits\" IS NULL AND \"RefundedUnits\" IS NULL AND \"RefundId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_value_legs_economy_marketplace_refunds_~",
                        column: x => x.RefundId,
                        principalTable: "economy_marketplace_refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_marketplace_value_legs_economy_marketplace_settleme~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxKind = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_outbox", x => x.Id);
                    table.CheckConstraint("ck_economy_outbox_attempts", "\"AttemptCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_outbox_economy_admin_withdrawal_runs_RunId",
                        column: x => x.RunId,
                        principalTable: "economy_admin_withdrawal_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_outbox_economy_compliance_evidence_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "economy_compliance_evidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_outbox_economy_marketplace_settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_outbox_economy_payout_operations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "economy_payout_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_outbox_economy_posting_groups_PostingGroupId",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_provider_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProviderEventKind = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultingState = table.Column<int>(type: "integer", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_provider_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_provider_events_economy_admin_withdrawal_runs_RunId",
                        column: x => x.RunId,
                        principalTable: "economy_admin_withdrawal_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_provider_events_economy_payout_operations_Operation~",
                        column: x => x.OperationId,
                        principalTable: "economy_payout_operations",
                        principalColumn: "Id",
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
                name: "ux_economy_approvals_kind_generation_actor",
                table: "economy_approvals",
                columns: new[] { "ApprovalKind", "Generation", "ActorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_approvals_kind_subject_actor",
                table: "economy_approvals",
                columns: new[] { "ApprovalKind", "SubjectId", "ActorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_inbox_provider_event",
                table: "economy_inbox",
                columns: new[] { "Provider", "Environment", "ProviderEventId" },
                unique: true,
                filter: "\"InboxKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_economy_inbox_tenant_provider_event",
                table: "economy_inbox",
                columns: new[] { "TenantId", "Provider", "ProviderEventId" },
                unique: true,
                filter: "\"InboxKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ix_economy_marketplace_value_legs_refund_settlement",
                table: "economy_marketplace_value_legs",
                columns: new[] { "SettlementId", "Currency" },
                filter: "\"LegKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_marketplace_value_legs_refund_currency",
                table: "economy_marketplace_value_legs",
                columns: new[] { "RefundId", "Currency" },
                unique: true,
                filter: "\"LegKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_marketplace_value_legs_settlement_currency",
                table: "economy_marketplace_value_legs",
                columns: new[] { "LegKind", "SettlementId", "Currency" },
                unique: true,
                filter: "\"LegKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_economy_outbox_dispatch_queue",
                table: "economy_outbox",
                columns: new[] { "CompletedAt", "AvailableAt", "LeaseExpiresAt" },
                filter: "\"OutboxKind\" IN (3, 4)");

            migrationBuilder.CreateIndex(
                name: "ix_economy_outbox_marketplace_dispatch",
                table: "economy_outbox",
                columns: new[] { "PublishedAt", "OccurredAt" },
                filter: "\"OutboxKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ix_economy_outbox_marketplace_lease",
                table: "economy_outbox",
                columns: new[] { "PublishedAt", "LeaseExpiresAt", "OccurredAt" },
                filter: "\"OutboxKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "IX_economy_outbox_PostingGroupId",
                table: "economy_outbox",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_outbox_SettlementId",
                table: "economy_outbox",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_admin_run",
                table: "economy_outbox",
                column: "RunId",
                unique: true,
                filter: "\"OutboxKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_admin_tenant_run",
                table: "economy_outbox",
                columns: new[] { "TenantId", "RunId" },
                unique: true,
                filter: "\"OutboxKind\" = 4");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_evidence",
                table: "economy_outbox",
                column: "EvidenceId",
                unique: true,
                filter: "\"OutboxKind\" = 5");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_payload_hash",
                table: "economy_outbox",
                column: "PayloadHash",
                unique: true,
                filter: "\"OutboxKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_payout_operation",
                table: "economy_outbox",
                column: "OperationId",
                unique: true,
                filter: "\"OutboxKind\" = 3");

            migrationBuilder.CreateIndex(
                name: "ix_economy_provider_events_operation_recorded",
                table: "economy_provider_events",
                columns: new[] { "OperationId", "RecordedAt" },
                filter: "\"ProviderEventKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ix_economy_provider_events_run_recorded",
                table: "economy_provider_events",
                columns: new[] { "RunId", "RecordedAt" },
                filter: "\"ProviderEventKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_events_admin_tenant_event",
                table: "economy_provider_events",
                columns: new[] { "TenantId", "EventId" },
                unique: true,
                filter: "\"ProviderEventKind\" = 2");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_events_payout_event",
                table: "economy_provider_events",
                column: "EventId",
                unique: true,
                filter: "\"ProviderEventKind\" = 1");

            // ===================================================================
            // #1092 Tier 1 consolidation — data move + SECURITY DEFINER retarget.
            // The unified tables are created above; existing rows are carried
            // over with their kind discriminators set, the writer functions
            // and the bridge trigger are retargeted, and only then are the
            // sixteen legacy tables dropped. The sections run in the merge
            // order T1.1 → T1.6 from docs/economy/06-consolidation-crossref.md.
            // ===================================================================

            // --- T1.1 economy_outbox (5 → 1) --------------------------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_outbox (
                    "Id", "OutboxKind", "PostingGroupId", "MessageType", "Payload", "PayloadHash", "OccurredAt")
                SELECT "Id", 1, "PostingGroupId", "Type", "Payload", "PayloadHash", "OccurredAt"
                FROM public.economy_outbox_messages;

                INSERT INTO public.economy_outbox (
                    "Id", "OutboxKind", "TenantId", "SettlementId", "MessageType", "Payload", "PayloadHash",
                    "OccurredAt", "PublishedAt", "AttemptCount", "LeaseOwner", "LeaseExpiresAt", "LastError")
                SELECT "Id", 2, "TenantId", "SettlementId", "MessageType", "Payload"::text, "PayloadHash",
                       "OccurredAt", "PublishedAt", "AttemptCount", "LeaseOwner", "LeaseExpiresAt", "LastError"
                FROM public.economy_marketplace_outbox;

                INSERT INTO public.economy_outbox (
                    "Id", "OutboxKind", "OperationId", "IdempotencyKey", "Payload", "PayloadHash",
                    "OccurredAt", "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount",
                    "CompletedAt", "LastError")
                SELECT "Id", 3, "OperationId", "IdempotencyKey", "Payload"::text, "PayloadHash",
                       "CreatedAt", "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount",
                       "CompletedAt", "LastErrorCode"
                FROM public.economy_payout_dispatch_outbox;

                INSERT INTO public.economy_outbox (
                    "Id", "OutboxKind", "TenantId", "RunId", "IdempotencyKey", "Payload", "PayloadHash",
                    "OccurredAt", "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount",
                    "CompletedAt", "LastError")
                SELECT "Id", 4, "TenantId", "RunId", "IdempotencyKey", "Payload"::text, "PayloadHash",
                       "CreatedAt", "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount",
                       "CompletedAt", "LastErrorCode"
                FROM public.economy_admin_withdrawal_dispatch_outbox;

                -- The compliance outbox never had a reader (consumers read the
                -- evidence tables directly); its rows are preserved here for
                -- audit and the write side is retired in the application layer.
                INSERT INTO public.economy_outbox (
                    "Id", "OutboxKind", "EvidenceId", "MessageType", "Payload", "PayloadHash",
                    "OccurredAt", "DispatchedAt")
                SELECT "Id", 5, "EvidenceId", "Type", "Payload"::text, "PayloadHash",
                       "OccurredAt", "DispatchedAt"
                FROM public.economy_compliance_outbox;
                """);

            // Retarget every SECURITY DEFINER posting writer from the legacy
            // core spool to the unified queue. The kind-1 inserts keep their
            // column list except the legacy discriminator column, which the
            // unified queue renamed "Type" -> "MessageType"; OutboxKind and
            // AttemptCount fall back to their column defaults, and the
            // enqueue stays inside the posting transaction.
            migrationBuilder.Sql("""
                DO $t11_writers$
                DECLARE
                    definition text;
                BEGIN
                    FOR definition IN
                        SELECT pg_get_functiondef(p.oid)
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND pg_get_functiondef(p.oid) LIKE '%public.economy_outbox_messages%'
                    LOOP
                        definition := replace(definition,
                            'public.economy_outbox_messages', 'public.economy_outbox');
                        definition := replace(definition,
                            '"Id", "PostingGroupId", "Type", "Payload", "PayloadHash", "OccurredAt")',
                            '"Id", "PostingGroupId", "MessageType", "Payload", "PayloadHash", "OccurredAt")');
                        EXECUTE definition;
                    END LOOP;

                    FOR definition IN
                        SELECT pg_get_functiondef(p.oid)
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND p.proname IN ('post_marketplace_settlement_v1', 'post_marketplace_refund_v1')
                    LOOP
                        -- Each writer carries exactly one of the two outbox
                        -- identities (settlement: grant, refund: revoke).
                        IF strpos(definition, 'INSERT INTO public.economy_marketplace_outbox (') = 0
                           OR (strpos(definition, '''entitlement-grant-outbox''),') = 0
                               AND strpos(definition, '''entitlement-revoke-outbox''),') = 0) THEN
                            RAISE EXCEPTION 'marketplace outbox writer shape is not recognized';
                        END IF;
                        definition := replace(definition,
                            'INSERT INTO public.economy_marketplace_outbox (',
                            'INSERT INTO public.economy_outbox ("OutboxKind", ');
                        definition := replace(definition,
                            '''entitlement-grant-outbox''),',
                            '''entitlement-grant-outbox''), 2,');
                        definition := replace(definition,
                            '''entitlement-revoke-outbox''),',
                            '''entitlement-revoke-outbox''), 2,');
                        EXECUTE definition;
                    END LOOP;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND pg_get_functiondef(p.oid) ~ 'economy_(outbox_messages|marketplace_outbox)')
                    THEN
                        RAISE EXCEPTION 'an economy writer still references a legacy outbox table';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND pg_get_functiondef(p.oid) LIKE '%public.economy_outbox%'
                          AND pg_get_functiondef(p.oid) LIKE '%"Id", "PostingGroupId", "Type", "Payload", "PayloadHash", "OccurredAt")'
                    ) THEN
                        RAISE EXCEPTION 'an economy writer still enqueues with the legacy outbox column list';
                    END IF;
                END
                $t11_writers$;
                """);

            // Move the integration bridge onto the unified queue. The trigger
            // fires inside the posting transaction, preserving the enqueue
            // guarantee for economy.posting.accepted.v1 rows.
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_bridge_economy_posting_outbox_v1
                    ON public.economy_outbox_messages;
                CREATE TRIGGER trg_bridge_economy_posting_outbox_v1
                    AFTER INSERT ON public.economy_outbox
                    FOR EACH ROW
                    WHEN (NEW."OutboxKind" = 1 AND NEW."MessageType" = 'economy.posting.accepted.v1')
                    EXECUTE FUNCTION public.bridge_economy_posting_outbox_v1();
                """);

            // --- T1.2 economy_inbox (2 → 1) --------------------------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_inbox (
                    "Id", "InboxKind", "Provider", "Environment", "ProviderEventId", "PayloadHash",
                    "RawObjectReference", "ReceivedAt", "ProcessedAt", "ProcessingError")
                SELECT "Id", 1, "Provider", "Environment", "ProviderEventId", "PayloadHash",
                       "RawObjectReference", "ReceivedAt", "ProcessedAt", "ProcessingError"
                FROM public.economy_compliance_inbox;

                INSERT INTO public.economy_inbox (
                    "Id", "InboxKind", "TenantId", "SessionId", "Provider", "ProviderEventId",
                    "PayloadHash", "EvidenceHash", "SignatureVerified", "ReceivedAt",
                    "ProcessedAt", "ProcessingError")
                SELECT "Id", 2, "TenantId", "SessionId", "Network", "ProviderEventId",
                       "PayloadHash", "EvidenceHash", "SignatureVerified", "ReceivedAt",
                       "ProcessedAt", "ProcessingError"
                FROM public.economy_ad_reward_provider_proof_inbox;
                """);

            // --- T1.3 economy_approvals (3 → 1) -----------------------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_approvals (
                    "Id", "ApprovalKind", "SubjectId", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", 1, "PolicyId", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_capability_policy_approvals;

                INSERT INTO public.economy_approvals (
                    "Id", "ApprovalKind", "SubjectId", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", 2, "KillSwitchId", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_kill_switch_release_approvals;

                INSERT INTO public.economy_approvals (
                    "Id", "ApprovalKind", "Generation", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", 3, "Generation", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_projection_generation_approvals;
                """);

            // --- T1.4 economy_provider_events (2 → 1) -----------------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_provider_events (
                    "ProviderEventKind", "EventId", "EventHash", "OperationId", "ResultingState", "RecordedAt")
                SELECT 1, "EventId", "EventHash", "OperationId", "ResultingState", "RecordedAt"
                FROM public.economy_payout_provider_events;

                INSERT INTO public.economy_provider_events (
                    "ProviderEventKind", "TenantId", "EventId", "EventHash", "RunId", "RecordedAt")
                SELECT 2, "TenantId", "EventId", "EventHash", "RunId", "RecordedAt"
                FROM public.economy_admin_withdrawal_provider_events;
                """);

            // Retarget the provider-event writers/readers. read_payout_provider_event_v1
            // changes its return type, so it is dropped and recreated against the
            // unified anchor with the kind discriminator in the lookup.
            migrationBuilder.Sql("""
                DO $t14_writers$
                DECLARE
                    definition text;
                BEGIN
                    SELECT pg_get_functiondef(
                        'economy_private.complete_payout_provider_event_v1(text,text,uuid,bigint,integer,text,timestamptz)'::regprocedure)
                    INTO definition;
                    IF strpos(definition, 'INSERT INTO public.economy_payout_provider_events (') = 0
                       OR strpos(definition, 'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_operation_id, p_state,') = 0 THEN
                        RAISE EXCEPTION 'payout provider event writer shape is not recognized';
                    END IF;
                    definition := replace(definition,
                        'INSERT INTO public.economy_payout_provider_events (',
                        'INSERT INTO public.economy_provider_events ("ProviderEventKind", ');
                    definition := replace(definition,
                        'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_operation_id, p_state,',
                        '1, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_operation_id, p_state,');
                    EXECUTE definition;

                    SELECT pg_get_functiondef(
                        'economy_private.complete_admin_withdrawal_provider_event_v2(uuid,text,text,uuid,bigint,integer,text,timestamptz)'::regprocedure)
                    INTO definition;
                    IF strpos(definition, 'INSERT INTO public.economy_admin_withdrawal_provider_events (') = 0
                       OR strpos(definition, 'p_tenant_id, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);') = 0 THEN
                        RAISE EXCEPTION 'admin withdrawal provider event writer shape is not recognized';
                    END IF;
                    definition := replace(definition,
                        'INSERT INTO public.economy_admin_withdrawal_provider_events (',
                        'INSERT INTO public.economy_provider_events ("ProviderEventKind", ');
                    definition := replace(definition,
                        'p_tenant_id, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);',
                        '2, p_tenant_id, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);');
                    EXECUTE definition;

                    -- complete_admin_withdrawal_provider_event_v1 is superseded
                    -- by v2 but still installed; retarget its legacy insert too
                    -- so no SECURITY DEFINER body survives pointing at the
                    -- retired table.
                    SELECT pg_get_functiondef(
                        'economy_private.complete_admin_withdrawal_provider_event_v1(text,text,uuid,bigint,integer,text,timestamptz)'::regprocedure)
                    INTO definition;
                    IF definition IS NOT NULL THEN
                        IF strpos(definition, 'INSERT INTO public.economy_admin_withdrawal_provider_events (') = 0
                           OR strpos(definition, 'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);') = 0 THEN
                            RAISE EXCEPTION 'legacy admin withdrawal provider event writer shape is not recognized';
                        END IF;
                        definition := replace(definition,
                            'INSERT INTO public.economy_admin_withdrawal_provider_events (',
                            'INSERT INTO public.economy_provider_events ("ProviderEventKind", ');
                        definition := replace(definition,
                            'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);',
                            '2, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);');
                        EXECUTE definition;
                    END IF;
                END
                $t14_writers$;

                DROP FUNCTION IF EXISTS economy_private.read_payout_provider_event_v1(text);
                CREATE FUNCTION economy_private.read_payout_provider_event_v1(p_event_id text)
                RETURNS SETOF public.economy_provider_events
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_provider_events
                    WHERE "ProviderEventKind" = 1
                      AND "EventId" = pg_catalog.btrim(p_event_id)
                $function$;
                ALTER FUNCTION economy_private.read_payout_provider_event_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_provider_event_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_provider_event_v1(text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_provider_event_v1(text);
                CREATE FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(p_event_id text)
                RETURNS SETOF public.economy_provider_events
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_provider_events
                    WHERE "ProviderEventKind" = 2
                      AND "EventId" = pg_catalog.btrim(p_event_id)
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text)
                    TO gameguild_economy_writer;

                DROP TRIGGER IF EXISTS deny_payout_provider_event_mutation
                    ON public.economy_payout_provider_events;
                DROP TRIGGER IF EXISTS deny_admin_withdrawal_provider_event_mutation
                    ON public.economy_admin_withdrawal_provider_events;
                CREATE TRIGGER deny_provider_event_mutation
                    BEFORE UPDATE OR DELETE ON public.economy_provider_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                """);

            // --- T1.5 economy_ad_reward_session_timeline (2 → 1) -------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_ad_reward_session_timeline (
                    "Id", "TimelineKind", "SessionId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                SELECT "Id", 1, "SessionId", "Sequence", "State", "EvidenceHash", "OccurredAt"
                FROM public.economy_ad_reward_session_events;

                INSERT INTO public.economy_ad_reward_session_timeline (
                    "Id", "TimelineKind", "SessionId", "Sequence", "Percentage", "EvidenceHash", "OccurredAt")
                SELECT "Id", 2, "SessionId", "Sequence", "Percentage", "EvidenceHash", "ObservedAt"
                FROM public.economy_ad_reward_playback_milestones;
                """);

            // --- T1.6 economy_marketplace_value_legs (2 → 1) -----------------
            migrationBuilder.Sql("""
                INSERT INTO public.economy_marketplace_value_legs (
                    "LegKind", "SettlementId", "Currency", "Units", "SellerUnits", "PlatformFeeUnits", "RefundedUnits")
                SELECT 1, "SettlementId", "Currency", "Units", "SellerUnits", "PlatformFeeUnits", "RefundedUnits"
                FROM public.economy_marketplace_settlement_legs;

                INSERT INTO public.economy_marketplace_value_legs (
                    "LegKind", "SettlementId", "RefundId", "Currency", "Units")
                SELECT 2, "SettlementId", "RefundId", "Currency", "Units"
                FROM public.economy_marketplace_refund_legs;
                """);

            // Retarget the settlement/refund writer functions. The settlement
            // UPDATE keeps RefundedUnits queryable per (SettlementId, Currency)
            // through the LegKind = 1 filter.
            migrationBuilder.Sql("""
                DO $t16_writers$
                DECLARE
                    definition text;
                BEGIN
                    FOR definition IN
                        SELECT pg_get_functiondef(p.oid)
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND p.proname = 'post_marketplace_settlement_v1'
                          AND pg_get_functiondef(p.oid) LIKE '%public.economy_marketplace_settlement_legs%'
                    LOOP
                        IF strpos(definition, 'INSERT INTO public.economy_marketplace_settlement_legs (') = 0
                           OR strpos(definition, 'SELECT p_settlement_id, leg.currency, leg.units, leg.seller_units, leg.platform_fee_units, 0') = 0 THEN
                            RAISE EXCEPTION 'marketplace settlement leg writer shape is not recognized';
                        END IF;
                        definition := replace(definition,
                            'INSERT INTO public.economy_marketplace_settlement_legs (',
                            'INSERT INTO public.economy_marketplace_value_legs ("Id", "LegKind", ');
                        definition := replace(definition,
                            'SELECT p_settlement_id, leg.currency, leg.units, leg.seller_units, leg.platform_fee_units, 0',
                            'SELECT gen_random_uuid(), 1, p_settlement_id, leg.currency, leg.units, leg.seller_units, leg.platform_fee_units, 0');
                        EXECUTE definition;
                    END LOOP;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.proname = 'post_marketplace_settlement_v1') THEN
                        RAISE EXCEPTION 'marketplace settlement writer is absent';
                    END IF;

                    FOR definition IN
                        SELECT pg_get_functiondef(p.oid)
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND p.proname = 'post_marketplace_refund_v1'
                          AND pg_get_functiondef(p.oid) LIKE '%public.economy_marketplace_refund_legs%'
                    LOOP
                        IF strpos(definition, 'INSERT INTO public.economy_marketplace_refund_legs (') = 0
                           OR strpos(definition, 'SELECT p_refund_id, p_settlement_id, leg.currency, leg.units') = 0
                           OR strpos(definition, 'UPDATE public.economy_marketplace_settlement_legs leg') = 0
                           OR strpos(definition, 'WHERE leg."SettlementId" = p_settlement_id') = 0
                           OR strpos(definition, 'LEFT JOIN public.economy_marketplace_settlement_legs persisted') = 0
                           OR strpos(definition, 'FROM public.economy_marketplace_settlement_legs persisted') = 0 THEN
                            RAISE EXCEPTION 'marketplace refund leg writer shape is not recognized';
                        END IF;
                        definition := replace(definition,
                            'INSERT INTO public.economy_marketplace_refund_legs (',
                            'INSERT INTO public.economy_marketplace_value_legs ("Id", "LegKind", ');
                        definition := replace(definition,
                            'SELECT p_refund_id, p_settlement_id, leg.currency, leg.units',
                            'SELECT gen_random_uuid(), 2, p_refund_id, p_settlement_id, leg.currency, leg.units');
                        definition := replace(definition,
                            'UPDATE public.economy_marketplace_settlement_legs leg',
                            'UPDATE public.economy_marketplace_value_legs leg');
                        definition := replace(definition,
                            'WHERE leg."SettlementId" = p_settlement_id',
                            'WHERE leg."LegKind" = 1 AND leg."SettlementId" = p_settlement_id');
                        -- The cumulative-refund counter reads (the refund math)
                        -- keep addressing settlement legs per (SettlementId, Currency).
                        definition := replace(definition,
                            'LEFT JOIN public.economy_marketplace_settlement_legs persisted',
                            'LEFT JOIN public.economy_marketplace_value_legs persisted');
                        definition := replace(definition,
                            'FROM public.economy_marketplace_settlement_legs persisted',
                            'FROM public.economy_marketplace_value_legs persisted');
                        definition := replace(definition,
                            'ON persisted."SettlementId" = p_settlement_id',
                            'ON persisted."LegKind" = 1 AND persisted."SettlementId" = p_settlement_id');
                        definition := replace(definition,
                            'WHERE persisted."SettlementId" = p_settlement_id',
                            'WHERE persisted."LegKind" = 1 AND persisted."SettlementId" = p_settlement_id');
                        EXECUTE definition;
                    END LOOP;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        WHERE n.nspname = 'economy_private'
                          AND p.proname = 'post_marketplace_refund_v1') THEN
                        RAISE EXCEPTION 'marketplace refund writer is absent';
                    END IF;
                END
                $t16_writers$;
                """);

            // Least-privilege grants for the unified relations (same posture as
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
            // carried over and every writer has been retargeted.
            migrationBuilder.DropTable(
                name: "economy_ad_reward_playback_milestones");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_provider_proof_inbox");

            migrationBuilder.DropTable(
                name: "economy_ad_reward_session_events");

            migrationBuilder.DropTable(
                name: "economy_admin_withdrawal_dispatch_outbox");

            migrationBuilder.DropTable(
                name: "economy_admin_withdrawal_provider_events");

            migrationBuilder.DropTable(
                name: "economy_capability_policy_approvals");

            migrationBuilder.DropTable(
                name: "economy_compliance_inbox");

            migrationBuilder.DropTable(
                name: "economy_compliance_outbox");

            migrationBuilder.DropTable(
                name: "economy_kill_switch_release_approvals");

            migrationBuilder.DropTable(
                name: "economy_marketplace_outbox");

            migrationBuilder.DropTable(
                name: "economy_marketplace_refund_legs");

            migrationBuilder.DropTable(
                name: "economy_marketplace_settlement_legs");

            migrationBuilder.DropTable(
                name: "economy_outbox_messages");

            migrationBuilder.DropTable(
                name: "economy_payout_dispatch_outbox");

            migrationBuilder.DropTable(
                name: "economy_payout_provider_events");

            migrationBuilder.DropTable(
                name: "economy_projection_generation_approvals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The legacy tables are recreated first so the unified rows can be
            // carried back before the unified tables are dropped. The SECURITY
            // DEFINER writers are restored to their legacy targets with the
            // mirrored token rewrites from Up.
            migrationBuilder.CreateTable(
                name: "economy_ad_reward_playback_milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Percentage = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_playback_milestones", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_reward_playback_milestones_percentage", "\"Percentage\" BETWEEN 0 AND 100 AND \"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_playback_milestones_economy_ad_reward_ses~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_provider_proof_inbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignatureVerified = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_provider_proof_inbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_provider_proof_inbox_economy_ad_reward_se~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_ad_reward_session_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_ad_reward_session_events", x => x.Id);
                    table.CheckConstraint("ck_economy_ad_reward_session_events_sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("ck_economy_ad_reward_session_events_state", "\"State\" BETWEEN 1 AND 7");
                    table.ForeignKey(
                        name: "FK_economy_ad_reward_session_events_economy_ad_reward_sessions~",
                        column: x => x.SessionId,
                        principalTable: "economy_ad_reward_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_admin_withdrawal_dispatch_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_admin_withdrawal_dispatch_outbox", x => x.Id);
                    table.CheckConstraint("ck_economy_admin_withdrawal_dispatch_outbox_attempts", "\"AttemptCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_admin_withdrawal_dispatch_outbox_economy_admin_with~",
                        column: x => x.RunId,
                        principalTable: "economy_admin_withdrawal_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_admin_withdrawal_provider_events",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_admin_withdrawal_provider_events", x => new { x.TenantId, x.EventId });
                    table.ForeignKey(
                        name: "FK_economy_admin_withdrawal_provider_events_economy_admin_with~",
                        column: x => x.RunId,
                        principalTable: "economy_admin_withdrawal_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_capability_policy_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_capability_policy_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_capability_policy_approvals_economy_capability_poli~",
                        column: x => x.PolicyId,
                        principalTable: "economy_capability_policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_compliance_inbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderEventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RawObjectReference = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_compliance_inbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_compliance_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_compliance_outbox", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_compliance_outbox_economy_compliance_evidence_Evide~",
                        column: x => x.EvidenceId,
                        principalTable: "economy_compliance_evidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_kill_switch_release_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    KillSwitchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_kill_switch_release_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_kill_switch_release_approvals_economy_kill_switches~",
                        column: x => x.KillSwitchId,
                        principalTable: "economy_kill_switches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MessageType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_outbox", x => x.Id);
                    table.CheckConstraint("ck_economy_marketplace_outbox_attempts", "\"AttemptCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_outbox_economy_marketplace_settlements_~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_refund_legs",
                columns: table => new
                {
                    RefundId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_refund_legs", x => new { x.RefundId, x.Currency });
                    table.CheckConstraint("ck_economy_marketplace_refund_legs_amount", "\"Units\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_legs_economy_marketplace_refunds~",
                        column: x => x.RefundId,
                        principalTable: "economy_marketplace_refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_marketplace_refund_legs_economy_marketplace_settlem~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_marketplace_settlement_legs",
                columns: table => new
                {
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    PlatformFeeUnits = table.Column<long>(type: "bigint", nullable: false),
                    RefundedUnits = table.Column<long>(type: "bigint", nullable: false),
                    SellerUnits = table.Column<long>(type: "bigint", nullable: false),
                    Units = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_marketplace_settlement_legs", x => new { x.SettlementId, x.Currency });
                    table.CheckConstraint("ck_economy_marketplace_settlement_legs_conservation", "\"Units\" > 0 AND \"SellerUnits\" >= 0 AND \"PlatformFeeUnits\" >= 0 AND \"SellerUnits\" + \"PlatformFeeUnits\" = \"Units\"");
                    table.CheckConstraint("ck_economy_marketplace_settlement_legs_refund", "\"RefundedUnits\" BETWEEN 0 AND \"Units\"");
                    table.ForeignKey(
                        name: "FK_economy_marketplace_settlement_legs_economy_marketplace_set~",
                        column: x => x.SettlementId,
                        principalTable: "economy_marketplace_settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_outbox_messages_economy_posting_groups_PostingGroup~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_payout_dispatch_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_payout_dispatch_outbox", x => x.Id);
                    table.CheckConstraint("ck_economy_payout_dispatch_outbox_attempts", "\"AttemptCount\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_payout_dispatch_outbox_economy_payout_operations_Op~",
                        column: x => x.OperationId,
                        principalTable: "economy_payout_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_payout_provider_events",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResultingState = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_payout_provider_events", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_economy_payout_provider_events_economy_payout_operations_Op~",
                        column: x => x.OperationId,
                        principalTable: "economy_payout_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_projection_generation_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_projection_generation_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_projection_generation_approvals_economy_projection_~",
                        column: x => x.Generation,
                        principalTable: "economy_projection_generations",
                        principalColumn: "Generation",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_playback_milestones_SessionId_Sequence",
                table: "economy_ad_reward_playback_milestones",
                columns: new[] { "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_provider_proof_inbox_SessionId",
                table: "economy_ad_reward_provider_proof_inbox",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_provider_proof_inbox_TenantId_Network_Pro~",
                table: "economy_ad_reward_provider_proof_inbox",
                columns: new[] { "TenantId", "Network", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_ad_reward_session_events_SessionId_Sequence",
                table: "economy_ad_reward_session_events",
                columns: new[] { "SessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_admin_withdrawal_dispatch_outbox_CompletedAt_Availa~",
                table: "economy_admin_withdrawal_dispatch_outbox",
                columns: new[] { "CompletedAt", "AvailableAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_admin_withdrawal_dispatch_outbox_RunId",
                table: "economy_admin_withdrawal_dispatch_outbox",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_admin_withdrawal_dispatch_outbox_TenantId_RunId",
                table: "economy_admin_withdrawal_dispatch_outbox",
                columns: new[] { "TenantId", "RunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_admin_withdrawal_provider_events_run_recorded",
                table: "economy_admin_withdrawal_provider_events",
                columns: new[] { "RunId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_capability_policy_approvals_policy_actor",
                table: "economy_capability_policy_approvals",
                columns: new[] { "PolicyId", "ActorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_compliance_inbox_provider_event",
                table: "economy_compliance_inbox",
                columns: new[] { "Provider", "Environment", "ProviderEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_compliance_outbox_evidence",
                table: "economy_compliance_outbox",
                column: "EvidenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_kill_switch_release_approvals_switch_actor",
                table: "economy_kill_switch_release_approvals",
                columns: new[] { "KillSwitchId", "ActorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_outbox_PublishedAt_LeaseExpiresAt_Occur~",
                table: "economy_marketplace_outbox",
                columns: new[] { "PublishedAt", "LeaseExpiresAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_outbox_PublishedAt_OccurredAt",
                table: "economy_marketplace_outbox",
                columns: new[] { "PublishedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_outbox_SettlementId",
                table: "economy_marketplace_outbox",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_refund_legs_SettlementId_Currency",
                table: "economy_marketplace_refund_legs",
                columns: new[] { "SettlementId", "Currency" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_outbox_messages_PostingGroupId",
                table: "economy_outbox_messages",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_messages_payload_hash",
                table: "economy_outbox_messages",
                column: "PayloadHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_payout_dispatch_outbox_CompletedAt_AvailableAt_Leas~",
                table: "economy_payout_dispatch_outbox",
                columns: new[] { "CompletedAt", "AvailableAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_payout_dispatch_outbox_OperationId",
                table: "economy_payout_dispatch_outbox",
                column: "OperationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_payout_provider_events_operation_recorded",
                table: "economy_payout_provider_events",
                columns: new[] { "OperationId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_projection_generation_approvals_Generation_ActorId",
                table: "economy_projection_generation_approvals",
                columns: new[] { "Generation", "ActorId" },
                unique: true);

            // Carry the unified rows back into the recreated legacy tables.
            migrationBuilder.Sql("""
                INSERT INTO public.economy_outbox_messages (
                    "Id", "PostingGroupId", "Type", "Payload", "PayloadHash", "OccurredAt")
                SELECT "Id", "PostingGroupId", "MessageType", "Payload", "PayloadHash", "OccurredAt"
                FROM public.economy_outbox WHERE "OutboxKind" = 1;

                INSERT INTO public.economy_marketplace_outbox (
                    "Id", "TenantId", "SettlementId", "MessageType", "Payload", "PayloadHash",
                    "OccurredAt", "PublishedAt", "AttemptCount", "LeaseOwner", "LeaseExpiresAt", "LastError")
                SELECT "Id", "TenantId", "SettlementId", "MessageType", "Payload"::jsonb, "PayloadHash",
                       "OccurredAt", "PublishedAt", "AttemptCount", "LeaseOwner", "LeaseExpiresAt", "LastError"
                FROM public.economy_outbox WHERE "OutboxKind" = 2;

                INSERT INTO public.economy_payout_dispatch_outbox (
                    "Id", "OperationId", "IdempotencyKey", "Payload", "PayloadHash", "CreatedAt",
                    "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount", "CompletedAt", "LastErrorCode")
                SELECT "Id", "OperationId", "IdempotencyKey", "Payload"::jsonb, "PayloadHash", "OccurredAt",
                       "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount", "CompletedAt", "LastError"
                FROM public.economy_outbox WHERE "OutboxKind" = 3;

                INSERT INTO public.economy_admin_withdrawal_dispatch_outbox (
                    "Id", "TenantId", "RunId", "IdempotencyKey", "Payload", "PayloadHash", "CreatedAt",
                    "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount", "CompletedAt", "LastErrorCode")
                SELECT "Id", "TenantId", "RunId", "IdempotencyKey", "Payload"::jsonb, "PayloadHash", "OccurredAt",
                       "AvailableAt", "LeaseExpiresAt", "LeaseOwner", "AttemptCount", "CompletedAt", "LastError"
                FROM public.economy_outbox WHERE "OutboxKind" = 4;

                INSERT INTO public.economy_compliance_outbox (
                    "Id", "EvidenceId", "Type", "Payload", "PayloadHash", "OccurredAt", "DispatchedAt")
                SELECT "Id", "EvidenceId", "MessageType", "Payload"::jsonb, "PayloadHash", "OccurredAt", "DispatchedAt"
                FROM public.economy_outbox WHERE "OutboxKind" = 5;

                INSERT INTO public.economy_compliance_inbox (
                    "Id", "Provider", "Environment", "ProviderEventId", "PayloadHash",
                    "RawObjectReference", "ReceivedAt", "ProcessedAt", "ProcessingError")
                SELECT "Id", "Provider", "Environment", "ProviderEventId", "PayloadHash",
                       "RawObjectReference", "ReceivedAt", "ProcessedAt", "ProcessingError"
                FROM public.economy_inbox WHERE "InboxKind" = 1;

                INSERT INTO public.economy_ad_reward_provider_proof_inbox (
                    "Id", "TenantId", "SessionId", "Network", "ProviderEventId", "PayloadHash",
                    "EvidenceHash", "SignatureVerified", "ReceivedAt", "ProcessedAt", "ProcessingError")
                SELECT "Id", "TenantId", "SessionId", "Provider", "ProviderEventId", "PayloadHash",
                       "EvidenceHash", "SignatureVerified", "ReceivedAt", "ProcessedAt", "ProcessingError"
                FROM public.economy_inbox WHERE "InboxKind" = 2;

                INSERT INTO public.economy_capability_policy_approvals (
                    "Id", "PolicyId", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", "SubjectId", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_approvals WHERE "ApprovalKind" = 1;

                INSERT INTO public.economy_kill_switch_release_approvals (
                    "Id", "KillSwitchId", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", "SubjectId", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_approvals WHERE "ApprovalKind" = 2;

                INSERT INTO public.economy_projection_generation_approvals (
                    "Id", "Generation", "ActorId", "ReauthenticationHash", "ApprovedAt")
                SELECT "Id", "Generation", "ActorId", "ReauthenticationHash", "ApprovedAt"
                FROM public.economy_approvals WHERE "ApprovalKind" = 3;

                INSERT INTO public.economy_payout_provider_events (
                    "EventId", "EventHash", "OperationId", "ResultingState", "RecordedAt")
                SELECT "EventId", "EventHash", "OperationId", "ResultingState", "RecordedAt"
                FROM public.economy_provider_events WHERE "ProviderEventKind" = 1;

                INSERT INTO public.economy_admin_withdrawal_provider_events (
                    "TenantId", "EventId", "EventHash", "RunId", "RecordedAt")
                SELECT "TenantId", "EventId", "EventHash", "RunId", "RecordedAt"
                FROM public.economy_provider_events WHERE "ProviderEventKind" = 2;

                INSERT INTO public.economy_ad_reward_session_events (
                    "Id", "SessionId", "Sequence", "State", "EvidenceHash", "OccurredAt")
                SELECT "Id", "SessionId", "Sequence", "State", "EvidenceHash", "OccurredAt"
                FROM public.economy_ad_reward_session_timeline WHERE "TimelineKind" = 1;

                INSERT INTO public.economy_ad_reward_playback_milestones (
                    "Id", "SessionId", "Sequence", "Percentage", "ObservedAt", "EvidenceHash")
                SELECT "Id", "SessionId", "Sequence", "Percentage", "OccurredAt", "EvidenceHash"
                FROM public.economy_ad_reward_session_timeline WHERE "TimelineKind" = 2;

                INSERT INTO public.economy_marketplace_settlement_legs (
                    "SettlementId", "Currency", "Units", "SellerUnits", "PlatformFeeUnits", "RefundedUnits")
                SELECT "SettlementId", "Currency", "Units", "SellerUnits", "PlatformFeeUnits", "RefundedUnits"
                FROM public.economy_marketplace_value_legs WHERE "LegKind" = 1;

                INSERT INTO public.economy_marketplace_refund_legs (
                    "RefundId", "SettlementId", "Currency", "Units")
                SELECT "RefundId", "SettlementId", "Currency", "Units"
                FROM public.economy_marketplace_value_legs WHERE "LegKind" = 2;
                """);

            // Restore the SECURITY DEFINER writers to their legacy targets
            // (mirrored token rewrites of Up) and put the bridge trigger and
            // the payout reader back onto the legacy relations.
            migrationBuilder.Sql("""
                DO $t1_restore$
                DECLARE
                    definition text;
                BEGIN
                    FOR definition IN
                        SELECT pg_get_functiondef(p.oid)
                        FROM pg_proc p
                        JOIN pg_namespace n ON n.oid = p.pronamespace
                        AND p.prokind = 'f'
                        WHERE n.nspname = 'economy_private'
                          AND (pg_get_functiondef(p.oid) LIKE '%public.economy_outbox%'
                               OR pg_get_functiondef(p.oid) LIKE '%public.economy_provider_events%'
                               OR pg_get_functiondef(p.oid) LIKE '%public.economy_marketplace_value_legs%')
                    LOOP
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_outbox \(\s*"Id", "PostingGroupId", "MessageType"',
                            'INSERT INTO public.economy_outbox_messages ("Id", "PostingGroupId", "Type"');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_outbox \("OutboxKind",\s*"Id", "TenantId", "SettlementId",',
                            'INSERT INTO public.economy_marketplace_outbox ("Id", "TenantId", "SettlementId",');
                        definition := replace(definition,
                            '''entitlement-grant-outbox''), 2,',
                            '''entitlement-grant-outbox''),');
                        definition := replace(definition,
                            '''entitlement-revoke-outbox''), 2,',
                            '''entitlement-revoke-outbox''),');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_provider_events \("ProviderEventKind",\s*"EventId", "EventHash", "OperationId",',
                            'INSERT INTO public.economy_payout_provider_events ("EventId", "EventHash", "OperationId",');
                        definition := replace(definition,
                            '1, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_operation_id, p_state,',
                            'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_operation_id, p_state,');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_provider_events \("ProviderEventKind",\s*"TenantId", "EventId", "EventHash", "RunId",',
                            'INSERT INTO public.economy_admin_withdrawal_provider_events ("TenantId", "EventId", "EventHash", "RunId",');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_provider_events \("ProviderEventKind",\s*"EventId", "EventHash", "RunId",',
                            'INSERT INTO public.economy_admin_withdrawal_provider_events ("EventId", "EventHash", "RunId",');
                        definition := replace(definition,
                            '2, p_tenant_id, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);',
                            'p_tenant_id, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_marketplace_value_legs \("Id", "LegKind",\s*"SettlementId", "Currency",',
                            'INSERT INTO public.economy_marketplace_settlement_legs ("SettlementId", "Currency",');
                        definition := replace(definition,
                            'SELECT gen_random_uuid(), 1, p_settlement_id, leg.currency,',
                            'SELECT p_settlement_id, leg.currency,');
                        definition := regexp_replace(definition,
                            'INSERT INTO public\.economy_marketplace_value_legs \("Id", "LegKind",\s*"RefundId",',
                            'INSERT INTO public.economy_marketplace_refund_legs ("RefundId",');
                        definition := replace(definition,
                            'SELECT gen_random_uuid(), 2, p_refund_id, p_settlement_id, leg.currency, leg.units',
                            'SELECT p_refund_id, p_settlement_id, leg.currency, leg.units');
                        definition := replace(definition,
                            'UPDATE public.economy_marketplace_value_legs leg',
                            'UPDATE public.economy_marketplace_settlement_legs leg');
                        definition := replace(definition,
                            'WHERE leg."LegKind" = 1 AND leg."SettlementId" = p_settlement_id',
                            'WHERE leg."SettlementId" = p_settlement_id');
                        definition := replace(definition,
                            'LEFT JOIN public.economy_marketplace_value_legs persisted',
                            'LEFT JOIN public.economy_marketplace_settlement_legs persisted');
                        definition := replace(definition,
                            'FROM public.economy_marketplace_value_legs persisted',
                            'FROM public.economy_marketplace_settlement_legs persisted');
                        definition := replace(definition,
                            'ON persisted."LegKind" = 1 AND persisted."SettlementId" = p_settlement_id',
                            'ON persisted."SettlementId" = p_settlement_id');
                        definition := replace(definition,
                            'WHERE persisted."LegKind" = 1 AND persisted."SettlementId" = p_settlement_id',
                            'WHERE persisted."SettlementId" = p_settlement_id');
                        definition := replace(definition,
                            '2, pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);',
                            'pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);');
                        EXECUTE definition;
                    END LOOP;
                END
                $t1_restore$;

                DROP TRIGGER IF EXISTS trg_bridge_economy_posting_outbox_v1
                    ON public.economy_outbox;
                CREATE TRIGGER trg_bridge_economy_posting_outbox_v1
                    AFTER INSERT ON public.economy_outbox_messages
                    FOR EACH ROW
                    WHEN (NEW."Type" = 'economy.posting.accepted.v1')
                    EXECUTE FUNCTION public.bridge_economy_posting_outbox_v1();

                DROP FUNCTION IF EXISTS economy_private.read_payout_provider_event_v1(text);
                CREATE FUNCTION economy_private.read_payout_provider_event_v1(p_event_id text)
                RETURNS SETOF public.economy_payout_provider_events
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_payout_provider_events
                    WHERE "EventId" = pg_catalog.btrim(p_event_id)
                $function$;
                ALTER FUNCTION economy_private.read_payout_provider_event_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_payout_provider_event_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_payout_provider_event_v1(text)
                    TO gameguild_economy_writer;

                DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_provider_event_v1(text);
                CREATE FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(p_event_id text)
                RETURNS SETOF public.economy_admin_withdrawal_provider_events
                LANGUAGE sql STABLE SECURITY DEFINER
                SET search_path = pg_catalog, economy_private
                AS $function$
                    SELECT * FROM public.economy_admin_withdrawal_provider_events
                    WHERE "EventId" = pg_catalog.btrim(p_event_id)
                $function$;
                ALTER FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text)
                    OWNER TO gameguild_economy_procedure_owner;
                REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text) FROM PUBLIC;
                GRANT EXECUTE ON FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text)
                    TO gameguild_economy_writer;

                DROP TRIGGER IF EXISTS deny_provider_event_mutation
                    ON public.economy_provider_events;
                CREATE TRIGGER deny_payout_provider_event_mutation
                    BEFORE UPDATE OR DELETE ON public.economy_payout_provider_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
                CREATE TRIGGER deny_admin_withdrawal_provider_event_mutation
                    BEFORE UPDATE OR DELETE ON public.economy_admin_withdrawal_provider_events
                    FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();

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
                name: "economy_ad_reward_session_timeline");

            migrationBuilder.DropTable(
                name: "economy_approvals");

            migrationBuilder.DropTable(
                name: "economy_inbox");

            migrationBuilder.DropTable(
                name: "economy_marketplace_value_legs");

            migrationBuilder.DropTable(
                name: "economy_outbox");

            migrationBuilder.DropTable(
                name: "economy_provider_events");
        }
    }
}
