using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811108000_CompleteBountyReclaimLedgerWriter")]
public partial class CompleteBountyReclaimLedgerWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallBountyReclaimLedgerWriter(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveBountyReclaimLedgerWriter(migrationBuilder);
}
