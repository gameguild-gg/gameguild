using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddTestingRequestPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestingRequestPermissions",
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
                    table.PrimaryKey("PK_TestingRequestPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_TestingRequests_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TestingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingRequestPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_CreatedAt",
                table: "TestingRequestPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_DeletedAt",
                table: "TestingRequestPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_ResourceId",
                table: "TestingRequestPermissions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_TenantId",
                table: "TestingRequestPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequestPermissions_UserId",
                table: "TestingRequestPermissions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestingRequestPermissions");
        }
    }
}
