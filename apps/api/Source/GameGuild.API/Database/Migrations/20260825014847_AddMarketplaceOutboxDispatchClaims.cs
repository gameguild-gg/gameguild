using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceOutboxDispatchClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "economy_marketplace_outbox",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeaseExpiresAt",
                table: "economy_marketplace_outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "economy_marketplace_outbox",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_marketplace_outbox_PublishedAt_LeaseExpiresAt_Occur~",
                table: "economy_marketplace_outbox",
                columns: new[] { "PublishedAt", "LeaseExpiresAt", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_economy_marketplace_outbox_PublishedAt_LeaseExpiresAt_Occur~",
                table: "economy_marketplace_outbox");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "economy_marketplace_outbox");

            migrationBuilder.DropColumn(
                name: "LeaseExpiresAt",
                table: "economy_marketplace_outbox");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "economy_marketplace_outbox");
        }
    }
}
