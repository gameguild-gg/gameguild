using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentGradingMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GradingMethods",
                table: "Assessments",
                type: "integer",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_GradingMethods",
                table: "Assessments",
                sql: "\"GradingMethods\" >= 0 AND (\"GradingMethods\" & ~15) = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_GradingMethods",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "GradingMethods",
                table: "Assessments");
        }
    }
}
