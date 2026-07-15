using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260715220000_AddLessonContentFormatsAndInteractionEvents")]
public partial class AddLessonContentFormatsAndInteractionEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LessonFormat",
            table: "program_contents",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "TimeSpentSeconds",
            table: "content_interactions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql(
            "UPDATE program_contents SET \"Type\" = 0, \"GradingMethod\" = 0, \"MaxPoints\" = NULL, \"LessonFormat\" = 0 WHERE \"Type\" IN (0, 1);");
        migrationBuilder.Sql(
            "UPDATE program_contents SET \"Type\" = 2, \"LessonFormat\" = NULL WHERE \"Type\" = 6;");
        migrationBuilder.Sql(
            "UPDATE content_interactions SET \"TimeSpentSeconds\" = GREATEST(COALESCE(\"TimeSpentMinutes\", 0), 0) * 60;");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_Lesson_NotGraded",
            table: "program_contents",
            sql: "\"Type\" NOT IN (0, 1) OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents",
            sql: "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IS NOT NULL) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_content_interactions_TimeSpentSeconds_NonNegative",
            table: "content_interactions",
            sql: "\"TimeSpentSeconds\" >= 0");

        migrationBuilder.CreateTable(
            name: "content_interaction_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InteractionId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                PositionSeconds = table.Column<decimal>(type: "numeric(12,3)", nullable: true),
                ProgressPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                Payload = table.Column<string>(type: "text", nullable: true),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_content_interaction_events", x => x.Id);
                table.CheckConstraint(
                    "CK_content_interaction_events_DurationSeconds_Positive",
                    "\"DurationSeconds\" IS NULL OR \"DurationSeconds\" > 0");
                table.CheckConstraint(
                    "CK_content_interaction_events_PositionSeconds_NonNegative",
                    "\"PositionSeconds\" IS NULL OR \"PositionSeconds\" >= 0");
                table.CheckConstraint(
                    "CK_content_interaction_events_ProgressPercentage_Range",
                    "\"ProgressPercentage\" IS NULL OR (\"ProgressPercentage\" >= 0 AND \"ProgressPercentage\" <= 100)");
                table.ForeignKey(
                    name: "FK_content_interaction_events_content_interactions_InteractionId",
                    column: x => x.InteractionId,
                    principalTable: "content_interactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_content_interaction_events_InteractionId_IdempotencyKey",
            table: "content_interaction_events",
            columns: new[] { "InteractionId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_content_interaction_events_InteractionId_OccurredAt",
            table: "content_interaction_events",
            columns: new[] { "InteractionId", "OccurredAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "content_interaction_events");

        migrationBuilder.DropCheckConstraint(
            name: "CK_content_interactions_TimeSpentSeconds_NonNegative",
            table: "content_interactions");
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_Lesson_NotGraded",
            table: "program_contents");
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents");

        migrationBuilder.DropColumn(name: "TimeSpentSeconds", table: "content_interactions");
        migrationBuilder.DropColumn(name: "LessonFormat", table: "program_contents");
    }
}
