using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260729120000_AddAssessmentDefinitionPayload")]
    public partial class AddAssessmentDefinitionPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefinitionPayload",
                table: "Assessments",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefinitionSchemaVersion",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefinitionPayload",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "DefinitionSchemaVersion",
                table: "Assessments");
        }
    }
}
