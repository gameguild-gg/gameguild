using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810104100_AllowFixedHardToSoftLineage")]
public partial class AllowFixedHardToSoftLineage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallFixedHardToSoftLineageValidation(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}