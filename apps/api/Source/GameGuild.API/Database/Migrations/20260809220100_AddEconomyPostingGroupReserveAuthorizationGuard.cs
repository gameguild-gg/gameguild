using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809220100_AddEconomyPostingGroupReserveAuthorizationGuard")]
public partial class AddEconomyPostingGroupReserveAuthorizationGuard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION economy_private.hydrate_posting_group_reserve_authorization_v1()
            RETURNS trigger
            LANGUAGE plpgsql
            SECURITY DEFINER
            SET search_path = pg_catalog, economy_private
            AS $function$
            DECLARE
                expected_epoch bigint;
            BEGIN
                IF NEW."RiskDecisionId" IS NULL THEN
                    RAISE EXCEPTION 'posting group requires a risk decision' USING ERRCODE = '23514';
                END IF;

                SELECT decision."ReserveAuthorizationEpoch"
                INTO expected_epoch
                FROM public.economy_risk_decisions decision
                WHERE decision."Id" = NEW."RiskDecisionId"
                FOR SHARE;
                IF NOT FOUND OR expected_epoch <= 0 THEN
                    RAISE EXCEPTION 'posting group risk decision has no active reserve authorization' USING ERRCODE = '23514';
                END IF;

                IF NEW."ReserveAuthorizationEpoch" IS NULL THEN
                    NEW."ReserveAuthorizationEpoch" := expected_epoch;
                ELSIF NEW."ReserveAuthorizationEpoch" <> expected_epoch THEN
                    RAISE EXCEPTION 'posting group reserve authorization does not match the risk decision' USING ERRCODE = '23514';
                END IF;

                RETURN NEW;
            END
            $function$;

            ALTER FUNCTION economy_private.hydrate_posting_group_reserve_authorization_v1()
                OWNER TO gameguild_economy_procedure_owner;
            REVOKE ALL ON FUNCTION economy_private.hydrate_posting_group_reserve_authorization_v1() FROM PUBLIC;

            CREATE TRIGGER tr_economy_posting_groups_reserve_authorization
            BEFORE INSERT OR UPDATE OF "RiskDecisionId", "ReserveAuthorizationEpoch"
            ON public.economy_posting_groups
            FOR EACH ROW EXECUTE FUNCTION economy_private.hydrate_posting_group_reserve_authorization_v1();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER IF EXISTS tr_economy_posting_groups_reserve_authorization ON public.economy_posting_groups;
            DROP FUNCTION IF EXISTS economy_private.hydrate_posting_group_reserve_authorization_v1();
            """);
    }
}
