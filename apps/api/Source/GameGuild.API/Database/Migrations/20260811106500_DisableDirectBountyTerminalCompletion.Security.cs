using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class DisableDirectBountyTerminalCompletion
{
    private static void DisableDirectBountyTerminalCompletionWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            REVOKE EXECUTE ON FUNCTION economy_private.complete_bounty_terminal_v1(
                uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz)
                FROM gameguild_economy_writer;
            """);
    }

    private static void RestoreDirectBountyTerminalCompletionWriter(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            GRANT EXECUTE ON FUNCTION economy_private.complete_bounty_terminal_v1(
                uuid,uuid,integer,uuid,uuid,text,uuid,uuid,uuid,bigint,bigint,bigint,jsonb,timestamptz)
                TO gameguild_economy_writer;
            """);
    }
}
