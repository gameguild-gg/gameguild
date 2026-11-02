using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDeviceFingerprintSeenDeviceRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "userdevicefingerprint",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userdevicefingerprint", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_userdevicefingerprint_user_id",
                schema: "gameguild.authentication",
                table: "userdevicefingerprint",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_userdevicefingerprint_user_id_device_fingerprint",
                schema: "gameguild.authentication",
                table: "userdevicefingerprint",
                columns: new[] { "UserId", "DeviceFingerprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userdevicefingerprint",
                schema: "gameguild.authentication");
        }
    }
}
