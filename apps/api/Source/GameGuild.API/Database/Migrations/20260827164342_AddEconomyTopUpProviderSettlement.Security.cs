using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyTopUpProviderSettlement
{
    private static void InstallEconomyTopUpSettlementSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO public.economy_registered_capabilities
                ("Id", "Name", "AllowedTemplateKinds", "IsEnabled", "CreatedAt", "RevokedAt")
            VALUES
                ('e1000000-0000-0000-0000-000000000014',
                 'economy.confirm-hard-coin-funding.v1', '[1]'::jsonb,
                 true, TIMESTAMPTZ '2026-08-27T00:00:00Z', NULL)
            ON CONFLICT ("Name") DO UPDATE
            SET "AllowedTemplateKinds" = EXCLUDED."AllowedTemplateKinds",
                "IsEnabled" = true,
                "RevokedAt" = NULL;

            CREATE OR REPLACE FUNCTION economy_private.initialize_economy_top_up_intent_timestamps_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                NEW."UpdatedAt" := NEW."RequestedAt";
                RETURN NEW;
            END
            $function$;

            DROP TRIGGER IF EXISTS initialize_economy_top_up_intent_timestamps
                ON public.economy_top_up_intents;
            CREATE TRIGGER initialize_economy_top_up_intent_timestamps
                BEFORE INSERT ON public.economy_top_up_intents
                FOR EACH ROW EXECUTE FUNCTION economy_private.initialize_economy_top_up_intent_timestamps_v1();

            CREATE OR REPLACE FUNCTION economy_private.guard_economy_top_up_intent_mutation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    RAISE EXCEPTION 'Economy top-up intents are append-preserving'
                        USING ERRCODE = '42501';
                END IF;
                IF NEW."Id" <> OLD."Id" OR NEW."PaymentId" <> OLD."PaymentId"
                   OR NEW."TenantId" <> OLD."TenantId" OR NEW."ActorId" <> OLD."ActorId"
                   OR NEW."WalletId" <> OLD."WalletId" OR NEW."HardCoinUnits" <> OLD."HardCoinUnits"
                   OR NEW."UsdMinorUnits" <> OLD."UsdMinorUnits"
                   OR NEW."JurisdictionCode" <> OLD."JurisdictionCode"
                   OR NEW."PolicyVersion" <> OLD."PolicyVersion" OR NEW."PolicyHash" <> OLD."PolicyHash"
                   OR NEW."Provider" <> OLD."Provider" OR NEW."IdempotencyKey" <> OLD."IdempotencyKey"
                   OR NEW."RequestHash" <> OLD."RequestHash" OR NEW."RequestedAt" <> OLD."RequestedAt" THEN
                    RAISE EXCEPTION 'Economy top-up authority is immutable'
                        USING ERRCODE = '42501';
                END IF;
                IF OLD."ProviderObjectId" IS NOT NULL AND
                   (NEW."ProviderEnvironment" IS DISTINCT FROM OLD."ProviderEnvironment"
                    OR NEW."ProviderAccountId" IS DISTINCT FROM OLD."ProviderAccountId"
                    OR NEW."ProviderObjectId" IS DISTINCT FROM OLD."ProviderObjectId"
                    OR NEW."ProviderObjectType" IS DISTINCT FROM OLD."ProviderObjectType"
                    OR NEW."ProviderMonetaryLeg" IS DISTINCT FROM OLD."ProviderMonetaryLeg"
                    OR NEW."ProviderBoundAt" IS DISTINCT FROM OLD."ProviderBoundAt") THEN
                    RAISE EXCEPTION 'Economy top-up provider binding is immutable'
                        USING ERRCODE = '42501';
                END IF;
                IF OLD."PostingGroupId" IS NOT NULL AND
                   NEW."PostingGroupId" IS DISTINCT FROM OLD."PostingGroupId" THEN
                    RAISE EXCEPTION 'Economy top-up posting binding is immutable'
                        USING ERRCODE = '42501';
                END IF;
                IF NEW."LastProviderEventAt" IS NOT NULL AND
                   (NEW."LastProviderEventAt" < NEW."RequestedAt" OR
                    (OLD."LastProviderEventAt" IS NOT NULL AND
                     NEW."LastProviderEventAt" < OLD."LastProviderEventAt")) THEN
                    RAISE EXCEPTION 'Economy top-up provider event time regressed'
                        USING ERRCODE = '22023';
                END IF;
                IF NEW."UpdatedAt" < OLD."UpdatedAt" THEN
                    RAISE EXCEPTION 'Economy top-up update time regressed'
                        USING ERRCODE = '22023';
                END IF;
                IF NOT (
                    (OLD."Status" = 1 AND NEW."Status" IN (2, 3)) OR
                    (OLD."Status" IN (2, 3) AND NEW."Status" IN (2, 3, 4, 5, 6, 7, 8, 9)) OR
                    (OLD."Status" = 4 AND NEW."Status" IN (4, 5, 6, 8, 9)) OR
                    (OLD."Status" = 5 AND NEW."Status" IN (5, 10)) OR
                    (OLD."Status" = 6 AND NEW."Status" IN (2, 3, 4, 5, 6, 9)) OR
                    (OLD."Status" = 9 AND NEW."Status" IN (5, 9)) OR
                    (OLD."Status" IN (7, 8, 10) AND NEW."Status" = OLD."Status")) THEN
                    RAISE EXCEPTION 'Economy top-up state transition is invalid'
                        USING ERRCODE = '22023';
                END IF;
                IF NEW."Version" <> OLD."Version" + 1 THEN
                    RAISE EXCEPTION 'Economy top-up version must advance exactly once'
                        USING ERRCODE = '40001';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.read_economy_top_up_payment_fact_v1(
                p_provider text,
                p_environment text,
                p_account_id text,
                p_object_id text,
                p_object_type text,
                p_monetary_leg text)
            RETURNS TABLE(
                "Id" uuid,
                "TenantId" uuid,
                "Amount" numeric,
                "Currency" text,
                "Provider" text,
                "ProviderEnvironment" text,
                "ProviderAccountId" text,
                "ProviderObjectId" text,
                "ProviderObjectType" text,
                "ProviderMonetaryLeg" text)
            LANGUAGE sql
            STABLE
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT payment."Id", payment."TenantId", payment."Amount", payment."Currency",
                       payment."Provider", payment."ProviderEnvironment", payment."ProviderAccountId",
                       payment."ProviderObjectId", payment."ProviderObjectType", payment."ProviderMonetaryLeg"
                FROM public.economy_top_up_intents intent
                JOIN public.payments payment ON payment."Id" = intent."PaymentId"
                WHERE intent."Provider" = p_provider
                  AND intent."ProviderEnvironment" = p_environment
                  AND intent."ProviderAccountId" = p_account_id
                  AND intent."ProviderObjectId" = p_object_id
                  AND intent."ProviderObjectType" = p_object_type
                  AND intent."ProviderMonetaryLeg" = p_monetary_leg
                  AND payment."TenantId" = intent."TenantId"
                  AND payment."Provider" = intent."Provider"
                  AND payment."ProviderEnvironment" = intent."ProviderEnvironment"
                  AND payment."ProviderAccountId" = intent."ProviderAccountId"
                  AND payment."ProviderObjectId" = intent."ProviderObjectId"
                  AND payment."ProviderObjectType" = intent."ProviderObjectType"
                  AND payment."ProviderMonetaryLeg" = intent."ProviderMonetaryLeg"
                  AND payment."Amount" = intent."UsdMinorUnits" / 100.0
                  AND upper(payment."Currency") = 'USD';
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.apply_economy_top_up_provider_event_v1(
                p_provider text,
                p_environment text,
                p_account_id text,
                p_object_id text,
                p_object_type text,
                p_monetary_leg text,
                p_event_id text,
                p_occurred_at timestamptz,
                p_status integer,
                p_evidence_hash text,
                p_usd_minor_units bigint,
                p_currency text,
                p_posting_group_id uuid,
                p_failure_code text)
            RETURNS TABLE("Applied" boolean, "Duplicate" boolean, "Status" integer)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                stored public.economy_top_up_intents%ROWTYPE;
                payment_status integer;
            BEGIN
                IF p_provider <> 'stripe'
                   OR length(btrim(COALESCE(p_environment, ''))) = 0
                   OR length(btrim(COALESCE(p_account_id, ''))) = 0
                   OR length(btrim(COALESCE(p_object_id, ''))) = 0
                   OR p_object_type <> 'payment_intent' OR p_monetary_leg <> 'capture'
                   OR length(btrim(COALESCE(p_event_id, ''))) = 0 OR length(p_event_id) > 255
                   OR p_occurred_at IS NULL OR p_status NOT IN (2, 3, 5, 6, 7, 9)
                   OR p_evidence_hash !~ '^[0-9a-fA-F]{64}$'
                   OR p_usd_minor_units <= 0 OR upper(btrim(COALESCE(p_currency, ''))) <> 'USD'
                   OR (p_status = 5) <> (p_posting_group_id IS NOT NULL)
                   OR (p_status IN (6, 7)) <> (length(btrim(COALESCE(p_failure_code, ''))) > 0) THEN
                    RAISE EXCEPTION 'Economy top-up provider event is invalid'
                        USING ERRCODE = '22023';
                END IF;

                SELECT intent.* INTO stored
                FROM public.economy_top_up_intents intent
                WHERE intent."Provider" = p_provider
                  AND intent."ProviderEnvironment" = p_environment
                  AND intent."ProviderAccountId" = p_account_id
                  AND intent."ProviderObjectId" = p_object_id
                  AND intent."ProviderObjectType" = p_object_type
                  AND intent."ProviderMonetaryLeg" = p_monetary_leg
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Economy top-up provider binding was not found'
                        USING ERRCODE = 'P0002';
                END IF;
                IF stored."UsdMinorUnits" <> p_usd_minor_units OR p_occurred_at < stored."RequestedAt" THEN
                    RAISE EXCEPTION 'Economy top-up provider fact does not match authoritative intent'
                        USING ERRCODE = '42501';
                END IF;
                PERFORM 1 FROM public.payments payment
                WHERE payment."Id" = stored."PaymentId"
                  AND payment."TenantId" = stored."TenantId"
                  AND payment."Provider" = p_provider
                  AND payment."ProviderEnvironment" = p_environment
                  AND payment."ProviderAccountId" = p_account_id
                  AND payment."ProviderObjectId" = p_object_id
                  AND payment."ProviderObjectType" = p_object_type
                  AND payment."ProviderMonetaryLeg" = p_monetary_leg
                  AND payment."Amount" = p_usd_minor_units / 100.0
                  AND upper(payment."Currency") = 'USD'
                FOR UPDATE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'Economy top-up Payment binding is invalid'
                        USING ERRCODE = '42501';
                END IF;

                IF stored."LastProviderEventId" = p_event_id AND stored."Status" = p_status THEN
                    RETURN QUERY SELECT false, true, stored."Status";
                    RETURN;
                END IF;
                IF stored."Status" IN (5, 7)
                   OR (stored."Status" = 9 AND p_status <> 5)
                   OR stored."LastProviderEventAt" > p_occurred_at THEN
                    RETURN QUERY SELECT false, false, stored."Status";
                    RETURN;
                END IF;

                UPDATE public.economy_top_up_intents
                SET "Status" = p_status,
                    "LastProviderEventId" = p_event_id,
                    "LastProviderEventAt" = p_occurred_at,
                    "LastProviderEvidenceHash" = lower(p_evidence_hash),
                    "PostingGroupId" = p_posting_group_id,
                    "FailureCode" = NULLIF(btrim(COALESCE(p_failure_code, '')), ''),
                    "UpdatedAt" = p_occurred_at,
                    "Version" = "Version" + 1
                WHERE "Id" = stored."Id";

                payment_status := CASE p_status
                    WHEN 2 THEN 5
                    WHEN 3 THEN 1
                    WHEN 5 THEN 2
                    WHEN 6 THEN 3
                    WHEN 7 THEN 4
                    WHEN 9 THEN 2
                END;
                UPDATE public.payments
                SET "Status" = payment_status,
                    "ExternalTransactionId" = p_object_id,
                    "ExternalPaymentId" = CASE WHEN p_status IN (5, 9)
                        THEN p_object_id ELSE "ExternalPaymentId" END,
                    "FailureReason" = CASE WHEN p_status = 6
                        THEN 'Stripe reported that the top-up payment failed.' ELSE NULL END,
                    "ErrorCode" = CASE WHEN p_status = 6 THEN p_failure_code ELSE NULL END,
                    "CancellationReason" = CASE WHEN p_status = 7
                        THEN 'Stripe cancelled the top-up payment.' ELSE NULL END,
                    "CancelledAt" = CASE WHEN p_status = 7 THEN p_occurred_at ELSE NULL END,
                    "ProcessedAt" = CASE WHEN p_status IN (5, 6, 9) THEN p_occurred_at ELSE NULL END,
                    "UpdatedAt" = p_occurred_at,
                    "Version" = "Version" + 1
                WHERE "Id" = stored."PaymentId";

                RETURN QUERY SELECT true, false, p_status;
            END
            $function$;

            ALTER FUNCTION economy_private.apply_economy_top_up_provider_event_v1(
                text,text,text,text,text,text,text,timestamptz,integer,text,bigint,text,uuid,text)
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.initialize_economy_top_up_intent_timestamps_v1()
                OWNER TO gameguild_economy_procedure_owner;
            ALTER FUNCTION economy_private.read_economy_top_up_payment_fact_v1(
                text,text,text,text,text,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.read_economy_top_up_payment_fact_v1(
                text,text,text,text,text,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.apply_economy_top_up_provider_event_v1(
                text,text,text,text,text,text,text,timestamptz,integer,text,bigint,text,uuid,text) FROM PUBLIC;
            REVOKE ALL ON FUNCTION economy_private.initialize_economy_top_up_intent_timestamps_v1()
                FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.read_economy_top_up_payment_fact_v1(
                text,text,text,text,text,text)
                TO gameguild_economy_runtime;
            GRANT EXECUTE ON FUNCTION economy_private.apply_economy_top_up_provider_event_v1(
                text,text,text,text,text,text,text,timestamptz,integer,text,bigint,text,uuid,text)
                TO gameguild_economy_runtime;
            """);
    }

    private static void RemoveEconomyTopUpSettlementSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.apply_economy_top_up_provider_event_v1(
                text,text,text,text,text,text,text,timestamptz,integer,text,bigint,text,uuid,text);
            DROP FUNCTION IF EXISTS economy_private.read_economy_top_up_payment_fact_v1(
                text,text,text,text,text,text);
            DROP TRIGGER IF EXISTS initialize_economy_top_up_intent_timestamps
                ON public.economy_top_up_intents;
            DROP FUNCTION IF EXISTS economy_private.initialize_economy_top_up_intent_timestamps_v1();
            DELETE FROM public.economy_registered_capabilities
            WHERE "Name" = 'economy.confirm-hard-coin-funding.v1';

            CREATE OR REPLACE FUNCTION economy_private.guard_economy_top_up_intent_mutation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    RAISE EXCEPTION 'Economy top-up intents are append-preserving'
                        USING ERRCODE = '42501';
                END IF;
                IF NEW."Id" <> OLD."Id" OR NEW."PaymentId" <> OLD."PaymentId"
                   OR NEW."TenantId" <> OLD."TenantId" OR NEW."ActorId" <> OLD."ActorId"
                   OR NEW."WalletId" <> OLD."WalletId" OR NEW."HardCoinUnits" <> OLD."HardCoinUnits"
                   OR NEW."UsdMinorUnits" <> OLD."UsdMinorUnits"
                   OR NEW."JurisdictionCode" <> OLD."JurisdictionCode"
                   OR NEW."PolicyVersion" <> OLD."PolicyVersion" OR NEW."PolicyHash" <> OLD."PolicyHash"
                   OR NEW."Provider" <> OLD."Provider" OR NEW."IdempotencyKey" <> OLD."IdempotencyKey"
                   OR NEW."RequestHash" <> OLD."RequestHash" OR NEW."RequestedAt" <> OLD."RequestedAt" THEN
                    RAISE EXCEPTION 'Economy top-up authority is immutable'
                        USING ERRCODE = '42501';
                END IF;
                IF OLD."ProviderObjectId" IS NOT NULL AND
                   (NEW."ProviderEnvironment" IS DISTINCT FROM OLD."ProviderEnvironment"
                    OR NEW."ProviderAccountId" IS DISTINCT FROM OLD."ProviderAccountId"
                    OR NEW."ProviderObjectId" IS DISTINCT FROM OLD."ProviderObjectId"
                    OR NEW."ProviderObjectType" IS DISTINCT FROM OLD."ProviderObjectType"
                    OR NEW."ProviderMonetaryLeg" IS DISTINCT FROM OLD."ProviderMonetaryLeg"
                    OR NEW."ProviderBoundAt" IS DISTINCT FROM OLD."ProviderBoundAt") THEN
                    RAISE EXCEPTION 'Economy top-up provider binding is immutable'
                        USING ERRCODE = '42501';
                END IF;
                IF NOT (
                    (OLD."Status" = 1 AND NEW."Status" IN (2, 3)) OR
                    (OLD."Status" IN (2, 3) AND NEW."Status" IN (2, 3, 4, 6, 7, 8, 9)) OR
                    (OLD."Status" = 4 AND NEW."Status" IN (4, 5, 6, 8, 9)) OR
                    (OLD."Status" = 5 AND NEW."Status" IN (5, 10)) OR
                    (OLD."Status" IN (6, 7, 8, 9, 10) AND NEW."Status" = OLD."Status")) THEN
                    RAISE EXCEPTION 'Economy top-up state transition is invalid'
                        USING ERRCODE = '22023';
                END IF;
                IF NEW."Version" <> OLD."Version" + 1 THEN
                    RAISE EXCEPTION 'Economy top-up version must advance exactly once'
                        USING ERRCODE = '40001';
                END IF;
                RETURN NEW;
            END
            $function$;
            """);
    }
}
