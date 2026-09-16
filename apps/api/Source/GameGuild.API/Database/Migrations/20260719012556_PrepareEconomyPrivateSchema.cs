using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260719012556_PrepareEconomyPrivateSchema")]
public class PrepareEconomyPrivateSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $roles$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_procedure_owner') THEN
                    CREATE ROLE gameguild_economy_procedure_owner NOLOGIN;
                END IF;
            END
            $roles$;

            CREATE SCHEMA IF NOT EXISTS economy_private;
            REVOKE ALL ON SCHEMA economy_private FROM PUBLIC;
            GRANT USAGE, CREATE ON SCHEMA economy_private
                TO gameguild_economy_procedure_owner;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $permissions$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'economy_private')
                   AND EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gameguild_economy_procedure_owner') THEN
                    REVOKE CREATE ON SCHEMA economy_private FROM gameguild_economy_procedure_owner;
                END IF;
            END
            $permissions$;
            """);
    }
}
