using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class AddEconomyFoundationSchemaRollup
{
    private static void InstallRolesAndExtensions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS pgcrypto;
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            DO $roles$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_migration') THEN
                    CREATE ROLE gameguild_economy_migration NOLOGIN;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_runtime') THEN
                    CREATE ROLE gameguild_economy_runtime NOLOGIN;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_writer') THEN
                    CREATE ROLE gameguild_economy_writer NOLOGIN;
                END IF;
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_procedure_owner') THEN
                    CREATE ROLE gameguild_economy_procedure_owner NOLOGIN;
                END IF;
            END
            $roles$;

            CREATE SCHEMA IF NOT EXISTS economy_private;
            REVOKE ALL ON SCHEMA economy_private FROM PUBLIC;
            GRANT USAGE ON SCHEMA economy_private
                TO gameguild_economy_writer, gameguild_economy_procedure_owner;
            """);
    }

    private static void HardenSchema(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.economy_fragment_root_ranges
                ADD CONSTRAINT ex_economy_fragment_root_ranges_no_overlap
                EXCLUDE USING gist (
                    "RootSourceStampId" WITH =,
                    "ReversalEpoch" WITH =,
                    int8range("StartInclusive", "EndExclusive", '[)') WITH &&
                );

            CREATE OR REPLACE FUNCTION economy_private.deny_immutable_mutation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                RAISE EXCEPTION 'immutable economy relation % rejects %', TG_TABLE_NAME, TG_OP
                    USING ERRCODE = '42501';
            END
            $function$;

            ALTER FUNCTION economy_private.deny_immutable_mutation_v1()
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.deny_immutable_mutation_v1() FROM PUBLIC;

            DO $immutable_triggers$
            DECLARE
                relation_name text;
            BEGIN
                FOREACH relation_name IN ARRAY ARRAY[
                    'economy_source_stamps',
                    'economy_source_stamp_events',
                    'economy_posting_groups',
                    'economy_journal_entries',
                    'economy_journal_lines',
                    'economy_credit_lots',
                    'economy_entry_allocations',
                    'economy_lot_lineage_edges',
                    'economy_fragment_root_ranges',
                    'economy_provider_fact_allocations',
                    'economy_dispatch_snapshots',
                    'economy_outbox_messages',
                    'economy_idempotency_records',
                    'economy_external_anchors',
                    'economy_risk_decisions',
                    'economy_risk_decision_consumptions',
                    'economy_risk_counter_reservations',
                    'economy_hold_events',
                    'economy_risk_review_events',
                    'economy_risk_audit_evidence'
                ]
                LOOP
                    EXECUTE format(
                        'CREATE TRIGGER deny_immutable_mutation BEFORE UPDATE OR DELETE ON public.%I '
                        'FOR EACH ROW EXECUTE FUNCTION economy_private.deny_immutable_mutation_v1()',
                        relation_name);
                END LOOP;
            END
            $immutable_triggers$;

            CREATE OR REPLACE FUNCTION economy_private.validate_entry_allocation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                lot_units bigint;
                line_units bigint;
                allocated_units bigint;
            BEGIN
                PERFORM pg_advisory_xact_lock(hashtextextended(NEW."ParentLotId"::text, 0));
                SELECT lot."AmountUnits" INTO lot_units
                FROM public.economy_credit_lots lot
                WHERE lot."Id" = NEW."ParentLotId"
                FOR SHARE;
                SELECT line."AmountUnits" INTO line_units
                FROM public.economy_journal_lines line
                WHERE line."Id" = NEW."JournalLineId"
                FOR SHARE;
                IF lot_units IS NULL OR line_units IS NULL THEN
                    RAISE EXCEPTION 'allocation references an absent lot or journal line' USING ERRCODE = '23503';
                END IF;
                SELECT COALESCE(sum(allocation."AmountUnits"), 0) INTO allocated_units
                FROM public.economy_entry_allocations allocation
                WHERE allocation."ParentLotId" = NEW."ParentLotId";
                IF NEW."AmountUnits" > line_units OR allocated_units > lot_units - NEW."AmountUnits" THEN
                    RAISE EXCEPTION 'allocation exceeds its journal line or parent lot' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.verify_entry_allocation_conservation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                line_units bigint;
                allocated_units bigint;
            BEGIN
                SELECT line."AmountUnits" INTO line_units
                FROM public.economy_journal_lines line WHERE line."Id" = NEW."JournalLineId";
                SELECT COALESCE(sum(allocation."AmountUnits"), 0) INTO allocated_units
                FROM public.economy_entry_allocations allocation
                WHERE allocation."JournalLineId" = NEW."JournalLineId";
                IF allocated_units <> line_units THEN
                    RAISE EXCEPTION 'journal-line allocation is not conserved' USING ERRCODE = '23514';
                END IF;
                RETURN NULL;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.validate_lineage_edge_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                parent_units bigint;
                child_units bigint;
                parent_currency integer;
                child_currency integer;
                allocated_units bigint;
            BEGIN
                IF NEW."ParentLotId" = NEW."ChildLotId" THEN
                    RAISE EXCEPTION 'lineage cannot reference the same parent and child' USING ERRCODE = '23514';
                END IF;
                PERFORM pg_advisory_xact_lock(hashtextextended(lock_id, 0))
                FROM (SELECT unnest(ARRAY[NEW."ParentLotId"::text, NEW."ChildLotId"::text]) AS lock_id
                      ORDER BY lock_id) locks;
                SELECT "AmountUnits", "Currency" INTO parent_units, parent_currency
                FROM public.economy_credit_lots WHERE "Id" = NEW."ParentLotId" FOR SHARE;
                SELECT "AmountUnits", "Currency" INTO child_units, child_currency
                FROM public.economy_credit_lots WHERE "Id" = NEW."ChildLotId" FOR SHARE;
                IF parent_units IS NULL OR child_units IS NULL
                   OR parent_currency <> NEW."Currency" OR child_currency <> NEW."Currency" THEN
                    RAISE EXCEPTION 'lineage references absent or currency-mismatched lots' USING ERRCODE = '23514';
                END IF;
                SELECT COALESCE(sum(edge."AmountUnits"), 0) INTO allocated_units
                FROM public.economy_lot_lineage_edges edge
                WHERE edge."ParentLotId" = NEW."ParentLotId";
                IF allocated_units > parent_units - NEW."AmountUnits" OR NEW."AmountUnits" > child_units THEN
                    RAISE EXCEPTION 'lineage exceeds parent or child amount' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.verify_child_lineage_conservation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                child_units bigint;
                lineage_units bigint;
            BEGIN
                SELECT lot."AmountUnits" INTO child_units
                FROM public.economy_credit_lots lot WHERE lot."Id" = NEW."ChildLotId";
                SELECT COALESCE(sum(edge."AmountUnits"), 0) INTO lineage_units
                FROM public.economy_lot_lineage_edges edge WHERE edge."ChildLotId" = NEW."ChildLotId";
                IF lineage_units <> child_units THEN
                    RAISE EXCEPTION 'child-lot lineage is not conserved' USING ERRCODE = '23514';
                END IF;
                RETURN NULL;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.validate_fragment_root_range_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                authoritative_units bigint;
                current_epoch bigint;
                owner_units bigint;
                owner_root uuid;
            BEGIN
                PERFORM pg_advisory_xact_lock(hashtextextended(NEW."RootSourceStampId"::text, 0));
                SELECT source."AuthoritativeUnits" INTO authoritative_units
                FROM public.economy_source_stamps source
                WHERE source."Id" = NEW."RootSourceStampId" FOR SHARE;
                SELECT COALESCE(reversal."Epoch", 0) INTO current_epoch
                FROM (SELECT NEW."RootSourceStampId" AS id) expected
                LEFT JOIN public.economy_root_reversal_states reversal
                    ON reversal."RootSourceStampId" = expected.id;

                IF NEW."CreditLotId" IS NOT NULL THEN
                    SELECT lot."AmountUnits", lot."RootSourceStampId" INTO owner_units, owner_root
                    FROM public.economy_credit_lots lot WHERE lot."Id" = NEW."CreditLotId" FOR SHARE;
                ELSE
                    SELECT allocation."AmountUnits", lot."RootSourceStampId" INTO owner_units, owner_root
                    FROM public.economy_entry_allocations allocation
                    JOIN public.economy_credit_lots lot ON lot."Id" = allocation."ParentLotId"
                    WHERE allocation."Id" = NEW."EntryAllocationId" FOR SHARE OF allocation, lot;
                END IF;

                IF authoritative_units IS NULL OR owner_units IS NULL OR owner_root <> NEW."RootSourceStampId"
                   OR NEW."ReversalEpoch" <> current_epoch
                   OR NEW."EndExclusive" > authoritative_units
                   OR NEW."EndExclusive" - NEW."StartInclusive" > owner_units THEN
                    RAISE EXCEPTION 'root range violates source, owner, amount, or reversal epoch' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.verify_fragment_root_range_conservation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                owner_units bigint;
                range_units bigint;
            BEGIN
                IF NEW."CreditLotId" IS NOT NULL THEN
                    SELECT "AmountUnits" INTO owner_units FROM public.economy_credit_lots WHERE "Id" = NEW."CreditLotId";
                    SELECT COALESCE(sum("EndExclusive" - "StartInclusive"), 0) INTO range_units
                    FROM public.economy_fragment_root_ranges WHERE "CreditLotId" = NEW."CreditLotId";
                ELSE
                    SELECT "AmountUnits" INTO owner_units FROM public.economy_entry_allocations WHERE "Id" = NEW."EntryAllocationId";
                    SELECT COALESCE(sum("EndExclusive" - "StartInclusive"), 0) INTO range_units
                    FROM public.economy_fragment_root_ranges WHERE "EntryAllocationId" = NEW."EntryAllocationId";
                END IF;
                IF range_units <> owner_units THEN
                    RAISE EXCEPTION 'root-range ownership is not conserved' USING ERRCODE = '23514';
                END IF;
                RETURN NULL;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.validate_risk_consumption_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_risk_decisions decision
                    JOIN public.economy_posting_groups posting ON posting."Id" = NEW."PostingGroupId"
                    WHERE decision."Id" = NEW."RiskDecisionId"
                      AND posting."RiskDecisionId" = decision."Id"
                      AND decision."OperationFingerprint" = NEW."OperationFingerprint") THEN
                    RAISE EXCEPTION 'risk consumption is not bound to its decision and posting' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.validate_provider_fact_allocation_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                PERFORM pg_advisory_xact_lock(hashtextextended(NEW."SourceStampId"::text, 0));
                IF NOT EXISTS (
                    SELECT 1 FROM public.economy_source_stamps source
                    WHERE source."Id" = NEW."SourceStampId"
                      AND source."State" = 2
                      AND source."ConfirmedAt" IS NOT NULL
                      AND source."Provider" = NEW."Provider"
                      AND source."ProviderReference" = NEW."ProviderObject"
                      AND source."AuthoritativeUnits" = NEW."AuthoritativeUnits") THEN
                    RAISE EXCEPTION 'provider fact is not bound to a confirmed authoritative source' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END
            $function$;

            DO $integrity_functions$
            DECLARE
                function_signature text;
            BEGIN
                FOREACH function_signature IN ARRAY ARRAY[
                    'economy_private.validate_entry_allocation_v1()',
                    'economy_private.verify_entry_allocation_conservation_v1()',
                    'economy_private.validate_lineage_edge_v1()',
                    'economy_private.verify_child_lineage_conservation_v1()',
                    'economy_private.validate_fragment_root_range_v1()',
                    'economy_private.verify_fragment_root_range_conservation_v1()',
                    'economy_private.validate_risk_consumption_v1()',
                    'economy_private.validate_provider_fact_allocation_v1()'
                ]
                LOOP
                    EXECUTE format('ALTER FUNCTION %s OWNER TO gameguild_economy_procedure_owner', function_signature);
                    EXECUTE format('REVOKE ALL ON FUNCTION %s FROM PUBLIC', function_signature);
                END LOOP;
            END
            $integrity_functions$;

            CREATE TRIGGER validate_entry_allocation
                BEFORE INSERT ON public.economy_entry_allocations
                FOR EACH ROW EXECUTE FUNCTION economy_private.validate_entry_allocation_v1();
            CREATE CONSTRAINT TRIGGER verify_entry_allocation_conservation
                AFTER INSERT ON public.economy_entry_allocations
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION economy_private.verify_entry_allocation_conservation_v1();
            CREATE TRIGGER validate_lineage_edge
                BEFORE INSERT ON public.economy_lot_lineage_edges
                FOR EACH ROW EXECUTE FUNCTION economy_private.validate_lineage_edge_v1();
            CREATE CONSTRAINT TRIGGER verify_child_lineage_conservation
                AFTER INSERT ON public.economy_lot_lineage_edges
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION economy_private.verify_child_lineage_conservation_v1();
            CREATE TRIGGER validate_fragment_root_range
                BEFORE INSERT ON public.economy_fragment_root_ranges
                FOR EACH ROW EXECUTE FUNCTION economy_private.validate_fragment_root_range_v1();
            CREATE CONSTRAINT TRIGGER verify_fragment_root_range_conservation
                AFTER INSERT ON public.economy_fragment_root_ranges
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION economy_private.verify_fragment_root_range_conservation_v1();
            CREATE TRIGGER validate_risk_consumption
                BEFORE INSERT ON public.economy_risk_decision_consumptions
                FOR EACH ROW EXECUTE FUNCTION economy_private.validate_risk_consumption_v1();
            CREATE TRIGGER validate_provider_fact_allocation
                BEFORE INSERT ON public.economy_provider_fact_allocations
                FOR EACH ROW EXECUTE FUNCTION economy_private.validate_provider_fact_allocation_v1();

            CREATE OR REPLACE FUNCTION economy_private.line_matches(
                p_line jsonb,
                p_side integer,
                p_account integer,
                p_currency integer)
            RETURNS boolean
            LANGUAGE sql
            IMMUTABLE
            STRICT
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT (p_line->>'side')::integer = p_side
                   AND (p_line->>'account_code')::integer = p_account
                   AND (p_line->>'currency')::integer = p_currency
            $function$;

            CREATE OR REPLACE FUNCTION economy_private.validate_posting_lines_v1(
                p_template_kind integer,
                p_lines jsonb)
            RETURNS boolean
            LANGUAGE plpgsql
            IMMUTABLE
            STRICT
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                expected_count integer;
                first_line jsonb;
                second_line jsonb;
                third_line jsonb;
                fourth_line jsonb;
            BEGIN
                IF jsonb_typeof(p_lines) <> 'array' OR p_template_kind NOT BETWEEN 1 AND 16 THEN
                    RETURN false;
                END IF;

                expected_count := CASE WHEN p_template_kind IN (5, 6) THEN 4 ELSE 2 END;
                IF jsonb_array_length(p_lines) <> expected_count THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) AS line
                    WHERE COALESCE((line->>'amount_units')::bigint, 0) <= 0
                       OR COALESCE((line->>'side')::integer, 0) NOT IN (1, 2)
                       OR COALESCE((line->>'currency')::integer, 0) NOT IN (1, 2)
                       OR COALESCE((line->>'account_code')::integer, 0) NOT BETWEEN 1 AND 14
                       OR NULLIF(line->>'id', '') IS NULL
                       OR NULLIF(line->>'account_id', '') IS NULL
                ) THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) AS line
                    GROUP BY (line->>'currency')::integer
                    HAVING sum(CASE WHEN (line->>'side')::integer = 1 THEN (line->>'amount_units')::bigint ELSE 0 END)
                         <> sum(CASE WHEN (line->>'side')::integer = 2 THEN (line->>'amount_units')::bigint ELSE 0 END)
                ) THEN
                    RETURN false;
                END IF;

                first_line := p_lines->0;
                second_line := p_lines->1;
                third_line := p_lines->2;
                fourth_line := p_lines->3;

                RETURN CASE p_template_kind
                    WHEN 1 THEN economy_private.line_matches(first_line, 1, 1, 1)
                                AND economy_private.line_matches(second_line, 2, 2, 1)
                    WHEN 2 THEN economy_private.line_matches(first_line, 1, 2, 1)
                                AND economy_private.line_matches(second_line, 2, 1, 1)
                    WHEN 3 THEN economy_private.line_matches(first_line, 1, 2, 1)
                                AND economy_private.line_matches(second_line, 2, 1, 1)
                    WHEN 4 THEN (first_line->>'side')::integer = 1
                                AND (second_line->>'side')::integer = 2
                                AND (first_line->>'account_code')::integer IN (2, 3, 4)
                                AND (second_line->>'account_code')::integer IN (2, 3, 4)
                                AND (first_line->>'currency') = (second_line->>'currency')
                    WHEN 5 THEN economy_private.line_matches(first_line, 1, 2, 1)
                                AND economy_private.line_matches(second_line, 2, 5, 1)
                                AND economy_private.line_matches(third_line, 1, 6, 2)
                                AND economy_private.line_matches(fourth_line, 2, 4, 2)
                                AND (fourth_line->>'amount_units')::bigint = (first_line->>'amount_units')::bigint * 1000
                    WHEN 6 THEN economy_private.line_matches(first_line, 1, 7, 1)
                                AND economy_private.line_matches(second_line, 2, 5, 1)
                                AND economy_private.line_matches(third_line, 1, 6, 2)
                                AND economy_private.line_matches(fourth_line, 2, 4, 2)
                                AND (fourth_line->>'amount_units')::bigint = (first_line->>'amount_units')::bigint * 1000
                    WHEN 7 THEN (first_line->>'side')::integer = 1
                                AND (first_line->>'account_code')::integer IN (2, 3, 4)
                                AND (second_line->>'side')::integer = 2
                                AND (second_line->>'account_code')::integer IN (5, 6)
                    WHEN 8 THEN (first_line->>'side')::integer = 1
                                AND (first_line->>'account_code')::integer IN (2, 3, 4)
                                AND (second_line->>'side')::integer = 2
                                AND (second_line->>'account_code')::integer IN (9, 10)
                    WHEN 9 THEN (first_line->>'side')::integer = 1
                                AND (first_line->>'account_code')::integer IN (9, 10)
                                AND (second_line->>'side')::integer = 2
                                AND (second_line->>'account_code')::integer IN (2, 3, 4)
                    WHEN 10 THEN (first_line->>'side')::integer = 1
                                 AND (second_line->>'side')::integer = 2
                                 AND (first_line->>'account_code')::integer IN (2, 3, 4)
                                 AND (second_line->>'account_code')::integer IN (2, 3, 4)
                    WHEN 11 THEN economy_private.line_matches(first_line, 1, 3, 1)
                                 AND economy_private.line_matches(second_line, 2, 11, 1)
                    WHEN 12 THEN economy_private.line_matches(first_line, 1, 11, 1)
                                 AND economy_private.line_matches(second_line, 2, 1, 1)
                    WHEN 13 THEN economy_private.line_matches(first_line, 1, 11, 1)
                                 AND economy_private.line_matches(second_line, 2, 3, 1)
                    WHEN 14 THEN economy_private.line_matches(first_line, 1, 7, 1)
                                 AND economy_private.line_matches(second_line, 2, 12, 1)
                    WHEN 15 THEN economy_private.line_matches(first_line, 1, 12, 1)
                                 AND economy_private.line_matches(second_line, 2, 1, 1)
                    WHEN 16 THEN economy_private.line_matches(first_line, 1, 12, 1)
                                 AND economy_private.line_matches(second_line, 2, 7, 1)
                    ELSE false
                END;
            END
            $function$;

            ALTER FUNCTION economy_private.line_matches(jsonb,integer,integer,integer)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.line_matches(jsonb,integer,integer,integer) FROM PUBLIC;
            ALTER FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb) FROM PUBLIC;
            """,
            suppressTransaction: false);

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
            """,
            suppressTransaction: false);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.post_registered_posting_v1(
                p_capability_id uuid,
                p_actor_id uuid,
                p_tenant_id uuid,
                p_posting_id uuid,
                p_idempotency_key text,
                p_template_kind integer,
                p_template_version integer,
                p_authority integer,
                p_policy_version bigint,
                p_reserve_version bigint,
                p_risk_decision_id uuid,
                p_risk_operation_fingerprint text,
                p_expected_counter_version bigint,
                p_source_stamp_id uuid,
                p_source_evidence_hash text,
                p_requested_at timestamptz,
                p_lines jsonb,
                p_allocations jsonb,
                p_root_ranges jsonb,
                p_expected_reversal_epochs jsonb,
                p_dispatch_snapshot_hash text)
            RETURNS TABLE(posting_id uuid, journal_sequence bigint, journal_hash text, duplicate boolean)
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                risk_record record;
                chain_record record;
                canonical text;
                request_hash text;
                existing_request_hash text;
            BEGIN
                IF p_posting_id IS NULL OR p_actor_id IS NULL OR p_tenant_id IS NULL
                   OR p_idempotency_key IS NULL OR length(btrim(p_idempotency_key)) = 0
                   OR p_template_version <> 1 OR p_policy_version <= 0 OR p_reserve_version <= 0
                   OR p_expected_counter_version <= 0
                   OR jsonb_typeof(p_lines) <> 'array'
                   OR jsonb_typeof(p_allocations) <> 'array'
                   OR jsonb_typeof(p_root_ranges) <> 'array'
                   OR jsonb_typeof(p_expected_reversal_epochs) <> 'array' THEN
                    RAISE EXCEPTION 'invalid registered posting arguments' USING ERRCODE = '22023';
                END IF;

                request_hash := encode(public.digest(convert_to(jsonb_build_object(
                    'capabilityId', p_capability_id,
                    'actorId', p_actor_id,
                    'tenantId', p_tenant_id,
                    'postingId', p_posting_id,
                    'idempotencyKey', p_idempotency_key,
                    'templateKind', p_template_kind,
                    'templateVersion', p_template_version,
                    'authority', p_authority,
                    'policyVersion', p_policy_version,
                    'reserveVersion', p_reserve_version,
                    'riskDecisionId', p_risk_decision_id,
                    'riskOperationFingerprint', p_risk_operation_fingerprint,
                    'counterVersion', p_expected_counter_version,
                    'sourceStampId', p_source_stamp_id,
                    'sourceEvidenceHash', p_source_evidence_hash,
                    'requestedAt', p_requested_at,
                    'lines', p_lines,
                    'allocations', p_allocations,
                    'rootRanges', p_root_ranges,
                    'reversalEpochs', p_expected_reversal_epochs,
                    'dispatchSnapshotHash', p_dispatch_snapshot_hash)::text, 'UTF8'), 'sha256'), 'hex');

                SELECT pg."Id", je."Sequence", je."Hash", idempotency."RequestHash"
                INTO posting_id, journal_sequence, journal_hash, existing_request_hash
                FROM public.economy_posting_groups pg
                JOIN public.economy_journal_entries je ON je."PostingGroupId" = pg."Id"
                JOIN public.economy_idempotency_records idempotency ON idempotency."PostingGroupId" = pg."Id"
                WHERE pg."IdempotencyKey" = p_idempotency_key;
                IF FOUND THEN
                    IF posting_id <> p_posting_id OR existing_request_hash <> request_hash THEN
                        RAISE EXCEPTION 'idempotency key is bound to another request' USING ERRCODE = '23505';
                    END IF;
                    duplicate := true;
                    RETURN NEXT;
                    RETURN;
                END IF;

                PERFORM 1
                FROM public.economy_registered_capabilities capability
                WHERE capability."Id" = p_capability_id
                  AND capability."IsEnabled"
                  AND capability."RevokedAt" IS NULL
                  AND capability."AllowedTemplateKinds" @> jsonb_build_array(p_template_kind)
                FOR SHARE;
                IF NOT FOUND THEN
                    RAISE EXCEPTION 'caller capability is absent, disabled, or unauthorized' USING ERRCODE = '42501';
                END IF;

                IF NOT economy_private.validate_posting_lines_v1(p_template_kind, p_lines) THEN
                    RAISE EXCEPTION 'posting lines do not match the registered template' USING ERRCODE = '23514';
                END IF;

                SELECT * INTO risk_record
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = p_risk_decision_id
                FOR UPDATE;
                IF NOT FOUND
                   OR risk_record."Outcome" <> 1
                   OR risk_record."OperationFingerprint" <> p_risk_operation_fingerprint
                   OR risk_record."TemplateKind" <> p_template_kind
                   OR risk_record."PolicyVersion" <> p_policy_version
                   OR risk_record."ReserveVersion" <> p_reserve_version
                   OR risk_record."CounterVersion" <> p_expected_counter_version
                   OR risk_record."Currency" <> (p_lines->0->>'currency')::integer
                   OR risk_record."AmountUnits" <> (p_lines->0->>'amount_units')::bigint
                   OR risk_record."IssuedAt" > p_requested_at
                   OR risk_record."ExpiresAt" <= p_requested_at THEN
                    RAISE EXCEPTION 'risk decision is missing, stale, denied, or operation-mismatched' USING ERRCODE = '42501';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM public.economy_risk_decision_consumptions
                    WHERE "RiskDecisionId" = p_risk_decision_id
                ) THEN
                    RAISE EXCEPTION 'risk decision has already been consumed' USING ERRCODE = '23505';
                END IF;
                IF NOT EXISTS (
                    SELECT 1
                    FROM public.economy_risk_counter_reservations reservation
                    JOIN public.economy_risk_counters counter ON counter."Id" = reservation."RiskCounterId"
                    WHERE reservation."RiskDecisionId" = p_risk_decision_id
                      AND reservation."AmountUnits" = risk_record."AmountUnits"
                      AND counter."CounterVersion" = p_expected_counter_version
                ) THEN
                    RAISE EXCEPTION 'risk decision has no persisted aggregate-counter reservation' USING ERRCODE = '42501';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) line
                    LEFT JOIN public.economy_accounts account ON account."Id" = (line->>'account_id')::uuid
                    WHERE account."Id" IS NULL
                       OR account."Code" <> (line->>'account_code')::integer
                       OR account."Currency" <> (line->>'currency')::integer
                       OR account."WalletId" IS DISTINCT FROM NULLIF(line->>'wallet_id', '')::uuid
                ) THEN
                    RAISE EXCEPTION 'posting line does not match its registered account partition' USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_expected_reversal_epochs) expected
                    LEFT JOIN public.economy_root_reversal_states reversal
                        ON reversal."RootSourceStampId" = (expected->>'root_source_stamp_id')::uuid
                    WHERE COALESCE(reversal."Epoch", 0) <> (expected->>'expected_epoch')::bigint
                ) OR EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_root_ranges) root_range
                    WHERE NOT EXISTS (
                        SELECT 1 FROM jsonb_array_elements(p_expected_reversal_epochs) expected
                        WHERE expected->>'root_source_stamp_id' = root_range->>'root_source_stamp_id'
                          AND (expected->>'expected_epoch')::bigint = (root_range->>'reversal_epoch')::bigint)
                ) THEN
                    RAISE EXCEPTION 'root range uses a stale or absent reversal epoch fence' USING ERRCODE = '23514';
                END IF;

                IF p_template_kind IN (1, 2, 3) AND p_source_stamp_id IS NULL THEN
                    RAISE EXCEPTION 'registered template requires source evidence' USING ERRCODE = '23514';
                END IF;
                IF p_source_stamp_id IS NOT NULL THEN
                    PERFORM 1
                    FROM public.economy_source_stamps source
                    WHERE source."Id" = p_source_stamp_id
                      AND source."EvidenceHash" = p_source_evidence_hash
                      AND source."PolicyVersion" = p_policy_version
                      AND (source."PostingReferenceId" IS NULL OR source."PostingReferenceId" = p_posting_id)
                      AND (p_template_kind <> 1 OR (
                          source."State" = 2
                          AND source."ConfirmedAt" IS NOT NULL
                          AND source."ConfirmedAt" <= p_requested_at
                          AND source."AuthoritativeUnits" >= (p_lines->0->>'amount_units')::bigint))
                    FOR SHARE;
                    IF NOT FOUND THEN
                        RAISE EXCEPTION 'source evidence is absent or mismatched' USING ERRCODE = '23514';
                    END IF;
                END IF;

                INSERT INTO public.economy_chain_head ("Id", "Sequence", "Hash", "UpdatedAt")
                VALUES (1, 0, repeat('0', 64), p_requested_at)
                ON CONFLICT ("Id") DO NOTHING;
                SELECT "Sequence", "Hash" INTO chain_record
                FROM public.economy_chain_head WHERE "Id" = 1 FOR UPDATE;

                journal_sequence := chain_record."Sequence" + 1;
                canonical := concat_ws('|', chain_record."Hash", p_posting_id::text,
                    journal_sequence::text, request_hash);
                journal_hash := encode(public.digest(convert_to(canonical, 'UTF8'), 'sha256'), 'hex');

                INSERT INTO public.economy_posting_groups (
                    "Id", "IdempotencyKey", "TemplateKind", "TemplateVersion", "Authority", "Status",
                    "CapabilityId", "ActorId", "TenantId", "RiskDecisionId", "PolicyVersion", "ReserveVersion",
                    "SourceStampId", "RecordedAt")
                VALUES (
                    p_posting_id, p_idempotency_key, p_template_kind, p_template_version, p_authority, 1,
                    p_capability_id, p_actor_id, p_tenant_id, p_risk_decision_id, p_policy_version,
                    p_reserve_version, p_source_stamp_id, p_requested_at);

                INSERT INTO public.economy_journal_entries (
                    "Id", "PostingGroupId", "Sequence", "PreviousHash", "Hash", "RecordedAt")
                VALUES (gen_random_uuid(), p_posting_id, journal_sequence, chain_record."Hash", journal_hash, p_requested_at);

                WITH entry AS (
                    SELECT "Id" FROM public.economy_journal_entries WHERE "PostingGroupId" = p_posting_id
                )
                INSERT INTO public.economy_journal_lines (
                    "Id", "JournalEntryId", "AccountId", "WalletId", "CreditLotId", "Sequence",
                    "Side", "Currency", "AmountUnits", "Provenance")
                SELECT
                    (line->>'id')::uuid,
                    entry."Id",
                    (line->>'account_id')::uuid,
                    NULLIF(line->>'wallet_id', '')::uuid,
                    NULLIF(line->>'credit_lot_id', '')::uuid,
                    ordinal::integer,
                    (line->>'side')::integer,
                    (line->>'currency')::integer,
                    (line->>'amount_units')::bigint,
                    NULLIF(line->>'provenance', '')::integer
                FROM jsonb_array_elements(p_lines) WITH ORDINALITY AS item(line, ordinal)
                CROSS JOIN entry;

                INSERT INTO public.economy_entry_allocations (
                    "Id", "JournalLineId", "ParentLotId", "AmountUnits")
                SELECT
                    (allocation->>'id')::uuid,
                    (allocation->>'journal_line_id')::uuid,
                    (allocation->>'parent_lot_id')::uuid,
                    (allocation->>'amount_units')::bigint
                FROM jsonb_array_elements(p_allocations) allocation;

                INSERT INTO public.economy_fragment_root_ranges (
                    "Id", "RootSourceStampId", "CreditLotId", "EntryAllocationId",
                    "StartInclusive", "EndExclusive", "ReversalEpoch")
                SELECT
                    (root_range->>'id')::uuid,
                    (root_range->>'root_source_stamp_id')::uuid,
                    NULLIF(root_range->>'credit_lot_id', '')::uuid,
                    NULLIF(root_range->>'entry_allocation_id', '')::uuid,
                    (root_range->>'start_inclusive')::bigint,
                    (root_range->>'end_exclusive')::bigint,
                    (root_range->>'reversal_epoch')::bigint
                FROM jsonb_array_elements(p_root_ranges) root_range;

                INSERT INTO public.economy_risk_decision_consumptions (
                    "Id", "RiskDecisionId", "PostingGroupId", "OperationFingerprint", "ConsumedAt")
                VALUES (gen_random_uuid(), p_risk_decision_id, p_posting_id, p_risk_operation_fingerprint, p_requested_at);

                INSERT INTO public.economy_idempotency_records (
                    "Id", "Key", "RequestHash", "PostingGroupId", "CreatedAt")
                VALUES (gen_random_uuid(), p_idempotency_key, request_hash, p_posting_id, p_requested_at);

                UPDATE public.economy_chain_head
                SET "Sequence" = journal_sequence, "Hash" = journal_hash, "UpdatedAt" = p_requested_at
                WHERE "Id" = 1;

                INSERT INTO public.economy_risk_audit_evidence (
                    "Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")
                VALUES (
                    gen_random_uuid(), p_risk_decision_id, 'posting-authorized', p_risk_operation_fingerprint,
                    journal_hash, jsonb_build_object('postingId', p_posting_id, 'sequence', journal_sequence), p_requested_at);

                posting_id := p_posting_id;
                duplicate := false;
                RETURN NEXT;
            END
            $function$;

            ALTER FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text) FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.post_registered_posting_v1(
                uuid,uuid,uuid,uuid,text,integer,integer,integer,bigint,bigint,uuid,text,bigint,
                uuid,text,timestamptz,jsonb,jsonb,jsonb,jsonb,text) TO gameguild_economy_writer;

            DO $grants$
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
                    EXECUTE format('GRANT SELECT, INSERT, UPDATE ON TABLE public.%I TO gameguild_economy_procedure_owner', relation_name);
                    EXECUTE format('GRANT ALL ON TABLE public.%I TO gameguild_economy_migration', relation_name);
                END LOOP;
            END
            $grants$;
            """);
    }

    private static void RemoveSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP SCHEMA IF EXISTS economy_private CASCADE;
            ALTER TABLE IF EXISTS public.economy_fragment_root_ranges
                DROP CONSTRAINT IF EXISTS ex_economy_fragment_root_ranges_no_overlap;
            """);
    }

    private static void RemoveRoles(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP OWNED BY gameguild_economy_runtime;
            DROP OWNED BY gameguild_economy_writer;
            DROP OWNED BY gameguild_economy_procedure_owner;
            DROP OWNED BY gameguild_economy_migration;
            DROP ROLE IF EXISTS gameguild_economy_runtime;
            DROP ROLE IF EXISTS gameguild_economy_writer;
            DROP ROLE IF EXISTS gameguild_economy_procedure_owner;
            DROP ROLE IF EXISTS gameguild_economy_migration;
            """);
    }
}
