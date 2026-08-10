using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810100100_HardenEconomyProviderReversalWriter")]
public partial class HardenEconomyProviderReversalWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        InstallHardenedProviderReversalWriter(migrationBuilder);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        RemoveHardenedProviderReversalWriter(migrationBuilder);
}
