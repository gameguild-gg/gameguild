using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class BindKycJurisdictionToEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JurisdictionCode",
                table: "economy_compliance_evidence",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JurisdictionCode",
                table: "compliance_sumsub_applicant_bindings",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_compliance_evidence_jurisdiction",
                table: "economy_compliance_evidence",
                sql: "\"JurisdictionCode\" IS NULL OR \"JurisdictionCode\" ~ '^[A-Z]{3}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_compliance_sumsub_applicant_bindings_jurisdiction",
                table: "compliance_sumsub_applicant_bindings",
                sql: "\"JurisdictionCode\" IS NULL OR \"JurisdictionCode\" ~ '^[A-Z]{3}$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_compliance_evidence_jurisdiction",
                table: "economy_compliance_evidence");

            migrationBuilder.DropCheckConstraint(
                name: "ck_compliance_sumsub_applicant_bindings_jurisdiction",
                table: "compliance_sumsub_applicant_bindings");

            migrationBuilder.DropColumn(
                name: "JurisdictionCode",
                table: "economy_compliance_evidence");

            migrationBuilder.DropColumn(
                name: "JurisdictionCode",
                table: "compliance_sumsub_applicant_bindings");
        }
    }
}
