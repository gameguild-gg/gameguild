using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AddCohortSchedules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "learning_cohort_schedules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                TimezoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MeetingDays = table.Column<string>(type: "text", nullable: false),
                MeetingStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                MeetingDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                PacingMode = table.Column<int>(type: "integer", nullable: false),
                UnitsPerPeriod = table.Column<int>(type: "integer", nullable: false),
                ReleasePolicy = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_learning_cohort_schedules", value => value.Id);
                table.UniqueConstraint("AK_learning_cohort_schedules_CohortId", value => value.CohortId);
                table.ForeignKey(
                    name: "FK_learning_cohort_schedules_learning_cohorts_CohortId",
                    column: value => value.CohortId,
                    principalTable: "learning_cohorts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "learning_cohort_schedule_items",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CohortId = table.Column<Guid>(type: "uuid", nullable: false),
                ProgramContentId = table.Column<Guid>(type: "uuid", nullable: true),
                AssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false),
                InstructionalWeek = table.Column<int>(type: "integer", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AvailableUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                MeetingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Status = table.Column<int>(type: "integer", nullable: false),
                VisibilityOverride = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_learning_cohort_schedule_items", value => value.Id);
                table.ForeignKey(
                    name: "FK_learning_cohort_schedule_items_learning_cohort_schedules_CohortId",
                    column: value => value.CohortId,
                    principalTable: "learning_cohort_schedules",
                    principalColumn: "CohortId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_learning_cohort_schedule_items_program_contents_ProgramContentId",
                    column: value => value.ProgramContentId,
                    principalTable: "program_contents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedules_CohortId",
            table: "learning_cohort_schedules",
            column: "CohortId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedules_TenantId",
            table: "learning_cohort_schedules",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedule_items_AssessmentId",
            table: "learning_cohort_schedule_items",
            column: "AssessmentId");

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedule_items_CohortId_InstructionalWeek_SortOrder",
            table: "learning_cohort_schedule_items",
            columns: new[] { "CohortId", "InstructionalWeek", "SortOrder" });

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedule_items_ProgramContentId",
            table: "learning_cohort_schedule_items",
            column: "ProgramContentId");

        migrationBuilder.CreateIndex(
            name: "IX_learning_cohort_schedule_items_TenantId",
            table: "learning_cohort_schedule_items",
            column: "TenantId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "learning_cohort_schedule_items");
        migrationBuilder.DropTable(name: "learning_cohort_schedules");
    }
}
