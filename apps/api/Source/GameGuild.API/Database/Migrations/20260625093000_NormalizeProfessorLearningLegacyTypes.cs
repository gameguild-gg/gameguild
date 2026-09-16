using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260625093000_NormalizeProfessorLearningLegacyTypes")]
    public partial class NormalizeProfessorLearningLegacyTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Assessments\" SET \"Type\" = 0 WHERE \"Type\" = 1;");
            migrationBuilder.Sql("UPDATE program_contents SET \"Type\" = 0 WHERE \"Type\" = 1;");
            migrationBuilder.Sql("UPDATE program_contents SET \"Type\" = 2 WHERE \"Type\" = 6;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Legacy Page/Challenge/Exam values are intentionally not restored.
        }
    }
}
