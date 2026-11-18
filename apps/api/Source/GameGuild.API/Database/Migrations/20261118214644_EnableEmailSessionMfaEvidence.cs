using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnableEmailSessionMfaEvidence : Migration
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
                sql: "method IN ('Totp', 'BackupCode', 'WebAuthn', 'Sms', 'Email')");
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
                sql: "method IN ('Totp', 'BackupCode', 'WebAuthn', 'Sms')");
        }
    }
}
