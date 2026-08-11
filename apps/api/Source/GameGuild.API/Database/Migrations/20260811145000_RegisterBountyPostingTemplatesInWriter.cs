using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811145000_RegisterBountyPostingTemplatesInWriter")]
public partial class RegisterBountyPostingTemplatesInWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallBountyPostingTemplateValidation(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveBountyPostingTemplateValidation(migrationBuilder);
}
