using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810107000_HardenPublicEconomyCommandBindings")]
public partial class HardenPublicEconomyCommandBindings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "economy_risk_decisions",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        AddPublicEconomyCommandBindings(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemovePublicEconomyCommandBindings(migrationBuilder);

        migrationBuilder.DropColumn(
            name: "IdempotencyKey",
            table: "economy_risk_decisions");
    }
}