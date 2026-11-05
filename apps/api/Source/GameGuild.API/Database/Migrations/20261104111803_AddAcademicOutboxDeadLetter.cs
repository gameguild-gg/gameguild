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
                sql: "(\"Status\" = 'processing') = (\"ClaimedAt\" IS NOT NULL AND \"ClaimedBy\" IS NOT NULL) AND (\"Status\" = 'confirmed') = (\"ConfirmedAt\" IS NOT NULL) AND (\"Status\" = 'failed') = (\"NextAttemptAt\" IS NOT NULL) AND (\"Status\" = 'deadlettered') = (\"DeadLetteredAt\" IS NOT NULL) AND (\"DeadLetteredAt\" IS NULL OR (\"NextAttemptAt\" IS NULL AND \"ConfirmedAt\" IS NULL))");

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

            // Rehearsal F4 (docs/operations/migration-rehearsal.md, 2026-11-04): the restored
            // status constraint below no longer admits 'deadlettered', so re-adding it with
            // terminal rows present fails with 23514 and strands the database mid-revert.
            // Rewrite terminal deliveries to 'failed' (with NextAttemptAt repopulated so the
            // restored lifecycle constraint holds) while both constraints are dropped.
            // DeadLetteredAt bookkeeping is forfeited with the column drop below — record the
            // rollback as a DLQ-history reset (mirrors finding F1's policy-reset trade-off).
            migrationBuilder.Sql(
                "UPDATE \"AcademicOutboxDeliveries\" " +
                "SET \"Status\" = 'failed', " +
                "\"NextAttemptAt\" = COALESCE(\"NextAttemptAt\", now()), " +
                "\"DeadLetteredAt\" = NULL " +
                "WHERE \"Status\" = 'deadlettered'");

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
