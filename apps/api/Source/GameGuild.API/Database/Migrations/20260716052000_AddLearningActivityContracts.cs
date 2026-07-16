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

        // Existing survey rows may predate the non-grading invariant. Preserve all historical
        // response and grade records while removing only invalid content-level grading values.
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
