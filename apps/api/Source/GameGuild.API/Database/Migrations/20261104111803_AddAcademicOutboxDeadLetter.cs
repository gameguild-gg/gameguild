using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademicOutboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Lifecycle",
                table: "AcademicOutboxDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Status",
                table: "AcademicOutboxDeliveries");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAt",
                table: "AcademicOutboxDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Lifecycle",
                table: "AcademicOutboxDeliveries",
                sql: "(\"Status\" = 'processing') = (\"ClaimedAt\" IS NOT NULL AND \"ClaimedBy\" IS NOT NULL) AND (\"Status\" = 'confirmed') = (\"ConfirmedAt\" IS NOT NULL) AND (\"Status\" = 'failed') = (\"NextAttemptAt\" IS NOT NULL) AND (\"Status\" = 'dead_lettered') = (\"DeadLetteredAt\" IS NOT NULL) AND (\"DeadLetteredAt\" IS NULL OR (\"NextAttemptAt\" IS NULL AND \"ConfirmedAt\" IS NULL))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Status",
                table: "AcademicOutboxDeliveries",
                sql: "\"Status\" IN ('pending', 'processing', 'confirmed', 'failed', 'deadlettered')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Lifecycle",
                table: "AcademicOutboxDeliveries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Status",
                table: "AcademicOutboxDeliveries");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAt",
                table: "AcademicOutboxDeliveries");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Lifecycle",
                table: "AcademicOutboxDeliveries",
                sql: "(\"Status\" = 'processing') = (\"ClaimedAt\" IS NOT NULL AND \"ClaimedBy\" IS NOT NULL) AND (\"Status\" = 'confirmed') = (\"ConfirmedAt\" IS NOT NULL) AND (\"Status\" = 'failed') = (\"NextAttemptAt\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AcademicOutboxDeliveries_Status",
                table: "AcademicOutboxDeliveries",
                sql: "\"Status\" IN ('pending', 'processing', 'confirmed', 'failed')");
        }
    }
}
