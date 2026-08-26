using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AllowPayoutProviderDispatchBinding
{
    private static void AllowProviderDispatchBinding(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.transition_payout_operation_v1(
                p_id uuid, p_expected_version bigint, p_state integer, p_dispatch_snapshot_hash text,
                p_provider_payout_id text, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current public.economy_payout_operations%ROWTYPE;
                next_dispatch_snapshot_hash text;
                next_provider_payout_id text;
            BEGIN
                SELECT * INTO current FROM public.economy_payout_operations WHERE "Id" = p_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout operation was not found' USING ERRCODE = 'P0002';
                END IF;
                IF current."Version" <> p_expected_version OR p_updated_at < current."UpdatedAt" THEN
                    RAISE EXCEPTION 'payout operation version is stale' USING ERRCODE = '40001';
                END IF;
                IF NOT (
                    (current."State" = 1 AND p_state IN (2, 6)) OR
                    (current."State" = 2 AND p_state IN (2, 3, 4, 5)) OR
                    (current."State" = 3 AND p_state IN (4, 5))) THEN
                    RAISE EXCEPTION 'payout operation transition is invalid' USING ERRCODE = '23514';
                END IF;

                next_dispatch_snapshot_hash := COALESCE(p_dispatch_snapshot_hash, current."DispatchSnapshotHash");
                next_provider_payout_id := COALESCE(p_provider_payout_id, current."ProviderPayoutId");
                IF p_state BETWEEN 2 AND 6 AND pg_catalog.length(pg_catalog.btrim(COALESCE(next_dispatch_snapshot_hash, ''))) = 0 THEN
                    RAISE EXCEPTION 'payout dispatch snapshot is required' USING ERRCODE = '23514';
                END IF;
                IF current."DispatchSnapshotHash" IS NOT NULL
                   AND current."DispatchSnapshotHash" IS DISTINCT FROM next_dispatch_snapshot_hash THEN
                    RAISE EXCEPTION 'payout dispatch snapshot is immutable' USING ERRCODE = '23514';
                END IF;
                IF current."ProviderPayoutId" IS NOT NULL
                   AND current."ProviderPayoutId" IS DISTINCT FROM next_provider_payout_id THEN
                    RAISE EXCEPTION 'payout provider binding is immutable' USING ERRCODE = '23514';
                END IF;

                UPDATE public.economy_payout_operations
                SET "State" = p_state,
                    "DispatchSnapshotHash" = pg_catalog.btrim(next_dispatch_snapshot_hash),
                    "ProviderPayoutId" = NULLIF(pg_catalog.btrim(COALESCE(next_provider_payout_id, '')), ''),
                    "Version" = current."Version" + 1,
                    "UpdatedAt" = p_updated_at
                WHERE "Id" = p_id;
            END
            $function$;
            """);
    }

    private static void RestoreOriginalPayoutTransitions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.transition_payout_operation_v1(
                p_id uuid, p_expected_version bigint, p_state integer, p_dispatch_snapshot_hash text,
                p_provider_payout_id text, p_updated_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current public.economy_payout_operations%ROWTYPE;
                next_dispatch_snapshot_hash text;
                next_provider_payout_id text;
            BEGIN
                SELECT * INTO current FROM public.economy_payout_operations WHERE "Id" = p_id FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout operation was not found' USING ERRCODE = 'P0002';
                END IF;
                IF current."Version" <> p_expected_version OR p_updated_at < current."UpdatedAt" THEN
                    RAISE EXCEPTION 'payout operation version is stale' USING ERRCODE = '40001';
                END IF;
                IF NOT (
                    (current."State" = 1 AND p_state IN (2, 6)) OR
                    (current."State" = 2 AND p_state IN (3, 4, 5)) OR
                    (current."State" = 3 AND p_state IN (4, 5))) THEN
                    RAISE EXCEPTION 'payout operation transition is invalid' USING ERRCODE = '23514';
                END IF;

                next_dispatch_snapshot_hash := COALESCE(p_dispatch_snapshot_hash, current."DispatchSnapshotHash");
                next_provider_payout_id := COALESCE(p_provider_payout_id, current."ProviderPayoutId");
                IF p_state BETWEEN 2 AND 6 AND pg_catalog.length(pg_catalog.btrim(COALESCE(next_dispatch_snapshot_hash, ''))) = 0 THEN
                    RAISE EXCEPTION 'payout dispatch snapshot is required' USING ERRCODE = '23514';
                END IF;
                IF current."DispatchSnapshotHash" IS NOT NULL
                   AND current."DispatchSnapshotHash" IS DISTINCT FROM next_dispatch_snapshot_hash THEN
                    RAISE EXCEPTION 'payout dispatch snapshot is immutable' USING ERRCODE = '23514';
                END IF;
                IF current."ProviderPayoutId" IS NOT NULL
                   AND current."ProviderPayoutId" IS DISTINCT FROM next_provider_payout_id THEN
                    RAISE EXCEPTION 'payout provider binding is immutable' USING ERRCODE = '23514';
                END IF;

                UPDATE public.economy_payout_operations
                SET "State" = p_state,
                    "DispatchSnapshotHash" = pg_catalog.btrim(next_dispatch_snapshot_hash),
                    "ProviderPayoutId" = NULLIF(pg_catalog.btrim(COALESCE(next_provider_payout_id, '')), ''),
                    "Version" = current."Version" + 1,
                    "UpdatedAt" = p_updated_at
                WHERE "Id" = p_id;
            END
            $function$;
            """);
    }
}
