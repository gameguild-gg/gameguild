using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809193000_AddEconomyConfirmedFundingWriter")]
public partial class AddEconomyConfirmedFundingWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InstallConfirmedFundingWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveConfirmedFundingWriter(migrationBuilder);
    }
}
