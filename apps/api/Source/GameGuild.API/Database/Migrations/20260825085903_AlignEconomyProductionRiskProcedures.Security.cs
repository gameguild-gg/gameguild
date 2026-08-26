using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AlignEconomyProductionRiskProcedures
{
    private static void InstallDurableRiskCounterReservationProcedure(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.reserve_risk_counter_v1(
                p_reservation_id uuid,
                p_risk_decision_id uuid,
                p_risk_counter_id uuid,
                p_expected_counter_version bigint,
                p_amount_units bigint,
                p_reserved_at timestamptz)
            RETURNS boolean
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                counter_record record;
                decision_record record;
                existing_record record;
            BEGIN
                IF p_reservation_id IS NULL OR p_risk_decision_id IS NULL OR p_risk_counter_id IS NULL
                   OR p_expected_counter_version <= 0 OR p_amount_units <= 0 OR p_reserved_at IS NULL THEN
                    RAISE EXCEPTION 'invalid risk counter reservation arguments' USING ERRCODE = '22023';
                END IF;

                SELECT decision."ExpiresAt", decision."OperationFingerprint"
                INTO decision_record
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                  AND decision."CounterVersion" = p_expected_counter_version
                  AND decision."AmountUnits" = p_amount_units
                  AND decision."ExpiresAt" > p_reserved_at
                  AND NOT EXISTS (
                      SELECT 1 FROM public.economy_risk_decision_consumptions consumption
                      WHERE consumption."RiskDecisionId" = decision."Id")
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'risk decision is missing, stale, consumed, or amount-mismatched' USING ERRCODE = '42501';
                END IF;

                SELECT reservation."Id", reservation."ReservationGroupId", reservation."AmountUnits",
                       reservation."ReservedAt", reservation."ExpiresAt", reservation."InputFingerprint"
                INTO existing_record
                FROM public.economy_risk_counter_reservations reservation
                WHERE reservation."RiskDecisionId" = p_risk_decision_id
                  AND reservation."RiskCounterId" = p_risk_counter_id;
                IF FOUND THEN
                    IF existing_record."Id" <> p_reservation_id
                       OR existing_record."ReservationGroupId" <> p_reservation_id
                       OR existing_record."AmountUnits" <> p_amount_units
                       OR existing_record."ReservedAt" <> p_reserved_at
                       OR existing_record."ExpiresAt" <> decision_record."ExpiresAt"
                       OR existing_record."InputFingerprint" <> decision_record."OperationFingerprint" THEN
                        RAISE EXCEPTION 'risk counter reservation idempotency conflict' USING ERRCODE = '23505';
                    END IF;
                    RETURN false;
                END IF;

                SELECT counter."CounterVersion", counter."MaxUnits", counter."UsedUnits",
                       counter."WindowStartedAt", counter."WindowEndsAt"
                INTO counter_record
                FROM public.economy_risk_counters counter
                WHERE counter."Id" = p_risk_counter_id
                FOR UPDATE;
                IF NOT FOUND
                   OR counter_record."CounterVersion" <> p_expected_counter_version
                   OR p_reserved_at < counter_record."WindowStartedAt"
                   OR p_reserved_at >= counter_record."WindowEndsAt" THEN
                    RAISE EXCEPTION 'risk counter is missing, stale, or outside its window' USING ERRCODE = '42501';
                END IF;
                IF counter_record."UsedUnits" > counter_record."MaxUnits" - p_amount_units THEN
                    RAISE EXCEPTION 'risk counter limit exceeded' USING ERRCODE = '22003';
                END IF;

                UPDATE public.economy_risk_counters
                SET "UsedUnits" = "UsedUnits" + p_amount_units,
                    "UpdatedAt" = p_reserved_at
                WHERE "Id" = p_risk_counter_id;

                INSERT INTO public.economy_risk_counter_reservations (
                    "Id", "ReservationGroupId", "RiskDecisionId", "RiskCounterId", "InputFingerprint",
                    "AmountUnits", "ReservedAt", "ExpiresAt", "Status", "ConsumedAt", "ReleasedAt")
                VALUES (
                    p_reservation_id, p_reservation_id, p_risk_decision_id, p_risk_counter_id,
                    decision_record."OperationFingerprint", p_amount_units, p_reserved_at,
                    decision_record."ExpiresAt", 1, NULL, NULL);

                RETURN true;
            END
            $function$;

            ALTER FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                TO gameguild_economy_writer;
            """);
    }

    private static void InstallTenantScopedSelfServiceRiskDecisionProcedure(MigrationBuilder migrationBuilder) =>
        RewriteSelfServiceRiskDecisionProcedure(migrationBuilder, tenantScoped: true);

    private static void RestoreLegacySelfServiceRiskDecisionProcedure(MigrationBuilder migrationBuilder) =>
        RewriteSelfServiceRiskDecisionProcedure(migrationBuilder, tenantScoped: false);

    private static void RewriteSelfServiceRiskDecisionProcedure(
        MigrationBuilder migrationBuilder,
        bool tenantScoped)
    {
        var oldColumns = tenantScoped
            ? """
              "Id", "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt", "WindowEndsAt",
                      "CounterVersion", "MaxUnits", "UsedUnits", "UpdatedAt"
              """
            : """
              "Id", "TenantId", "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt", "WindowEndsAt",
                      "CounterVersion", "MaxUnits", "UsedUnits", "UpdatedAt"
              """;
        var newColumns = tenantScoped
            ? """
              "Id", "TenantId", "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt", "WindowEndsAt",
                      "CounterVersion", "MaxUnits", "UsedUnits", "UpdatedAt"
              """
            : """
              "Id", "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt", "WindowEndsAt",
                      "CounterVersion", "MaxUnits", "UsedUnits", "UpdatedAt"
              """;
        var oldValues = tenantScoped
            ? """
              gen_random_uuid(), 1, counter_subject_hash, 5, 1, day_started_at, day_ends_at,
                      1, p_max_daily_hard_units, 0, p_requested_at
              """
            : """
              gen_random_uuid(), p_tenant_id, 1, counter_subject_hash, 5, 1, day_started_at, day_ends_at,
                      1, p_max_daily_hard_units, 0, p_requested_at
              """;
        var newValues = tenantScoped
            ? """
              gen_random_uuid(), p_tenant_id, 1, counter_subject_hash, 5, 1, day_started_at, day_ends_at,
                      1, p_max_daily_hard_units, 0, p_requested_at
              """
            : """
              gen_random_uuid(), 1, counter_subject_hash, 5, 1, day_started_at, day_ends_at,
                      1, p_max_daily_hard_units, 0, p_requested_at
              """;
        var oldConflict = tenantScoped
            ? "ON CONFLICT (\"Dimension\", \"SubjectHash\", \"Operation\", \"Currency\", \"WindowStartedAt\") DO NOTHING;"
            : "ON CONFLICT (\"TenantId\", \"Dimension\", \"SubjectHash\", \"Operation\", \"Currency\", \"WindowStartedAt\") DO NOTHING;";
        var newConflict = tenantScoped
            ? "ON CONFLICT (\"TenantId\", \"Dimension\", \"SubjectHash\", \"Operation\", \"Currency\", \"WindowStartedAt\") DO NOTHING;"
            : "ON CONFLICT (\"Dimension\", \"SubjectHash\", \"Operation\", \"Currency\", \"WindowStartedAt\") DO NOTHING;";
        var oldLookup = tenantScoped
            ? """
              FROM public.economy_risk_counters counter
                  WHERE counter."Dimension" = 1
              """
            : """
              FROM public.economy_risk_counters counter
                  WHERE counter."TenantId" = p_tenant_id
                    AND counter."Dimension" = 1
              """;
        var newLookup = tenantScoped
            ? """
              FROM public.economy_risk_counters counter
                  WHERE counter."TenantId" = p_tenant_id
                    AND counter."Dimension" = 1
              """
            : """
              FROM public.economy_risk_counters counter
                  WHERE counter."Dimension" = 1
              """;

        migrationBuilder.Sql($$"""
            DO $rewrite$
            DECLARE
                function_definition text;
                rewritten_definition text;
            BEGIN
                SELECT pg_get_functiondef(procedure.oid)
                INTO function_definition
                FROM pg_proc procedure
                WHERE procedure.oid =
                    'economy_private.issue_self_service_hard_to_soft_risk_decision_v1(uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz)'::regprocedure;
                IF function_definition IS NULL THEN
                    RAISE EXCEPTION 'self-service hard-to-soft risk decision procedure is missing';
                END IF;
                function_definition := replace(function_definition, E'\r\n', E'\n');

                rewritten_definition := replace(function_definition, $old_columns${{oldColumns}}$old_columns$, $new_columns${{newColumns}}$new_columns$);
                IF rewritten_definition = function_definition THEN
                    RAISE EXCEPTION 'self-service hard-to-soft risk counter columns no longer match the expected definition';
                END IF;
                function_definition := rewritten_definition;

                rewritten_definition := replace(function_definition, $old_values${{oldValues}}$old_values$, $new_values${{newValues}}$new_values$);
                IF rewritten_definition = function_definition THEN
                    RAISE EXCEPTION 'self-service hard-to-soft risk counter values no longer match the expected definition';
                END IF;
                function_definition := rewritten_definition;

                rewritten_definition := replace(function_definition, $old_conflict${{oldConflict}}$old_conflict$, $new_conflict${{newConflict}}$new_conflict$);
                IF rewritten_definition = function_definition THEN
                    RAISE EXCEPTION 'self-service hard-to-soft risk counter conflict target no longer matches the expected definition';
                END IF;
                function_definition := rewritten_definition;

                rewritten_definition := replace(function_definition, $old_lookup${{oldLookup}}$old_lookup$, $new_lookup${{newLookup}}$new_lookup$);
                IF rewritten_definition = function_definition THEN
                    RAISE EXCEPTION 'self-service hard-to-soft risk counter lookup no longer matches the expected definition';
                END IF;

                EXECUTE rewritten_definition;
            END
            $rewrite$;

            ALTER FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.issue_self_service_hard_to_soft_risk_decision_v1(
                uuid,uuid,uuid,uuid,text,bigint,bigint,bigint,text,timestamptz,timestamptz) TO gameguild_economy_writer;
            """);
    }

    private static void RestoreLegacyRiskCounterReservationProcedure(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.reserve_risk_counter_v1(
                p_reservation_id uuid,
                p_risk_decision_id uuid,
                p_risk_counter_id uuid,
                p_expected_counter_version bigint,
                p_amount_units bigint,
                p_reserved_at timestamptz)
            RETURNS boolean
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                counter_record record;
                existing_record record;
            BEGIN
                IF p_reservation_id IS NULL OR p_risk_decision_id IS NULL OR p_risk_counter_id IS NULL
                   OR p_expected_counter_version <= 0 OR p_amount_units <= 0 OR p_reserved_at IS NULL THEN
                    RAISE EXCEPTION 'invalid risk counter reservation arguments' USING ERRCODE = '22023';
                END IF;

                SELECT reservation."Id", reservation."AmountUnits"
                INTO existing_record
                FROM public.economy_risk_counter_reservations reservation
                WHERE reservation."RiskDecisionId" = p_risk_decision_id
                  AND reservation."RiskCounterId" = p_risk_counter_id;
                IF FOUND THEN
                    IF existing_record."Id" <> p_reservation_id
                       OR existing_record."AmountUnits" <> p_amount_units THEN
                        RAISE EXCEPTION 'risk counter reservation idempotency conflict' USING ERRCODE = '23505';
                    END IF;
                    RETURN false;
                END IF;

                PERFORM 1
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                  AND decision."CounterVersion" = p_expected_counter_version
                  AND decision."AmountUnits" = p_amount_units
                  AND decision."ExpiresAt" > p_reserved_at
                  AND NOT EXISTS (
                      SELECT 1 FROM public.economy_risk_decision_consumptions consumption
                      WHERE consumption."RiskDecisionId" = decision."Id")
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'risk decision is missing, stale, consumed, or amount-mismatched' USING ERRCODE = '42501';
                END IF;

                SELECT counter."CounterVersion", counter."MaxUnits", counter."UsedUnits",
                       counter."WindowStartedAt", counter."WindowEndsAt"
                INTO counter_record
                FROM public.economy_risk_counters counter
                WHERE counter."Id" = p_risk_counter_id
                FOR UPDATE;
                IF NOT FOUND
                   OR counter_record."CounterVersion" <> p_expected_counter_version
                   OR p_reserved_at < counter_record."WindowStartedAt"
                   OR p_reserved_at >= counter_record."WindowEndsAt" THEN
                    RAISE EXCEPTION 'risk counter is missing, stale, or outside its window' USING ERRCODE = '42501';
                END IF;
                IF counter_record."UsedUnits" > counter_record."MaxUnits" - p_amount_units THEN
                    RAISE EXCEPTION 'risk counter limit exceeded' USING ERRCODE = '22003';
                END IF;

                UPDATE public.economy_risk_counters
                SET "UsedUnits" = "UsedUnits" + p_amount_units,
                    "UpdatedAt" = p_reserved_at
                WHERE "Id" = p_risk_counter_id;

                INSERT INTO public.economy_risk_counter_reservations (
                    "Id", "RiskDecisionId", "RiskCounterId", "AmountUnits", "ReservedAt")
                VALUES (p_reservation_id, p_risk_decision_id, p_risk_counter_id, p_amount_units, p_reserved_at);

                RETURN true;
            END
            $function$;

            ALTER FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.reserve_risk_counter_v1(uuid,uuid,uuid,bigint,bigint,timestamptz)
                TO gameguild_economy_writer;
            """);
    }
}
