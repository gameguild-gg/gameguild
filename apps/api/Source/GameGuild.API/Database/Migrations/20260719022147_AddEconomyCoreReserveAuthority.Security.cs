using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyCoreReserveAuthority
{
    private static void AddCoreReserveSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.activate_reserve_head_v1(
                p_version bigint,
                p_expected_active_version bigint,
                p_policy_version bigint,
                p_authorization_epoch bigint,
                p_observed_at timestamptz,
                p_expires_at timestamptz,
                p_hard_face_value_usd_minor bigint,
                p_required_hard_reserve_usd_minor bigint,
                p_soft_face_value_usd_nanos bigint,
                p_stressed_expected_redemption_cost_usd_nanos bigint,
                p_required_soft_reserve_usd_nanos bigint,
                p_hard_backing_usd_nanos bigint,
                p_soft_backing_usd_nanos bigint,
                p_coverage integer,
                p_evidence_hash text,
                p_activated_at timestamptz,
                p_asset_allocations jsonb)
            RETURNS bigint
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                current_version bigint;
                current_epoch bigint;
                asset_count bigint;
                distinct_asset_count bigint;
                calculated_hard_backing numeric;
                calculated_soft_backing numeric;
                calculated_coverage integer;
            BEGIN
                PERFORM pg_advisory_xact_lock(pg_catalog.hashtextextended('economy.reserve.head', 0));

                IF p_version <= 0 OR p_policy_version <= 0 OR p_authorization_epoch <= 0 THEN
                    RAISE EXCEPTION 'Reserve version, policy version, and authorization epoch must be positive'
                        USING ERRCODE = '23514';
                END IF;
                IF p_observed_at > p_activated_at OR p_expires_at <= p_observed_at OR p_expires_at <= p_activated_at THEN
                    RAISE EXCEPTION 'Reserve evidence window is invalid or stale'
                        USING ERRCODE = '23514';
                END IF;
                IF p_hard_face_value_usd_minor < 0
                   OR p_required_hard_reserve_usd_minor < p_hard_face_value_usd_minor
                   OR p_soft_face_value_usd_nanos < 0
                   OR p_stressed_expected_redemption_cost_usd_nanos < 0
                   OR p_required_soft_reserve_usd_nanos < GREATEST(
                       p_soft_face_value_usd_nanos, p_stressed_expected_redemption_cost_usd_nanos)
                   OR p_hard_backing_usd_nanos < 0
                   OR p_soft_backing_usd_nanos < 0 THEN
                    RAISE EXCEPTION 'Reserve amounts violate conservative bounds'
                        USING ERRCODE = '23514';
                END IF;
                IF p_coverage NOT IN (1, 2) OR pg_catalog.length(pg_catalog.btrim(p_evidence_hash)) = 0 THEN
                    RAISE EXCEPTION 'Reserve coverage or evidence is invalid'
                        USING ERRCODE = '23514';
                END IF;
                IF p_asset_allocations IS NULL OR pg_catalog.jsonb_typeof(p_asset_allocations) <> 'array' THEN
                    RAISE EXCEPTION 'Reserve asset allocations must be a JSON array'
                        USING ERRCODE = '23514';
                END IF;
                IF EXISTS (
                    SELECT 1
                    FROM pg_catalog.jsonb_array_elements(p_asset_allocations) AS allocation(value)
                    WHERE pg_catalog.jsonb_typeof(allocation.value) <> 'object'
                       OR pg_catalog.length(pg_catalog.btrim(allocation.value ->> 'assetKey')) = 0
                       OR COALESCE(allocation.value ->> 'purpose', '') !~ '^[12]$'
                       OR COALESCE(allocation.value ->> 'eligibleUsdNanos', '') !~ '^[1-9][0-9]*$') THEN
                    RAISE EXCEPTION 'Reserve asset allocation is malformed'
                        USING ERRCODE = '23514';
                END IF;

                SELECT
                    pg_catalog.count(*),
                    pg_catalog.count(DISTINCT allocation.value ->> 'assetKey'),
                    COALESCE(pg_catalog.sum((allocation.value ->> 'eligibleUsdNanos')::numeric)
                        FILTER (WHERE (allocation.value ->> 'purpose')::integer = 1), 0),
                    COALESCE(pg_catalog.sum((allocation.value ->> 'eligibleUsdNanos')::numeric)
                        FILTER (WHERE (allocation.value ->> 'purpose')::integer = 2), 0)
                INTO asset_count, distinct_asset_count, calculated_hard_backing, calculated_soft_backing
                FROM pg_catalog.jsonb_array_elements(p_asset_allocations) AS allocation(value);

                IF asset_count <> distinct_asset_count THEN
                    RAISE EXCEPTION 'One external asset cannot back multiple reserve pools'
                        USING ERRCODE = '23505';
                END IF;
                IF calculated_hard_backing <> p_hard_backing_usd_nanos
                   OR calculated_soft_backing <> p_soft_backing_usd_nanos THEN
                    RAISE EXCEPTION 'Reserve backing totals do not match asset allocations'
                        USING ERRCODE = '23514';
                END IF;

                calculated_coverage := CASE
                    WHEN calculated_hard_backing >= p_required_hard_reserve_usd_minor::numeric * 10000000
                     AND calculated_soft_backing >= p_required_soft_reserve_usd_nanos::numeric
                    THEN 1 ELSE 2 END;
                IF calculated_coverage <> p_coverage THEN
                    RAISE EXCEPTION 'Reserve coverage state does not match calculated backing'
                        USING ERRCODE = '23514';
                END IF;

                SELECT head."Version", head."AuthorizationEpoch"
                INTO current_version, current_epoch
                FROM public.economy_reserve_heads head
                WHERE head."IsActive"
                FOR UPDATE;

                IF current_version IS NULL THEN
                    IF p_expected_active_version IS NOT NULL THEN
                        RAISE EXCEPTION 'Reserve active version changed'
                            USING ERRCODE = '40001';
                    END IF;
                ELSE
                    IF p_expected_active_version IS DISTINCT FROM current_version
                       OR p_version <= current_version THEN
                        RAISE EXCEPTION 'Reserve active version changed'
                            USING ERRCODE = '40001';
                    END IF;
                    IF p_authorization_epoch <= current_epoch THEN
                        RAISE EXCEPTION 'Reserve authorization epoch must increase'
                            USING ERRCODE = '40001';
                    END IF;
                    UPDATE public.economy_reserve_heads
                    SET "IsActive" = FALSE
                    WHERE "Version" = current_version;
                END IF;

                INSERT INTO public.economy_reserve_heads (
                    "Version", "IsActive", "PolicyVersion", "AuthorizationEpoch", "ObservedAt", "ExpiresAt",
                    "HardFaceValueUsdMinor", "RequiredHardReserveUsdMinor", "SoftFaceValueUsdNanos",
                    "StressedExpectedRedemptionCostUsdNanos", "RequiredSoftReserveUsdNanos",
                    "HardBackingUsdNanos", "SoftBackingUsdNanos", "Coverage", "EvidenceHash", "ActivatedAt")
                VALUES (
                    p_version, TRUE, p_policy_version, p_authorization_epoch, p_observed_at, p_expires_at,
                    p_hard_face_value_usd_minor, p_required_hard_reserve_usd_minor, p_soft_face_value_usd_nanos,
                    p_stressed_expected_redemption_cost_usd_nanos, p_required_soft_reserve_usd_nanos,
                    p_hard_backing_usd_nanos, p_soft_backing_usd_nanos, p_coverage,
                    pg_catalog.btrim(p_evidence_hash), p_activated_at);

                INSERT INTO public.economy_reserve_asset_allocations (
                    "Id", "ReserveVersion", "AssetKey", "Purpose", "EligibleUsdNanos")
                SELECT
                    public.gen_random_uuid(),
                    p_version,
                    pg_catalog.btrim(allocation.value ->> 'assetKey'),
                    (allocation.value ->> 'purpose')::integer,
                    (allocation.value ->> 'eligibleUsdNanos')::bigint
                FROM pg_catalog.jsonb_array_elements(p_asset_allocations) AS allocation(value);

                RETURN p_version;
            END
            $function$;

            ALTER FUNCTION economy_private.activate_reserve_head_v1(
                bigint,bigint,bigint,bigint,timestamptz,timestamptz,bigint,bigint,bigint,bigint,
                bigint,bigint,bigint,integer,text,timestamptz,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.activate_reserve_head_v1(
                bigint,bigint,bigint,bigint,timestamptz,timestamptz,bigint,bigint,bigint,bigint,
                bigint,bigint,bigint,integer,text,timestamptz,jsonb) FROM PUBLIC;

            REVOKE ALL ON TABLE public.economy_reserve_heads FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_reserve_asset_allocations FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_reserve_heads FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_reserve_asset_allocations FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_reserve_heads FROM gameguild_economy_runtime;
            REVOKE ALL ON TABLE public.economy_reserve_asset_allocations FROM gameguild_economy_runtime;
            GRANT SELECT ON TABLE public.economy_reserve_heads TO gameguild_economy_runtime;
            GRANT SELECT ON TABLE public.economy_reserve_asset_allocations TO gameguild_economy_runtime;
            GRANT SELECT, INSERT, UPDATE ON TABLE public.economy_reserve_heads TO gameguild_economy_procedure_owner;
            GRANT SELECT, INSERT ON TABLE public.economy_reserve_asset_allocations TO gameguild_economy_procedure_owner;
            GRANT ALL ON TABLE public.economy_reserve_heads TO gameguild_economy_migration;
            GRANT ALL ON TABLE public.economy_reserve_asset_allocations TO gameguild_economy_migration;
            GRANT EXECUTE ON FUNCTION economy_private.activate_reserve_head_v1(
                bigint,bigint,bigint,bigint,timestamptz,timestamptz,bigint,bigint,bigint,bigint,
                bigint,bigint,bigint,integer,text,timestamptz,jsonb) TO gameguild_economy_writer;
            """,
            suppressTransaction: false);
    }

    private static void RemoveCoreReserveSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.activate_reserve_head_v1(
                bigint,bigint,bigint,bigint,timestamptz,timestamptz,bigint,bigint,bigint,bigint,
                bigint,bigint,bigint,integer,text,timestamptz,jsonb);
            """,
            suppressTransaction: false);
    }
}
