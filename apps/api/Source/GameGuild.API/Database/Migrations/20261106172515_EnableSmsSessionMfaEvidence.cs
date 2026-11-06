using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnableSmsSessionMfaEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_session_mfa_method",
                schema: "gameguild.authentication",
                table: "session_mfa_evidence");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_mfa_method",
                schema: "gameguild.authentication",
                table: "session_mfa_evidence",
                sql: "method IN ('Totp', 'BackupCode', 'WebAuthn', 'Sms')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_session_mfa_method",
                schema: "gameguild.authentication",
                table: "session_mfa_evidence");

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_mfa_method",
                schema: "gameguild.authentication",
                table: "session_mfa_evidence",
                sql: "method IN ('Totp', 'BackupCode', 'WebAuthn')");
        }
    }
}
