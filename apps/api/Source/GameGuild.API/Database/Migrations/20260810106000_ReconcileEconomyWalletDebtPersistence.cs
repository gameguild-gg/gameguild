using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810106000_ReconcileEconomyWalletDebtPersistence")]
public partial class ReconcileEconomyWalletDebtPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ReconcileWalletDebtPersistence(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RestoreWalletDebtPersistence(migrationBuilder);
    }
}
