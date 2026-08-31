using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddPayoutAuthorizationEvidence
{
    private static void InstallPayoutAuthorizationEvidence(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_payout_authorization_evidence
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON TABLE public.economy_payout_authorization_evidence FROM PUBLIC;
            REVOKE ALL ON TABLE public.economy_payout_authorization_evidence
                FROM gameguild_economy_writer;
            REVOKE ALL ON TABLE public.economy_payout_authorization_evidence
                FROM gameguild_economy_runtime;
            GRANT SELECT, INSERT ON TABLE public.economy_payout_authorization_evidence
                TO gameguild_economy_procedure_owner;

            CREATE OR REPLACE FUNCTION economy_private.append_payout_authorization_evidence_v1(
                p_operation_id uuid,
                p_tenant_id uuid,
                p_actor_id uuid,
                p_phase integer,
                p_risk_decision_id uuid,
                p_reauthentication_evidence_hash text,
                p_operation_fingerprint_hash text,
                p_capability_receipt_id uuid,
                p_capability_receipt_hash text,
                p_recorded_at timestamptz)
            RETURNS void
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                operation public.economy_payout_operations%ROWTYPE;
            BEGIN
                IF p_operation_id IS NULL OR p_tenant_id IS NULL OR p_actor_id IS NULL
                   OR p_risk_decision_id IS NULL OR p_capability_receipt_id IS NULL
                   OR p_phase NOT IN (1, 2) OR p_recorded_at IS NULL
                   OR pg_catalog.length(pg_catalog.btrim(
                        COALESCE(p_reauthentication_evidence_hash, ''))) <> 64
                   OR pg_catalog.length(pg_catalog.btrim(
                        COALESCE(p_operation_fingerprint_hash, ''))) <> 64
                   OR pg_catalog.length(pg_catalog.btrim(
                        COALESCE(p_capability_receipt_hash, ''))) = 0
                   OR pg_catalog.length(p_capability_receipt_hash) > 128 THEN
                    RAISE EXCEPTION 'payout authorization evidence is invalid'
                        USING ERRCODE = '22023';
                END IF;

                SELECT * INTO operation
                FROM public.economy_payout_operations
                WHERE "Id" = p_operation_id
                FOR SHARE;
                IF NOT FOUND OR operation."TenantId" <> p_tenant_id
                   OR operation."ActorId" <> p_actor_id
                   OR operation."RiskDecisionId" <> p_risk_decision_id
                   OR (p_phase = 1 AND operation."State" <> 1)
                   OR (p_phase = 2 AND operation."State" < 2) THEN
                    RAISE EXCEPTION 'payout authorization evidence is not bound to the operation'
                        USING ERRCODE = '42501';
                END IF;

                PERFORM 1
                FROM public.economy_capability_receipts receipt
                JOIN public.economy_capability_receipt_consumptions consumption
                  ON consumption."ReceiptId" = receipt."Id"
                WHERE receipt."Id" = p_capability_receipt_id
                  AND receipt."ReceiptHash" = pg_catalog.btrim(p_capability_receipt_hash)
                  AND receipt."TenantId" = p_tenant_id
                  AND receipt."ActorId" = p_actor_id
                  AND receipt."Capability" = 9
                  AND receipt."RiskDecisionId" = p_risk_decision_id
                  AND pg_catalog.encode(public.digest(
                        pg_catalog.convert_to(receipt."OperationFingerprint", 'UTF8'), 'sha256'),
                        'hex') = pg_catalog.btrim(p_operation_fingerprint_hash)
                  AND receipt."IssuedAt" <= p_recorded_at
                  AND receipt."ExpiresAt" > p_recorded_at
                  AND consumption."TenantId" = p_tenant_id
                  AND consumption."ActorId" = p_actor_id
                  AND consumption."OperationFingerprint" = receipt."OperationFingerprint"
                  AND consumption."KillSwitchEpoch" = receipt."KillSwitchEpoch"
                FOR SHARE OF receipt, consumption;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'payout authorization receipt is absent, stale, or mismatched'
                        USING ERRCODE = '42501';
                END IF;

                INSERT INTO public.economy_payout_authorization_evidence (
                    "OperationId", "Phase", "TenantId", "ActorId", "RiskDecisionId",
                    "ReauthenticationEvidenceHash", "OperationFingerprintHash",
                    "CapabilityReceiptId", "CapabilityReceiptHash", "RecordedAt")
                VALUES (
                    p_operation_id, p_phase, p_tenant_id, p_actor_id, p_risk_decision_id,
                    pg_catalog.btrim(p_reauthentication_evidence_hash),
                    pg_catalog.btrim(p_operation_fingerprint_hash), p_capability_receipt_id,
                    pg_catalog.btrim(p_capability_receipt_hash), p_recorded_at);
            EXCEPTION WHEN unique_violation THEN
                RAISE EXCEPTION 'payout authorization evidence already exists'
                    USING ERRCODE = '23505';
            END
            $function$;

            ALTER FUNCTION economy_private.append_payout_authorization_evidence_v1(
                uuid,uuid,uuid,integer,uuid,text,text,uuid,text,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.append_payout_authorization_evidence_v1(
                uuid,uuid,uuid,integer,uuid,text,text,uuid,text,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.append_payout_authorization_evidence_v1(
                uuid,uuid,uuid,integer,uuid,text,text,uuid,text,timestamptz)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemovePayoutAuthorizationEvidence(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.append_payout_authorization_evidence_v1(
                uuid,uuid,uuid,integer,uuid,text,text,uuid,text,timestamptz);
            """);
    }
}
