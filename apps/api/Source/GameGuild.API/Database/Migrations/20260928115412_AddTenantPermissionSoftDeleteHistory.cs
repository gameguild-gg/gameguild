using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantPermissionSoftDeleteHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantPermissions_TenantId_UserId",
                table: "TenantPermissions");

            migrationBuilder.DropIndex(
                name: "IX_TenantPermissions_User_Tenant",
                table: "TenantPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_User_Tenant",
                table: "TenantPermissions",
                columns: new[] { "UserId", "TenantId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenantPermissions_User_Tenant",
                table: "TenantPermissions");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_TenantId_UserId",
                table: "TenantPermissions",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_User_Tenant",
                table: "TenantPermissions",
                columns: new[] { "UserId", "TenantId" },
                unique: true);
        }
    }
}
