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

        migrationBuilder.DropIndex(
            name: "IX_content_interactions_UserId_ContentId",
            table: "content_interactions");

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION pg_temp.gameguild_is_lexical_lesson(body text)
            RETURNS boolean
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF body IS NULL THEN
                    RETURN FALSE;
                END IF;

                RETURN jsonb_typeof(body::jsonb) = 'object' AND body::jsonb ? 'root';
            EXCEPTION WHEN invalid_text_representation THEN
                RETURN FALSE;
            END;
            $$;

            UPDATE program_contents
            SET "Type" = 0,
                "GradingMethod" = 0,
                "MaxPoints" = NULL,
                "LessonFormat" = CASE
                    WHEN pg_temp.gameguild_is_lexical_lesson("Body") THEN 1
                    ELSE 0
                END
            WHERE "Type" IN (0, 1);
            """);
        migrationBuilder.Sql(
            "UPDATE program_contents SET \"Type\" = 2, \"LessonFormat\" = NULL WHERE \"Type\" = 6;");
        migrationBuilder.Sql(
            "UPDATE content_interactions SET \"TimeSpentSeconds\" = GREATEST(COALESCE(\"TimeSpentMinutes\", 0), 0) * 60;");
        migrationBuilder.Sql(
            "UPDATE content_interactions AS interaction SET \"UserId\" = enrollment.\"UserId\" FROM program_users AS enrollment WHERE interaction.\"ProgramUserId\" = enrollment.\"Id\" AND interaction.\"UserId\" <> enrollment.\"UserId\";");
        migrationBuilder.Sql(
            """
            WITH ranked_active_attempts AS (
                SELECT "Id",
                       ROW_NUMBER() OVER (
                           PARTITION BY "UserId", "ContentId"
                           ORDER BY "CreatedAt" DESC, "Id" DESC) AS attempt_rank
                FROM content_interactions
                WHERE "SubmittedAt" IS NULL AND "DeletedAt" IS NULL
            )
            UPDATE content_interactions AS interaction
            SET "SubmittedAt" = COALESCE(
                interaction."CompletedAt",
                interaction."LastAccessedAt",
                interaction."UpdatedAt",
                interaction."CreatedAt")
            FROM ranked_active_attempts AS ranked
            WHERE interaction."Id" = ranked."Id" AND ranked.attempt_rank > 1;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_content_interactions_UserId_ContentId",
            table: "content_interactions",
            columns: new[] { "UserId", "ContentId" },
            unique: true,
            filter: "\"SubmittedAt\" IS NULL AND \"DeletedAt\" IS NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_Lesson_NotGraded",
            table: "program_contents",
            sql: "\"Type\" NOT IN (0, 1) OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents",
            sql: "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IN (0, 1, 2, 3)) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");

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
                    "CK_content_interaction_events_Type_Valid",
                    "\"Type\" BETWEEN 0 AND 8");
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

        migrationBuilder.DropIndex(
            name: "IX_content_interactions_UserId_ContentId",
            table: "content_interactions");

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

        migrationBuilder.CreateIndex(
            name: "IX_content_interactions_UserId_ContentId",
            table: "content_interactions",
            columns: new[] { "UserId", "ContentId" });
    }
}
