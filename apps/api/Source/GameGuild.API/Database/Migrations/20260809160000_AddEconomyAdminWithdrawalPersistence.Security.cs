using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyAdminWithdrawalPersistence
{
    private static void InstallAdminWithdrawalSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.create_admin_withdrawal_run_v1(
                p_id uuid, p_idempotency_key text, p_request_hash text, p_period_start date,
                p_requested_by uuid, p_platform_fee_wallet_id uuid, p_amount_units bigint,
                p_source_asset_key text, p_destination_hash text, p_state integer,
                p_version bigint, p_fencing_token bigint, p_execution_epoch bigint,
                p_reserve_version bigint, p_reserve_authorization_epoch bigint,
                p_policy_version bigint, p_dispatch_snapshot_hash text,
                p_provider_transfer_id text, p_created_at timestamptz, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_id IS NULL OR p_requested_by IS NULL OR p_platform_fee_wallet_id IS NULL
                   OR p_period_start IS NULL OR p_amount_units <= 0
                   OR p_version <= 0 OR p_fencing_token <= 0 OR p_execution_epoch <= 0
                   OR p_reserve_version <= 0 OR p_reserve_authorization_epoch <= 0
                   OR p_policy_version <= 0 OR p_created_at IS NULL OR p_updated_at < p_created_at
                   OR p_state <> 1 OR p_dispatch_snapshot_hash IS NOT NULL OR p_provider_transfer_id IS NOT NULL
                   OR pg_catalog.length(pg_catalog.btrim(p_idempotency_key)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_request_hash)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_source_asset_key)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_destination_hash)) = 0 THEN
                    RAISE EXCEPTION 'admin withdrawal creation violates immutable request rules'
                        USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_admin_withdrawal_runs (
                    "Id", "IdempotencyKey", "RequestHash", "PeriodStart", "RequestedBy", "ApprovedBy",
                    "PlatformFeeWalletId", "AmountUnits", "SourceAssetKey", "DestinationHash", "State",
                    "Version", "FencingToken", "ExecutionEpoch", "ReserveVersion",
                    "ReserveAuthorizationEpoch", "PolicyVersion", "DispatchSnapshotHash",
                    "ProviderTransferId", "CreatedAt", "UpdatedAt")
                VALUES (
                    p_id, pg_catalog.btrim(p_idempotency_key), pg_catalog.btrim(p_request_hash), p_period_start,
                    p_requested_by, NULL, p_platform_fee_wallet_id, p_amount_units,
                    pg_catalog.btrim(p_source_asset_key), pg_catalog.btrim(p_destination_hash), p_state,
                    p_version, p_fencing_token, p_execution_epoch, p_reserve_version,
                    p_reserve_authorization_epoch, p_policy_version, NULL, NULL, p_created_at, p_updated_at);
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'admin withdrawal run overlaps an existing request or period'
                    USING ERRCODE = '23505';
            END
            $function$;
            """);
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.transition_admin_withdrawal_run_v1(
                p_id uuid, p_expected_version bigint, p_state integer, p_approved_by uuid,
                p_dispatch_snapshot_hash text, p_provider_transfer_id text, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current public.economy_admin_withdrawal_runs%ROWTYPE;
                transition_allowed boolean := false;
            BEGIN
                SELECT * INTO current FROM public.economy_admin_withdrawal_runs
                WHERE "Id" = p_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'admin withdrawal run was not found' USING ERRCODE = 'P0002';
                END IF;
                IF current."Version" <> p_expected_version OR p_updated_at < current."UpdatedAt" THEN
                    RAISE EXCEPTION 'admin withdrawal run version is stale' USING ERRCODE = '40001';
                END IF;

                transition_allowed :=
                    (current."State" = 1 AND p_state = 2)
                    OR (current."State" = 2 AND p_state = 3)
                    OR (current."State" = 3 AND p_state IN (3, 4))
                    OR (current."State" = 4 AND p_state IN (4, 5, 6, 7));
                IF NOT transition_allowed THEN
                    RAISE EXCEPTION 'admin withdrawal state transition is invalid' USING ERRCODE = '23514';
                END IF;
                IF p_state >= 2 AND p_approved_by IS NULL THEN
                    RAISE EXCEPTION 'admin withdrawal approval is required' USING ERRCODE = '23514';
                END IF;
                IF p_state >= 3
                   AND pg_catalog.length(pg_catalog.btrim(COALESCE(p_dispatch_snapshot_hash, ''))) = 0 THEN
                    RAISE EXCEPTION 'admin withdrawal dispatch evidence is required' USING ERRCODE = '23514';
                END IF;
                IF p_state = 3 AND current."DispatchSnapshotHash" IS NOT NULL
                   AND p_dispatch_snapshot_hash IS DISTINCT FROM current."DispatchSnapshotHash" THEN
                    RAISE EXCEPTION 'admin withdrawal dispatch snapshot is immutable' USING ERRCODE = '23514';
                END IF;
                IF p_provider_transfer_id IS NOT NULL AND current."ProviderTransferId" IS NOT NULL
                   AND p_provider_transfer_id IS DISTINCT FROM current."ProviderTransferId" THEN
                    RAISE EXCEPTION 'admin withdrawal provider evidence is immutable' USING ERRCODE = '23514';
                END IF;

                UPDATE public.economy_admin_withdrawal_runs
                SET "State" = p_state,
                    "ApprovedBy" = COALESCE(p_approved_by, current."ApprovedBy"),
                    "DispatchSnapshotHash" = COALESCE(p_dispatch_snapshot_hash, current."DispatchSnapshotHash"),
                    "ProviderTransferId" = COALESCE(p_provider_transfer_id, current."ProviderTransferId"),
                    "Version" = current."Version" + 1,
                    "UpdatedAt" = p_updated_at
                WHERE "Id" = p_id;
            END
            $function$;
            """);
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.complete_admin_withdrawal_provider_event_v1(
                p_event_id text, p_event_hash text, p_run_id uuid, p_expected_version bigint,
                p_state integer, p_provider_transfer_id text, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current public.economy_admin_withdrawal_runs%ROWTYPE;
            BEGIN
                IF pg_catalog.length(pg_catalog.btrim(p_event_id)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_event_hash)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(COALESCE(p_provider_transfer_id, ''))) = 0
                   OR p_state NOT IN (5, 6) THEN
                    RAISE EXCEPTION 'admin withdrawal provider evidence is invalid' USING ERRCODE = '23514';
                END IF;

                SELECT * INTO current FROM public.economy_admin_withdrawal_runs
                WHERE "Id" = p_run_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'admin withdrawal run was not found' USING ERRCODE = 'P0002';
                END IF;
                IF current."Version" <> p_expected_version OR p_updated_at < current."UpdatedAt"
                   OR current."State" NOT IN (3, 4) THEN
                    RAISE EXCEPTION 'admin withdrawal provider event is stale' USING ERRCODE = '40001';
                END IF;
                IF current."ProviderTransferId" IS NOT NULL
                   AND current."ProviderTransferId" IS DISTINCT FROM p_provider_transfer_id THEN
                    RAISE EXCEPTION 'admin withdrawal provider evidence is immutable' USING ERRCODE = '23514';
                END IF;

                INSERT INTO public.economy_admin_withdrawal_provider_events (
                    "EventId", "EventHash", "RunId", "RecordedAt")
                VALUES (
                    pg_catalog.btrim(p_event_id), pg_catalog.btrim(p_event_hash), p_run_id, p_updated_at);

                UPDATE public.economy_admin_withdrawal_runs
                SET "State" = p_state,
                    "ProviderTransferId" = p_provider_transfer_id,
                    "Version" = current."Version" + 1,
                    "UpdatedAt" = p_updated_at
                WHERE "Id" = p_run_id;
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'admin withdrawal provider evidence was already recorded'
                    USING ERRCODE = '23505';
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.append_admin_withdrawal_audit_event_v1(
                p_run_id uuid, p_kind text, p_actor_id uuid, p_evidence text, p_occurred_at timestamptz)
            RETURNS TABLE(
                "RunId" uuid, "Sequence" bigint, "Kind" text, "ActorId" uuid, "Evidence" text,
                "OccurredAt" timestamptz, "PreviousHash" text, "Hash" text)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                previous_hash text;
                next_sequence bigint;
                next_hash text;
            BEGIN
                IF pg_catalog.length(pg_catalog.btrim(p_kind)) = 0
                   OR pg_catalog.length(pg_catalog.btrim(p_evidence)) = 0 THEN
                    RAISE EXCEPTION 'admin withdrawal audit evidence is required' USING ERRCODE = '23514';
                END IF;
                PERFORM 1 FROM public.economy_admin_withdrawal_runs
                WHERE "Id" = p_run_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'admin withdrawal run was not found' USING ERRCODE = 'P0002';
                END IF;
                SELECT COALESCE(MAX("Sequence"), 0) + 1,
                       COALESCE((array_agg("Hash" ORDER BY "Sequence" DESC))[1], repeat('0', 64))
                INTO next_sequence, previous_hash
                FROM public.economy_admin_withdrawal_audit_events
                WHERE "RunId" = p_run_id;
                next_hash := encode(public.digest(convert_to(
                    replace(p_run_id::text, '-', '') || '|' || next_sequence::text || '|' ||
                    pg_catalog.btrim(p_kind) || '|' ||
                    COALESCE(replace(p_actor_id::text, '-', ''), '') || '|' ||
                    pg_catalog.btrim(p_evidence) || '|' ||
                    to_char(p_occurred_at AT TIME ZONE 'UTC', 'YYYY-MM-DD"T"HH24:MI:SS.US"Z"') || '|' ||
                    previous_hash, 'UTF8'), 'sha256'), 'hex');

                INSERT INTO public.economy_admin_withdrawal_audit_events (
                    "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")
                VALUES (
                    p_run_id, next_sequence, pg_catalog.btrim(p_kind), p_actor_id, pg_catalog.btrim(p_evidence),
                    p_occurred_at, previous_hash, next_hash);

                RETURN QUERY SELECT p_run_id, next_sequence, pg_catalog.btrim(p_kind), p_actor_id,
                    pg_catalog.btrim(p_evidence), p_occurred_at, previous_hash, next_hash;
            END
            $function$;
            """);
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(p_id uuid)
            RETURNS SETOF public.economy_admin_withdrawal_runs
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_admin_withdrawal_runs WHERE "Id" = p_id
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(p_idempotency_key text)
            RETURNS SETOF public.economy_admin_withdrawal_runs
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_admin_withdrawal_runs
                WHERE "IdempotencyKey" = pg_catalog.btrim(p_idempotency_key)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(p_period_start date)
            RETURNS SETOF public.economy_admin_withdrawal_runs
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_admin_withdrawal_runs
                WHERE "PeriodStart" = p_period_start AND "State" NOT IN (6, 7)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(p_event_id text)
            RETURNS SETOF public.economy_admin_withdrawal_provider_events
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_admin_withdrawal_provider_events
                WHERE "EventId" = pg_catalog.btrim(p_event_id)
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(p_run_id uuid)
            RETURNS SETOF public.economy_admin_withdrawal_audit_events
            LANGUAGE sql STABLE SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT * FROM public.economy_admin_withdrawal_audit_events
                WHERE "RunId" = p_run_id ORDER BY "Sequence"
            $function$;
            """);
        migrationBuilder.Sql(
            """
            ALTER FUNCTION economy_private.create_admin_withdrawal_run_v1(
                uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.transition_admin_withdrawal_run_v1(
                uuid,bigint,integer,uuid,text,text,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.complete_admin_withdrawal_provider_event_v1(
                text,text,uuid,bigint,integer,text,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.append_admin_withdrawal_audit_event_v1(
                uuid,text,uuid,text,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                OWNER TO gameguild_economy_procedure_owner;

            REVOKE ALL ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events FROM gameguild_economy_runtime;

            GRANT SELECT ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events TO gameguild_economy_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_admin_withdrawal_runs,
                public.economy_admin_withdrawal_provider_events,
                public.economy_admin_withdrawal_audit_events TO gameguild_economy_migration;

            REVOKE ALL ON FUNCTION economy_private.create_admin_withdrawal_run_v1(
                uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.transition_admin_withdrawal_run_v1(
                uuid,bigint,integer,uuid,text,text,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.complete_admin_withdrawal_provider_event_v1(
                text,text,uuid,bigint,integer,text,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.append_admin_withdrawal_audit_event_v1(
                uuid,text,uuid,text,timestamptz) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_id_v1(uuid) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_run_by_idempotency_v1(text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_active_admin_withdrawal_run_by_period_v1(date) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_provider_event_v1(text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.read_admin_withdrawal_audit_events_v1(uuid) FROM PUBLIC;

            GRANT EXECUTE ON FUNCTION economy_private.create_admin_withdrawal_run_v1(
                uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz),
                economy_private.transition_admin_withdrawal_run_v1(uuid,bigint,integer,uuid,text,text,timestamptz),
                economy_private.complete_admin_withdrawal_provider_event_v1(text,text,uuid,bigint,integer,text,timestamptz),
                economy_private.append_admin_withdrawal_audit_event_v1(uuid,text,uuid,text,timestamptz),
                economy_private.read_admin_withdrawal_run_by_id_v1(uuid),
                economy_private.read_admin_withdrawal_run_by_idempotency_v1(text),
                economy_private.read_active_admin_withdrawal_run_by_period_v1(date),
                economy_private.read_admin_withdrawal_provider_event_v1(text),
                economy_private.read_admin_withdrawal_audit_events_v1(uuid)
                TO gameguild_economy_writer;

            CREATE TRIGGER deny_admin_withdrawal_provider_event_mutation
                BEFORE UPDATE OR DELETE ON public.economy_admin_withdrawal_provider_events
                FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
            CREATE TRIGGER deny_admin_withdrawal_audit_event_mutation
                BEFORE UPDATE OR DELETE ON public.economy_admin_withdrawal_audit_events
                FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1();
            """);
    }

    private static void RemoveAdminWithdrawalSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS deny_admin_withdrawal_provider_event_mutation
                ON public.economy_admin_withdrawal_provider_events;
            DROP TRIGGER IF EXISTS deny_admin_withdrawal_audit_event_mutation
                ON public.economy_admin_withdrawal_audit_events;
            DROP FUNCTION IF EXISTS economy_private.create_admin_withdrawal_run_v1(
                uuid,text,text,date,uuid,uuid,bigint,text,text,integer,bigint,bigint,bigint,bigint,bigint,bigint,text,text,timestamptz,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.transition_admin_withdrawal_run_v1(
                uuid,bigint,integer,uuid,text,text,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.complete_admin_withdrawal_provider_event_v1(
                text,text,uuid,bigint,integer,text,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.append_admin_withdrawal_audit_event_v1(
                uuid,text,uuid,text,timestamptz);
            DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_id_v1(uuid);
            DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_run_by_idempotency_v1(text);
            DROP FUNCTION IF EXISTS economy_private.read_active_admin_withdrawal_run_by_period_v1(date);
            DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_provider_event_v1(text);
            DROP FUNCTION IF EXISTS economy_private.read_admin_withdrawal_audit_events_v1(uuid);
            """);
    }
}
