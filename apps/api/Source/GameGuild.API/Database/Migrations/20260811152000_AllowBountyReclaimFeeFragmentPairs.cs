using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811152000_AllowBountyReclaimFeeFragmentPairs")]
public partial class AllowBountyReclaimFeeFragmentPairs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallBountyReclaimFeeFragmentValidation(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RestoreBountyReclaimFeeFragmentValidation(migrationBuilder);
}
