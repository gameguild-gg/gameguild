using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class AddProgramCategoryAndDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "programs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "programs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_programs_Category",
                table: "programs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Difficulty",
                table: "programs",
                column: "Difficulty");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_programs_Category",
                table: "programs");

            migrationBuilder.DropIndex(
                name: "IX_programs_Difficulty",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "programs");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "programs");
        }
    }
}
