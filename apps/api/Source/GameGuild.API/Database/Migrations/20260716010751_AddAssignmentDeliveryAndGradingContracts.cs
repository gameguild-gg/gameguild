using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
public partial class AddAssignmentDeliveryAndGradingContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "AllowLateSubmissions",
            table: "Assessments",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "DueAt",
            table: "Assessments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LateSubmissionDeadline",
            table: "Assessments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PresentationMode",
            table: "Assessments",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "SubmissionModalities",
            table: "Assessments",
            type: "integer",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "CodePayload",
            table: "AssessmentSubmissions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FilePayload",
            table: "AssessmentSubmissions",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "IsLate",
            table: "AssessmentSubmissions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "MediaPayload",
            table: "AssessmentSubmissions",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProjectPayload",
            table: "AssessmentSubmissions",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StructuredAnswerPayload",
            table: "AssessmentSubmissions",
            type: "jsonb",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SubmittedModalities",
            table: "AssessmentSubmissions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "TextPayload",
            table: "AssessmentSubmissions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "UrlPayload",
            table: "AssessmentSubmissions",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE \"Assessments\" SET \"AvailableFrom\" = \"AvailableUntil\", \"AvailableUntil\" = \"AvailableFrom\" WHERE \"AvailableFrom\" > \"AvailableUntil\"");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Assessments_SubmissionModalities",
            table: "Assessments",
            sql: "\"SubmissionModalities\" > 0 AND (\"SubmissionModalities\" & ~127) = 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Assessments_PresentationMode",
            table: "Assessments",
            sql: "\"PresentationMode\" IN (0, 1)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Assessments_DeliverySchedule",
            table: "Assessments",
            sql: "(\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" <= \"AvailableUntil\") AND (\"DueAt\" IS NULL OR \"AvailableFrom\" IS NULL OR \"DueAt\" >= \"AvailableFrom\") AND (\"DueAt\" IS NULL OR \"AvailableUntil\" IS NULL OR \"DueAt\" <= \"AvailableUntil\") AND (NOT \"AllowLateSubmissions\" OR (\"DueAt\" IS NOT NULL AND \"LateSubmissionDeadline\" IS NOT NULL AND \"LateSubmissionDeadline\" > \"DueAt\" AND (\"AvailableUntil\" IS NULL OR \"LateSubmissionDeadline\" <= \"AvailableUntil\"))) AND (\"AllowLateSubmissions\" OR \"LateSubmissionDeadline\" IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AssessmentSubmissions_SubmittedModalities",
            table: "AssessmentSubmissions",
            sql: "\"SubmittedModalities\" >= 0 AND (\"SubmittedModalities\" & ~127) = 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AssessmentSubmissions_PayloadConsistency",
            table: "AssessmentSubmissions",
            sql: "((\"SubmittedModalities\" & 1) = 0 OR \"TextPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 2) = 0 OR \"FilePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 4) = 0 OR \"UrlPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 8) = 0 OR \"CodePayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 16) = 0 OR \"MediaPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 32) = 0 OR \"ProjectPayload\" IS NOT NULL) AND ((\"SubmittedModalities\" & 64) = 0 OR \"StructuredAnswerPayload\" IS NOT NULL)");

        migrationBuilder.CreateTable(
            name: "InteractiveVideoAssessmentCues",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                CueId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                CuePositionSeconds = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InteractiveVideoAssessmentCues", x => x.Id);
                table.ForeignKey(
                    name: "FK_InteractiveVideoAssessmentCues_Assessments_AssessmentId",
                    column: x => x.AssessmentId,
                    principalTable: "Assessments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InteractiveVideoAssessmentCues_AssessmentId_ContentId_CueId",
            table: "InteractiveVideoAssessmentCues",
            columns: new[] { "AssessmentId", "ContentId", "CueId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InteractiveVideoAssessmentCues_ContentId",
            table: "InteractiveVideoAssessmentCues",
            column: "ContentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InteractiveVideoAssessmentCues");

        migrationBuilder.DropCheckConstraint(name: "CK_Assessments_DeliverySchedule", table: "Assessments");
        migrationBuilder.DropCheckConstraint(name: "CK_AssessmentSubmissions_PayloadConsistency", table: "AssessmentSubmissions");
        migrationBuilder.DropCheckConstraint(name: "CK_AssessmentSubmissions_SubmittedModalities", table: "AssessmentSubmissions");
        migrationBuilder.DropCheckConstraint(name: "CK_Assessments_PresentationMode", table: "Assessments");
        migrationBuilder.DropCheckConstraint(name: "CK_Assessments_SubmissionModalities", table: "Assessments");

        migrationBuilder.DropColumn(name: "AllowLateSubmissions", table: "Assessments");
        migrationBuilder.DropColumn(name: "DueAt", table: "Assessments");
        migrationBuilder.DropColumn(name: "LateSubmissionDeadline", table: "Assessments");
        migrationBuilder.DropColumn(name: "PresentationMode", table: "Assessments");
        migrationBuilder.DropColumn(name: "SubmissionModalities", table: "Assessments");

        migrationBuilder.DropColumn(name: "CodePayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "FilePayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "IsLate", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "MediaPayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "ProjectPayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "StructuredAnswerPayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "SubmittedModalities", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "TextPayload", table: "AssessmentSubmissions");
        migrationBuilder.DropColumn(name: "UrlPayload", table: "AssessmentSubmissions");
    }
}
}
