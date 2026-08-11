using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class HardenPublicEconomyCommandBindings
{
    private static void AddPublicEconomyCommandBindings(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.post_self_service_hard_to_soft_conversion_v1(
                p_actor_id uuid,
                p_tenant_id uuid,
                p_principal_posting_id uuid,
                p_fee_posting_id uuid,
                p_idempotency_key text,
                p_risk_decision_id uuid,
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
            DECLARE
                risk_record record;
                capability_id uuid;
                effective_requested_at timestamptz;
            BEGIN
                IF p_actor_id IS NULL OR p_tenant_id IS NULL OR p_principal_posting_id IS NULL
                   OR p_wallet_id IS NULL OR p_output_lot_id IS NULL OR p_risk_decision_id IS NULL
                   OR p_principal_hard_units <= 0 OR p_fee_hard_units < 0 OR p_requested_at IS NULL
                   OR length(btrim(p_idempotency_key)) = 0
                   OR (p_fee_hard_units = 0 AND p_fee_posting_id IS NOT NULL)
                   OR (p_fee_hard_units > 0 AND p_fee_posting_id IS NULL)
                   OR p_authorized_root_ids IS NULL OR cardinality(p_authorized_root_ids) = 0
                   OR EXISTS (
                        SELECT root_id
                        FROM unnest(p_authorized_root_ids) root_id
                        GROUP BY root_id
                        HAVING count(*) > 1) THEN
                    RAISE EXCEPTION 'self-service conversion arguments are invalid' USING ERRCODE = '22023';
                END IF;

                SELECT operation."CreatedAt" INTO effective_requested_at
                FROM public.economy_hard_to_soft_conversion_operations operation
                WHERE operation."IdempotencyKey" = btrim(p_idempotency_key)
                FOR SHARE;
                IF effective_requested_at IS NULL THEN
                    effective_requested_at := p_requested_at;
                END IF;

                PERFORM 1
                FROM public.economy_wallets wallet
                WHERE wallet."Id" = p_wallet_id
                  AND wallet."OwnerId" = p_actor_id
                  AND wallet."TenantId" = p_tenant_id
                  AND wallet."State" = 1
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'conversion wallet is absent, inactive, or not owned by the authenticated actor' USING ERRCODE = '42501';
                END IF;

                SELECT * INTO risk_record
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                  AND decision."Outcome" = 1
                  AND decision."TemplateKind" = 5
                  AND decision."SourceWalletId" = p_wallet_id
                  AND decision."DestinationWalletId" = p_wallet_id
                  AND decision."Currency" = 1
                  AND decision."AmountUnits" = p_principal_hard_units + p_fee_hard_units
                  AND decision."IdempotencyKey" = btrim(p_idempotency_key)
                  AND decision."IssuedAt" <= effective_requested_at
                  AND decision."ExpiresAt" > effective_requested_at
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'conversion risk decision is absent, expired, or not bound to this self-service request' USING ERRCODE = '42501';
                END IF;

                IF EXISTS (
                    SELECT value::uuid
                    FROM jsonb_array_elements_text(risk_record."SourceRoots") AS roots(value)
                    EXCEPT
                    SELECT root_id
                    FROM unnest(p_authorized_root_ids) AS expected(root_id))
                   OR EXISTS (
                    SELECT root_id
                    FROM unnest(p_authorized_root_ids) AS expected(root_id)
                    EXCEPT
                    SELECT value::uuid
                    FROM jsonb_array_elements_text(risk_record."SourceRoots") AS roots(value)) THEN
                    RAISE EXCEPTION 'conversion source roots do not match the durable risk decision' USING ERRCODE = '42501';
                END IF;

                SELECT capability."Id" INTO capability_id
                FROM public.economy_registered_capabilities capability
                WHERE capability."IsEnabled"
                  AND capability."RevokedAt" IS NULL
                  AND capability."AllowedTemplateKinds" @> jsonb_build_array(5)
                  AND (p_fee_hard_units = 0 OR capability."AllowedTemplateKinds" @> jsonb_build_array(17))
                ORDER BY capability."CreatedAt", capability."Id"
                LIMIT 1
                FOR SHARE;
                IF capability_id IS NULL THEN
                    RAISE EXCEPTION 'no active hard-to-soft conversion capability is registered' USING ERRCODE = '42501';
                END IF;

                RETURN QUERY
                SELECT *
                FROM economy_private.post_authorized_hard_to_soft_conversion_v1(
                    capability_id,
                    p_actor_id,
                    p_tenant_id,
                    p_principal_posting_id,
                    p_fee_posting_id,
                    btrim(p_idempotency_key),
                    risk_record."PolicyVersion",
                    risk_record."ReserveVersion",
                    p_risk_decision_id,
                    risk_record."OperationFingerprint",
                    risk_record."CounterVersion",
                    p_wallet_id,
                    p_output_lot_id,
                    p_authorized_root_ids,
                    p_principal_hard_units,
                    p_fee_hard_units,
                    effective_requested_at,
                    p_dispatch_snapshot_hash);
            END
            $function$;

            ALTER FUNCTION economy_private.post_self_service_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,text,uuid,uuid,uuid,uuid[],bigint,bigint,timestamptz,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.post_self_service_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,text,uuid,uuid,uuid,uuid[],bigint,bigint,timestamptz,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.post_self_service_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,text,uuid,uuid,uuid,uuid[],bigint,bigint,timestamptz,text)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemovePublicEconomyCommandBindings(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.post_self_service_hard_to_soft_conversion_v1(
                uuid,uuid,uuid,uuid,text,uuid,uuid,uuid,uuid[],bigint,bigint,timestamptz,text);
            """);
    }
}