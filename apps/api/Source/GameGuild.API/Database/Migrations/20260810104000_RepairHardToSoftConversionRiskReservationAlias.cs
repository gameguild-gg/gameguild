using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810104000_RepairHardToSoftConversionRiskReservationAlias")]
public partial class RepairHardToSoftConversionRiskReservationAlias : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallHardToSoftConversionRiskReservationAliasRepair(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}