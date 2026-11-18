using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyCreatorCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_api_key_id",
                schema: "gameguild.authentication",
                table: "api_keys",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_created_by_api_key_id",
                schema: "gameguild.authentication",
                table: "api_keys",
                column: "created_by_api_key_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_api_keys_created_by_api_key_id",
                schema: "gameguild.authentication",
                table: "api_keys");

            migrationBuilder.DropColumn(
                name: "created_by_api_key_id",
                schema: "gameguild.authentication",
                table: "api_keys");
        }
    }
}
