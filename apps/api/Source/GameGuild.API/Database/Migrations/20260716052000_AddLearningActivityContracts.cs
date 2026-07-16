using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

public partial class AddLearningActivityContracts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActivitySettingsData",
            table: "program_contents",
            type: "jsonb",
            nullable: true);

        // Survey responses are never graded. Remove legacy grade rows before installing
        // the content-level invariant, preserving the underlying audit interaction.
        migrationBuilder.Sql(
            "DELETE FROM \"activity_grades\" AS grade USING \"content_interactions\" AS interaction, \"program_contents\" AS content " +
            "WHERE grade.\"ContentInteractionId\" = interaction.\"Id\" AND interaction.\"ContentId\" = content.\"Id\" AND content.\"Type\" = 8");

        // Existing survey rows may predate the non-grading invariant. Preserve the content and
        // remove only invalid grading values before the constraint is installed.
        migrationBuilder.Sql(
            "UPDATE \"program_contents\" SET \"GradingMethod\" = 0, \"MaxPoints\" = NULL WHERE \"Type\" = 8 AND (\"GradingMethod\" <> 0 OR \"MaxPoints\" IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_Survey_NotGraded",
            table: "program_contents",
            sql: "\"Type\" <> 8 OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_Survey_NotGraded",
            table: "program_contents");

        migrationBuilder.DropColumn(
            name: "ActivitySettingsData",
            table: "program_contents");
    }
}
