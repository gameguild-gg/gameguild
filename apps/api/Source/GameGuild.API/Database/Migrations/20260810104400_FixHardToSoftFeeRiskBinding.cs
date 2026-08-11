using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810104400_FixHardToSoftFeeRiskBinding")]
public partial class FixHardToSoftFeeRiskBinding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallHardToSoftFeeRiskBinding(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}