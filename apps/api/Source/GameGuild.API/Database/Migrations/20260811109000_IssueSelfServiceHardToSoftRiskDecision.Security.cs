using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class IssueSelfServiceHardToSoftRiskDecision
{
    private static void InstallSelfServiceHardToSoftRiskDecisionIssuer(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                p_actor_id uuid,
                p_tenant_id uuid,
                p_wallet_id uuid,
                p_reservation_operation_id uuid,
                p_idempotency_key text,
                p_fee_hard_units bigint,
                p_total_hard_units bigint,
                p_max_daily_hard_units bigint,
                p_external_evidence text,
                p_requested_at timestamptz,
                p_expires_at timestamptz)
            RETURNS TABLE(risk_decision_id uuid, source_roots text)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                normalized_key text;
                evidence jsonb;
                operation_fingerprint text;
                existing_decision record;
                reserve_head record;
                counter_record record;
                selected_roots uuid[];
                selected_counter_id uuid;
                selected_decision_id uuid;
                counter_subject_hash text;
                day_started_at timestamptz;
                day_ends_at timestamptz;
            BEGIN
                IF p_actor_id IS NULL OR p_tenant_id IS NULL OR p_wallet_id IS NULL
                   OR p_reservation_operation_id IS NULL OR p_total_hard_units <= 0
                   OR p_fee_hard_units < 0 OR p_fee_hard_units > p_total_hard_units
                   OR p_max_daily_hard_units <= 0 OR p_requested_at IS NULL OR p_expires_at IS NULL
                   OR p_expires_at <= p_requested_at OR p_expires_at > p_requested_at + interval '15 minutes'
                   OR length(btrim(p_idempotency_key)) = 0 OR length(btrim(p_external_evidence)) = 0 THEN
                    RAISE EXCEPTION 'self-service conversion risk decision arguments are invalid' USING ERRCODE = '22023';
                END IF;

                normalized_key := btrim(p_idempotency_key);
                IF p_total_hard_units > p_max_daily_hard_units THEN
                    RAISE EXCEPTION 'self-service conversion exceeds the configured daily risk limit' USING ERRCODE = '22003';
                END IF;

                BEGIN
                    evidence := p_external_evidence::jsonb;
                EXCEPTION WHEN OTHERS THEN
                    RAISE EXCEPTION 'self-service conversion external evidence is malformed' USING ERRCODE = '22023';
                END;
                IF jsonb_typeof(evidence) <> 'array'
                   OR NOT EXISTS (
                       SELECT 1
                       FROM jsonb_array_elements(evidence) item
                       WHERE (item->>'source')::integer = 1
                         AND (item->>'outcome')::integer = 1
                         AND COALESCE((item->>'isAuditable')::boolean, false)
                         AND NULLIF(btrim(item->>'version'), '') IS NOT NULL
                         AND NULLIF(btrim(item->>'evidenceHash'), '') IS NOT NULL
                         AND (item->>'issuedAt')::timestamptz <= p_requested_at
                         AND (item->>'expiresAt')::timestamptz > p_requested_at)
                   OR NOT EXISTS (
                       SELECT 1
                       FROM jsonb_array_elements(evidence) item
                       WHERE (item->>'source')::integer = 2
                         AND (item->>'outcome')::integer = 1
                         AND COALESCE((item->>'isAuditable')::boolean, false)
                         AND NULLIF(btrim(item->>'version'), '') IS NOT NULL
                         AND NULLIF(btrim(item->>'evidenceHash'), '') IS NOT NULL
                         AND (item->>'issuedAt')::timestamptz <= p_requested_at
                         AND (item->>'expiresAt')::timestamptz > p_requested_at) THEN
                    RAISE EXCEPTION 'self-service conversion requires current auditable allow evidence from financial-crime and trust-safety controls' USING ERRCODE = '42501';
                END IF;

                operation_fingerprint := encode(public.digest(convert_to(jsonb_build_object(
                    'actorId', p_actor_id,
                    'tenantId', p_tenant_id,
                    'walletId', p_wallet_id,
                    'reservationOperationId', p_reservation_operation_id,
                    'idempotencyKey', normalized_key,
                    'feeHardUnits', p_fee_hard_units,
                    'totalHardUnits', p_total_hard_units)::text, 'UTF8'), 'sha256'), 'hex');
                PERFORM pg_advisory_xact_lock(hashtextextended('economy:hard-to-soft:' || p_wallet_id::text || ':' || normalized_key, 0));

                SELECT decision."Id", decision."OperationFingerprint", decision."SourceRoots"
                INTO existing_decision
                FROM public.economy_risk_decisions decision
                WHERE decision."TemplateKind" = 5
                  AND decision."SourceWalletId" = p_wallet_id
                  AND decision."DestinationWalletId" = p_wallet_id
                  AND decision."IdempotencyKey" = normalized_key
                ORDER BY decision."IssuedAt" DESC, decision."Id" DESC
                LIMIT 1
                FOR UPDATE;
                IF FOUND THEN
                    IF existing_decision."OperationFingerprint" <> operation_fingerprint THEN
                        RAISE EXCEPTION 'self-service conversion idempotency key is bound to another request' USING ERRCODE = '23505';
                    END IF;
                    risk_decision_id := existing_decision."Id";
                    source_roots := existing_decision."SourceRoots"::text;
                    RETURN NEXT;
                    RETURN;
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

                PERFORM 1
                FROM public.economy_registered_capabilities capability
                WHERE capability."IsEnabled"
                  AND capability."RevokedAt" IS NULL
                  AND capability."AllowedTemplateKinds" @> jsonb_build_array(5)
                  AND (p_fee_hard_units = 0 OR capability."AllowedTemplateKinds" @> jsonb_build_array(17))
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'no active hard-to-soft conversion capability is registered' USING ERRCODE = '42501';
                END IF;

                SELECT head."Version", head."PolicyVersion", head."AuthorizationEpoch"
                INTO reserve_head
                FROM public.economy_reserve_heads head
                WHERE head."IsActive"
                  AND head."Coverage" = 1
                  AND head."ObservedAt" <= p_requested_at
                  AND head."ExpiresAt" > p_requested_at
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'no current covered reserve head is available for self-service conversion' USING ERRCODE = '42501';
                END IF;

                SELECT ARRAY(
                    SELECT DISTINCT reservation.root_source_stamp_id
                    FROM economy_private.reserve_fifo_fragments_v1(
                        p_reservation_operation_id,
                        p_wallet_id,
                        1,
                        1,
                        p_total_hard_units,
                        3,
                        p_requested_at) reservation
                    ORDER BY reservation.root_source_stamp_id)
                INTO selected_roots;
                IF selected_roots IS NULL OR cardinality(selected_roots) = 0 THEN
                    RAISE EXCEPTION 'self-service conversion could not reserve confirmed FIFO fragments' USING ERRCODE = '42501';
                END IF;

                selected_decision_id := gen_random_uuid();
                day_started_at := date_trunc('day', p_requested_at AT TIME ZONE 'UTC') AT TIME ZONE 'UTC';
                day_ends_at := day_started_at + interval '1 day';
                counter_subject_hash := encode(public.digest(convert_to(
                    'economy:hard-to-soft-wallet:' || p_wallet_id::text, 'UTF8'), 'sha256'), 'hex');

                INSERT INTO public.economy_risk_decisions (
                    "Id", "Outcome", "OperationFingerprint", "IdempotencyKey", "ActorHash", "TemplateKind",
                    "SourceWalletId", "DestinationWalletId", "Currency", "AmountUnits", "CurrencyLegs", "SourceRoots",
                    "ProviderReferenceHash", "PolicyVersion", "ReserveVersion", "ReserveAuthorizationEpoch", "FeatureVersion",
                    "KillSwitchEpoch", "CounterVersion", "EntityGraphVersion", "EntityGraphEvidenceHash", "ReasonCodes", "IssuedAt", "ExpiresAt")
                VALUES (
                    selected_decision_id, 1, operation_fingerprint, normalized_key,
                    encode(public.digest(convert_to('economy:actor:' || p_actor_id::text || ':' || p_tenant_id::text, 'UTF8'), 'sha256'), 'hex'),
                    5, p_wallet_id, p_wallet_id, 1, p_total_hard_units,
                    jsonb_build_array(jsonb_build_object('currency', 1, 'units', p_total_hard_units)),
                    to_jsonb(selected_roots),
                    encode(public.digest(convert_to(evidence::text, 'UTF8'), 'sha256'), 'hex'),
                    reserve_head."PolicyVersion", reserve_head."Version", reserve_head."AuthorizationEpoch", 1,
                    0, 1, 0,
                    encode(public.digest(convert_to('economy:entity:' || p_actor_id::text || ':' || p_tenant_id::text || ':' || p_wallet_id::text, 'UTF8'), 'sha256'), 'hex'),
                    jsonb_build_array(1), p_requested_at, p_expires_at);

                INSERT INTO public.economy_risk_counters (
                    "Id", "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt", "WindowEndsAt",
                    "CounterVersion", "MaxUnits", "UsedUnits", "UpdatedAt")
                VALUES (
                    gen_random_uuid(), 1, counter_subject_hash, 5, 1, day_started_at, day_ends_at,
                    1, p_max_daily_hard_units, 0, p_requested_at)
                ON CONFLICT ("Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt") DO NOTHING;

                SELECT counter."Id", counter."CounterVersion", counter."MaxUnits", counter."WindowEndsAt"
                INTO counter_record
                FROM public.economy_risk_counters counter
                WHERE counter."Dimension" = 1
                  AND counter."SubjectHash" = counter_subject_hash
                  AND counter."Operation" = 5
                  AND counter."Currency" = 1
                  AND counter."WindowStartedAt" = day_started_at
                FOR UPDATE;
                IF NOT FOUND OR counter_record."CounterVersion" <> 1
                   OR counter_record."MaxUnits" <> p_max_daily_hard_units
                   OR counter_record."WindowEndsAt" <> day_ends_at THEN
                    RAISE EXCEPTION 'durable self-service conversion risk counter does not match the active policy' USING ERRCODE = '42501';
                END IF;

                PERFORM economy_private.reserve_risk_counter_v1(
                    gen_random_uuid(), selected_decision_id, counter_record."Id", counter_record."CounterVersion",
                    p_total_hard_units, p_requested_at);

                INSERT INTO public.economy_risk_audit_evidence (
                    "Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")
                SELECT gen_random_uuid(), selected_decision_id, 'external-risk-evidence', operation_fingerprint,
                       item->>'evidenceHash', item, p_requested_at
                FROM jsonb_array_elements(evidence) item;

                risk_decision_id := selected_decision_id;
                source_roots := to_jsonb(selected_roots)::text;
                RETURN NEXT;
            END
            $function$;

            ALTER FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveSelfServiceHardToSoftRiskDecisionIssuer(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz);
            """);
    }
}
