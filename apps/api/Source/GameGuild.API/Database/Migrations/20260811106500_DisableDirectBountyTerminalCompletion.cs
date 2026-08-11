using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811106500_DisableDirectBountyTerminalCompletion")]
public partial class DisableDirectBountyTerminalCompletion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        DisableDirectBountyTerminalCompletionWriter(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RestoreDirectBountyTerminalCompletionWriter(migrationBuilder);
}
