using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantScopedBounties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_economy_bounties_PosterId_Status_ExpiresAt",
                table: "economy_bounties");

            migrationBuilder.DropIndex(
                name: "IX_economy_bounties_Status_ExpiresAt",
                table: "economy_bounties");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_bounty_terminal_events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "economy_bounties",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_TenantId_PosterId_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "TenantId", "PosterId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_TenantId_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "TenantId", "Status", "ExpiresAt" });

            InstallTenantScopedBountySecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveTenantScopedBountySecurity(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_economy_bounties_TenantId_PosterId_Status_ExpiresAt",
                table: "economy_bounties");

            migrationBuilder.DropIndex(
                name: "IX_economy_bounties_TenantId_Status_ExpiresAt",
                table: "economy_bounties");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_bounty_terminal_events");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "economy_bounties");

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_PosterId_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "PosterId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_bounties_Status_ExpiresAt",
                table: "economy_bounties",
                columns: new[] { "Status", "ExpiresAt" });
        }
    }
}
