using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AddSelfServicePayoutReadModel
{
    private static void AddSelfServicePayoutReadModelSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.read_payout_operations_by_payee_v1(
                p_payee_id uuid,
                p_take integer)
            RETURNS SETOF public.economy_payout_operations
            LANGUAGE plpgsql
            STABLE
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            BEGIN
                IF p_payee_id IS NULL THEN
                    RAISE EXCEPTION 'Payee ID is required.' USING ERRCODE = '22023';
                END IF;

                IF p_take < 1 OR p_take > 100 THEN
                    RAISE EXCEPTION 'Take must be between 1 and 100.' USING ERRCODE = '22023';
                END IF;

                RETURN QUERY
                SELECT *
                FROM public.economy_payout_operations
                WHERE "PayeeId" = p_payee_id
                ORDER BY "CreatedAt" DESC, "Id" DESC
                LIMIT p_take;
            END;
            $function$;

            ALTER FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer)
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer)
                FROM PUBLIC;
            GRANT EXECUTE ON FUNCTION economy_private.read_payout_operations_by_payee_v1(uuid, integer)
                TO gameguild_economy_writer;
            """);
    }

    private static void RemoveSelfServicePayoutReadModelSecurity(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS economy_private.read_payout_operations_by_payee_v1(uuid, integer);
            """);
    }
}
