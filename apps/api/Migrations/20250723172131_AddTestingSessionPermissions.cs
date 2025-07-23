using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingSessionPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestingSessionPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingSessionPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_TestingSessions_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TestingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingSessionPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_CreatedAt",
                table: "TestingSessionPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_DeletedAt",
                table: "TestingSessionPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_Expiration",
                table: "TestingSessionPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_Resource_User",
                table: "TestingSessionPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_TenantId",
                table: "TestingSessionPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessionPermissions_User_Tenant_Resource",
                table: "TestingSessionPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestingSessionPermissions");
        }
    }
}
