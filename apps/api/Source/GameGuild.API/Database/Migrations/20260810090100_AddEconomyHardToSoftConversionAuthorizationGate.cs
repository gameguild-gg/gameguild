using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810090100_AddEconomyHardToSoftConversionAuthorizationGate")]
public partial class AddEconomyHardToSoftConversionAuthorizationGate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.post_authorized_hard_to_soft_conversion_v1(
                p_capability_id uuid,
                p_actor_id uuid,
                p_tenant_id uuid,
                p_principal_posting_id uuid,
                p_fee_posting_id uuid,
                p_idempotency_key text,
                p_policy_version bigint,
                p_reserve_version bigint,
                p_risk_decision_id uuid,
                p_risk_operation_fingerprint text,
                p_expected_counter_version bigint,
                p_wallet_id uuid,
                p_output_lot_id uuid,
                p_authorized_root_ids uuid[],
                p_principal_hard_units bigint,
                p_fee_hard_units bigint,
                p_requested_at timestamptz,
                p_dispatch_snapshot_hash text)
            RETURNS TABLE(posting_id uuid, journal_sequence bigint, journal_hash text, duplicate boolean)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_authorized_root_ids IS NULL OR cardinality(p_authorized_root_ids) = 0
                   OR EXISTS (
                       SELECT root_id
                       FROM unnest(p_authorized_root_ids) root_id
                       GROUP BY root_id
                       HAVING count(*) > 1) THEN
                    RAISE EXCEPTION 'conversion root authorization is invalid' USING ERRCODE = '22023';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM public.economy_hard_to_soft_conversion_operations operation
                    WHERE operation."IdempotencyKey" = btrim(p_idempotency_key)) THEN
                    IF EXISTS (
                        SELECT DISTINCT reservation."RootSourceStampId"
                        FROM public.economy_fragment_reservations reservation
                        WHERE reservation."OperationId" = p_principal_posting_id
                        EXCEPT
                        SELECT root_id FROM unnest(p_authorized_root_ids) root_id)
                       OR EXISTS (
                        SELECT root_id FROM unnest(p_authorized_root_ids) root_id
                        EXCEPT
                        SELECT DISTINCT reservation."RootSourceStampId"
                        FROM public.economy_fragment_reservations reservation
                        WHERE reservation."OperationId" = p_principal_posting_id) THEN
                        RAISE EXCEPTION 'conversion source roots do not match the authorization' USING ERRCODE = '42501';
                    END IF;
                ELSE
                    PERFORM economy_private.reserve_fifo_fragments_v1(
                        p_principal_posting_id,
                        p_wallet_id,
                        1,
                        1,
                        p_principal_hard_units + p_fee_hard_units,
                        3,
                        p_requested_at);
                    IF EXISTS (
                        SELECT DISTINCT reservation."RootSourceStampId"
                        FROM public.economy_fragment_reservations reservation
                        WHERE reservation."OperationId" = p_principal_posting_id
                        EXCEPT
                        SELECT root_id FROM unnest(p_authorized_root_ids) root_id)
                       OR EXISTS (
                        SELECT root_id FROM unnest(p_authorized_root_ids) root_id
                        EXCEPT
                        SELECT DISTINCT reservation."RootSourceStampId"
                        FROM public.economy_fragment_reservations reservation
                        WHERE reservation."OperationId" = p_principal_posting_id) THEN
                        RAISE EXCEPTION 'conversion source roots do not match the authorization' USING ERRCODE = '42501';
                    END IF;
                END IF;

                RETURN QUERY
                SELECT * FROM economy_private.post_hard_to_soft_conversion_v1(
                    p_capability_id,
                    p_actor_id,
                    p_tenant_id,
                    p_principal_posting_id,
                    p_fee_posting_id,
                    p_idempotency_key,
                    p_policy_version,
                    p_reserve_version,
                    p_risk_decision_id,
                    p_risk_operation_fingerprint,
                    p_expected_counter_version,
                    p_wallet_id,
                    p_output_lot_id,
                    p_principal_hard_units,
                    p_fee_hard_units,
                    p_requested_at,
                    p_dispatch_snapshot_hash);
            END
            $function$;

            ALTER FUNCTION economy_private.post_authorized_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,uuid,text,bigint,bigint,uuid,text,bigint,uuid,uuid,uuid[],bigint,bigint,timestamptz,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.post_authorized_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,uuid,text,bigint,bigint,uuid,text,bigint,uuid,uuid,uuid[],bigint,bigint,timestamptz,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.post_authorized_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,uuid,text,bigint,bigint,uuid,text,bigint,uuid,uuid,uuid[],bigint,bigint,timestamptz,text)
                TO gameguild_economy_writer;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.post_authorized_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,uuid,text,bigint,bigint,uuid,text,bigint,uuid,uuid,uuid[],bigint,bigint,timestamptz,text);
            """);
    }
}
