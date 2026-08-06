using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805120000_WidenLessonFormatConstraint")]
public partial class WidenLessonFormatConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents",
            sql: "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IN (0, 1, 2, 3, 4, 5)) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents");

        migrationBuilder.AddCheckConstraint(
            name: "CK_program_contents_LessonFormat",
            table: "program_contents",
            sql: "((\"Type\" IN (0, 1)) AND \"LessonFormat\" IN (0, 1, 2, 3)) OR ((\"Type\" NOT IN (0, 1)) AND \"LessonFormat\" IS NULL)");
    }
}
