using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddContentAndAssessmentSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "program_contents",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Assessments",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                defaultValue: "");

            // Backfill: slug = sluggified title (mirrors StringExtensions.ToSlugCase),
            // falling back to the GUID id text when the title yields no slug chars.
            migrationBuilder.Sql(@"
                UPDATE program_contents SET ""Slug"" = COALESCE(NULLIF(TRIM(BOTH '-' FROM REGEXP_REPLACE(REGEXP_REPLACE(LOWER(""Title""), '[\s_.]+', '-', 'g'), '[^a-z0-9-]', '', 'g')), ''), ""Id""::text);
                UPDATE ""Assessments"" SET ""Slug"" = COALESCE(NULLIF(TRIM(BOTH '-' FROM REGEXP_REPLACE(REGEXP_REPLACE(LOWER(""Title""), '[\s_.]+', '-', 'g'), '[^a-z0-9-]', '', 'g')), ''), ""Id""::text);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_Slug",
                table: "program_contents",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_Slug",
                table: "Assessments",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_program_contents_Slug",
                table: "program_contents");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_Slug",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "program_contents");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Assessments");
        }
    }
}
