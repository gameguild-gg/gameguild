using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811105500_AddBountyTerminalEventWriter")]
public partial class AddBountyTerminalEventWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallBountyTerminalEventWriter(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveBountyTerminalEventWriter(migrationBuilder);
}
