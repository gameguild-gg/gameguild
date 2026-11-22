using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class BindStepUpWebAuthnChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "webauthn_challenge_hash",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_step_up_challenges_webauthn_hash",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                sql: "webauthn_challenge_hash IS NULL OR webauthn_challenge_hash ~ '^[0-9a-f]{64}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Downgrading must not silently erase the binding evidence attached to
            // existing challenges. Expired-row retention/cleanup remains explicit.
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "gameguild.authentication"."step_up_challenges"
                        WHERE webauthn_challenge_hash IS NOT NULL
                    ) THEN
                        RAISE EXCEPTION 'Cannot drop bound WebAuthn step-up evidence.'
                            USING ERRCODE = '23514',
                                CONSTRAINT = 'ck_step_up_challenges_webauthn_downgrade';
                    END IF;
                END
                $guard$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_step_up_challenges_webauthn_hash",
                schema: "gameguild.authentication",
                table: "step_up_challenges");

            migrationBuilder.DropColumn(
                name: "webauthn_challenge_hash",
                schema: "gameguild.authentication",
                table: "step_up_challenges");
        }
    }
}
