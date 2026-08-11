using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811108500_RequireConfirmedFragmentReservationSources")]
public partial class RequireConfirmedFragmentReservationSources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallConfirmedFragmentReservationSourceGuard(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveConfirmedFragmentReservationSourceGuard(migrationBuilder);
}
