using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddJwtSigningKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "jwt_signing_keys",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    key_material = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    algorithm = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rotation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    key_version = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jwt_signing_keys", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_jwt_signing_keys_expires_at",
                schema: "gameguild.authentication",
                table: "jwt_signing_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_jwt_signing_keys_is_active",
                schema: "gameguild.authentication",
                table: "jwt_signing_keys",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ux_jwt_signing_keys_key_id",
                schema: "gameguild.authentication",
                table: "jwt_signing_keys",
                column: "key_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jwt_signing_keys",
                schema: "gameguild.authentication");
        }
    }
}
