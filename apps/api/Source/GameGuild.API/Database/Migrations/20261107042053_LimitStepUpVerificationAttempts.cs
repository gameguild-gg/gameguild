using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class LimitStepUpVerificationAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "verification_attempts",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_step_up_challenges_expiry_cleanup",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                column: "expires_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_step_up_challenges_verification_attempts",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                sql: "verification_attempts >= 0 AND verification_attempts <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A downgrade must not reopen a retained challenge's spent budget or
            // silently erase the security evidence kept for the retention window.
            migrationBuilder.Sql("""
                DO $guard$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "gameguild.authentication"."step_up_challenges"
                        WHERE verification_attempts <> 0
                    ) THEN
                        RAISE EXCEPTION 'Cannot drop spent step-up verification attempt evidence.'
                            USING ERRCODE = '23514',
                                CONSTRAINT = 'ck_step_up_challenges_attempts_downgrade';
                    END IF;
                END
                $guard$;
                """);

            migrationBuilder.DropIndex(
                name: "ix_step_up_challenges_expiry_cleanup",
                schema: "gameguild.authentication",
                table: "step_up_challenges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_step_up_challenges_verification_attempts",
                schema: "gameguild.authentication",
                table: "step_up_challenges");

            migrationBuilder.DropColumn(
                name: "verification_attempts",
                schema: "gameguild.authentication",
                table: "step_up_challenges");
        }
    }
}
