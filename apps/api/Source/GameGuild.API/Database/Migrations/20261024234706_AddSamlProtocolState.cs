using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSamlProtocolState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saml_protocol_state",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    state_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    state_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    relay_state_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: ""),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saml_protocol_state", x => new { x.state_kind, x.provider_slug, x.state_key });
                    table.CheckConstraint("ck_saml_protocol_state_kind", "state_kind IN ('pending_request', 'assertion_reservation')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_saml_protocol_state_expires_at",
                schema: "gameguild.authentication",
                table: "saml_protocol_state",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saml_protocol_state",
                schema: "gameguild.authentication");
        }
    }
}
