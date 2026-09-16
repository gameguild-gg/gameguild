using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    public partial class AddProgramPassingScore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE programs
                    ADD COLUMN IF NOT EXISTS "PassingScore" numeric(5,2) NOT NULL DEFAULT 60;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE programs
                    DROP COLUMN IF EXISTS "PassingScore";
                """);
        }
    }
}
