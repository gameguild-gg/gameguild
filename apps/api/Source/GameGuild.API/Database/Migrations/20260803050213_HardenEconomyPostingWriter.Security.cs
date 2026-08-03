using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

public partial class HardenEconomyPostingWriter
{
    private static void HardenWriterFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.line_matches_v2(
                p_line jsonb,
                p_side integer,
                p_account integer,
                p_currency integer,
                p_wallet_required boolean,
                p_provenance integer)
            RETURNS boolean
            LANGUAGE sql
            IMMUTABLE
            SET search_path = pg_catalog, economy_private
            AS $function$
                SELECT (p_line->>'side')::integer = p_side
                   AND (p_line->>'account_code')::integer = p_account
                   AND (p_line->>'currency')::integer = p_currency
                   AND CASE
                       WHEN p_wallet_required THEN NULLIF(p_line->>'wallet_id', '') IS NOT NULL
                       ELSE NULLIF(p_line->>'wallet_id', '') IS NULL
                   END
                   AND NULLIF(p_line->>'provenance', '')::integer IS NOT DISTINCT FROM p_provenance
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
                first_currency integer;
                first_account integer;
                second_account integer;
            BEGIN
                IF jsonb_typeof(p_lines) <> 'array' OR p_template_kind NOT BETWEEN 1 AND 21 THEN
                    RETURN false;
                END IF;

                expected_count := CASE WHEN p_template_kind IN (5, 6, 18) THEN 4 ELSE 2 END;
                IF jsonb_array_length(p_lines) <> expected_count THEN
                    RETURN false;
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM jsonb_array_elements(p_lines) AS line
                    WHERE COALESCE((line->>'amount_units')::bigint, 0) <= 0
                       OR COALESCE((line->>'side')::integer, 0) NOT IN (1, 2)
                       OR COALESCE((line->>'currency')::integer, 0) NOT IN (1, 2)
                       OR COALESCE((line->>'account_code')::integer, 0) NOT BETWEEN 1 AND 15
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
                first_currency := (first_line->>'currency')::integer;
                first_account := (first_line->>'account_code')::integer;
                second_account := (second_line->>'account_code')::integer;

