using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProgramContentGradingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_program_contents_Lesson_NotGraded",
                table: "program_contents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_program_contents_Survey_NotGraded",
                table: "program_contents");

            migrationBuilder.DropColumn(
                name: "GradingMethod",
                table: "program_contents");

            migrationBuilder.DropColumn(
                name: "MaxPoints",
                table: "program_contents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradingMethod",
                table: "program_contents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxPoints",
                table: "program_contents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_contents_Lesson_NotGraded",
                table: "program_contents",
                sql: "\"Type\" NOT IN (0, 1) OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_program_contents_Survey_NotGraded",
                table: "program_contents",
                sql: "\"Type\" <> 8 OR (\"GradingMethod\" = 0 AND \"MaxPoints\" IS NULL)");
        }
    }
}
