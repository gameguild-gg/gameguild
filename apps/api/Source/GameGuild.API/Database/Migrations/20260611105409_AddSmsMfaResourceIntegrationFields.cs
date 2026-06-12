using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsMfaResourceIntegrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_sms_enabled",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sms_phone_number",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sms_verification_code_hash",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sms_verification_expires_at",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sms_enabled",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_phone_number",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_verification_code_hash",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");

            migrationBuilder.DropColumn(
                name: "sms_verification_expires_at",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration");
        }
    }
}