                RETURN CASE p_template_kind
                    WHEN 1 THEN economy_private.line_matches_v2(first_line, 1, 1, 1, false, NULL)
                                AND economy_private.line_matches_v2(second_line, 2, 2, 1, true, 1)
                    WHEN 2 THEN economy_private.line_matches_v2(first_line, 1, 2, 1, true, 1)
                                AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 3 THEN economy_private.line_matches_v2(first_line, 1, 2, 1, true, 1)
                                AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 4 THEN (first_line->>'side')::integer = 1
                                AND (second_line->>'side')::integer = 2
                                AND first_account = second_account
                                AND (second_line->>'currency')::integer = first_currency
                                AND ((first_currency = 1 AND first_account IN (2, 3))
                                  OR (first_currency = 2 AND first_account = 4))
                                AND NULLIF(first_line->>'wallet_id', '') IS NOT NULL
                                AND NULLIF(second_line->>'wallet_id', '') IS NOT NULL
                    WHEN 5 THEN economy_private.line_matches_v2(first_line, 1, 2, 1, true, 1)
                                AND economy_private.line_matches_v2(second_line, 2, 5, 1, false, NULL)
                                AND economy_private.line_matches_v2(third_line, 1, 6, 2, false, NULL)
                                AND economy_private.line_matches_v2(fourth_line, 2, 4, 2, true, 3)
                                AND (first_line->>'amount_units')::bigint <= 9223372036854775
                                AND (fourth_line->>'amount_units')::bigint = (first_line->>'amount_units')::bigint * 1000
                    WHEN 6 THEN economy_private.line_matches_v2(first_line, 1, 7, 1, false, NULL)
                                AND economy_private.line_matches_v2(second_line, 2, 5, 1, false, NULL)
                                AND economy_private.line_matches_v2(third_line, 1, 6, 2, false, NULL)
                                AND economy_private.line_matches_v2(fourth_line, 2, 4, 2, true, 5)
                                AND (first_line->>'amount_units')::bigint <= 9223372036854775
                                AND (fourth_line->>'amount_units')::bigint = (first_line->>'amount_units')::bigint * 1000
                    WHEN 7 THEN (first_line->>'side')::integer = 1
                                AND (second_line->>'side')::integer = 2
                                AND ((first_currency = 1 AND first_account IN (2, 3) AND second_account = 5)
                                  OR (first_currency = 2 AND first_account = 4 AND second_account = 6))
                                AND (second_line->>'currency')::integer = first_currency
                                AND NULLIF(first_line->>'wallet_id', '') IS NOT NULL
                                AND NULLIF(second_line->>'wallet_id', '') IS NULL
                                AND NULLIF(second_line->>'provenance', '') IS NULL
                    WHEN 8 THEN (first_line->>'side')::integer = 1
                                AND (second_line->>'side')::integer = 2
                                AND ((first_currency = 1 AND first_account IN (2, 3) AND second_account = 9)
                                  OR (first_currency = 2 AND first_account = 4 AND second_account = 10))
                                AND (second_line->>'currency')::integer = first_currency
                                AND NULLIF(first_line->>'wallet_id', '') IS NOT NULL
                                AND NULLIF(second_line->>'wallet_id', '') IS NULL
                                AND NULLIF(second_line->>'provenance', '') IS NULL
                    WHEN 9 THEN (first_line->>'side')::integer = 1
                                AND (second_line->>'side')::integer = 2
                                AND ((first_currency = 1 AND first_account = 9 AND second_account IN (2, 3))
                                  OR (first_currency = 2 AND first_account = 10 AND second_account = 4))
                                AND (second_line->>'currency')::integer = first_currency
                                AND NULLIF(first_line->>'wallet_id', '') IS NULL
                                AND NULLIF(second_line->>'wallet_id', '') IS NOT NULL
                                AND NULLIF(first_line->>'provenance', '') IS NULL
                                AND NULLIF(second_line->>'provenance', '')::integer = 7
                    WHEN 10 THEN (first_line->>'side')::integer = 1
                                 AND (second_line->>'side')::integer = 2
                                 AND first_account = second_account
                                 AND (second_line->>'currency')::integer = first_currency
                                 AND ((first_currency = 1 AND first_account IN (2, 3))
                                   OR (first_currency = 2 AND first_account = 4))
                                 AND NULLIF(first_line->>'wallet_id', '') IS NOT NULL
                                 AND NULLIF(second_line->>'wallet_id', '') IS NOT NULL
                                 AND NULLIF(second_line->>'provenance', '')::integer = 6
                    WHEN 11 THEN economy_private.line_matches_v2(first_line, 1, 3, 1, true, 2)
                                 AND economy_private.line_matches_v2(second_line, 2, 11, 1, false, NULL)
                    WHEN 12 THEN economy_private.line_matches_v2(first_line, 1, 11, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 13 THEN economy_private.line_matches_v2(first_line, 1, 11, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 3, 1, true, 2)
                    WHEN 14 THEN economy_private.line_matches_v2(first_line, 1, 7, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 12, 1, false, NULL)
                    WHEN 15 THEN economy_private.line_matches_v2(first_line, 1, 12, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 16 THEN economy_private.line_matches_v2(first_line, 1, 12, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 7, 1, false, NULL)
                    WHEN 17 THEN economy_private.line_matches_v2(first_line, 1, 2, 1, true, 1)
                                 AND economy_private.line_matches_v2(second_line, 2, 14, 1, false, NULL)
                    WHEN 18 THEN economy_private.line_matches_v2(first_line, 1, 4, 2, true, 3)
                                 AND economy_private.line_matches_v2(second_line, 2, 6, 2, false, NULL)
                                 AND economy_private.line_matches_v2(third_line, 1, 5, 1, false, NULL)
                                 AND economy_private.line_matches_v2(fourth_line, 2, 1, 1, false, NULL)
                                 AND (third_line->>'amount_units')::bigint <= 9223372036854775
                                 AND (first_line->>'amount_units')::bigint = (third_line->>'amount_units')::bigint * 1000
                    WHEN 19 THEN economy_private.line_matches_v2(first_line, 1, 13, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 20 THEN economy_private.line_matches_v2(first_line, 1, 15, 1, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 1, 1, false, NULL)
                    WHEN 21 THEN economy_private.line_matches_v2(first_line, 1, 6, 2, false, NULL)
                                 AND economy_private.line_matches_v2(second_line, 2, 4, 2, true, 4)
                    ELSE false
                END;
            END
            $function$;

            ALTER FUNCTION economy_private.line_matches_v2(jsonb,integer,integer,integer,boolean,integer)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.line_matches_v2(jsonb,integer,integer,integer,boolean,integer)
                FROM PUBLIC;
            ALTER FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb) FROM PUBLIC;
            """);
    }

    private static void RestoreWriterFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
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

            ALTER FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.validate_posting_lines_v1(integer,jsonb) FROM PUBLIC;

            DROP FUNCTION IF EXISTS economy_private.line_matches_v2(jsonb,integer,integer,integer,boolean,integer);
            """);
    }
}
