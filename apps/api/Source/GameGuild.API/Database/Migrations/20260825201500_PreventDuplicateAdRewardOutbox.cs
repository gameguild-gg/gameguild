using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260825201500_PreventDuplicateAdRewardOutbox")]
public partial class PreventDuplicateAdRewardOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(RewriteProcedure(addConflictGuard: true));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(RewriteProcedure(addConflictGuard: false));
    }

    private static string RewriteProcedure(bool addConflictGuard)
    {
        var original = """
                    encode(public.digest(convert_to(outbox_payload, 'UTF8'), 'sha256'), 'hex'), p_issued_at);
            """;
        var guarded = """
                    encode(public.digest(convert_to(outbox_payload, 'UTF8'), 'sha256'), 'hex'), p_issued_at)
                ON CONFLICT ("PayloadHash") DO NOTHING;
            """;
        var from = addConflictGuard ? original : guarded;
        var to = addConflictGuard ? guarded : original;
        return $$"""
            DO $migration$
            DECLARE
                definition text;
                expected text := {{SqlLiteral(from)}};
                replacement text := {{SqlLiteral(to)}};
            BEGIN
                SELECT pg_get_functiondef(
                    'economy_private.post_ad_reward_issuance_v1(uuid,uuid,uuid,uuid,text,bigint,bigint,uuid,text,bigint,uuid,uuid,uuid,bigint,text,text,text,timestamptz,text)'::regprocedure)
                INTO definition;
                IF strpos(definition, expected) = 0 THEN
                    RAISE EXCEPTION 'ad reward outbox writer shape is not recognized';
                END IF;
                EXECUTE replace(definition, expected, replacement);
            END
            $migration$;
            """;
    }

    private static string SqlLiteral(string value) =>
        $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
