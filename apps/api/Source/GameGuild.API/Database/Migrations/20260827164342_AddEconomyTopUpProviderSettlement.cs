using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyTopUpProviderSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "economy_top_up_intents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastProviderEventAt",
                table: "economy_top_up_intents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEventId",
                table: "economy_top_up_intents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProviderEvidenceHash",
                table: "economy_top_up_intents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PostingGroupId",
                table: "economy_top_up_intents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "economy_top_up_intents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_economy_top_up_intents_PostingGroupId",
                table: "economy_top_up_intents",
                column: "PostingGroupId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_top_up_intents_event_state",
                table: "economy_top_up_intents",
                sql: "(\"LastProviderEventId\" IS NULL AND \"LastProviderEventAt\" IS NULL AND \"LastProviderEvidenceHash\" IS NULL) OR (\"LastProviderEventId\" IS NOT NULL AND \"LastProviderEventAt\" IS NOT NULL AND \"LastProviderEvidenceHash\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_top_up_intents_posting_state",
                table: "economy_top_up_intents",
                sql: "(\"Status\" = 5 AND \"PostingGroupId\" IS NOT NULL) OR (\"Status\" <> 5 AND \"PostingGroupId\" IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_economy_top_up_intents_economy_posting_groups_PostingGroupId",
                table: "economy_top_up_intents",
                column: "PostingGroupId",
                principalTable: "economy_posting_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            InstallEconomyTopUpSettlementSecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveEconomyTopUpSettlementSecurity(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_economy_top_up_intents_economy_posting_groups_PostingGroupId",
                table: "economy_top_up_intents");

            migrationBuilder.DropIndex(
                name: "IX_economy_top_up_intents_PostingGroupId",
                table: "economy_top_up_intents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_top_up_intents_event_state",
                table: "economy_top_up_intents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_top_up_intents_posting_state",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "LastProviderEventAt",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "LastProviderEventId",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "LastProviderEvidenceHash",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "PostingGroupId",
                table: "economy_top_up_intents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "economy_top_up_intents");
        }
    }
}
