using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class HardenEconomyPostingWriter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_posting_groups_authority_template",
                table: "economy_posting_groups",
                sql: "(\"TemplateKind\" IN (1, 2, 3, 18, 19, 20) AND \"Authority\" = 1) OR (\"TemplateKind\" IN (4, 5, 7, 8, 17) AND \"Authority\" = 2) OR (\"TemplateKind\" IN (6, 21) AND \"Authority\" = 3) OR (\"TemplateKind\" IN (9, 10) AND \"Authority\" = 4) OR (\"TemplateKind\" IN (11, 12, 13) AND \"Authority\" = 5) OR (\"TemplateKind\" IN (14, 15, 16) AND \"Authority\" = 6)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_posting_groups_source_requirement",
                table: "economy_posting_groups",
                sql: "\"TemplateKind\" NOT IN (1, 2, 3, 18, 19, 20) OR \"SourceStampId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_posting_groups_template_state",
                table: "economy_posting_groups",
                sql: "\"TemplateKind\" BETWEEN 1 AND 21 AND \"TemplateVersion\" = 1 AND \"Status\" = 1");

            HardenWriterFunctions(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RestoreWriterFunctions(migrationBuilder);

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_posting_groups_authority_template",
                table: "economy_posting_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_posting_groups_source_requirement",
                table: "economy_posting_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_posting_groups_template_state",
                table: "economy_posting_groups");
        }
    }
}
