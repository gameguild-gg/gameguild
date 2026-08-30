using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeComplianceHoldRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReleasePolicyEvidenceHash",
                table: "economy_compliance_holds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReleaseProposedAt",
                table: "economy_compliance_holds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReleaseProposedBy",
                table: "economy_compliance_holds",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredReleaseApprovals",
                table: "economy_compliance_holds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_compliance_holds_release_proposal",
                table: "economy_compliance_holds",
                sql: "(\"ReleaseProposedAt\" IS NULL AND \"ReleaseProposedBy\" IS NULL AND \"RequiredReleaseApprovals\" IS NULL AND \"ReleasePolicyEvidenceHash\" IS NULL) OR (\"ReleaseProposedAt\" >= \"ActivatedAt\" AND \"ReleaseProposedBy\" IS NOT NULL AND \"RequiredReleaseApprovals\" BETWEEN 1 AND 2 AND length(btrim(\"ReleasePolicyEvidenceHash\")) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_economy_compliance_hold_events_HoldId_Kind_ActorId",
                table: "economy_compliance_hold_events",
                columns: new[] { "HoldId", "Kind", "ActorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_compliance_holds_release_proposal",
                table: "economy_compliance_holds");

            migrationBuilder.DropIndex(
                name: "IX_economy_compliance_hold_events_HoldId_Kind_ActorId",
                table: "economy_compliance_hold_events");

            migrationBuilder.DropColumn(
                name: "ReleasePolicyEvidenceHash",
                table: "economy_compliance_holds");

            migrationBuilder.DropColumn(
                name: "ReleaseProposedAt",
                table: "economy_compliance_holds");

            migrationBuilder.DropColumn(
                name: "ReleaseProposedBy",
                table: "economy_compliance_holds");

            migrationBuilder.DropColumn(
                name: "RequiredReleaseApprovals",
                table: "economy_compliance_holds");
        }
    }
}
